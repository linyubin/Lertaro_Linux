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
    private readonly TextBox _queryBox;
    private readonly ListBox _resultsList;
    private readonly TextBlock _statusText;
    private int _searchGeneration;

    public MainWindow()
    {
        AvaloniaXamlLoader.Load(this);
        _queryBox = this.FindControl<TextBox>("QueryBox")
            ?? throw new InvalidOperationException("QueryBox was not created from MainWindow XAML.");
        _resultsList = this.FindControl<ListBox>("ResultsList")
            ?? throw new InvalidOperationException("ResultsList was not created from MainWindow XAML.");
        _statusText = this.FindControl<TextBlock>("StatusText")
            ?? throw new InvalidOperationException("StatusText was not created from MainWindow XAML.");
        _resultsList.ItemsSource = _results;
        var socketPath = Environment.GetEnvironmentVariable("LERTARO_SOCKET");
        if (string.IsNullOrWhiteSpace(socketPath))
            socketPath = LinuxDaemonPaths.CreateDefault().SocketPath;
        _client = new LinuxDaemonClient(socketPath);
        Opened += (_, _) => _queryBox.Focus();
    }

    private async void OnQueryChanged(object? sender, TextChangedEventArgs args)
    {
        var generation = Interlocked.Increment(ref _searchGeneration);
        var rawQuery = _queryBox.Text?.Trim() ?? string.Empty;
        if (rawQuery.Length == 0)
        {
            _results.Clear();
            _statusText.Text = "Type to search files, or prefix with > to launch applications.";
            return;
        }

        var applicationMode = rawQuery.StartsWith('>');
        var query = applicationMode ? rawQuery[1..].Trim() : rawQuery;
        _statusText.Text = applicationMode ? "Searching applications…" : "Searching…";
        try
        {
            var command = applicationMode ? "application-list" : "search";
            var response = await _client.SendAsync(new LinuxDaemonRequest(command, query, 80));
            if (generation != Volatile.Read(ref _searchGeneration))
                return;
            if (!response.Ok)
            {
                _statusText.Text = response.Error ?? "Search failed.";
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
                _resultsList.SelectedIndex = 0;
            _statusText.Text = $"{_results.Count} result{(_results.Count == 1 ? string.Empty : "s")}";
        }
        catch (Exception ex) when (ex is IOException or System.Net.Sockets.SocketException)
        {
            if (generation == Volatile.Read(ref _searchGeneration))
                _statusText.Text = $"Daemon unavailable: {ex.Message}";
        }
    }

    private void OnQueryKeyDown(object? sender, KeyEventArgs args)
    {
        if (args.Key == Key.Down && _results.Count > 0)
        {
            _resultsList.SelectedIndex = Math.Max(0, _resultsList.SelectedIndex);
            _resultsList.Focus();
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
            _queryBox.Focus();
            args.Handled = true;
        }
    }

    private void OnResultsDoubleTapped(object? sender, RoutedEventArgs args) => ActivateSelected(reveal: false);

    private void ActivateSelected(bool reveal)
    {
        if (_resultsList.SelectedItem is not LinuxDesktopResult item)
            return;

        try
        {
            if (item.Kind == LinuxDesktopResultKind.Application)
            {
                LinuxDesktopActions.LaunchApplication(item.Target);
                _statusText.Text = "Launched application.";
            }
            else if (reveal && !item.IsDirectory)
            {
                LinuxDesktopActions.Reveal(item.Target);
                _statusText.Text = "Opened containing folder.";
            }
            else
            {
                LinuxDesktopActions.Open(item.Target);
                _statusText.Text = "Opened selection.";
            }
        }
        catch (Exception ex) when (ex is IOException or System.ComponentModel.Win32Exception or ArgumentException)
        {
            _statusText.Text = ex.Message;
        }
    }
}
