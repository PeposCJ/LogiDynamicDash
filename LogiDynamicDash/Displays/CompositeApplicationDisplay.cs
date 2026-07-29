using LogiDynamicDash.Models;

namespace LogiDynamicDash.Displays;

internal sealed class CompositeApplicationDisplay(
    params IApplicationDisplay[] displays) : IApplicationDisplay
{
    private int initializedCount;
    private bool stopped;

    public void Initialize()
    {
        if (initializedCount != 0 || stopped)
        {
            throw new InvalidOperationException(
                "The composite display cannot be initialized again.");
        }

        try
        {
            foreach (IApplicationDisplay display in displays)
            {
                display.Initialize();
                initializedCount++;
            }
        }
        catch
        {
            try
            {
                StopInitializedDisplays();
            }
            catch
            {
                // Preserve the initialization failure after best-effort
                // cleanup of displays that opened successfully.
            }

            stopped = true;
            throw;
        }
    }

    public void Render(TelemetrySnapshot snapshot, DisplayMode mode)
    {
        if (initializedCount != displays.Length || stopped)
        {
            throw new InvalidOperationException(
                "The composite display is not initialized.");
        }

        foreach (IApplicationDisplay display in displays)
        {
            display.Render(snapshot, mode);
        }
    }

    public void Stop()
    {
        if (stopped)
        {
            return;
        }

        stopped = true;
        StopInitializedDisplays();
    }

    private void StopInitializedDisplays()
    {
        Exception? firstException = null;
        for (int index = initializedCount - 1; index >= 0; index--)
        {
            try
            {
                displays[index].Stop();
            }
            catch (Exception exception)
            {
                firstException ??= exception;
            }
        }

        initializedCount = 0;
        if (firstException is not null)
        {
            throw firstException;
        }
    }
}
