using System.Text;
using System.Text.Json;

namespace Lertaro.Linux.Core;

public sealed record LinuxDaemonRequest(string Command, string? Query = null, int Limit = 50);

public sealed record LinuxDaemonSearchItem(
    string Path,
    string Name,
    bool IsDirectory,
    long Size,
    int Score);

public sealed record LinuxDaemonStatus(
    string Root,
    string IndexPath,
    int EntryCount,
    string? WatcherError);

public sealed record LinuxDaemonResponse(
    bool Ok,
    string? Error = null,
    IReadOnlyList<LinuxDaemonSearchItem>? Results = null,
    LinuxDaemonStatus? Status = null);

public static class LinuxDaemonProtocol
{
    private const int MaxMessageChars = 4 * 1024 * 1024;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly Encoding Utf8 = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

    public static async Task WriteAsync<T>(Stream stream, T message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);
        var json = JsonSerializer.Serialize(message, JsonOptions);
        if (json.Length > MaxMessageChars)
            throw new InvalidDataException("Daemon protocol message is too large.");

        using var writer = new StreamWriter(stream, Utf8, bufferSize: 4096, leaveOpen: true) { AutoFlush = true };
        await writer.WriteLineAsync(json.AsMemory(), cancellationToken);
    }

    public static async Task<T> ReadAsync<T>(Stream stream, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);
        using var reader = new StreamReader(stream, Utf8, detectEncodingFromByteOrderMarks: false, bufferSize: 4096, leaveOpen: true);
        var line = await reader.ReadLineAsync(cancellationToken);
        if (line is null)
            throw new EndOfStreamException("Daemon peer closed the connection before sending a message.");
        if (line.Length > MaxMessageChars)
            throw new InvalidDataException("Daemon protocol message is too large.");

        return JsonSerializer.Deserialize<T>(line, JsonOptions)
            ?? throw new InvalidDataException("Daemon protocol message could not be deserialized.");
    }
}
