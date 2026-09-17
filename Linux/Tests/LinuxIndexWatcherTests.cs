using Lertaro.Linux.Core;

namespace Lertaro.Linux.Tests;

[TestClass]
public sealed class LinuxIndexWatcherTests
{
    [TestMethod]
    [Timeout(15_000)]
    public async Task Watcher_TracksCreateRenameDeleteAndPersists()
    {
        var workspace = CreateTempDirectory();
        try
        {
            var root = Path.Combine(workspace, "root");
            Directory.CreateDirectory(root);
            var indexPath = Path.Combine(workspace, "index.bin");
            var store = new LinuxIndexStore();
            var initial = LinuxIndexSnapshot.Build(root);
            store.Save(initial, indexPath);
            var index = new LinuxMutableIndex(initial);

            using var watcher = new LinuxIndexWatcher(index, indexPath, store, TimeSpan.FromMilliseconds(50));
            watcher.Start();

            var incoming = Path.Combine(root, "incoming");
            Directory.CreateDirectory(incoming);
            var originalFile = Path.Combine(incoming, "report.txt");
            File.WriteAllText(originalFile, "report");
            await WaitUntilAsync(() => index.Contains(originalFile));

            var renamed = Path.Combine(root, "renamed");
            Directory.Move(incoming, renamed);
            var renamedFile = Path.Combine(renamed, "report.txt");
            await WaitUntilAsync(() => !index.Contains(originalFile) && index.Contains(renamedFile));

            Directory.Delete(renamed, recursive: true);
            await WaitUntilAsync(() => !index.Contains(renamed) && !index.Contains(renamedFile));

            watcher.Flush();
            var persisted = store.Load(indexPath);
            Assert.HasCount(0, persisted.Entries);
            Assert.IsNull(watcher.LastError);
        }
        finally
        {
            Directory.Delete(workspace, recursive: true);
        }
    }

    private static async Task WaitUntilAsync(Func<bool> condition)
    {
        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(5);
        while (DateTime.UtcNow < deadline)
        {
            if (condition())
                return;
            await Task.Delay(25);
        }

        Assert.IsTrue(condition(), "Filesystem watcher condition was not observed before timeout.");
    }

    private static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), "lertaro-linux-watch-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }
}
