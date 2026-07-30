using LogiDynamicDash.Models;

namespace LogiDynamicDash.Diagnostics;

internal interface IApplicationRuntimeDiagnostics : IDisposable
{
    void RecordRender(
        string trigger,
        TelemetrySnapshot snapshot,
        DisplayMode mode);

    void RecordStop(ApplicationLifecycleState state);
}
