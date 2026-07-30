using LogiDynamicDash.Displays;
using LogiDynamicDash.Models;

namespace LogiDynamicDash.Tests;

public sealed class ConsoleDashboardTests
{
    [Fact]
    public void RedirectedMode_IsSafeAndSilent()
    {
        IApplicationDisplay dashboard =
            new ConsoleDashboard(interactiveOverride: false);

        dashboard.Initialize();
        dashboard.Render(
            new TelemetrySnapshot
            {
                ConnectionState = "CONNECTED",
                Gear = 0,
                SpeedMetersPerSecond = 0
            },
            DisplayMode.Normal);
        dashboard.Flush();
        dashboard.Stop();
        dashboard.Stop();
    }
}
