using System.Globalization;
using LogiDynamicDash.Models;

namespace LogiDynamicDash.Displays;

/// <summary>
/// Maps iRacing telemetry onto the recovered RS50 Layout J field limits.
/// This formatter does not encode or transmit a HID or DirectInput command.
/// </summary>
internal static class LayoutJTelemetryFormatter
{
    public static LayoutJFrame Format(
        TelemetrySnapshot snapshot,
        DisplayMode mode)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        return mode switch
        {
            DisplayMode.BrakeBias => new(
                "BRAKE BIAS",
                FormatBrakeBias(snapshot.BrakeBiasPercent),
                string.Empty,
                string.Empty),
            DisplayMode.LastLap => new(
                "LAST LAP",
                FormatLapTime(snapshot.LastLapTimeSeconds),
                string.Empty,
                string.Empty),
            DisplayMode.ConnectionProblem => new(
                "IRACING",
                FormatConnectionState(snapshot.ConnectionState),
                string.Empty,
                string.Empty),
            _ => new(
                "SPEED",
                FormatSpeed(snapshot.SpeedMetersPerSecond),
                "GEAR",
                FormatGear(snapshot.Gear))
        };
    }

    private static string FormatSpeed(float? metersPerSecond)
    {
        if (metersPerSecond is not float value ||
            !float.IsFinite(value) ||
            value < 0)
        {
            return "N/A";
        }

        float kilometersPerHour = Math.Clamp(value * 3.6f, 0.0f, 999.0f);
        return
            kilometersPerHour.ToString("F0", CultureInfo.InvariantCulture) +
            " KMH";
    }

    private static string FormatGear(int? gear) => gear switch
    {
        -1 => "R",
        0 => "N",
        >= 1 and <= 9 => gear.Value.ToString(CultureInfo.InvariantCulture),
        _ => "?"
    };

    private static string FormatBrakeBias(float? brakeBiasPercent)
    {
        if (brakeBiasPercent is not float value ||
            !float.IsFinite(value) ||
            value is < 0 or > 100)
        {
            return "N/A";
        }

        return value.ToString("F1", CultureInfo.InvariantCulture) + "%";
    }

    private static string FormatLapTime(float? totalSeconds)
    {
        if (totalSeconds is not float value ||
            !float.IsFinite(value) ||
            value <= 0 ||
            value >= 6000)
        {
            return "N/A";
        }

        TimeSpan time = TimeSpan.FromSeconds(value);
        int minutes = (int)time.TotalMinutes;
        return
            $"{minutes.ToString(CultureInfo.InvariantCulture)}:" +
            $"{time.Seconds:00}.{time.Milliseconds:000}";
    }

    private static string FormatConnectionState(string state) =>
        state.ToUpperInvariant() switch
        {
            "WAITING" => "WAITING",
            "ERROR" => "ERROR",
            _ => "OFFLINE"
        };
}
