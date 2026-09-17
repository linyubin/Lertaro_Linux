using Lertaro.Linux.Core;

namespace Lertaro.Linux.Tests;

[TestClass]
public sealed class LinuxDesktopResultTests
{
    [TestMethod]
    public void FromSearch_PreservesFilesystemActivationData()
    {
        var source = new LinuxDaemonSearchItem("/tmp/report.txt", "report.txt", false, 42, 17);

        var result = LinuxDesktopResult.FromSearch(source);

        Assert.AreEqual(LinuxDesktopResultKind.FileSystem, result.Kind);
        Assert.AreEqual("report.txt", result.Name);
        Assert.AreEqual("/tmp/report.txt", result.Target);
        Assert.IsFalse(result.IsDirectory);
        Assert.AreEqual(17, result.Score);
    }

    [TestMethod]
    public void FromApplication_UsesDesktopFileAsActivationTarget()
    {
        var source = new LinuxDaemonApplicationItem("org.example.Editor.desktop", "Editor", "editor", "/usr/share/applications/editor.desktop");

        var result = LinuxDesktopResult.FromApplication(source);

        Assert.AreEqual(LinuxDesktopResultKind.Application, result.Kind);
        Assert.AreEqual("Editor", result.Name);
        Assert.AreEqual("/usr/share/applications/editor.desktop", result.Target);
        Assert.IsFalse(result.IsDirectory);
    }
}
