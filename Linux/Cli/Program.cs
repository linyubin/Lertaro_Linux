using Lertaro.Linux.Core;

try
{
    return args.Length == 0
        ? Usage()
        : args[0] switch
        {
            "index" => RunIndex(args),
            "search" => RunSearch(args),
            "watch" => RunWatch(args),
            "daemon-search" => RunDaemonSearch(args),
            "daemon-status" => RunDaemonStatus(args),
            "daemon-rebuild" => RunDaemonCommand(args, "rebuild"),
            "daemon-shutdown" => RunDaemonCommand(args, "shutdown"),
            "bookmark-list" => RunBookmarkList(args),
            "bookmark-add" => RunBookmarkMutation(args, "bookmark-add"),
            "bookmark-remove" => RunBookmarkMutation(args, "bookmark-remove"),
            "apps" => RunApplications(args),
            "app-launch" => RunApplicationLaunch(args),
            _ => RunDirectSearch(args)
        };
}
catch (Exception ex) when (ex is ArgumentException or DirectoryNotFoundException or InvalidDataException or UnauthorizedAccessException or IOException or System.Net.Sockets.SocketException)
{
    Console.Error.WriteLine(ex.Message);
    return 1;
}

static int RunIndex(string[] commandArgs)
{
    if (commandArgs.Length != 3) return Usage();
    var started = System.Diagnostics.Stopwatch.StartNew();
    var snapshot = LinuxIndexSnapshot.Build(commandArgs[1]);
    new LinuxIndexStore().Save(snapshot, commandArgs[2]);
    Console.Error.WriteLine($"Indexed {snapshot.Entries.Count:N0} entries in {started.ElapsedMilliseconds:N0} ms -> {Path.GetFullPath(commandArgs[2])}");
    return 0;
}

static int RunSearch(string[] commandArgs)
{
    if (commandArgs.Length is < 3 or > 4) return Usage();
    var limit = ParseLimit(commandArgs, 3);
    var started = System.Diagnostics.Stopwatch.StartNew();
    var snapshot = new LinuxIndexStore().Load(commandArgs[1]);
    var loadMs = started.Elapsed.TotalMilliseconds;
    started.Restart();
    var results = LinuxFuzzySearch.Search(snapshot.Entries, commandArgs[2], limit);
    PrintResults(results);
    Console.Error.WriteLine($"Loaded {snapshot.Entries.Count:N0} entries in {loadMs:F2} ms; search returned {results.Count} results in {started.Elapsed.TotalMilliseconds:F2} ms.");
    return 0;
}

static int RunWatch(string[] commandArgs)
{
    if (commandArgs.Length != 2) return Usage();
    var store = new LinuxIndexStore();
    var index = new LinuxMutableIndex(store.Load(commandArgs[1]));
    using var watcher = new LinuxIndexWatcher(index, commandArgs[1], store);
    using var stopped = new ManualResetEventSlim();
    ConsoleCancelEventHandler handler = (_, eventArgs) => { eventArgs.Cancel = true; stopped.Set(); };
    Console.CancelKeyPress += handler;
    try
    {
        watcher.Start();
        Console.Error.WriteLine($"Watching {index.Root}. Press Ctrl+C to stop.");
        stopped.Wait();
        watcher.Stop();
        return 0;
    }
    finally { Console.CancelKeyPress -= handler; }
}

static int RunDaemonSearch(string[] commandArgs)
{
    if (commandArgs.Length is < 2 or > 3) return Usage();
    var response = CreateDaemonClient().Send(new LinuxDaemonRequest("search", commandArgs[1], ParseLimit(commandArgs, 2)));
    if (!response.Ok) return PrintDaemonError(response);
    foreach (var result in response.Results ?? []) Console.WriteLine($"{result.Score,6}  {result.Path}");
    return 0;
}

static int RunDaemonStatus(string[] commandArgs)
{
    if (commandArgs.Length != 1) return Usage();
    var response = CreateDaemonClient().Send(new LinuxDaemonRequest("status"));
    if (!response.Ok) return PrintDaemonError(response);
    if (response.Status is null) throw new InvalidDataException("Daemon returned no status payload.");
    PrintStatus(response.Status);
    return 0;
}

static int RunDaemonCommand(string[] commandArgs, string command)
{
    if (commandArgs.Length != 1) return Usage();
    var response = CreateDaemonClient().Send(new LinuxDaemonRequest(command));
    if (!response.Ok) return PrintDaemonError(response);
    if (response.Status is not null) PrintStatus(response.Status);
    return 0;
}

