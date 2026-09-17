using Lertaro.Linux.Core;

namespace Lertaro.Linux.Tests;

[TestClass]
public sealed class LinuxBookmarksTests
{
    private string _root = null!;
    private string _store = null!;

    [TestInitialize]
    public void Initialize()
    {
        _root = Path.Combine(Path.GetTempPath(), "lertaro-bookmarks-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_root);
        _store = Path.Combine(_root, "bookmarks.json");
    }

    [TestCleanup]
    public void Cleanup() => Directory.Delete(_root, true);

    [TestMethod]
    public void Add_PersistsAcrossInstances()
    {
        var target = Path.Combine(_root, "docs", "report.pdf");
        var bookmarks = new LinuxBookmarks(_store);
        Assert.IsTrue(bookmarks.Add(target));
        Assert.IsFalse(bookmarks.Add(target));

        var reloaded = new LinuxBookmarks(_store);
        Assert.IsTrue(reloaded.Contains(target));
        Assert.AreEqual(1, reloaded.Paths.Count);
    }

    [TestMethod]
    public void Remove_PersistsAcrossInstances()
    {
        var target = Path.Combine(_root, "notes.txt");
        var bookmarks = new LinuxBookmarks(_store);
        bookmarks.Add(target);
        Assert.IsTrue(bookmarks.Remove(target));
        Assert.IsFalse(bookmarks.Remove(target));

        Assert.IsFalse(new LinuxBookmarks(_store).Contains(target));
    }

    [TestMethod]
    public void CorruptFile_DoesNotPreventStartup()
    {
        File.WriteAllText(_store, "{not-json");
        var bookmarks = new LinuxBookmarks(_store);
        Assert.AreEqual(0, bookmarks.Paths.Count);
    }
}
