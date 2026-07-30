using System.Text.Json;
using LogiDynamicDash.Hidpp;
using LogiDynamicDash.Models;

namespace LogiDynamicDash.Diagnostics;

internal sealed class SanitizedRs50OledDiagnostics(
    TextWriter writer,
    TimeProvider? timeProvider = null,
    bool ownsWriter = false) : IRs50OledDiagnostics
{
    private readonly TimeProvider clock =
        timeProvider ?? TimeProvider.System;
    private readonly object synchronization = new();
    private bool disposed;

    internal static SanitizedRs50OledDiagnostics CreateLocal()
    {
        string directory = Path.Combine(
            Environment.GetFolderPath(
                Environment.SpecialFolder.LocalApplicationData),
            "LogiDynamicDash",
            "logs");
        Directory.CreateDirectory(directory);
        string timestamp =
            DateTimeOffset.UtcNow.ToString("yyyyMMdd-HHmmss-fffffff");
        string path = Path.Combine(
            directory,
            $"rs50-oled-{timestamp}.jsonl");
        StreamWriter writer = new(path, append: false)
        {
            AutoFlush = true
        };
        return new SanitizedRs50OledDiagnostics(
            writer,
            ownsWriter: true);
    }

    public void RecordOpen(long elapsedMicroseconds) =>
        Write(
            "open",
            new Dictionary<string, object?>
            {
                ["result"] = "acknowledged",
                ["elapsed_microseconds"] = elapsedMicroseconds
            });

    public void RecordFrame(
        Rs50OledLayout layout,
        Rs50OledSendResult result,
        long elapsedMicroseconds) =>
        Write(
            "frame",
            new Dictionary<string, object?>
            {
                ["layout"] = layout.ToString(),
                ["result"] = ToDiagnosticResult(result),
                ["elapsed_microseconds"] = elapsedMicroseconds
            });

    public void RecordFailure(string operation, Type exceptionType)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(operation);
        ArgumentNullException.ThrowIfNull(exceptionType);
        Write(
            "failure",
            new Dictionary<string, object?>
            {
                ["operation"] = operation,
                ["error_type"] = exceptionType.Name
            });
    }

    public void RecordClose() =>
        Write("close", []);

    public void Dispose()
    {
        lock (synchronization)
        {
            if (disposed)
            {
                return;
            }

            disposed = true;
            if (ownsWriter)
            {
                writer.Dispose();
            }
        }
    }

    private void Write(
        string eventType,
        Dictionary<string, object?> fields)
    {
        lock (synchronization)
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            fields["timestamp_utc"] = clock.GetUtcNow();
            fields["event"] = eventType;
            writer.WriteLine(JsonSerializer.Serialize(fields));
            writer.Flush();
        }
    }

    private static string ToDiagnosticResult(Rs50OledSendResult result) =>
        result switch
        {
            Rs50OledSendResult.Transmitted => "acknowledged",
            Rs50OledSendResult.Unacknowledged => "unacknowledged",
            Rs50OledSendResult.Unchanged => "unchanged",
            Rs50OledSendResult.RateLimited => "rate_limited",
            _ => throw new ArgumentOutOfRangeException(nameof(result))
        };
}
