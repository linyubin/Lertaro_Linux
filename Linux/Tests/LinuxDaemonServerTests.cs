using System.Net.Sockets;
using Lertaro.Linux.Core;

namespace Lertaro.Linux.Tests;

[TestClass]
public sealed class LinuxDaemonServerTests
{
    [TestMethod]
    [Timeout(20_000, CooperativeCancellation = true)]
    public async Task Server_HandlesStatusSearchRebuildAndShutdown()
    {
        var workspace = CreateTempDirectory();
        try
        {
            var root = Path.Combine(workspace, "root");
            Directory.CreateDirectory(root);
            var target = Path.Combine(root, "calibration-report.txt");
            File.WriteAllText(target, "report");
            var indexPath = Path.Combine(workspace, "index.bin");
            var socketPath = Path.Combine(workspace, "search.sock");
            var store = new LinuxIndexStore();
            var snapshot = LinuxIndexSnapshot.Build(root);
            store.Save(snapshot, indexPath);
            var index = new LinuxMutableIndex(snapshot);

            using var watcher = new LinuxIndexWatcher(index, indexPath, store, TimeSpan.FromMilliseconds(50));
            watcher.Start();
            await using var server = new LinuxDaemonServer(index, watcher, indexPath, socketPath);
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            var runTask = server.RunAsync(timeout.Token);
            var client = new LinuxDaemonClient(socketPath);
            await WaitForServerAsync(client, timeout.Token);

            var status = await client.SendAsync(new LinuxDaemonRequest("status"), timeout.Token);
            Assert.IsTrue(status.Ok);
            Assert.IsNotNull(status.Status);
            Assert.AreEqual(root, status.Status.Root);

            var search = await client.SendAsync(new LinuxDaemonRequest("search", "calrep", 10), timeout.Token);
            Assert.IsTrue(search.Ok);
            Assert.IsNotNull(search.Results);
            Assert.HasCount(1, search.Results);
            Assert.AreEqual(target, search.Results[0].Path);

            var rebuild = await client.SendAsync(new LinuxDaemonRequest("rebuild"), timeout.Token);
            Assert.IsTrue(rebuild.Ok);
            Assert.IsNotNull(rebuild.Status);

            var shutdown = await client.SendAsync(new LinuxDaemonRequest("shutdown"), timeout.Token);
            Assert.IsTrue(shutdown.Ok);
            await runTask.WaitAsync(timeout.Token);
            watcher.Stop();
        }
        finally
        {
            Directory.Delete(workspace, recursive: true);
        }
    }

    private static async Task WaitForServerAsync(LinuxDaemonClient client, CancellationToken cancellationToken)
    {
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                var response = await client.SendAsync(new LinuxDaemonRequest("status"), cancellationToken);
                if (response.Ok)
                    return;
            }
            catch (SocketException)
            {
            }
            catch (IOException)
            {
            }

            await Task.Delay(25, cancellationToken);
        }
    }

    private static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), "lertaro-linux-daemon-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }
}
