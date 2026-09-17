using Lertaro.Linux.Core;

namespace Lertaro.Linux.Tests;

[TestClass]
public sealed class LinuxSearchQueryTests
{
    [TestMethod]
    public void Parse_ExtractsFiltersAndText()
    {
        var query = LinuxSearchQuery.Parse("report ext:pdf type:file path:docs");

        Assert.AreEqual("report", query.Text);
        Assert.AreEqual("pdf", query.Extension);
        Assert.AreEqual("docs", query.PathContains);
        Assert.AreEqual(LinuxEntryKind.File, query.Kind);
    }

    [TestMethod]
    public void Search_FilterOnlyQuery_ReturnsMatchingEntries()
    {
        LinuxFileEntry[] entries =
        [
            new("/home/a/docs/report.pdf", "report.pdf", false, 10),
            new("/home/a/docs/report.txt", "report.txt", false, 10),
            new("/home/a/docs/archive", "archive", true, 0)
        ];

        var results = LinuxFuzzySearch.Search(entries, "ext:pdf type:file", 10);

        Assert.AreEqual(1, results.Count);
        Assert.AreEqual("report.pdf", results[0].Entry.Name);
    }

    [TestMethod]
    public void Search_PathAndFuzzyText_AreCombined()
    {
        LinuxFileEntry[] entries =
        [
            new("/home/a/docs/annual-report.pdf", "annual-report.pdf", false, 10),
            new("/home/a/tmp/annual-report.pdf", "annual-report.pdf", false, 10)
        ];

        var results = LinuxFuzzySearch.Search(entries, "anrep path:docs", 10);

        Assert.AreEqual(1, results.Count);
        StringAssert.Contains(results[0].Entry.Path, "/docs/");
    }

    [TestMethod]
    public void Parse_InvalidType_Throws()
    {
        Assert.ThrowsExactly<ArgumentException>(() => LinuxSearchQuery.Parse("type:device"));
    }
}
