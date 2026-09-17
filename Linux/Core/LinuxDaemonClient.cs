using System.Net.Sockets;

namespace Lertaro.Linux.Core;

public sealed class LinuxDaemonClient
{
    public LinuxDaemonClient(string socketPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(socketPath);
        SocketPath = Path.GetFullPath(socketPath);
    }

    public string SocketPath { get; }

    public LinuxDaemonResponse Send(LinuxDaemonRequest request, CancellationToken cancellationToken = default) =>
        SendAsync(request, cancellationToken).GetAwaiter().GetResult();

    public async Task<LinuxDaemonResponse> SendAsync(
        LinuxDaemonRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        using var socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
        await socket.ConnectAsync(new UnixDomainSocketEndPoint(SocketPath), cancellationToken);
        using var stream = new NetworkStream(socket, ownsSocket: false);
        await LinuxDaemonProtocol.WriteAsync(stream, request, cancellationToken);
        return await LinuxDaemonProtocol.ReadAsync<LinuxDaemonResponse>(stream, cancellationToken);
    }
}
