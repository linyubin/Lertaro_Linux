using Lertaro.Linux.Core;

namespace Lertaro.Linux.Tests;

[TestClass]
public sealed class LinuxIndexStoreTests
{
    [TestMethod]
    public void SaveLoad_RoundTripsSnapshot()
    {
        var workspace = CreateTempDirectory();
        try
        {
            var root = Path.Combine(workspace, "root");
            Directory.CreateDirectory(Path.Combine(root, "nested"));
            File.WriteAllText(Path.Combine(root, "alpha.txt"), "alpha");
            File.WriteAllText(Path.Combine(root, "nested", "report.md"), "report");
            var indexPath = Path.Combine(workspace, "index.bin");

            var original = LinuxIndexSnapshot.Build(root);
            var store = new LinuxIndexStore();
            store.Save(original, indexPath);
            var loaded = store.Load(indexPath);

            Assert.AreEqual(Path.GetFullPath(root), loaded.Root);
            Assert.HasCount(original.Entries.Count, loaded.Entries);
            CollectionAssert.AreEquivalent(
                original.Entries.Select(entry => (entry.Path, entry.Name, entry.IsDirectory, entry.Size)).ToArray(),
                loaded.Entries.Select(entry => (entry.Path, entry.Name, entry.IsDirectory, entry.Size)).ToArray());
        }
        finally
        {
            Directory.Delete(workspace, recursive: true);
        }
    }

    [TestMethod]
    public void Save_RejectsEntryOutsideRoot()
    {
        var workspace = CreateTempDirectory();
        try
        {
            var root = Path.Combine(workspace, "root");
            Directory.CreateDirectory(root);
            var outside = Path.Combine(workspace, "outside.txt");
            var snapshot = new LinuxIndexSnapshot(
                root,
                DateTime.UtcNow,
                [new LinuxFileEntry(outside, "outside.txt", false, 1)]);

            Assert.ThrowsExactly<InvalidDataException>(() =>
                new LinuxIndexStore().Save(snapshot, Path.Combine(workspace, "index.bin")));
        }
        finally
        {
            Directory.Delete(workspace, recursive: true);
        }
    }

    [TestMethod]
    public void Load_RejectsInvalidMagic()
    {
        var workspace = CreateTempDirectory();
        try
        {
            var path = Path.Combine(workspace, "broken.bin");
            File.WriteAllBytes(path, [1, 2, 3, 4, 5, 6]);

            Assert.ThrowsExactly<InvalidDataException>(() => new LinuxIndexStore().Load(path));
        }
        finally
        {
            Directory.Delete(workspace, recursive: true);
        }
    }

    private static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), "lertaro-linux-index-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }
}
