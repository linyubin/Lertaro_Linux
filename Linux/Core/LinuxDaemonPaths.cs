namespace Lertaro.Linux.Core;

public sealed record LinuxDaemonPaths(
    string Root,
    string StateDirectory,
    string IndexPath,
    string RuntimeDirectory,
    string SocketPath)
{
    public static LinuxDaemonPaths CreateDefault()
    {
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        if (string.IsNullOrWhiteSpace(home))
            throw new InvalidOperationException("Cannot determine the current user's home directory.");

        return Resolve(
            home,
            Environment.GetEnvironmentVariable("XDG_STATE_HOME"),
            Environment.GetEnvironmentVariable("XDG_RUNTIME_DIR"),
            Environment.UserName,
            Path.GetTempPath());
    }

    public static LinuxDaemonPaths Resolve(
        string userHome,
        string? xdgStateHome,
        string? xdgRuntimeDirectory,
        string userName,
        string tempPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userHome);
        ArgumentException.ThrowIfNullOrWhiteSpace(userName);
        ArgumentException.ThrowIfNullOrWhiteSpace(tempPath);

        var home = Path.GetFullPath(userHome);
        var stateBase = IsAbsoluteDirectory(xdgStateHome)
            ? Path.GetFullPath(xdgStateHome!)
            : Path.Combine(home, ".local", "state");
        var stateDirectory = Path.Combine(stateBase, "lertaro");

        var runtimeDirectory = IsAbsoluteDirectory(xdgRuntimeDirectory)
            ? Path.Combine(Path.GetFullPath(xdgRuntimeDirectory!), "lertaro")
            : Path.Combine(Path.GetFullPath(tempPath), "lertaro-" + userName);

        return new LinuxDaemonPaths(
            home,
            stateDirectory,
            Path.Combine(stateDirectory, "index.bin"),
            runtimeDirectory,
            Path.Combine(runtimeDirectory, "search.sock"));
    }

    private static bool IsAbsoluteDirectory(string? path) =>
        !string.IsNullOrWhiteSpace(path) && Path.IsPathFullyQualified(path);
}
