using LogiDynamicDash.Models;

namespace LogiDynamicDash.Displays;

internal interface IApplicationDisplay
{
    void Initialize();

    void Render(TelemetrySnapshot snapshot, DisplayMode mode);

    void Flush()
    {
    }

    void Stop();
}
