using LogiDynamicDash.Models;

namespace LogiDynamicDash.Displays;

internal sealed class CompositeDisplaySink(
    params IDisplaySink[] sinks) : IDisplaySink
{
    private int initializedCount;

    public void Initialize()
    {
        try
        {
            foreach (IDisplaySink sink in sinks)
            {
                sink.Initialize();
                initializedCount++;
            }
        }
        catch
        {
            Stop();
            throw;
        }
    }

    public void Render(LayoutJFrame frame)
    {
        ArgumentNullException.ThrowIfNull(frame);

        foreach (IDisplaySink sink in sinks)
        {
            sink.Render(frame);
        }
    }

    public void Stop()
    {
        List<Exception>? failures = null;

        for (int index = initializedCount - 1; index >= 0; index--)
        {
            try
            {
                sinks[index].Stop();
            }
            catch (Exception exception)
            {
                failures ??= [];
                failures.Add(exception);
            }
        }

        initializedCount = 0;
        if (failures is not null)
        {
            throw new AggregateException(
                "One or more display sinks failed to stop.",
                failures);
        }
    }
}
