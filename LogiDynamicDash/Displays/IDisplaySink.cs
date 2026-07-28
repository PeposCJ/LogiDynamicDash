using LogiDynamicDash.Models;

namespace LogiDynamicDash.Displays;

/// <summary>
/// Consumes already validated Layout J presentation frames.
/// Implementations own rendering only; they do not interpret telemetry.
/// </summary>
internal interface IDisplaySink
{
    void Initialize();

    void Render(LayoutJFrame frame);

    void Stop();
}
