using Lertaro.Linux.Core;

if (args.Length < 2)
{
    Console.Error.WriteLine("Usage: lertaro-linux <root> <query> [limit]");
    return 2;
}

var root = args[0];
var query = args[1];
var limit = 50;
if (args.Length >= 3 && (!int.TryParse(args[2], out limit) || limit <= 0))
{
    Console.Error.WriteLine("limit must be a positive integer.");
    return 2;
}

try
{
    var started = System.Diagnostics.Stopwatch.StartNew();
    var entries = new LinuxFileScanner().Scan(root).ToArray();
    var scanMs = started.ElapsedMilliseconds;

    started.Restart();
    var results = LinuxFuzzySearch.Search(entries, query, limit);
    var searchMs = started.Elapsed.TotalMilliseconds;

    foreach (var result in results)
        Console.WriteLine($"{result.Score,6}  {result.Entry.Path}");

    Console.Error.WriteLine($"Indexed {entries.Length:N0} entries in {scanMs:N0} ms; search returned {results.Count} results in {searchMs:F2} ms.");
    return 0;
}
catch (Exception ex) when (ex is ArgumentException or DirectoryNotFoundException or UnauthorizedAccessException or IOException)
{
    Console.Error.WriteLine(ex.Message);
    return 1;
}
