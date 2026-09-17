using System.ComponentModel;
using System.Diagnostics;

namespace Lertaro.Linux.Core;

public static class LinuxDesktopActions
{
    public static void Open(string path)
    {
        var startInfo = BuildOpenStartInfo(path);
        StartRequired(startInfo);
    }

    public static void Reveal(string path)
    {
        var fullPath = ValidatePath(path);
        if (TryStart(BuildRevealStartInfo(fullPath), waitForExit: true))
            return;

        var directory = Directory.Exists(fullPath)
            ? fullPath
            : Path.GetDirectoryName(fullPath) ?? throw new IOException($"Cannot determine parent directory for {fullPath}.");
        StartRequired(BuildOpenStartInfo(directory));
    }

    public static ProcessStartInfo BuildOpenStartInfo(string path)
    {
        var fullPath = ValidatePath(path);
        var info = new ProcessStartInfo
        {
            FileName = "xdg-open",
            UseShellExecute = false,
            CreateNoWindow = true
        };
        info.ArgumentList.Add(fullPath);
        return info;
    }

    public static ProcessStartInfo BuildRevealStartInfo(string path)
    {
        var fullPath = ValidatePath(path);
        var info = new ProcessStartInfo
        {
            FileName = "gdbus",
            UseShellExecute = false,
            CreateNoWindow = true
        };
        info.ArgumentList.Add("call");
        info.ArgumentList.Add("--session");
        info.ArgumentList.Add("--dest");
        info.ArgumentList.Add("org.freedesktop.FileManager1");
        info.ArgumentList.Add("--object-path");
        info.ArgumentList.Add("/org/freedesktop/FileManager1");
        info.ArgumentList.Add("--method");
        info.ArgumentList.Add("org.freedesktop.FileManager1.ShowItems");
        info.ArgumentList.Add($"['{new Uri(fullPath).AbsoluteUri.Replace("'", "\\'")}']");
        info.ArgumentList.Add(string.Empty);
        return info;
    }

    private static string ValidatePath(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        return Path.GetFullPath(path);
    }

    private static void StartRequired(ProcessStartInfo startInfo)
    {
        if (!TryStart(startInfo, waitForExit: false))
            throw new IOException($"Failed to launch {startInfo.FileName}.");
    }

    private static bool TryStart(ProcessStartInfo startInfo, bool waitForExit)
    {
        try
        {
            using var process = Process.Start(startInfo);
            if (process is null)
                return false;
            if (!waitForExit)
                return true;
            process.WaitForExit(3_000);
            return process.HasExited && process.ExitCode == 0;
        }
        catch (Win32Exception)
        {
            return false;
        }
    }
}
