using System.Net.Sockets;

namespace Lertaro.Linux.Core;

public sealed class LinuxDaemonServer : IAsyncDisposable
{
    private readonly object _clientsSync = new();
    private readonly HashSet<Task> _clientTasks = [];
    private readonly LinuxMutableIndex _index;
    private readonly LinuxIndexWatcher _watcher;
    private readonly LinuxBookmarks _bookmarks;
    private readonly string _indexPath;
    private readonly CancellationTokenSource _shutdown = new();
    private Socket? _listener;

    public LinuxDaemonServer(LinuxMutableIndex index, LinuxIndexWatcher watcher, LinuxBookmarks bookmarks, string indexPath, string socketPath)
    {
        ArgumentNullException.ThrowIfNull(index);
        ArgumentNullException.ThrowIfNull(watcher);
        ArgumentNullException.ThrowIfNull(bookmarks);
        ArgumentException.ThrowIfNullOrWhiteSpace(indexPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(socketPath);
        _index = index;
        _watcher = watcher;
        _bookmarks = bookmarks;
        _indexPath = Path.GetFullPath(indexPath);
        SocketPath = Path.GetFullPath(socketPath);
    }

    public string SocketPath { get; }

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        PrepareSocketDirectory();
        await RemoveStaleSocketAsync(cancellationToken);
        using var linkedCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _shutdown.Token);
        var token = linkedCancellation.Token;
        var listener = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
        _listener = listener;
        try
        {
            listener.Bind(new UnixDomainSocketEndPoint(SocketPath));
            RestrictSocketPermissions();
            listener.Listen(64);
            while (!token.IsCancellationRequested)
            {
                Socket client;
                try { client = await listener.AcceptAsync(token); }
                catch (OperationCanceledException) when (token.IsCancellationRequested) { break; }
                catch (ObjectDisposedException) when (token.IsCancellationRequested) { break; }
                TrackClient(HandleClientAsync(client, token));
            }
        }
        finally
        {
            listener.Dispose();
            _listener = null;
            await AwaitClientsAsync();
            DeleteSocketPath();
        }
    }

    public void RequestShutdown() => _shutdown.Cancel();

    public async ValueTask DisposeAsync()
    {
        _shutdown.Cancel();
        _listener?.Dispose();
        await AwaitClientsAsync();
        DeleteSocketPath();
        _shutdown.Dispose();
    }

    private async Task HandleClientAsync(Socket client, CancellationToken cancellationToken)
    {
        using (client)
        using (var stream = new NetworkStream(client, ownsSocket: false))
        {
            try
            {
                var request = await LinuxDaemonProtocol.ReadAsync<LinuxDaemonRequest>(stream, cancellationToken);
                var (response, shutdown) = HandleRequest(request);
                await LinuxDaemonProtocol.WriteAsync(stream, response, shutdown ? CancellationToken.None : cancellationToken);
                if (shutdown) _shutdown.Cancel();
            }
            catch (Exception ex) when (IsClientError(ex))
            {
                try { await LinuxDaemonProtocol.WriteAsync(stream, new LinuxDaemonResponse(false, ex.Message), CancellationToken.None); }
                catch { }
            }
        }
    }

    private (LinuxDaemonResponse Response, bool Shutdown) HandleRequest(LinuxDaemonRequest request)
    {
        var command = request.Command?.Trim().ToLowerInvariant() ?? string.Empty;
        return command switch
        {
            "search" => (Search(request), false),
            "status" => (StatusResponse(), false),
            "rebuild" => (Rebuild(), false),
            "bookmark-list" => (BookmarkList(), false),
            "bookmark-add" => (BookmarkAdd(request), false),
            "bookmark-remove" => (BookmarkRemove(request), false),
            "application-list" => (ApplicationList(request), false),
            "shutdown" => (new LinuxDaemonResponse(true), true),
            _ => (new LinuxDaemonResponse(false, $"Unknown daemon command: {request.Command}"), false)
        };
    }

    private LinuxDaemonResponse Search(LinuxDaemonRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Query)) return new LinuxDaemonResponse(false, "Search query must not be empty.");
        if (request.Limit is < 1 or > 200) return new LinuxDaemonResponse(false, "Search limit must be between 1 and 200.");
        var results = LinuxFuzzySearch.Search(_index.GetEntries(), request.Query, request.Limit)
            .Select(result => new LinuxDaemonSearchItem(result.Entry.Path, result.Entry.Name, result.Entry.IsDirectory, result.Entry.Size, result.Score))
            .ToArray();
        return new LinuxDaemonResponse(true, Results: results);
    }

    private LinuxDaemonResponse ApplicationList(LinuxDaemonRequest request)
    {
        if (request.Limit is < 1 or > 200) return new LinuxDaemonResponse(false, "Application limit must be between 1 and 200.");
        var applications = LinuxApplicationCatalog.Discover()
            .Where(entry => string.IsNullOrWhiteSpace(request.Query) || entry.Name.Contains(request.Query, StringComparison.CurrentCultureIgnoreCase))
            .Take(request.Limit)
            .Select(entry => new LinuxDaemonApplicationItem(entry.DesktopId, entry.Name, entry.Icon, entry.DesktopFile))
            .ToArray();
        return new LinuxDaemonResponse(true, Applications: applications);
    }

    private LinuxDaemonResponse Rebuild() { _watcher.Reconcile(); _watcher.Flush(); return StatusResponse(); }
    private LinuxDaemonResponse BookmarkList() => new(true, Bookmarks: _bookmarks.Paths.Order(StringComparer.Ordinal).ToArray());
    private LinuxDaemonResponse BookmarkAdd(LinuxDaemonRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Path)) return new LinuxDaemonResponse(false, "Bookmark path must not be empty.");
        _bookmarks.Add(request.Path); return BookmarkList();
    }
    private LinuxDaemonResponse BookmarkRemove(LinuxDaemonRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Path)) return new LinuxDaemonResponse(false, "Bookmark path must not be empty.");
        _bookmarks.Remove(request.Path); return BookmarkList();
    }
    private LinuxDaemonResponse StatusResponse() => new(true, Status: new LinuxDaemonStatus(_index.Root, _indexPath, _index.Count, _watcher.LastError?.Message));

    private void TrackClient(Task task)
    {
        lock (_clientsSync) _clientTasks.Add(task);
        _ = task.ContinueWith(completed => { lock (_clientsSync) _clientTasks.Remove(completed); }, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
    }
    private async Task AwaitClientsAsync()
    {
        Task[] tasks; lock (_clientsSync) tasks = [.. _clientTasks];
        if (tasks.Length == 0) return;
        try { await Task.WhenAll(tasks); } catch { }
    }
    private void PrepareSocketDirectory()
    {
        var directory = Path.GetDirectoryName(SocketPath) ?? throw new InvalidOperationException("Socket path has no parent directory.");
        Directory.CreateDirectory(directory);
        if (OperatingSystem.IsLinux()) File.SetUnixFileMode(directory, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
    }
    private async Task RemoveStaleSocketAsync(CancellationToken cancellationToken)
    {
        if (!Path.Exists(SocketPath)) return;
        using var probe = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
        try { await probe.ConnectAsync(new UnixDomainSocketEndPoint(SocketPath), cancellationToken); throw new IOException($"Another Lertaro daemon is already listening at {SocketPath}."); }
        catch (SocketException) { File.Delete(SocketPath); }
    }
    private void RestrictSocketPermissions()
    {
        if (OperatingSystem.IsLinux()) File.SetUnixFileMode(SocketPath, UnixFileMode.UserRead | UnixFileMode.UserWrite);
    }
    private void DeleteSocketPath()
    {
        try { File.Delete(SocketPath); } catch (IOException) { } catch (UnauthorizedAccessException) { }
    }
    private static bool IsClientError(Exception ex) => ex is ArgumentException or InvalidDataException or IOException or SocketException or System.Text.Json.JsonException;
}
