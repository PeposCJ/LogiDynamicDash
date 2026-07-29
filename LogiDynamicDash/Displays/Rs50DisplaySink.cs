using LogiDynamicDash.Models;
using LogiDynamicDash.Native;

namespace LogiDynamicDash.Displays;

internal sealed class Rs50DisplaySink(
    IRs50DisplayBridge bridge,
    IRs50OwnerWindow ownerWindow,
    Action<TimeSpan>? delay = null) : IDisplaySink
{
    private static readonly TimeSpan RetryDelay =
        TimeSpan.FromMilliseconds(200);

    private readonly Action<TimeSpan> delayAction =
        delay ?? Thread.Sleep;

    private bool initialized;

    public void Initialize()
    {
        if (initialized)
        {
            throw new InvalidOperationException(
                "The RS50 display sink is already initialized.");
        }

        try
        {
            ownerWindow.Create();
            bridge.Open(ownerWindow.Handle);
            bridge.BeginLayoutJStream();
            initialized = true;
        }
        catch
        {
            bridge.Dispose();
            ownerWindow.Dispose();
            throw;
        }
    }

    public void Render(LayoutJFrame frame)
    {
        ArgumentNullException.ThrowIfNull(frame);
        if (!initialized)
        {
            throw new InvalidOperationException(
                "The RS50 display sink is not initialized.");
        }

        Rs50FrameOutcome outcome = bridge.Send(frame);
        if (outcome.RateLimited)
        {
            delayAction(RetryDelay);
            outcome = bridge.Send(frame);
        }

        if (outcome.RateLimited ||
            (!outcome.Transmitted && !outcome.Unchanged))
        {
            throw new InvalidOperationException(
                "The RS50 bridge did not accept the Layout J frame.");
        }
    }

    public void Stop()
    {
        try
        {
            if (initialized)
            {
                bridge.EndLayoutJStream();
            }
        }
        finally
        {
            initialized = false;
            bridge.Dispose();
            ownerWindow.Dispose();
        }
    }
}
