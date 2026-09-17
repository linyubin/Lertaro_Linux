using System.Collections.ObjectModel;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Lertaro.Linux.Core;

namespace Lertaro.Linux.App;

public sealed partial class MainWindow : Window
{
    private readonly ObservableCollection<LinuxDesktopResult> _results = [];
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
        var rawQuery = QueryBox.Text?.Trim() ?? string.Empty;
        if (rawQuery.Length == 0)
        {
            _results.Clear();
            StatusText.Text = "Type to search files, or prefix with > to launch applications.";
            return;
        }

        var applicationMode = rawQuery.StartsWith('>');
        var query = applicationMode ? rawQuery[1..].Trim() : rawQuery;
        StatusText.Text = applicationMode ? "Searching applications…" : "Searching…";
        try
        {
            var command = applicationMode ? "application-list" : "search";
            var response = await _client.SendAsync(new LinuxDaemonRequest(command, query, 80));
            if (generation != Volatile.Read(ref _searchGeneration))
                return;
            if (!response.Ok)
            {
                StatusText.Text = response.Error ?? "Search failed.";
                return;
            }

            _results.Clear();
            if (applicationMode)
            {
                foreach (var application in response.Applications ?? [])
                    _results.Add(LinuxDesktopResult.FromApplication(application));
            }
            else
            {
                foreach (var result in response.Results ?? [])
                    _results.Add(LinuxDesktopResult.FromSearch(result));
            }

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
        if (ResultsList.SelectedItem is not LinuxDesktopResult item)
            return;

        try
        {
            if (item.Kind == LinuxDesktopResultKind.Application)
            {
                LinuxDesktopActions.LaunchApplication(item.Target);
                StatusText.Text = "Launched application.";
            }
            else if (reveal && !item.IsDirectory)
            {
                LinuxDesktopActions.Reveal(item.Target);
                StatusText.Text = "Opened containing folder.";
            }
            else
            {
                LinuxDesktopActions.Open(item.Target);
                StatusText.Text = "Opened selection.";
            }
        }
        catch (Exception ex) when (ex is IOException or System.ComponentModel.Win32Exception or ArgumentException)
        {
            StatusText.Text = ex.Message;
        }
    }
}
