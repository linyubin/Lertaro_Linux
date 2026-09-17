using Lertaro.Linux.Core;

if (args.Length > 3)
{
    Console.Error.WriteLine("Usage: lertarod [root] [index-file] [socket-path]");
    return 2;
}

try
{
    var defaults = LinuxDaemonPaths.CreateDefault();
    var root = Path.GetFullPath(args.ElementAtOrDefault(0) ?? defaults.Root);
    var indexPath = Path.GetFullPath(args.ElementAtOrDefault(1) ?? defaults.IndexPath);
    var socketPath = Path.GetFullPath(args.ElementAtOrDefault(2) ?? defaults.SocketPath);
    var stateDirectory = Path.GetDirectoryName(indexPath) ?? defaults.StateDirectory;
    var bookmarks = new LinuxBookmarks(Path.Combine(stateDirectory, "bookmarks.json"));
    var store = new LinuxIndexStore();
    var snapshot = LoadOrBuild(store, root, indexPath);
    var index = new LinuxMutableIndex(snapshot);

    using var watcher = new LinuxIndexWatcher(index, indexPath, store);
    watcher.Start();
    await using var server = new LinuxDaemonServer(index, watcher, bookmarks, indexPath, socketPath);
    using var stopping = new CancellationTokenSource();
    ConsoleCancelEventHandler handler = (_, eventArgs) =>
    {
        eventArgs.Cancel = true;
        stopping.Cancel();
    };

    Console.CancelKeyPress += handler;
    try
    {
        Console.Error.WriteLine($"lertarod indexing {root}");
        Console.Error.WriteLine($"index:  {indexPath}");
        Console.Error.WriteLine($"socket: {socketPath}");
        await server.RunAsync(stopping.Token);
    }
    finally
    {
        Console.CancelKeyPress -= handler;
        watcher.Stop();
    }

    return 0;
}
catch (OperationCanceledException)
{
    return 0;
}
catch (Exception ex) when (ex is ArgumentException or InvalidDataException or UnauthorizedAccessException or IOException or System.Net.Sockets.SocketException)
{
    Console.Error.WriteLine(ex.Message);
    return 1;
}

static LinuxIndexSnapshot LoadOrBuild(LinuxIndexStore store, string root, string indexPath)
{
    if (File.Exists(indexPath))
    {
        try
        {
            var loaded = store.Load(indexPath);
            if (string.Equals(loaded.Root, Path.TrimEndingDirectorySeparator(root), StringComparison.Ordinal))
                return loaded;
            Console.Error.WriteLine("Configured root changed; rebuilding Linux index.");
        }
        catch (Exception ex) when (ex is InvalidDataException or EndOfStreamException or IOException)
        {
            Console.Error.WriteLine($"Existing Linux index is unusable and will be rebuilt: {ex.Message}");
        }
    }

    var snapshot = LinuxIndexSnapshot.Build(root);
    store.Save(snapshot, indexPath);
    return snapshot;
}
