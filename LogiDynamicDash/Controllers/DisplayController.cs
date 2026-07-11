using LogiDynamicDash.Models;

namespace LogiDynamicDash.Controllers;

internal sealed class DisplayController
{
    private static readonly TimeSpan BrakeBiasDuration =
        TimeSpan.FromSeconds(2);

    private static readonly TimeSpan LastLapDuration =
        TimeSpan.FromSeconds(3);

    private float? _previousBrakeBiasPercent;
    private float? _previousLastLapTimeSeconds;

    private bool _lastLapInitialized;

    private DateTime _brakeBiasExpiresAt =
        DateTime.MinValue;

    private DateTime _lastLapExpiresAt =
        DateTime.MinValue;

    public DisplayMode SelectMode(
        TelemetrySnapshot snapshot)
    {
        DateTime now = DateTime.UtcNow;

        DetectCompletedLap(snapshot, now);
        DetectBrakeBiasChange(snapshot, now);

        if (!IsConnected(snapshot))
        {
            return DisplayMode.ConnectionProblem;
        }

        if (now < _brakeBiasExpiresAt)
        {
            return DisplayMode.BrakeBias;
        }

        if (now < _lastLapExpiresAt)
        {
            return DisplayMode.LastLap;
        }

        return DisplayMode.Normal;
    }

    private void DetectBrakeBiasChange(
        TelemetrySnapshot snapshot,
        DateTime now)
    {
        if (snapshot.BrakeBiasPercent is not float currentBrakeBias)
        {
            return;
        }

        if (_previousBrakeBiasPercent is null)
        {
            _previousBrakeBiasPercent = currentBrakeBias;
            return;
        }

        float difference =
            MathF.Abs(
                currentBrakeBias -
                _previousBrakeBiasPercent.Value);

        _previousBrakeBiasPercent =
            currentBrakeBias;

        if (difference < 0.01f)
        {
            return;
        }

        _brakeBiasExpiresAt =
            now.Add(BrakeBiasDuration);
    }

    private void DetectCompletedLap(
        TelemetrySnapshot snapshot,
        DateTime now)
    {
        if (!_lastLapInitialized)
        {
            _lastLapInitialized = true;

            _previousLastLapTimeSeconds =
                snapshot.LastLapTimeSeconds;

            return;
        }

        if (snapshot.LastLapTimeSeconds is not float currentLastLap ||
            currentLastLap <= 0)
        {
            return;
        }

        if (_previousLastLapTimeSeconds is float previousLastLap)
        {
            float difference =
                MathF.Abs(
                    currentLastLap -
                    previousLastLap);

            if (difference < 0.001f)
            {
                return;
            }
        }

        _previousLastLapTimeSeconds =
            currentLastLap;

        _lastLapExpiresAt =
            now.Add(LastLapDuration);
    }

    private static bool IsConnected(
        TelemetrySnapshot snapshot)
    {
        return string.Equals(
            snapshot.ConnectionState,
            "CONNECTED",
            StringComparison.OrdinalIgnoreCase);
    }
}