namespace Lertaro.Linux.Core;

public sealed class LinuxIndexSnapshot
{
    public LinuxIndexSnapshot(string root, DateTime createdUtc, IReadOnlyList<LinuxFileEntry> entries)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(root);
        ArgumentNullException.ThrowIfNull(entries);

        Root = NormalizeRoot(root);
        CreatedUtc = createdUtc.Kind == DateTimeKind.Utc ? createdUtc : createdUtc.ToUniversalTime();
        Entries = entries;
    }

    public string Root { get; }
    public DateTime CreatedUtc { get; }
    public IReadOnlyList<LinuxFileEntry> Entries { get; }

    public static LinuxIndexSnapshot Build(
        string root,
        LinuxFileScanner? scanner = null,
        CancellationToken cancellationToken = default)
    {
        scanner ??= new LinuxFileScanner();
        var normalizedRoot = NormalizeRoot(root);
        var entries = scanner.Scan(normalizedRoot, cancellationToken).ToArray();
        return new LinuxIndexSnapshot(normalizedRoot, DateTime.UtcNow, entries);
    }

    private static string NormalizeRoot(string root)
    {
        var fullPath = Path.GetFullPath(root);
        return Path.TrimEndingDirectorySeparator(fullPath);
    }
}
