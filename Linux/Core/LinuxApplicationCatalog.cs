namespace Lertaro.Linux.Core;

public sealed record LinuxApplicationEntry(string DesktopId, string Name, string? Icon, string DesktopFile);

public static class LinuxApplicationCatalog
{
    public static IReadOnlyList<LinuxApplicationEntry> Discover(IEnumerable<string>? dataDirectories = null)
    {
        var directories = dataDirectories?.ToArray() ?? GetDataDirectories();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var applications = new List<LinuxApplicationEntry>();

        foreach (var dataDirectory in directories)
        {
            var applicationsDirectory = Path.Combine(dataDirectory, "applications");
            if (!Directory.Exists(applicationsDirectory))
                continue;

            IEnumerable<string> files;
            try
            {
                files = Directory.EnumerateFiles(applicationsDirectory, "*.desktop", SearchOption.AllDirectories);
            }
            catch (UnauthorizedAccessException)
            {
                continue;
            }
            catch (IOException)
            {
                continue;
            }

            foreach (var file in files)
            {
                var desktopId = Path.GetRelativePath(applicationsDirectory, file)
                    .Replace(Path.DirectorySeparatorChar, '-')
                    .Replace(Path.AltDirectorySeparatorChar, '-');

                // A higher-priority entry, including Hidden=true, masks lower-priority entries.
                if (!seen.Add(desktopId))
                    continue;

                var entry = TryRead(file, desktopId);
                if (entry is not null)
                    applications.Add(entry);
            }
        }

        return applications.OrderBy(entry => entry.Name, StringComparer.CurrentCultureIgnoreCase).ToArray();
    }

    internal static LinuxApplicationEntry? TryRead(string file, string desktopId)
    {
        try
        {
            var values = ReadDesktopEntry(file);
            if (!values.TryGetValue("Type", out var type) || type != "Application")
                return null;
            if (IsTrue(values, "Hidden") || IsTrue(values, "NoDisplay"))
                return null;
            if (!values.TryGetValue("Name", out var name) || string.IsNullOrWhiteSpace(name))
                return null;
            if (!values.TryGetValue("Exec", out var exec) || string.IsNullOrWhiteSpace(exec))
                return null;

            values.TryGetValue("Icon", out var icon);
            return new LinuxApplicationEntry(desktopId, name, icon, file);
        }
        catch (IOException)
        {
            return null;
        }
        catch (UnauthorizedAccessException)
        {
            return null;
        }
    }

    private static Dictionary<string, string> ReadDesktopEntry(string file)
    {
        var values = new Dictionary<string, string>(StringComparer.Ordinal);
        var inDesktopEntry = false;

        foreach (var rawLine in File.ReadLines(file))
        {
            var line = rawLine.Trim();
            if (line.Length == 0 || line.StartsWith('#'))
                continue;

            if (line.StartsWith('[') && line.EndsWith(']'))
            {
                if (inDesktopEntry)
                    break;
                inDesktopEntry = line == "[Desktop Entry]";
                continue;
            }

            if (!inDesktopEntry)
                continue;

            var separator = line.IndexOf('=');
            if (separator <= 0)
                continue;

            var key = line[..separator];
            if (key.Contains('['))
                continue;

            values[key] = line[(separator + 1)..];
        }

        return values;
    }

    private static bool IsTrue(IReadOnlyDictionary<string, string> values, string key) =>
        values.TryGetValue(key, out var value) && value.Equals("true", StringComparison.OrdinalIgnoreCase);

    private static string[] GetDataDirectories()
    {
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var dataHome = Environment.GetEnvironmentVariable("XDG_DATA_HOME");
        if (string.IsNullOrWhiteSpace(dataHome))
            dataHome = Path.Combine(home, ".local", "share");

        var dataDirectories = Environment.GetEnvironmentVariable("XDG_DATA_DIRS");
        if (string.IsNullOrWhiteSpace(dataDirectories))
            dataDirectories = "/usr/local/share:/usr/share";

        return new[] { dataHome }
            .Concat(dataDirectories.Split(':', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .Distinct(StringComparer.Ordinal)
            .ToArray();
    }
}
