using LogiDynamicDash.Controllers;
using LogiDynamicDash.Displays;
using LogiDynamicDash.Models;
using LogiDynamicDash.Services;

namespace LogiDynamicDash;

internal enum ApplicationLifecycleState
{
    Disabled,
    Opening,
    Active,
    Faulted,
    Stopped
}

internal sealed class LogiDynamicDashApplication(
    ITelemetrySource telemetrySource,
    IApplicationDisplay display,
    DisplayController controller,
    TimeProvider? timeProvider = null)
{
    private static readonly TimeSpan RefreshInterval =
        TimeSpan.FromMilliseconds(200);

    private readonly TimeProvider clock = timeProvider ?? TimeProvider.System;
    private readonly TelemetrySnapshot snapshot = new();
    private readonly object renderSynchronization = new();
    private long lastRefreshTimestamp;
    private bool hasRefreshed;

    internal ApplicationLifecycleState State { get; private set; } =
        ApplicationLifecycleState.Disabled;

    internal async Task RunAsync(CancellationToken cancellationToken)
    {
        if (State != ApplicationLifecycleState.Disabled)
        {
            throw new InvalidOperationException(
                "The application lifecycle cannot be restarted.");
        }

        State = ApplicationLifecycleState.Opening;
        bool initialized = false;
        try
        {
            display.Initialize();
            initialized = true;
            State = ApplicationLifecycleState.Active;
            Render(snapshot);

            await telemetrySource.MonitorAsync(
                snapshot,
                HandleTelemetryUpdated,
                HandleStatusChanged,
                cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Normal bounded-run or Ctrl+C completion.
        }
        catch
        {
            State = ApplicationLifecycleState.Faulted;
            throw;
        }
        finally
        {
            if (initialized)
            {
                try
                {
                    display.Stop();
                }
                catch
                {
                    State = ApplicationLifecycleState.Faulted;
                    throw;
                }
            }

            if (State != ApplicationLifecycleState.Faulted)
            {
                State = ApplicationLifecycleState.Stopped;
            }
        }
    }

    private void HandleTelemetryUpdated(TelemetrySnapshot current)
    {
        lock (renderSynchronization)
        {
            long now = clock.GetTimestamp();
            if (hasRefreshed &&
                clock.GetElapsedTime(
                    lastRefreshTimestamp,
                    now) < RefreshInterval)
            {
                return;
            }

            RenderCore(current);
        }
    }

    private void HandleStatusChanged(TelemetrySnapshot current)
    {
        lock (renderSynchronization)
        {
            RenderCore(current);
        }
    }

    private void Render(TelemetrySnapshot current)
    {
        lock (renderSynchronization)
        {
            RenderCore(current);
        }
    }

    private void RenderCore(TelemetrySnapshot current)
    {
        display.Render(current, controller.SelectMode(current));
        lastRefreshTimestamp = clock.GetTimestamp();
        hasRefreshed = true;
    }
}
