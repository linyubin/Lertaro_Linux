using Lertaro.Linux.Core;

try
{
    return args.Length == 0
        ? Usage()
        : args[0] switch
        {
            "index" => RunIndex(args),
            "search" => RunSearch(args),
            _ => RunDirectSearch(args)
        };
}
catch (Exception ex) when (ex is ArgumentException or DirectoryNotFoundException or InvalidDataException or UnauthorizedAccessException or IOException)
{
    Console.Error.WriteLine(ex.Message);
    return 1;
}

static int RunIndex(string[] commandArgs)
{
    if (commandArgs.Length != 3)
        return Usage();

    var root = commandArgs[1];
    var indexPath = commandArgs[2];
    var started = System.Diagnostics.Stopwatch.StartNew();
    var snapshot = LinuxIndexSnapshot.Build(root);
    new LinuxIndexStore().Save(snapshot, indexPath);

    Console.Error.WriteLine($"Indexed {snapshot.Entries.Count:N0} entries in {started.ElapsedMilliseconds:N0} ms -> {Path.GetFullPath(indexPath)}");
    return 0;
}

static int RunSearch(string[] commandArgs)
{
    if (commandArgs.Length is < 3 or > 4)
        return Usage();

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

static int RunDirectSearch(string[] commandArgs)
{
    if (commandArgs.Length is < 2 or > 3)
        return Usage();

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
    if (commandArgs.Length <= index)
        return 50;
    if (!int.TryParse(commandArgs[index], out var limit) || limit <= 0)
        throw new ArgumentException("limit must be a positive integer.");
    return limit;
}

static void PrintResults(IReadOnlyList<LinuxSearchResult> results)
{
    foreach (var result in results)
        Console.WriteLine($"{result.Score,6}  {result.Entry.Path}");
}

static int Usage()
{
    Console.Error.WriteLine("Usage:");
    Console.Error.WriteLine("  lertaro-linux index <root> <index-file>");
    Console.Error.WriteLine("  lertaro-linux search <index-file> <query> [limit]");
    Console.Error.WriteLine("  lertaro-linux <root> <query> [limit]  # direct scan compatibility mode");
    return 2;
}
