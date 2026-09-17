namespace Lertaro.Linux.Core;

public enum LinuxEntryKind
{
    Any,
    File,
    Directory
}

public sealed record LinuxSearchQuery(string Text, string? Extension, string? PathContains, LinuxEntryKind Kind)
{
    public static LinuxSearchQuery Parse(string query)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(query);
        string? extension = null;
        string? path = null;
        var kind = LinuxEntryKind.Any;
        var terms = new List<string>();

        foreach (var token in query.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (token.StartsWith("ext:", StringComparison.OrdinalIgnoreCase))
            {
                extension = token[4..].TrimStart('.');
                if (extension.Length == 0)
                    throw new ArgumentException("ext: filter requires an extension.", nameof(query));
                continue;
            }
            if (token.StartsWith("path:", StringComparison.OrdinalIgnoreCase))
            {
                path = token[5..];
                if (path.Length == 0)
                    throw new ArgumentException("path: filter requires a value.", nameof(query));
                continue;
            }
            if (token.StartsWith("type:", StringComparison.OrdinalIgnoreCase))
            {
                kind = token[5..].ToLowerInvariant() switch
                {
                    "file" or "f" => LinuxEntryKind.File,
                    "dir" or "directory" or "folder" or "d" => LinuxEntryKind.Directory,
                    _ => throw new ArgumentException("type: must be file or dir.", nameof(query))
                };
                continue;
            }
            terms.Add(token);
        }

        return new LinuxSearchQuery(string.Join(' ', terms), extension, path, kind);
    }

    public bool Matches(LinuxFileEntry entry)
    {
        if (Kind == LinuxEntryKind.File && entry.IsDirectory)
            return false;
        if (Kind == LinuxEntryKind.Directory && !entry.IsDirectory)
            return false;
        if (Extension is not null)
        {
            if (entry.IsDirectory || !string.Equals(System.IO.Path.GetExtension(entry.Name).TrimStart('.'), Extension, StringComparison.OrdinalIgnoreCase))
                return false;
        }
        if (PathContains is not null && !entry.Path.Contains(PathContains, StringComparison.OrdinalIgnoreCase))
            return false;
        return true;
    }
}
