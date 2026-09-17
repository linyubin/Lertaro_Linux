using System.Collections.ObjectModel;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Lertaro.Linux.Core;

namespace Lertaro.Linux.App;

public sealed partial class MainWindow : Window
{
    private readonly ObservableCollection<LinuxDaemonSearchItem> _results = [];
    private readonly LinuxDaemonClient _client;
    private int _searchGeneration;

    public MainWindow()
    {
        AvaloniaXamlLoader.Load(this);
        ResultsList.ItemsSource = _results;
        var socketPath = Environment.GetEnvironmentVariable("LERTARO_SOCKET");
        if (string.IsNullOrWhiteSpace(socketPath))
            socketPath = LinuxDaemonPaths.CreateDefault().SocketPath;
        _client = new LinuxDaemonClient(socketPath);
        Opened += (_, _) => QueryBox.Focus();
    }

    private async void OnQueryChanged(object? sender, TextChangedEventArgs args)
    {
        var generation = Interlocked.Increment(ref _searchGeneration);
        var query = QueryBox.Text?.Trim() ?? string.Empty;
        if (query.Length == 0)
        {
            _results.Clear();
            StatusText.Text = "Type to search the live Linux index.";
            return;
        }

        StatusText.Text = "Searching…";
        try
        {
            var response = await _client.SendAsync(new LinuxDaemonRequest("search", query, 80));
            if (generation != Volatile.Read(ref _searchGeneration))
                return;
            if (!response.Ok)
            {
                StatusText.Text = response.Error ?? "Search failed.";
                return;
            }

            _results.Clear();
            foreach (var result in response.Results ?? [])
                _results.Add(result);
            if (_results.Count > 0)
                ResultsList.SelectedIndex = 0;
            StatusText.Text = $"{_results.Count} result{(_results.Count == 1 ? string.Empty : "s")}";
        }
        catch (Exception ex) when (ex is IOException or System.Net.Sockets.SocketException)
        {
            if (generation == Volatile.Read(ref _searchGeneration))
                StatusText.Text = $"Daemon unavailable: {ex.Message}";
        }
    }

    private void OnQueryKeyDown(object? sender, KeyEventArgs args)
    {
        if (args.Key == Key.Down && _results.Count > 0)
        {
            ResultsList.SelectedIndex = Math.Max(0, ResultsList.SelectedIndex);
            ResultsList.Focus();
            args.Handled = true;
            return;
        }

        if (args.Key == Key.Enter)
        {
            ActivateSelected(reveal: args.KeyModifiers.HasFlag(KeyModifiers.Control));
            args.Handled = true;
        }
    }

    private void OnResultsKeyDown(object? sender, KeyEventArgs args)
    {
        if (args.Key == Key.Enter)
        {
            ActivateSelected(reveal: args.KeyModifiers.HasFlag(KeyModifiers.Control));
            args.Handled = true;
        }
        else if (args.Key == Key.Escape)
        {
            QueryBox.Focus();
            args.Handled = true;
        }
    }

    private void OnResultsDoubleTapped(object? sender, RoutedEventArgs args) => ActivateSelected(reveal: false);

    private void ActivateSelected(bool reveal)
    {
        if (ResultsList.SelectedItem is not LinuxDaemonSearchItem item)
            return;

        try
        {
            if (reveal && !item.IsDirectory)
                LinuxDesktopActions.Reveal(item.Path);
            else
                LinuxDesktopActions.Open(item.Path);
            StatusText.Text = reveal ? "Opened containing folder." : "Opened selection.";
        }
        catch (Exception ex) when (ex is IOException or System.ComponentModel.Win32Exception)
        {
            StatusText.Text = ex.Message;
        }
    }
}
