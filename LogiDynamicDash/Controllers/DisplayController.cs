using LogiDynamicDash.Models;

namespace LogiDynamicDash.Controllers;

internal sealed class DisplayController(TimeProvider? timeProvider = null)
{
    private static readonly TimeSpan BrakeBiasDuration =
        TimeSpan.FromSeconds(2);

    private static readonly TimeSpan LastLapDuration =
        TimeSpan.FromSeconds(3);

    private float? _previousBrakeBiasPercent;
    private float? _previousLastLapTimeSeconds;

    private bool _lastLapInitialized;

    private readonly TimeProvider _timeProvider =
        timeProvider ?? TimeProvider.System;

    private DateTimeOffset _brakeBiasExpiresAt =
        DateTimeOffset.MinValue;

    private DateTimeOffset _lastLapExpiresAt =
        DateTimeOffset.MinValue;

    public DisplayMode SelectMode(
        TelemetrySnapshot snapshot)
    {
        DateTimeOffset now = _timeProvider.GetUtcNow();

        if (!IsConnected(snapshot))
        {
            return DisplayMode.ConnectionProblem;
        }

        DetectCompletedLap(snapshot, now);
        DetectBrakeBiasChange(snapshot, now);

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
        DateTimeOffset now)
    {
        if (snapshot.BrakeBiasPercent is not float currentBrakeBias ||
            !float.IsFinite(currentBrakeBias) ||
            currentBrakeBias is < 0 or > 100)
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
        DateTimeOffset now)
    {
        if (!_lastLapInitialized)
        {
            _lastLapInitialized = true;

            _previousLastLapTimeSeconds =
                snapshot.LastLapTimeSeconds;

            return;
        }

        if (snapshot.LastLapTimeSeconds is not float currentLastLap ||
            !float.IsFinite(currentLastLap) ||
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
