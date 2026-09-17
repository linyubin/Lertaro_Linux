using Lertaro.Linux.Core;

namespace Lertaro.Linux.Tests;

[TestClass]
public sealed class LinuxFuzzySearchTests
{
    [TestMethod]
    public void Search_FindsSubsequenceUsingUpstreamFzf()
    {
        var entries = new[]
        {
            new LinuxFileEntry("/tmp/calibration_report.html", "calibration_report.html", false, 10),
            new LinuxFileEntry("/tmp/camera.txt", "camera.txt", false, 5),
            new LinuxFileEntry("/tmp/notes.md", "notes.md", false, 3)
        };

        var results = LinuxFuzzySearch.Search(entries, "calrep");

        Assert.HasCount(1, results);
        Assert.AreEqual("calibration_report.html", results[0].Entry.Name);
    }

    [TestMethod]
    public void Search_IsCaseInsensitive()
    {
        var entries = new[] { new LinuxFileEntry("/tmp/LiDAR.pcd", "LiDAR.pcd", false, 10) };
        var results = LinuxFuzzySearch.Search(entries, "lidar");
        Assert.HasCount(1, results);
    }

    [TestMethod]
    public void Search_RejectsInvalidLimit()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => LinuxFuzzySearch.Search(Array.Empty<LinuxFileEntry>(), "x", 0));
    }
}
