using Lertaro.Linux.Core;

namespace Lertaro.Linux.Tests;

[TestClass]
public sealed class LinuxApplicationCatalogTests
{
    private string _root = null!;
    private string _userData = null!;
    private string _systemData = null!;

    [TestInitialize]
    public void Initialize()
    {
        _root = Path.Combine(Path.GetTempPath(), "lertaro-applications-" + Guid.NewGuid().ToString("N"));
        _userData = Path.Combine(_root, "user");
        _systemData = Path.Combine(_root, "system");
        Directory.CreateDirectory(Path.Combine(_userData, "applications"));
        Directory.CreateDirectory(Path.Combine(_systemData, "applications"));
    }

    [TestCleanup]
    public void Cleanup() => Directory.Delete(_root, true);

    [TestMethod]
    public void Discover_ReturnsVisibleApplicationsSortedByName()
    {
        Write(_systemData, "z.desktop", "Name=Zulu\nExec=zulu\nIcon=z-icon");
        Write(_systemData, "a.desktop", "Name=Alpha\nExec=alpha");
        Write(_systemData, "hidden.desktop", "Name=Hidden\nExec=hidden\nNoDisplay=true");
        Write(_systemData, "link.desktop", "Type=Link\nName=Link\nURL=https://example.com");

        var results = LinuxApplicationCatalog.Discover(new[] { _userData, _systemData });

        Assert.AreEqual(2, results.Count);
        Assert.AreEqual("Alpha", results[0].Name);
        Assert.AreEqual("Zulu", results[1].Name);
        Assert.AreEqual("z-icon", results[1].Icon);
    }

    [TestMethod]
    public void Discover_UserHiddenEntryMasksSystemEntryWithSameDesktopId()
    {
        Write(_userData, "editor.desktop", "Name=Editor\nExec=editor\nHidden=true");
        Write(_systemData, "editor.desktop", "Name=Editor\nExec=editor");

        var results = LinuxApplicationCatalog.Discover(new[] { _userData, _systemData });

        Assert.AreEqual(0, results.Count);
    }

    [TestMethod]
    public void Discover_UsesRelativePathAsDesktopId()
    {
        var nested = Path.Combine(_systemData, "applications", "vendor");
        Directory.CreateDirectory(nested);
        File.WriteAllText(Path.Combine(nested, "viewer.desktop"), Desktop("Name=Viewer\nExec=viewer"));

        var result = LinuxApplicationCatalog.Discover(new[] { _systemData }).Single();

        Assert.AreEqual("vendor-viewer.desktop", result.DesktopId);
    }

    [TestMethod]
    public void Discover_RequiresExecAndIgnoresLocalizedNameKeys()
    {
        Write(_systemData, "broken.desktop", "Name=Broken");
        Write(_systemData, "viewer.desktop", "Name=Viewer\nName[zh_CN]=查看器\nExec=viewer");

        var result = LinuxApplicationCatalog.Discover(new[] { _systemData }).Single();

        Assert.AreEqual("Viewer", result.Name);
    }

    private void Write(string dataDirectory, string name, string body) =>
        File.WriteAllText(Path.Combine(dataDirectory, "applications", name), Desktop(body));

    private static string Desktop(string body) => $"[Desktop Entry]\nType=Application\n{body}\n";
}
