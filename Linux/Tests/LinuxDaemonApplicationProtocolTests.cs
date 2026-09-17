using Lertaro.Linux.Core;

namespace Lertaro.Linux.Tests;

[TestClass]
public sealed class LinuxDaemonApplicationProtocolTests
{
    [TestMethod]
    public async Task ApplicationPayloadRoundTrips()
    {
        var expected = new LinuxDaemonResponse(
            true,
            Applications:
            [
                new LinuxDaemonApplicationItem(
                    "org.example.Editor.desktop",
                    "Example Editor",
                    "example-editor",
                    "/usr/share/applications/org.example.Editor.desktop")
            ]);

        await using var stream = new MemoryStream();
        await LinuxDaemonProtocol.WriteAsync(stream, expected);
        stream.Position = 0;

        var actual = await LinuxDaemonProtocol.ReadAsync<LinuxDaemonResponse>(stream);

        Assert.IsTrue(actual.Ok);
        Assert.IsNotNull(actual.Applications);
        Assert.HasCount(1, actual.Applications);
        Assert.AreEqual(expected.Applications![0], actual.Applications[0]);
    }
}
