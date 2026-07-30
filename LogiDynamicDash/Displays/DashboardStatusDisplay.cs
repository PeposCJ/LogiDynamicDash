using LogiDynamicDash.Models;
using LogiDynamicDash.Runtime;

namespace LogiDynamicDash.Displays;

internal sealed class DashboardStatusDisplay(
    Action<TelemetrySnapshot> statusChanged) : IApplicationDisplay
{
    private TelemetrySnapshot? previous;

    public void Initialize()
    {
    }

    public void Render(TelemetrySnapshot snapshot, DisplayMode mode)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        if (SameStatus(previous, snapshot))
        {
            return;
        }

        previous = snapshot.Copy();
        statusChanged(snapshot.Copy());
    }

    public void Flush()
    {
    }

    public void Stop()
    {
    }

    private static bool SameStatus(
        TelemetrySnapshot? left,
        TelemetrySnapshot right) =>
        left is not null &&
        string.Equals(
            left.ConnectionState,
            right.ConnectionState,
            StringComparison.OrdinalIgnoreCase) &&
        Equals(left.SessionIdentity, right.SessionIdentity);
}
