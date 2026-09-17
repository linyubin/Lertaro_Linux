using Lertaro.Linux.Core;

namespace Lertaro.Linux.Tests;

[TestClass]
public sealed class LinuxSearchHistoryTests
{
    [TestMethod]
    public async Task RecordAsync_PersistsAndIncrementsExistingQuery()
    {
        var path = TempPath();
        try
        {
            var history = new LinuxSearchHistory(path);
            var first = new DateTimeOffset(2026, 9, 17, 10, 0, 0, TimeSpan.Zero);
            var second = first.AddMinutes(1);
            await history.RecordAsync("report", first);
            await history.RecordAsync("report", second);

            var reloaded = await new LinuxSearchHistory(path).LoadAsync();
            Assert.AreEqual(1, reloaded.Count);
            Assert.AreEqual("report", reloaded[0].Query);
            Assert.AreEqual(2, reloaded[0].UseCount);
            Assert.AreEqual(second, reloaded[0].LastUsedUtc);
        }
        finally { Cleanup(path); }
    }

    [TestMethod]
    public async Task RecordAsync_OrdersByRecencyAndEnforcesCapacity()
    {
        var path = TempPath();
        try
        {
            var history = new LinuxSearchHistory(path, capacity: 2);
            var start = new DateTimeOffset(2026, 9, 17, 10, 0, 0, TimeSpan.Zero);
            await history.RecordAsync("one", start);
            await history.RecordAsync("two", start.AddMinutes(1));
            await history.RecordAsync("three", start.AddMinutes(2));

            var entries = await history.LoadAsync();
            CollectionAssert.AreEqual(new[] { "three", "two" }, entries.Select(x => x.Query).ToArray());
        }
        finally { Cleanup(path); }
    }

    [TestMethod]
    public async Task RecordAsync_IgnoresBlankQueries()
    {
        var path = TempPath();
        try
        {
            var history = new LinuxSearchHistory(path);
            await history.RecordAsync("   ");
            Assert.AreEqual(0, (await history.LoadAsync()).Count);
            Assert.IsFalse(File.Exists(path));
        }
        finally { Cleanup(path); }
    }

    private static string TempPath() => Path.Combine(Path.GetTempPath(), $"lertaro-history-{Guid.NewGuid():N}", "history.json");
    private static void Cleanup(string path)
    {
        var directory = Path.GetDirectoryName(path);
        if (directory is not null && Directory.Exists(directory)) Directory.Delete(directory, true);
    }
}
