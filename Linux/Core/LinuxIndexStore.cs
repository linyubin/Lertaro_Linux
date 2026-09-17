using System.Text;

namespace Lertaro.Linux.Core;

public sealed class LinuxIndexStore
{
    private const uint Magic = 0x584E4C4C;
    private const ushort Version = 1;
    private const int MaxEntryCount = 50_000_000;

    public void Save(LinuxIndexSnapshot snapshot, string path)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var targetPath = Path.GetFullPath(path);
        var directory = Path.GetDirectoryName(targetPath) ?? throw new InvalidOperationException("Index path has no parent directory.");
        Directory.CreateDirectory(directory);

        var tempPath = targetPath + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            using (var stream = new FileStream(tempPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            using (var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true))
            {
                writer.Write(Magic);
                writer.Write(Version);
                writer.Write(snapshot.Root);
                writer.Write(snapshot.CreatedUtc.Ticks);
                writer.Write(snapshot.Entries.Count);

                foreach (var entry in snapshot.Entries.OrderBy(entry => entry.Path, StringComparer.Ordinal))
                    WriteEntry(writer, snapshot.Root, entry);

                writer.Flush();
                stream.Flush(flushToDisk: true);
            }

            File.Move(tempPath, targetPath, overwrite: true);
        }
        finally
        {
            if (File.Exists(tempPath))
                File.Delete(tempPath);
        }
    }

    public LinuxIndexSnapshot Load(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        using var stream = new FileStream(Path.GetFullPath(path), FileMode.Open, FileAccess.Read, FileShare.Read);
        using var reader = new BinaryReader(stream, Encoding.UTF8);

        if (reader.ReadUInt32() != Magic)
            throw new InvalidDataException("Not a Lertaro Linux index file.");

        var version = reader.ReadUInt16();
        if (version != Version)
            throw new InvalidDataException($"Unsupported Linux index version: {version}.");

        var root = NormalizeRoot(reader.ReadString());
        var createdUtc = new DateTime(reader.ReadInt64(), DateTimeKind.Utc);
        var count = reader.ReadInt32();
        if (count < 0 || count > MaxEntryCount)
            throw new InvalidDataException($"Invalid Linux index entry count: {count}.");

        var entries = new LinuxFileEntry[count];
        for (var i = 0; i < count; i++)
            entries[i] = ReadEntry(reader, root);

        if (stream.Position != stream.Length)
            throw new InvalidDataException("Linux index contains trailing data.");

        return new LinuxIndexSnapshot(root, createdUtc, entries);
    }

    private static void WriteEntry(BinaryWriter writer, string root, LinuxFileEntry entry)
    {
        var fullPath = Path.GetFullPath(entry.Path);
        var relativePath = Path.GetRelativePath(root, fullPath);
        if (!IsSafeRelativePath(relativePath))
            throw new InvalidDataException($"Indexed path is outside the configured root: {entry.Path}");
        if (entry.Size < 0)
            throw new InvalidDataException($"Indexed file has a negative size: {entry.Path}");

        writer.Write(relativePath);
        writer.Write(entry.IsDirectory);
        writer.Write(entry.Size);
    }

    private static LinuxFileEntry ReadEntry(BinaryReader reader, string root)
    {
        var relativePath = reader.ReadString();
        if (!IsSafeRelativePath(relativePath))
            throw new InvalidDataException($"Index contains an unsafe relative path: {relativePath}");

        var path = Path.GetFullPath(Path.Combine(root, relativePath));
        if (!IsWithinRoot(root, path))
            throw new InvalidDataException($"Index path escapes configured root: {relativePath}");

        var isDirectory = reader.ReadBoolean();
        var size = reader.ReadInt64();
        if (size < 0)
            throw new InvalidDataException($"Index contains a negative file size: {relativePath}");

        return new LinuxFileEntry(path, Path.GetFileName(path), isDirectory, size);
    }

    private static bool IsSafeRelativePath(string path) =>
        path.Length > 0 &&
        path != "." &&
        !Path.IsPathRooted(path) &&
        path != ".." &&
        !path.StartsWith(".." + Path.DirectorySeparatorChar, StringComparison.Ordinal) &&
        !path.StartsWith(".." + Path.AltDirectorySeparatorChar, StringComparison.Ordinal);

    private static bool IsWithinRoot(string root, string path)
    {
        var relativePath = Path.GetRelativePath(root, path);
        return relativePath == "." || IsSafeRelativePath(relativePath);
    }

    private static string NormalizeRoot(string root) =>
        Path.TrimEndingDirectorySeparator(Path.GetFullPath(root));
}
