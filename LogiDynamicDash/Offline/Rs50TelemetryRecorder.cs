using System.Text.Json;
using LogiDynamicDash.Models;
using LogiDynamicDash.Services;

namespace LogiDynamicDash.Offline;

internal sealed class Rs50TelemetryRecorder(
    ITelemetrySource source,
    TimeProvider? timeProvider = null)
{
    private const int MaximumEvents = 10000;
    private static readonly TimeSpan UpdateInterval =
        TimeSpan.FromMilliseconds(200);

    private readonly TimeProvider clock = timeProvider ?? TimeProvider.System;
    private readonly object synchronization = new();
    private readonly List<TelemetryReplayEvent> events = [];
    private long started;
    private long lastUpdate;
    private bool hasUpdate;

    internal async Task RecordAsync(
        string outputPath,
        TimeSpan duration,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);
        if (duration < TimeSpan.FromSeconds(1) ||
            duration > TimeSpan.FromMinutes(30))
        {
            throw new ArgumentOutOfRangeException(nameof(duration));
        }

        string fullPath = Path.GetFullPath(outputPath);
        if (File.Exists(fullPath))
        {
            throw new IOException(
                "The telemetry replay output already exists.");
        }

        string? directory = Path.GetDirectoryName(fullPath);
        if (string.IsNullOrEmpty(directory) || !Directory.Exists(directory))
        {
            throw new DirectoryNotFoundException(
                "The telemetry replay output directory does not exist.");
        }

        started = clock.GetTimestamp();
        using CancellationTokenSource bounded =
            CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        bounded.CancelAfter(duration);
        try
        {
            await source.MonitorAsync(
                snapshot => Record(snapshot, statusChanged: false),
                snapshot => Record(snapshot, statusChanged: true),
                bounded.Token);
        }
        catch (OperationCanceledException) when (bounded.IsCancellationRequested)
        {
            // Expected at the requested recording duration or caller cancel.
        }

        IReadOnlyList<TelemetryReplayEvent> captured;
        lock (synchronization)
        {
            captured = events.ToArray();
        }

        if (captured.Count == 0)
        {
            throw new InvalidOperationException(
                "No telemetry events were captured.");
        }

        TelemetryReplayFile.Save(fullPath, captured);
    }

    private void Record(TelemetrySnapshot snapshot, bool statusChanged)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        lock (synchronization)
        {
            long now = clock.GetTimestamp();
            if (!statusChanged &&
                hasUpdate &&
                clock.GetElapsedTime(lastUpdate, now) < UpdateInterval)
            {
                return;
            }

            if (events.Count >= MaximumEvents)
            {
                throw new InvalidOperationException(
                    $"Telemetry recording is limited to {MaximumEvents} events.");
            }

            int milliseconds = checked(
                (int)Math.Min(
                    clock.GetElapsedTime(started, now).TotalMilliseconds,
                    86_400_000));
            events.Add(new TelemetryReplayEvent(
                milliseconds,
                statusChanged,
                snapshot.ConnectionState,
                snapshot.IsOnTrack,
                snapshot.Gear,
                snapshot.Rpm,
                snapshot.SpeedMetersPerSecond,
                snapshot.BrakeBiasPercent,
                snapshot.LastLapTimeSeconds));
            if (!statusChanged)
            {
                lastUpdate = now;
                hasUpdate = true;
            }
        }
    }
}

internal static partial class TelemetryReplayFileExtensions
{
    internal static object ToSerializable(TelemetryReplayEvent replayEvent) =>
        new
        {
            replayEvent.AtMilliseconds,
            replayEvent.StatusChanged,
            replayEvent.ConnectionState,
            replayEvent.IsOnTrack,
            replayEvent.Gear,
            replayEvent.Rpm,
            replayEvent.SpeedMetersPerSecond,
            replayEvent.BrakeBiasPercent,
            replayEvent.LastLapTimeSeconds
        };
}