static int RunBookmarkList(string[] commandArgs)
{
    if (commandArgs.Length != 1) return Usage();
    var response = CreateDaemonClient().Send(new LinuxDaemonRequest("bookmark-list"));
    return PrintBookmarks(response);
}

static int RunBookmarkMutation(string[] commandArgs, string command)
{
    if (commandArgs.Length != 2) return Usage();
    var response = CreateDaemonClient().Send(new LinuxDaemonRequest(command, Path: commandArgs[1]));
    return PrintBookmarks(response);
}

static int PrintBookmarks(LinuxDaemonResponse response)
{
    if (!response.Ok) return PrintDaemonError(response);
    foreach (var path in response.Bookmarks ?? []) Console.WriteLine(path);
    return 0;
}

static int RunApplications(string[] commandArgs)
{
    if (commandArgs.Length is < 1 or > 3) return Usage();
    var query = commandArgs.ElementAtOrDefault(1) ?? string.Empty;
    var limit = ParseLimit(commandArgs, 2);
    var response = CreateDaemonClient().Send(new LinuxDaemonRequest("application-list", query, limit));
    if (!response.Ok) return PrintDaemonError(response);
    foreach (var app in response.Applications ?? []) Console.WriteLine($"{app.DesktopId}\t{app.Name}\t{app.DesktopFile}");
    return 0;
}

static int RunApplicationLaunch(string[] commandArgs)
{
    if (commandArgs.Length != 2) return Usage();
    LinuxDesktopActions.LaunchApplication(commandArgs[1]);
    return 0;
}

static LinuxDaemonClient CreateDaemonClient()
{
    var socketPath = Environment.GetEnvironmentVariable("LERTARO_SOCKET");
    if (string.IsNullOrWhiteSpace(socketPath)) socketPath = LinuxDaemonPaths.CreateDefault().SocketPath;
    return new LinuxDaemonClient(socketPath);
}

static void PrintStatus(LinuxDaemonStatus status)
{
    Console.WriteLine($"root={status.Root}");
    Console.WriteLine($"index={status.IndexPath}");
    Console.WriteLine($"entries={status.EntryCount}");
    Console.WriteLine($"watcherError={status.WatcherError ?? string.Empty}");
}

static int PrintDaemonError(LinuxDaemonResponse response) { Console.Error.WriteLine(response.Error ?? "Daemon request failed."); return 1; }

static int RunDirectSearch(string[] commandArgs)
{
    if (commandArgs.Length is < 2 or > 3) return Usage();
    var limit = ParseLimit(commandArgs, 2);
    var started = System.Diagnostics.Stopwatch.StartNew();
    var entries = new LinuxFileScanner().Scan(commandArgs[0]).ToArray();
    var scanMs = started.ElapsedMilliseconds;
    started.Restart();
    var results = LinuxFuzzySearch.Search(entries, commandArgs[1], limit);
    PrintResults(results);
    Console.Error.WriteLine($"Indexed {entries.Length:N0} entries in {scanMs:N0} ms; search returned {results.Count} results in {started.Elapsed.TotalMilliseconds:F2} ms.");
    return 0;
}

static int ParseLimit(string[] commandArgs, int index)
{
    if (commandArgs.Length <= index) return 50;
    if (!int.TryParse(commandArgs[index], out var limit) || limit <= 0) throw new ArgumentException("limit must be a positive integer.");
    return limit;
}

static void PrintResults(IReadOnlyList<LinuxSearchResult> results)
{
    foreach (var result in results) Console.WriteLine($"{result.Score,6}  {result.Entry.Path}");
}

static int Usage()
{
    Console.Error.WriteLine("Usage:");
    Console.Error.WriteLine("  lertaro-linux index <root> <index-file>");
    Console.Error.WriteLine("  lertaro-linux search <index-file> <query> [limit]");
    Console.Error.WriteLine("  lertaro-linux watch <index-file>");
    Console.Error.WriteLine("  lertaro-linux daemon-search <query> [limit]");
    Console.Error.WriteLine("  lertaro-linux daemon-status|daemon-rebuild|daemon-shutdown");
    Console.Error.WriteLine("  lertaro-linux bookmark-list");
    Console.Error.WriteLine("  lertaro-linux bookmark-add|bookmark-remove <path>");
    Console.Error.WriteLine("  lertaro-linux apps [query] [limit]");
    Console.Error.WriteLine("  lertaro-linux app-launch <desktop-file>");
    Console.Error.WriteLine("  lertaro-linux <root> <query> [limit]  # direct scan compatibility mode");
    Console.Error.WriteLine("Set LERTARO_SOCKET to override the default daemon socket path.");
    return 2;
}
