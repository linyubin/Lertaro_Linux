using Lertaro.Linux.Core;

namespace Lertaro.Linux.Tests;

[TestClass]
public sealed class LinuxDesktopActionsTests
{
    [TestMethod]
    public void BuildOpenStartInfo_PassesPathAsSingleArgumentWithoutShell()
    {
        var path = Path.GetFullPath("folder with spaces/file;name.txt");

        var info = LinuxDesktopActions.BuildOpenStartInfo(path);

        Assert.AreEqual("xdg-open", info.FileName);
        Assert.IsFalse(info.UseShellExecute);
        Assert.HasCount(1, info.ArgumentList);
        Assert.AreEqual(path, info.ArgumentList[0]);
    }

    [TestMethod]
    public void BuildLaunchApplicationStartInfo_UsesGioWithoutShellParsing()
    {
        var desktopFile = Path.GetFullPath("applications/My App.desktop");

        var info = LinuxDesktopActions.BuildLaunchApplicationStartInfo(desktopFile);

        Assert.AreEqual("gio", info.FileName);
        Assert.IsFalse(info.UseShellExecute);
        Assert.HasCount(2, info.ArgumentList);
        Assert.AreEqual("launch", info.ArgumentList[0]);
        Assert.AreEqual(desktopFile, info.ArgumentList[1]);
    }

    [TestMethod]
    public void BuildLaunchApplicationStartInfo_RejectsNonDesktopFile()
    {
        Assert.Throws<ArgumentException>(() => LinuxDesktopActions.BuildLaunchApplicationStartInfo("not-an-app.txt"));
    }

    [TestMethod]
    public void BuildRevealStartInfo_UsesFreedesktopFileManagerDbusInterface()
    {
        var path = Path.GetFullPath("folder/report.txt");

        var info = LinuxDesktopActions.BuildRevealStartInfo(path);

        Assert.AreEqual("gdbus", info.FileName);
        Assert.Contains("org.freedesktop.FileManager1", info.ArgumentList);
        Assert.Contains("org.freedesktop.FileManager1.ShowItems", info.ArgumentList);
        Assert.IsTrue(info.ArgumentList.Any(argument => argument.Contains(new Uri(path).AbsoluteUri, StringComparison.Ordinal)));
    }
}
