using System.Text.Json;

namespace Lertaro.Linux.Core;

public sealed class LinuxBookmarks
{
    private readonly string _path;
    private readonly HashSet<string> _paths = new(StringComparer.Ordinal);

    public LinuxBookmarks(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        _path = Path.GetFullPath(path);
        Load();
    }

    public IReadOnlyCollection<string> Paths => _paths;

    public bool Contains(string path) => _paths.Contains(Normalize(path));

    public bool Add(string path)
    {
        var changed = _paths.Add(Normalize(path));
        if (changed) Save();
        return changed;
    }

    public bool Remove(string path)
    {
        var changed = _paths.Remove(Normalize(path));
        if (changed) Save();
        return changed;
    }

    private void Load()
    {
        if (!File.Exists(_path)) return;
        try
        {
            var items = JsonSerializer.Deserialize<string[]>(File.ReadAllText(_path)) ?? [];
            foreach (var item in items)
                if (!string.IsNullOrWhiteSpace(item)) _paths.Add(Normalize(item));
        }
        catch (JsonException)
        {
            // A corrupt preference file must not prevent search startup.
        }
    }

    private void Save()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
        var temp = _path + ".tmp";
        File.WriteAllText(temp, JsonSerializer.Serialize(_paths.Order(StringComparer.Ordinal).ToArray()));
        File.Move(temp, _path, true);
    }

    private static string Normalize(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        return Path.GetFullPath(path);
    }
}
