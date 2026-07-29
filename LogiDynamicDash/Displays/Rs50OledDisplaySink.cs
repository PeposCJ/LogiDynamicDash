using LogiDynamicDash.Hidpp;
using LogiDynamicDash.Models;

namespace LogiDynamicDash.Displays;

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
    private bool stopped;

    public void Initialize()
    {
        lock (synchronization)
        {
            if (session is not null || stopped)
            {
                throw new InvalidOperationException(
                    "The RS50 OLED display sink cannot be initialized again.");
            }

            IRs50OledSession created = sessionFactory();
            try
            {
                created.Open();
                session = created;
            }
            catch
            {
                created.Dispose();
                throw;
            }
        }
    }

    public void Render(TelemetrySnapshot snapshot, DisplayMode mode)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        lock (synchronization)
        {
            if (session is null || stopped)
            {
                throw new InvalidOperationException(
                    "The RS50 OLED display sink is not initialized.");
            }

            RequireStationary(snapshot);
            Rs50OledFrame frame = formatter.Format(snapshot, mode);
            session.Send(frame);
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
            session?.Dispose();
            session = null;
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
