namespace Lertaro.Linux.Core;

public readonly record struct LinuxFileEntry(string Path, string Name, bool IsDirectory, long Size);

public sealed class LinuxFileScanner
{
    public IEnumerable<LinuxFileEntry> Scan(string root, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(root);
        var fullRoot = Path.GetFullPath(root);
        if (!Directory.Exists(fullRoot))
            throw new DirectoryNotFoundException($"Scan root does not exist: {fullRoot}");

        var pending = new Stack<string>();
        pending.Push(fullRoot);

        while (pending.Count > 0)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var directory = pending.Pop();
            IEnumerator<string>? enumerator = null;
            try
            {
                enumerator = Directory.EnumerateFileSystemEntries(directory).GetEnumerator();
                while (MoveNext(enumerator))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var path = enumerator.Current;
                    FileAttributes attributes;
                    try { attributes = File.GetAttributes(path); }
                    catch (Exception ex) when (IsSkippable(ex)) { continue; }

                    var isDirectory = (attributes & FileAttributes.Directory) != 0;
                    var isSymlink = (attributes & FileAttributes.ReparsePoint) != 0;
                    var size = isDirectory ? 0 : TryGetLength(path);
                    yield return new LinuxFileEntry(path, Path.GetFileName(path), isDirectory, size);

                    if (isDirectory && !isSymlink)
                        pending.Push(path);
                }
            }
            finally
            {
                enumerator?.Dispose();
            }
        }
    }

    private static bool MoveNext(IEnumerator<string> enumerator)
    {
        try { return enumerator.MoveNext(); }
        catch (Exception ex) when (IsSkippable(ex)) { return false; }
    }

    private static long TryGetLength(string path)
    {
        try { return new FileInfo(path).Length; }
        catch (Exception ex) when (IsSkippable(ex)) { return 0; }
    }

    private static bool IsSkippable(Exception ex) =>
        ex is UnauthorizedAccessException or IOException or System.Security.SecurityException;
}
