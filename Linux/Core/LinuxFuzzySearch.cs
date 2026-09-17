using Lertaro.Core.SearchIndex.Fzf;

namespace Lertaro.Linux.Core;

public readonly record struct LinuxSearchResult(LinuxFileEntry Entry, int Score, int Start);

public static class LinuxFuzzySearch
{
    public static IReadOnlyList<LinuxSearchResult> Search(
        IEnumerable<LinuxFileEntry> entries,
        string query,
        int limit = 50)
    {
        ArgumentNullException.ThrowIfNull(entries);
        if (limit <= 0)
            throw new ArgumentOutOfRangeException(nameof(limit));

        var parsed = LinuxSearchQuery.Parse(query);
        var pattern = parsed.Text.ToLowerInvariant();
        var slab = new FzfSlab();
        var results = new List<LinuxSearchResult>();

        foreach (var entry in entries)
        {
            if (!parsed.Matches(entry))
                continue;

            if (pattern.Length == 0)
            {
                results.Add(new LinuxSearchResult(entry, 0, 0));
                continue;
            }

            var match = FzfFuzzyMatcher.FuzzyMatchV2(
                entry.Name.AsSpan(), pattern, caseSensitive: false, FzfScoringScheme.Default, slab);
            if (match.IsMatch)
                results.Add(new LinuxSearchResult(entry, match.Score, match.Start));
        }

        return results
            .OrderByDescending(result => result.Score)
            .ThenBy(result => result.Start)
            .ThenBy(result => result.Entry.Name.Length)
            .ThenBy(result => result.Entry.Name, StringComparer.OrdinalIgnoreCase)
            .Take(limit)
            .ToArray();
    }
}
