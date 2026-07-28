using LogiDynamicDash.Models;

namespace LogiDynamicDash.Displays;

/// <summary>
/// Prevents identical presentation frames from reaching a display transport.
/// The remembered frame advances only after the wrapped sink accepts it.
/// </summary>
internal sealed class DistinctDisplaySink(IDisplaySink inner) : IDisplaySink
{
    private LayoutJFrame? lastFrame;

    public void Initialize()
    {
        inner.Initialize();
        lastFrame = null;
    }

    public void Render(LayoutJFrame frame)
    {
        ArgumentNullException.ThrowIfNull(frame);

        if (frame == lastFrame)
        {
            return;
        }

        inner.Render(frame);
        lastFrame = frame;
    }

    public void Stop()
    {
        try
        {
            inner.Stop();
        }
        finally
        {
            lastFrame = null;
        }
    }
}
