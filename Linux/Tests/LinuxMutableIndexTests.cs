using Lertaro.Linux.Core;

namespace Lertaro.Linux.Tests;

[TestClass]
public sealed class LinuxMutableIndexTests
{
    [TestMethod]
    public void RemovePathAndDescendants_DoesNotRemoveSiblingPrefix()
    {
        var root = CreateTempDirectory();
        try
        {
            var folder = Path.Combine(root, "data");
            var child = Path.Combine(folder, "child.txt");
            var sibling = Path.Combine(root, "database.txt");
            var snapshot = new LinuxIndexSnapshot(
                root,
                DateTime.UtcNow,
                [
                    new LinuxFileEntry(folder, "data", true, 0),
                    new LinuxFileEntry(child, "child.txt", false, 1),
                    new LinuxFileEntry(sibling, "database.txt", false, 2)
                ]);
            var index = new LinuxMutableIndex(snapshot);

            var removed = index.RemovePathAndDescendants(folder);

            Assert.AreEqual(2, removed);
            Assert.IsFalse(index.Contains(folder));
            Assert.IsFalse(index.Contains(child));
            Assert.IsTrue(index.Contains(sibling));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [TestMethod]
    public void ReplaceSubtree_CapturesPreexistingChildren()
    {
        var root = CreateTempDirectory();
        try
        {
            var movedIn = Path.Combine(root, "moved-in");
            Directory.CreateDirectory(Path.Combine(movedIn, "nested"));
            var file = Path.Combine(movedIn, "nested", "report.txt");
            File.WriteAllText(file, "report");
            var index = new LinuxMutableIndex(new LinuxIndexSnapshot(root, DateTime.UtcNow, []));

            var changed = index.ReplaceSubtree(movedIn);

            Assert.IsTrue(changed);
            Assert.IsTrue(index.Contains(movedIn));
            Assert.IsTrue(index.Contains(Path.Combine(movedIn, "nested")));
            Assert.IsTrue(index.Contains(file));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [TestMethod]
    public void RefreshSingle_UpdatesFileSize()
    {
        var root = CreateTempDirectory();
        try
        {
            var file = Path.Combine(root, "sample.bin");
            File.WriteAllText(file, "a");
            var index = new LinuxMutableIndex(new LinuxIndexSnapshot(root, DateTime.UtcNow, []));
            index.RefreshSingle(file);

            File.WriteAllText(file, "abcd");
            var changed = index.RefreshSingle(file);
            var entry = index.GetEntries().Single(item => item.Path == file);

            Assert.IsTrue(changed);
            Assert.AreEqual(4, entry.Size);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), "lertaro-linux-mutable-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }
}
