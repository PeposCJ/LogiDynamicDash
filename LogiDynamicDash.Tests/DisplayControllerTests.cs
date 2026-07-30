using LogiDynamicDash.Controllers;
using LogiDynamicDash.Models;

namespace LogiDynamicDash.Tests;

public sealed class DisplayControllerTests
{
    [Fact]
    public void SelectMode_DoesNotTreatFirstAvailableLapAsNewCompletion()
    {
        DisplayController controller = new();
        TelemetrySnapshot snapshot = new()
        {
            ConnectionState = "CONNECTED"
        };

        Assert.Equal(DisplayMode.Normal, controller.SelectMode(snapshot));
        snapshot.LastLapTimeSeconds = 91.2f;

        Assert.Equal(DisplayMode.Normal, controller.SelectMode(snapshot));
    }

    [Fact]
    public void LastLapDuration_IsConfigurable()
    {
        ManualTimeProvider clock = new();
        DisplayController controller = new(
            clock,
            TimeSpan.FromSeconds(8));
        TelemetrySnapshot snapshot = new()
        {
            ConnectionState = "CONNECTED",
            LastLapTimeSeconds = 90
        };
        Assert.Equal(DisplayMode.Normal, controller.SelectMode(snapshot));
        snapshot.LastLapTimeSeconds = 91;

        Assert.Equal(DisplayMode.LastLap, controller.SelectMode(snapshot));
        clock.Advance(TimeSpan.FromSeconds(7));
        Assert.Equal(DisplayMode.LastLap, controller.SelectMode(snapshot));
        clock.Advance(TimeSpan.FromSeconds(1));
        Assert.Equal(DisplayMode.Normal, controller.SelectMode(snapshot));
    }

    private sealed class ManualTimeProvider : TimeProvider
    {
        private DateTimeOffset now = DateTimeOffset.UnixEpoch;

        public override DateTimeOffset GetUtcNow() => now;

        internal void Advance(TimeSpan duration) =>
            now += duration;
    }
}
