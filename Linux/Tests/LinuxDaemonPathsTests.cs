using Lertaro.Linux.Core;

namespace Lertaro.Linux.Tests;

[TestClass]
public sealed class LinuxDaemonPathsTests
{
    [TestMethod]
    public void Resolve_UsesXdgDirectoriesWhenAbsolute()
    {
        var paths = LinuxDaemonPaths.Resolve(
            "/home/tester",
            "/var/state/tester",
            "/run/user/1000",
            "tester",
            "/tmp");

        Assert.AreEqual("/home/tester", paths.Root);
        Assert.AreEqual("/var/state/tester/lertaro/index.bin", paths.IndexPath);
        Assert.AreEqual("/run/user/1000/lertaro/search.sock", paths.SocketPath);
    }

    [TestMethod]
    public void Resolve_IgnoresRelativeXdgDirectories()
    {
        var paths = LinuxDaemonPaths.Resolve(
            "/home/tester",
            "relative-state",
            "relative-runtime",
            "tester",
            "/tmp");

        Assert.AreEqual("/home/tester/.local/state/lertaro/index.bin", paths.IndexPath);
        Assert.AreEqual("/tmp/lertaro-tester/search.sock", paths.SocketPath);
    }
}
