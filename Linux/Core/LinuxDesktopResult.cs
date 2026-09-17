namespace Lertaro.Linux.Core;

public enum LinuxDesktopResultKind
{
    FileSystem,
    Application
}

public sealed record LinuxDesktopResult(
    LinuxDesktopResultKind Kind,
    string Name,
    string Target,
    bool IsDirectory,
    int Score = 0)
{
    public string Subtitle => Target;

    public static LinuxDesktopResult FromSearch(LinuxDaemonSearchItem item) =>
        new(LinuxDesktopResultKind.FileSystem, item.Name, item.Path, item.IsDirectory, item.Score);

    public static LinuxDesktopResult FromApplication(LinuxDaemonApplicationItem item) =>
        new(LinuxDesktopResultKind.Application, item.Name, item.DesktopFile, false);
}
