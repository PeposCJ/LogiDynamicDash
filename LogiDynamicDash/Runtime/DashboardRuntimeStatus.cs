using LogiDynamicDash.Models;

namespace LogiDynamicDash.Runtime;

internal enum DashboardOledState
{
    Waiting,
    Connected,
    Reconnecting,
    Stopped
}

internal sealed record DashboardRuntimeStatus(
    DashboardOledState Oled,
    string Telemetry,
    string Car,
    int? CarId,
    IRacingDiscipline Discipline,
    string Message)
{
    internal static DashboardRuntimeStatus Initial { get; } =
        new(
            DashboardOledState.Waiting,
            "WAITING",
            "Unknown car",
            null,
            IRacingDiscipline.Unknown,
            "Waiting for RS50 and iRacing.");
}
