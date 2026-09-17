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
