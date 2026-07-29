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
}
