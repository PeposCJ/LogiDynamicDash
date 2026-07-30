using System.Text.Json;
using LogiDynamicDash.Models;

namespace LogiDynamicDash.Diagnostics;

internal sealed class SanitizedApplicationRuntimeDiagnostics(
    TextWriter writer,
    TimeProvider? timeProvider = null,
    bool ownsWriter = false) : IApplicationRuntimeDiagnostics
{
    private readonly TimeProvider clock =
        timeProvider ?? TimeProvider.System;
    private readonly object synchronization = new();
    private bool disposed;

    internal static SanitizedApplicationRuntimeDiagnostics CreateLocal()
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
            $"application-{timestamp}.jsonl");
        StreamWriter writer = new(path, append: false)
        {
            AutoFlush = true
        };
        return new SanitizedApplicationRuntimeDiagnostics(
            writer,
            ownsWriter: true);
    }

    public void RecordRender(
        string trigger,
        TelemetrySnapshot snapshot,
        DisplayMode mode)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(trigger);
        ArgumentNullException.ThrowIfNull(snapshot);
        Write(
            "render",
            new Dictionary<string, object?>
            {
                ["trigger"] = trigger,
                ["connection_state"] = snapshot.ConnectionState,
                ["mode"] = mode.ToString(),
                ["is_on_track"] = snapshot.IsOnTrack,
                ["gear"] = snapshot.Gear,
                ["speed_meters_per_second"] =
                    snapshot.SpeedMetersPerSecond,
                ["has_session_identity"] =
                    snapshot.SessionIdentity is not null
            });
    }

    public void RecordStop(ApplicationLifecycleState state) =>
        Write(
            "stop",
            new Dictionary<string, object?>
            {
                ["state"] = state.ToString()
            });

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
}
