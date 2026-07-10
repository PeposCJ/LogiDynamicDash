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

    private DisplayMode _temporaryMode =
        DisplayMode.Normal;

    private DateTime _temporaryModeExpiresAt =
        DateTime.MinValue;

    public DisplayMode SelectMode(
        TelemetrySnapshot snapshot)
    {
        DetectCompletedLap(snapshot);
        DetectBrakeBiasChange(snapshot);

        if (!IsConnected(snapshot))
        {
            ClearTemporaryMode();
            return DisplayMode.ConnectionProblem;
        }

        if (DateTime.UtcNow < _temporaryModeExpiresAt)
        {
            return _temporaryMode;
        }

        ClearTemporaryMode();

        return DisplayMode.Normal;
    }

    private void DetectBrakeBiasChange(
        TelemetrySnapshot snapshot)
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

        ShowTemporaryMode(
            DisplayMode.BrakeBias,
            BrakeBiasDuration);
    }

    private void DetectCompletedLap(
        TelemetrySnapshot snapshot)
    {
        if (snapshot.LastLapTimeSeconds is not float currentLastLap)
        {
            return;
        }

        if (currentLastLap <= 0)
        {
            return;
        }

        if (_previousLastLapTimeSeconds is null)
        {
            _previousLastLapTimeSeconds = currentLastLap;
            return;
        }

        float difference =
            MathF.Abs(
                currentLastLap -
                _previousLastLapTimeSeconds.Value);

        _previousLastLapTimeSeconds =
            currentLastLap;

        if (difference < 0.001f)
        {
            return;
        }

        ShowTemporaryMode(
            DisplayMode.LastLap,
            LastLapDuration);
    }

    private static bool IsConnected(
        TelemetrySnapshot snapshot)
    {
        return string.Equals(
            snapshot.ConnectionState,
            "CONNECTED",
            StringComparison.OrdinalIgnoreCase);
    }

    private void ShowTemporaryMode(
        DisplayMode mode,
        TimeSpan duration)
    {
        _temporaryMode = mode;

        _temporaryModeExpiresAt =
            DateTime.UtcNow.Add(duration);
    }

    private void ClearTemporaryMode()
    {
        _temporaryMode =
            DisplayMode.Normal;

        _temporaryModeExpiresAt =
            DateTime.MinValue;
    }
}