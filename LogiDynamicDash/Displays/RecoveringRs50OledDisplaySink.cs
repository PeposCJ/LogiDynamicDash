using LogiDynamicDash.Hidpp;
using LogiDynamicDash.Models;
using LogiDynamicDash.Runtime;

namespace LogiDynamicDash.Displays;

internal sealed class RecoveringRs50OledDisplaySink(
    Func<IRs50OledSession> sessionFactory,
    IRs50TelemetryFrameFormatter formatter,
    Action<DashboardOledState> stateChanged,
    TimeProvider? timeProvider = null) : IApplicationDisplay
{
    private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(2);
    private readonly object synchronization = new();
    private readonly TimeProvider clock = timeProvider ?? TimeProvider.System;
    private IRs50OledSession? session;
    private Rs50OledFrameScheduler? scheduler;
    private Rs50OledFrame? pendingFrame;
    private bool pendingUrgent;
    private long retryAfter;
    private bool initialized;
    private bool stopped;
    private DashboardOledState state = DashboardOledState.Waiting;

    public void Initialize()
    {
        lock (synchronization)
        {
            if (initialized || stopped)
            {
                throw new InvalidOperationException(
                    "The production OLED display cannot be initialized again.");
            }

            initialized = true;
            SetState(DashboardOledState.Waiting);
            TryConnect(force: true);
        }
    }

    public void Render(TelemetrySnapshot snapshot, DisplayMode mode)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        lock (synchronization)
        {
            RequireRunning();
            pendingFrame = formatter.Format(snapshot, mode);
            pendingUrgent = mode == DisplayMode.ConnectionProblem;
            TrySend();
        }
    }

    public void Flush()
    {
        lock (synchronization)
        {
            RequireRunning();
            if (scheduler is null)
            {
                TryConnect(force: false);
                TrySend();
                return;
            }

            try
            {
                scheduler.Flush();
            }
            catch (Exception exception) when (IsDeviceFailure(exception))
            {
                LoseConnection();
            }
        }
    }

    public void Stop()
    {
        lock (synchronization)
        {
            if (stopped)
            {
                return;
            }

            stopped = true;
            DisposeSession();
            pendingFrame = null;
            SetState(DashboardOledState.Stopped);
        }
    }

    private void TrySend()
    {
        if (scheduler is null)
        {
            TryConnect(force: false);
        }

        if (scheduler is null || pendingFrame is null)
        {
            return;
        }

        try
        {
            scheduler.Submit(pendingFrame, pendingUrgent);
        }
        catch (Exception exception) when (IsDeviceFailure(exception))
        {
            LoseConnection();
        }
    }

    private void TryConnect(bool force)
    {
        if (scheduler is not null)
        {
            return;
        }

        long now = clock.GetTimestamp();
        if (!force && retryAfter != 0 && now < retryAfter)
        {
            return;
        }

        IRs50OledSession? created = null;
        try
        {
            created = sessionFactory();
            created.Open();
            session = created;
            scheduler = new Rs50OledFrameScheduler(created);
            retryAfter = 0;
            SetState(DashboardOledState.Connected);
        }
        catch (Exception exception) when (IsDeviceFailure(exception))
        {
            created?.Dispose();
            retryAfter = now + ToTimestampTicks(RetryDelay);
            SetState(
                state == DashboardOledState.Waiting
                    ? DashboardOledState.Waiting
                    : DashboardOledState.Reconnecting);
        }
    }

    private void LoseConnection()
    {
        DisposeSession();
        retryAfter =
            clock.GetTimestamp() + ToTimestampTicks(RetryDelay);
        SetState(DashboardOledState.Reconnecting);
    }

    private long ToTimestampTicks(TimeSpan duration) =>
        (long)(duration.TotalSeconds * clock.TimestampFrequency);

    private void DisposeSession()
    {
        scheduler = null;
        session?.Dispose();
        session = null;
    }

    private void SetState(DashboardOledState next)
    {
        if (state == next && next != DashboardOledState.Waiting)
        {
            return;
        }

        state = next;
        stateChanged(next);
    }

    private void RequireRunning()
    {
        if (!initialized || stopped)
        {
            throw new InvalidOperationException(
                "The production OLED display is not running.");
        }
    }

    private static bool IsDeviceFailure(Exception exception) =>
        exception is IOException or
            TimeoutException or
            UnauthorizedAccessException or
            InvalidOperationException or
            Rs50OledProtocolException;
}
