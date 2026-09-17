namespace Lertaro.Linux.Core;

public sealed class LinuxMutableIndex
{
    private readonly object _sync = new();
    private readonly Dictionary<string, LinuxFileEntry> _entries = new(StringComparer.Ordinal);

    public LinuxMutableIndex(LinuxIndexSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        Root = NormalizeRoot(snapshot.Root);
        ReplaceAll(snapshot);
    }

    public string Root { get; }

    public int Count
    {
        get
        {
            lock (_sync)
                return _entries.Count;
        }
    }

    public bool Contains(string path)
    {
        var fullPath = NormalizeContainedPath(path);
        lock (_sync)
            return _entries.ContainsKey(fullPath);
    }

    public IReadOnlyList<LinuxFileEntry> GetEntries()
    {
        lock (_sync)
            return _entries.Values.ToArray();
    }

    public LinuxIndexSnapshot Snapshot()
    {
        lock (_sync)
            return new LinuxIndexSnapshot(Root, DateTime.UtcNow, _entries.Values.ToArray());
    }

    public bool RefreshSingle(string path)
    {
        var fullPath = NormalizeContainedPath(path);
        if (!TryReadEntry(fullPath, out var entry, out _))
            return RemovePathAndDescendants(fullPath) > 0;

        lock (_sync)
        {
            if (_entries.TryGetValue(fullPath, out var existing) && existing.Equals(entry))
                return false;
            _entries[fullPath] = entry;
            return true;
        }
    }

    public bool ReplaceSubtree(string path, LinuxFileScanner? scanner = null)
    {
        var fullPath = NormalizeContainedPath(path);
        if (!TryReadEntry(fullPath, out var rootEntry, out var isSymlink))
            return RemovePathAndDescendants(fullPath) > 0;

        var replacements = new List<LinuxFileEntry> { rootEntry };
        if (rootEntry.IsDirectory && !isSymlink)
        {
            scanner ??= new LinuxFileScanner();
            try
            {
                replacements.AddRange(scanner.Scan(fullPath));
            }
            catch (Exception ex) when (IsSkippable(ex))
            {
                // The path disappeared or became inaccessible while the watcher event was processed.
            }
        }

        lock (_sync)
        {
            var changed = RemovePathAndDescendantsLocked(fullPath) > 0;
            foreach (var entry in replacements)
            {
                if (!_entries.TryGetValue(entry.Path, out var existing) || !existing.Equals(entry))
                    changed = true;
                _entries[entry.Path] = entry;
            }
            return changed;
        }
    }

    public int RemovePathAndDescendants(string path)
    {
        var fullPath = NormalizeContainedPath(path);
        lock (_sync)
            return RemovePathAndDescendantsLocked(fullPath);
    }

    public void ReplaceAll(LinuxIndexSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        if (!string.Equals(Root, NormalizeRoot(snapshot.Root), StringComparison.Ordinal))
            throw new ArgumentException("Replacement snapshot root does not match the mutable index root.", nameof(snapshot));

        var validated = new Dictionary<string, LinuxFileEntry>(StringComparer.Ordinal);
        foreach (var entry in snapshot.Entries)
        {
            var fullPath = NormalizeContainedPath(entry.Path);
            validated[fullPath] = entry with { Path = fullPath, Name = Path.GetFileName(fullPath) };
        }

        lock (_sync)
        {
            _entries.Clear();
            foreach (var pair in validated)
                _entries.Add(pair.Key, pair.Value);
        }
    }

    private int RemovePathAndDescendantsLocked(string fullPath)
    {
        if (string.Equals(fullPath, Root, StringComparison.Ordinal))
        {
            var count = _entries.Count;
            _entries.Clear();
            return count;
        }

        var prefix = fullPath + Path.DirectorySeparatorChar;
        var keys = _entries.Keys
            .Where(key => string.Equals(key, fullPath, StringComparison.Ordinal) || key.StartsWith(prefix, StringComparison.Ordinal))
            .ToArray();
        foreach (var key in keys)
            _entries.Remove(key);
        return keys.Length;
    }

    private static bool TryReadEntry(string path, out LinuxFileEntry entry, out bool isSymlink)
    {
        try
        {
            var attributes = File.GetAttributes(path);
            var isDirectory = (attributes & FileAttributes.Directory) != 0;
            isSymlink = (attributes & FileAttributes.ReparsePoint) != 0;
            var size = isDirectory ? 0 : new FileInfo(path).Length;
            entry = new LinuxFileEntry(path, Path.GetFileName(path), isDirectory, size);
            return true;
        }
        catch (Exception ex) when (IsSkippable(ex))
        {
            entry = default;
            isSymlink = false;
            return false;
        }
    }

    private string NormalizeContainedPath(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        var fullPath = Path.GetFullPath(path);
        var relativePath = Path.GetRelativePath(Root, fullPath);
        if (relativePath == ".")
            return fullPath;
        if (Path.IsPathRooted(relativePath) || relativePath == ".." ||
            relativePath.StartsWith(".." + Path.DirectorySeparatorChar, StringComparison.Ordinal) ||
            relativePath.StartsWith(".." + Path.AltDirectorySeparatorChar, StringComparison.Ordinal))
            throw new ArgumentException($"Path is outside the configured root: {path}", nameof(path));
        return fullPath;
    }

    private static string NormalizeRoot(string root) =>
        Path.TrimEndingDirectorySeparator(Path.GetFullPath(root));

    private static bool IsSkippable(Exception ex) =>
        ex is UnauthorizedAccessException or IOException or System.Security.SecurityException;
}
