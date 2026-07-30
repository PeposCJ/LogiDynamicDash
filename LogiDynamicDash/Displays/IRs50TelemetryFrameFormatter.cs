using LogiDynamicDash.Models;

namespace LogiDynamicDash.Displays;

internal interface IRs50TelemetryFrameFormatter
{
    Rs50OledFrame Format(TelemetrySnapshot snapshot, DisplayMode mode);
}
