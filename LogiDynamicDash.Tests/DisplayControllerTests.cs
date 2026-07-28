using LogiDynamicDash.Controllers;
using LogiDynamicDash.Models;

namespace LogiDynamicDash.Tests;

public sealed class DisplayControllerTests
{
    [Fact]
    public void SelectMode_UsesConnectionStateAsTopLevelGate()
    {
        DisplayController controller = new(new ManualTimeProvider());

        DisplayMode mode = controller.SelectMode(new TelemetrySnapshot
        {
            ConnectionState = "WAITING"
        });

        Assert.Equal(DisplayMode.ConnectionProblem, mode);
    }

    [Fact]
    public void DisconnectedTelemetry_DoesNotCreateTransientModesOnReconnect()
    {
        DisplayController controller = new(new ManualTimeProvider());
        TelemetrySnapshot snapshot = new()
        {
            ConnectionState = "WAITING",
            BrakeBiasPercent = 50.0f,
            LastLapTimeSeconds = 80.0f
        };

        controller.SelectMode(snapshot);
        snapshot = snapshot with
        {
            BrakeBiasPercent = 51.0f,
            LastLapTimeSeconds = 79.0f
        };
        controller.SelectMode(snapshot);

        snapshot = snapshot with
        {
            ConnectionState = "CONNECTED"
        };

        Assert.Equal(DisplayMode.Normal, controller.SelectMode(snapshot));
    }

    [Fact]
    public void BrakeBiasChange_ExpiresAfterTwoSeconds()
    {
        ManualTimeProvider time = new();
        DisplayController controller = new(time);
        TelemetrySnapshot snapshot = ConnectedSnapshot();
        snapshot = snapshot with { BrakeBiasPercent = 50.0f };

        Assert.Equal(DisplayMode.Normal, controller.SelectMode(snapshot));

        snapshot = snapshot with { BrakeBiasPercent = 50.1f };
        Assert.Equal(DisplayMode.BrakeBias, controller.SelectMode(snapshot));

        time.Advance(TimeSpan.FromMilliseconds(1999));
        Assert.Equal(DisplayMode.BrakeBias, controller.SelectMode(snapshot));

        time.Advance(TimeSpan.FromMilliseconds(1));
        Assert.Equal(DisplayMode.Normal, controller.SelectMode(snapshot));
    }

    [Fact]
    public void LastLapChange_ExpiresAfterThreeSeconds()
    {
        ManualTimeProvider time = new();
        DisplayController controller = new(time);
        TelemetrySnapshot snapshot = ConnectedSnapshot();
        snapshot = snapshot with { LastLapTimeSeconds = 80.0f };

        Assert.Equal(DisplayMode.Normal, controller.SelectMode(snapshot));

        snapshot = snapshot with { LastLapTimeSeconds = 79.5f };
        Assert.Equal(DisplayMode.LastLap, controller.SelectMode(snapshot));

        time.Advance(TimeSpan.FromSeconds(3));
        Assert.Equal(DisplayMode.Normal, controller.SelectMode(snapshot));
    }

    [Fact]
    public void BrakeBiasTemporarilyTakesPriorityOverLastLap()
    {
        ManualTimeProvider time = new();
        DisplayController controller = new(time);
        TelemetrySnapshot snapshot = ConnectedSnapshot();
        snapshot = snapshot with
        {
            BrakeBiasPercent = 50.0f,
            LastLapTimeSeconds = 80.0f
        };
        controller.SelectMode(snapshot);

        snapshot = snapshot with
        {
            BrakeBiasPercent = 50.1f,
            LastLapTimeSeconds = 79.5f
        };
        Assert.Equal(DisplayMode.BrakeBias, controller.SelectMode(snapshot));

        time.Advance(TimeSpan.FromSeconds(2));
        Assert.Equal(DisplayMode.LastLap, controller.SelectMode(snapshot));

        time.Advance(TimeSpan.FromSeconds(1));
        Assert.Equal(DisplayMode.Normal, controller.SelectMode(snapshot));
    }

    [Fact]
    public void InvalidTelemetry_DoesNotActivateTransientModes()
    {
        DisplayController controller = new(new ManualTimeProvider());
        TelemetrySnapshot snapshot = ConnectedSnapshot();
        snapshot = snapshot with
        {
            BrakeBiasPercent = 50.0f,
            LastLapTimeSeconds = 80.0f
        };
        controller.SelectMode(snapshot);

        snapshot = snapshot with
        {
            BrakeBiasPercent = float.NaN,
            LastLapTimeSeconds = float.PositiveInfinity
        };

        Assert.Equal(DisplayMode.Normal, controller.SelectMode(snapshot));
    }

    private static TelemetrySnapshot ConnectedSnapshot() => new()
    {
        ConnectionState = "CONNECTED"
    };

    private sealed class ManualTimeProvider : TimeProvider
    {
        private DateTimeOffset utcNow =
            new(2026, 7, 22, 0, 0, 0, TimeSpan.Zero);

        public override DateTimeOffset GetUtcNow() => utcNow;

        public void Advance(TimeSpan duration) => utcNow += duration;
    }
}
