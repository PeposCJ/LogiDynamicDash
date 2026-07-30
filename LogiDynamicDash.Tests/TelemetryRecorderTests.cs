using LogiDynamicDash.Models;
using LogiDynamicDash.Offline;
using LogiDynamicDash.Services;

namespace LogiDynamicDash.Tests;

public sealed class TelemetryRecorderTests
{
    [Fact]
    public async Task RecordAsync_WritesStrictReplayAndSamplesAtFiveHertz()
    {
        ManualTimeProvider clock = new();
        ScriptedSource source = new(clock);
        Rs50TelemetryRecorder recorder = new(source, clock);
        string directory = Path.Combine(
            Path.GetTempPath(),
            $"logidynamicdash-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        string path = Path.Combine(directory, "recording.json");

        try
        {
            await recorder.RecordAsync(
                path,
                TimeSpan.FromSeconds(1),
                CancellationToken.None);

            IReadOnlyList<TelemetryReplayEvent> events =
                TelemetryReplayFile.Load(path);
            Assert.Equal(3, events.Count);
            Assert.True(events[0].StatusChanged);
            Assert.Equal(0, events[1].AtMilliseconds);
            Assert.Equal(200, events[2].AtMilliseconds);
        }
        finally
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }

            if (Directory.Exists(directory))
            {
                Directory.Delete(directory);
            }
        }
    }

    private sealed class ScriptedSource(ManualTimeProvider clock)
        : ITelemetrySource
    {
        public Task MonitorAsync(
            Action<TelemetrySnapshot> onTelemetryUpdated,
            Action<TelemetrySnapshot> onStatusChanged,
            CancellationToken cancellationToken)
        {
            TelemetrySnapshot snapshot = new()
            {
                ConnectionState = "CONNECTED",
                SpeedMetersPerSecond = 0,
                Gear = 0
            };
            onStatusChanged(snapshot.Copy());
            onTelemetryUpdated(snapshot.Copy());
            clock.Advance(TimeSpan.FromMilliseconds(100));
            snapshot.Gear = 1;
            onTelemetryUpdated(snapshot.Copy());
            clock.Advance(TimeSpan.FromMilliseconds(100));
            snapshot.Gear = 2;
            onTelemetryUpdated(snapshot.Copy());
            return Task.CompletedTask;
        }
    }

    private sealed class ManualTimeProvider : TimeProvider
    {
        private long timestamp;

        public override long TimestampFrequency => TimeSpan.TicksPerSecond;
        public override long GetTimestamp() => timestamp;

        public void Advance(TimeSpan duration) => timestamp += duration.Ticks;
    }
}
