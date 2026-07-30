using LogiDynamicDash.Models;

namespace LogiDynamicDash.Controllers;

internal sealed class DisplayController(
    TimeProvider? timeProvider = null,
    TimeSpan? lastLapDuration = null)
{
    private readonly TimeProvider clock = timeProvider ?? TimeProvider.System;

    private static readonly TimeSpan BrakeBiasDuration =
        TimeSpan.FromSeconds(2);

    private readonly TimeSpan lastLapDisplayDuration =
        ValidateLastLapDuration(lastLapDuration ?? TimeSpan.FromSeconds(5));

    private float? _previousBrakeBiasPercent;
    private float? _previousLastLapTimeSeconds;

    private bool _lastLapInitialized;

    private DateTimeOffset _brakeBiasExpiresAt =
        DateTimeOffset.MinValue;

    private DateTimeOffset _lastLapExpiresAt =
        DateTimeOffset.MinValue;

    public DisplayMode SelectMode(
        TelemetrySnapshot snapshot)
    {
        DateTimeOffset now = clock.GetUtcNow();

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
        DateTimeOffset now)
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
            currentLastLap <= 0)
        {
            return;
        }

        if (_previousLastLapTimeSeconds is not float previousLastLap)
        {
            _previousLastLapTimeSeconds = currentLastLap;
            return;
        }

        float difference =
            MathF.Abs(
                currentLastLap -
                previousLastLap);

        if (difference < 0.001f)
        {
            return;
        }

        _previousLastLapTimeSeconds =
            currentLastLap;

        _lastLapExpiresAt =
            now.Add(lastLapDisplayDuration);
    }

    private static bool IsConnected(
        TelemetrySnapshot snapshot)
    {
        return string.Equals(
            snapshot.ConnectionState,
            "CONNECTED",
            StringComparison.OrdinalIgnoreCase);
    }

    private static TimeSpan ValidateLastLapDuration(TimeSpan duration)
    {
        if (duration < TimeSpan.FromSeconds(1) ||
            duration > TimeSpan.FromSeconds(15))
        {
            throw new ArgumentOutOfRangeException(
                nameof(lastLapDuration));
        }

        return duration;
    }
}
