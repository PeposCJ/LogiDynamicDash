using LogiDynamicDash.Displays;
using LogiDynamicDash.Models;

namespace LogiDynamicDash.Tests;

public sealed class VirtualEnduranceTests
{
    [Fact]
    public void Formatter_ProcessesSixVirtualHoursAtTwentyHertz()
    {
        const int updates = 6 * 60 * 60 * 20;
        Rs50TelemetryFrameFormatter formatter = new(
            new Rs50OledConfiguration(Rs50OledLayout.E));
        TelemetrySnapshot snapshot = new()
        {
            ConnectionState = "CONNECTED",
            IsOnTrack = true,
            BrakeBiasPercent = 52.3f,
            LastLapTimeSeconds = 91.2f
        };
        Rs50OledFrame? final = null;

        for (int index = 0; index < updates; index++)
        {
            snapshot.Gear = index % 8;
            snapshot.Rpm = 900 + index % 7500;
            snapshot.SpeedMetersPerSecond = index % 90;
            DisplayMode mode = index % 50_000 == 0
                ? DisplayMode.ConnectionProblem
                : DisplayMode.Normal;
            final = formatter.Format(snapshot, mode);
        }

        Assert.NotNull(final);
        Assert.IsType<Rs50LayoutEFrame>(final);
    }
}
