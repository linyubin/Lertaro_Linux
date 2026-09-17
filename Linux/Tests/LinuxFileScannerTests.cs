using Lertaro.Linux.Core;

namespace Lertaro.Linux.Tests;

[TestClass]
public sealed class LinuxFileScannerTests
{
    [TestMethod]
    public void Scan_RecursesAndPreservesDotFiles()
    {
        var root = CreateTempDirectory();
        try
        {
            Directory.CreateDirectory(Path.Combine(root, "nested"));
            File.WriteAllText(Path.Combine(root, ".hidden"), "x");
            File.WriteAllText(Path.Combine(root, "nested", "report.txt"), "report");

            var entries = new LinuxFileScanner().Scan(root).ToArray();

            Assert.IsTrue(entries.Any(entry => entry.Name == ".hidden" && !entry.IsDirectory));
            Assert.IsTrue(entries.Any(entry => entry.Name == "nested" && entry.IsDirectory));
            Assert.IsTrue(entries.Any(entry => entry.Name == "report.txt" && entry.Size == 6));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [TestMethod]
    public void Scan_MissingRootThrows()
    {
        var missing = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Assert.ThrowsExactly<DirectoryNotFoundException>(() => new LinuxFileScanner().Scan(missing).ToArray());
    }

    private static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), "lertaro-linux-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }
}
