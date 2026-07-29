using LogiDynamicDash.Models;

namespace LogiDynamicDash.Services;

internal interface ITelemetrySource
{
    Task MonitorAsync(
        TelemetrySnapshot snapshot,
        Action<TelemetrySnapshot> onTelemetryUpdated,
        Action<TelemetrySnapshot> onStatusChanged,
        CancellationToken cancellationToken);
}
