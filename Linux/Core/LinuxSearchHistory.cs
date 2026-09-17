using System.Text.Json;

namespace Lertaro.Linux.Core;

public sealed record LinuxSearchHistoryEntry(string Query, DateTimeOffset LastUsedUtc, int UseCount);

public sealed class LinuxSearchHistory
{
    private readonly string _path;
    private readonly int _capacity;
    private readonly SemaphoreSlim _gate = new(1, 1);

    public LinuxSearchHistory(string path, int capacity = 100)
    {
        if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("History path is required.", nameof(path));
        if (capacity < 1) throw new ArgumentOutOfRangeException(nameof(capacity));
        _path = path;
        _capacity = capacity;
    }

    public async Task<IReadOnlyList<LinuxSearchHistoryEntry>> LoadAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try { return await LoadUnsafeAsync(cancellationToken).ConfigureAwait(false); }
        finally { _gate.Release(); }
    }

    public async Task RecordAsync(string query, DateTimeOffset? usedUtc = null, CancellationToken cancellationToken = default)
    {
        query = query.Trim();
        if (query.Length == 0) return;

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var entries = (await LoadUnsafeAsync(cancellationToken).ConfigureAwait(false)).ToList();
            var existing = entries.FindIndex(x => string.Equals(x.Query, query, StringComparison.Ordinal));
            var count = existing >= 0 ? entries[existing].UseCount + 1 : 1;
            if (existing >= 0) entries.RemoveAt(existing);
            entries.Add(new LinuxSearchHistoryEntry(query, usedUtc ?? DateTimeOffset.UtcNow, count));
            entries = entries.OrderByDescending(x => x.LastUsedUtc).Take(_capacity).ToList();
            await SaveUnsafeAsync(entries, cancellationToken).ConfigureAwait(false);
        }
        finally { _gate.Release(); }
    }

    private async Task<IReadOnlyList<LinuxSearchHistoryEntry>> LoadUnsafeAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(_path)) return Array.Empty<LinuxSearchHistoryEntry>();
        await using var stream = File.OpenRead(_path);
        var entries = await JsonSerializer.DeserializeAsync<List<LinuxSearchHistoryEntry>>(stream, cancellationToken: cancellationToken).ConfigureAwait(false);
        return entries?.OrderByDescending(x => x.LastUsedUtc).Take(_capacity).ToArray() ?? Array.Empty<LinuxSearchHistoryEntry>();
    }

    private async Task SaveUnsafeAsync(IReadOnlyList<LinuxSearchHistoryEntry> entries, CancellationToken cancellationToken)
    {
        var directory = Path.GetDirectoryName(_path);
        if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
        var temp = _path + ".tmp";
        await using (var stream = new FileStream(temp, FileMode.Create, FileAccess.Write, FileShare.None))
            await JsonSerializer.SerializeAsync(stream, entries, cancellationToken: cancellationToken).ConfigureAwait(false);
        File.Move(temp, _path, true);
    }
}
