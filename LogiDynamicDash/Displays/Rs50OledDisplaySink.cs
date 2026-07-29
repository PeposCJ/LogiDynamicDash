using LogiDynamicDash.Hidpp;
using LogiDynamicDash.Models;

namespace LogiDynamicDash.Displays;

internal enum OledDeviceState
{
    Disabled,
    Opening,
    Active,
    Faulted,
    Stopped
}

/// <summary>
/// Bounded stationary-validation sink. Moving telemetry fails closed before
/// a frame is formatted or transmitted.
/// </summary>
internal sealed class Rs50OledDisplaySink(
    Func<IRs50OledSession> sessionFactory,
    Rs50TelemetryFrameFormatter formatter) : IApplicationDisplay
{
    private const float MaximumStationarySpeedMetersPerSecond = 0.5f;

    private readonly object synchronization = new();
    private IRs50OledSession? session;
    private Rs50OledFrameScheduler? scheduler;

    internal OledDeviceState State { get; private set; } =
        OledDeviceState.Disabled;

    public void Initialize()
    {
        lock (synchronization)
        {
            if (State != OledDeviceState.Disabled)
            {
                throw new InvalidOperationException(
                    "The RS50 OLED display sink cannot be initialized again.");
            }

            State = OledDeviceState.Opening;
            IRs50OledSession? created = null;
            try
            {
                created = sessionFactory();
                created.Open();
                session = created;
                scheduler = new Rs50OledFrameScheduler(created);
                State = OledDeviceState.Active;
            }
            catch
            {
                State = OledDeviceState.Faulted;
                created?.Dispose();
                throw;
            }
        }
    }

    public void Render(TelemetrySnapshot snapshot, DisplayMode mode)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        lock (synchronization)
        {
            if (State != OledDeviceState.Active || scheduler is null)
            {
                throw new InvalidOperationException(
                    "The RS50 OLED display sink is not initialized.");
            }

            try
            {
                RequireStationary(snapshot);
                Rs50OledFrame frame = formatter.Format(snapshot, mode);
                scheduler.Submit(
                    frame,
                    mode == DisplayMode.ConnectionProblem);
            }
            catch
            {
                State = OledDeviceState.Faulted;
                throw;
            }
        }
    }

    public void Stop()
    {
        lock (synchronization)
        {
            if (State == OledDeviceState.Stopped)
            {
                return;
            }

            State = OledDeviceState.Stopped;
            session?.Dispose();
            session = null;
            scheduler = null;
        }
    }

    private static void RequireStationary(TelemetrySnapshot snapshot)
    {
        if (snapshot.IsOnTrack != true)
        {
            return;
        }

        if (snapshot.SpeedMetersPerSecond is not float speed ||
            !float.IsFinite(speed) ||
            speed < 0 ||
            speed > MaximumStationarySpeedMetersPerSecond)
        {
            throw new InvalidOperationException(
                "RS50 OLED output stopped because moving-car telemetry was " +
                "detected. This build is limited to stationary validation.");
        }
    }
}
