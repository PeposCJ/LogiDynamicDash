using LogiDynamicDash.Models;

namespace LogiDynamicDash.Tests;

public sealed class TelemetrySnapshotTests
{
    [Fact]
    public void WithUpdate_LeavesPublishedSnapshotUnchanged()
    {
        TelemetrySnapshot published = new()
        {
            ConnectionState = "CONNECTED",
            Gear = 3,
            SpeedMetersPerSecond = 40.0f
        };

        TelemetrySnapshot updated = published with
        {
            Gear = 4,
            SpeedMetersPerSecond = 45.0f
        };

        Assert.Equal(3, published.Gear);
        Assert.Equal(40.0f, published.SpeedMetersPerSecond);
        Assert.Equal(4, updated.Gear);
        Assert.Equal(45.0f, updated.SpeedMetersPerSecond);
    }
}
