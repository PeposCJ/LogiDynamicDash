using System.Globalization;
using LogiDynamicDash.Models;

namespace LogiDynamicDash.Displays;

/// <summary>
/// Maps application telemetry to the fixed semantic fields exposed by the
/// confirmed RS50 OLED layouts. It does not encode or transmit HID data.
/// </summary>
internal sealed class Rs50TelemetryFrameFormatter(
    Rs50OledConfiguration configuration) : IRs50TelemetryFrameFormatter
{
    public Rs50OledFrame Format(
        TelemetrySnapshot snapshot,
        DisplayMode mode)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        Rs50OledLayout layout = configuration.LayoutFor(mode);
        return layout switch
        {
            Rs50OledLayout.A => new Rs50LayoutAFrame(),
            Rs50OledLayout.B => new Rs50LayoutBFrame(),
            Rs50OledLayout.C => new Rs50LayoutCFrame(RpmGauge(snapshot)),
            Rs50OledLayout.D => FormatLayoutD(snapshot, mode),
            Rs50OledLayout.E => FormatLayoutE(snapshot, mode),
            Rs50OledLayout.F => FormatCompact(snapshot, mode, layoutF: true),
            Rs50OledLayout.G => FormatCompact(snapshot, mode, layoutF: false),
            Rs50OledLayout.H => FormatLayoutH(snapshot, mode),
            Rs50OledLayout.I => FormatFourRows(snapshot, mode, layoutI: true),
            Rs50OledLayout.J => FormatFourRows(snapshot, mode, layoutI: false),
            _ => throw new ArgumentOutOfRangeException(
                nameof(mode),
                "Unsupported RS50 OLED layout.")
        };
    }

    private Rs50LayoutDFrame FormatLayoutD(
        TelemetrySnapshot snapshot,
        DisplayMode mode) =>
        new(
            RpmGauge(snapshot),
            SpeedGauge(snapshot),
            mode switch
            {
                DisplayMode.BrakeBias =>
                    $"BB {FormatBrakeBias(snapshot.BrakeBiasPercent)}",
                DisplayMode.LastLap =>
                    $"L {FormatLapTime(snapshot.LastLapTimeSeconds)}",
                DisplayMode.ConnectionProblem =>
                    "OFFLINE",
                _ =>
                    $"{FormatGear(snapshot.Gear)} " +
                    $"{FormatSpeedNumber(snapshot.SpeedMetersPerSecond)}" +
                    SpeedUnitSuffix(compact: true)
            });

    private Rs50LayoutEFrame FormatLayoutE(
        TelemetrySnapshot snapshot,
        DisplayMode mode)
    {
        (string left, string right) = mode switch
        {
            DisplayMode.BrakeBias =>
                (FormatBrakeBias(snapshot.BrakeBiasPercent), "BB"),
            DisplayMode.LastLap =>
                (FormatLapTimeShort(snapshot.LastLapTimeSeconds), "LAP"),
            DisplayMode.ConnectionProblem =>
                ("OFFLINE", "ERR"),
            _ =>
                (FormatSpeed(snapshot.SpeedMetersPerSecond),
                    FormatGear(snapshot.Gear))
        };

        return new Rs50LayoutEFrame(
            RpmGauge(snapshot),
            SpeedGauge(snapshot),
            left,
            right);
    }

    private Rs50OledFrame FormatCompact(
        TelemetrySnapshot snapshot,
        DisplayMode mode,
        bool layoutF)
    {
        (string left, string right) = mode switch
        {
            DisplayMode.BrakeBias =>
                ("B", FormatBrakeBiasWhole(snapshot.BrakeBiasPercent)),
            DisplayMode.LastLap =>
                ("L", FormatLapSeconds(snapshot.LastLapTimeSeconds)),
            DisplayMode.ConnectionProblem =>
                ("!", "ERR"),
            _ =>
                (FormatGearOneCharacter(snapshot.Gear),
                    FormatSpeedNumber(snapshot.SpeedMetersPerSecond))
        };

        return layoutF
            ? new Rs50LayoutFFrame(left, right)
            : new Rs50LayoutGFrame(left, right);
    }

    private Rs50LayoutHFrame FormatLayoutH(
        TelemetrySnapshot snapshot,
        DisplayMode mode) =>
        mode switch
        {
            DisplayMode.BrakeBias =>
                new("BRAKE BIAS", FormatBrakeBias(snapshot.BrakeBiasPercent)),
            DisplayMode.LastLap =>
                new("LAST LAP", FormatLapTime(snapshot.LastLapTimeSeconds)),
            DisplayMode.ConnectionProblem =>
                new("IRACING", FormatConnection(snapshot.ConnectionState)),
            _ =>
                new(
                    $"SPEED {FormatSpeed(snapshot.SpeedMetersPerSecond)}",
                    $"GEAR {FormatGear(snapshot.Gear)}")
        };

    private Rs50OledFrame FormatFourRows(
        TelemetrySnapshot snapshot,
        DisplayMode mode,
        bool layoutI)
    {
        (string line1, string line2, string line3, string line4) =
            mode switch
            {
                DisplayMode.BrakeBias =>
                    ("BRAKE BIAS",
                        FormatBrakeBias(snapshot.BrakeBiasPercent),
                        string.Empty,
                        string.Empty),
                DisplayMode.LastLap =>
                    ("LAST LAP",
                        FormatLapTime(snapshot.LastLapTimeSeconds),
                        string.Empty,
                        string.Empty),
                DisplayMode.ConnectionProblem =>
                    ("IRACING",
                        FormatConnection(snapshot.ConnectionState),
                        string.Empty,
                        string.Empty),
                _ =>
                    ("SPEED",
                        FormatSpeed(snapshot.SpeedMetersPerSecond),
                        "GEAR",
                        FormatGear(snapshot.Gear))
            };

        return layoutI
            ? new Rs50LayoutIFrame(line1, line2, line3, line4)
            : new Rs50LayoutJFrame(line1, line2, line3, line4);
    }

    private Rs50GaugeLevel RpmGauge(TelemetrySnapshot snapshot)
    {
        double ratio =
            snapshot.Rpm is float rpm && float.IsFinite(rpm) && rpm > 0
                ? rpm / configuration.MaximumRpm
                : 0;
        return Rs50GaugeLevel.FromRatio(ratio);
    }

    private Rs50GaugeLevel SpeedGauge(TelemetrySnapshot snapshot)
    {
        double speed = ConvertSpeed(snapshot.SpeedMetersPerSecond);
        double ratio = speed >= 0
            ? speed / configuration.GaugeMaximumSpeed
            : 0;
        return Rs50GaugeLevel.FromRatio(ratio);
    }

    private string FormatSpeed(float? metersPerSecond) =>
        $"{FormatSpeedNumber(metersPerSecond)}{SpeedUnitSuffix(false)}";

    private string FormatSpeedNumber(float? metersPerSecond)
    {
        double speed = ConvertSpeed(metersPerSecond);
        if (speed < 0)
        {
            return "---";
        }

        int rounded = Math.Clamp(
            (int)Math.Round(speed, MidpointRounding.AwayFromZero),
            0,
            999);
        return rounded.ToString(CultureInfo.InvariantCulture);
    }

    private double ConvertSpeed(float? metersPerSecond)
    {
        if (metersPerSecond is not float value ||
            !float.IsFinite(value) ||
            value < 0)
        {
            return -1;
        }

        return configuration.SpeedUnit switch
        {
            SpeedUnit.KilometersPerHour => value * 3.6,
            SpeedUnit.MilesPerHour => value * 2.2369362920544,
            _ => throw new ArgumentOutOfRangeException(
                nameof(configuration))
        };
    }

    private string SpeedUnitSuffix(bool compact) =>
        configuration.SpeedUnit switch
        {
            SpeedUnit.KilometersPerHour => compact ? "K" : " KMH",
            SpeedUnit.MilesPerHour => compact ? "M" : " MPH",
            _ => throw new ArgumentOutOfRangeException(
                nameof(configuration))
        };

    private static string FormatGear(int? gear) => gear switch
    {
        -1 => "R",
        0 => "N",
        >= 1 and <= 99 => gear.Value.ToString(CultureInfo.InvariantCulture),
        _ => "?"
    };

    private static string FormatGearOneCharacter(int? gear) => gear switch
    {
        -1 => "R",
        0 => "N",
        >= 1 and <= 9 => gear.Value.ToString(CultureInfo.InvariantCulture),
        _ => "?"
    };

    private static string FormatBrakeBias(float? percent)
    {
        if (percent is not float value ||
            !float.IsFinite(value) ||
            value is < 0 or > 100)
        {
            return "N/A";
        }

        return value.ToString("F1", CultureInfo.InvariantCulture) + "%";
    }

    private static string FormatBrakeBiasWhole(float? percent)
    {
        if (percent is not float value ||
            !float.IsFinite(value) ||
            value is < 0 or > 100)
        {
            return "---";
        }

        return Math.Clamp(
                (int)Math.Round(value, MidpointRounding.AwayFromZero),
                0,
                100)
            .ToString(CultureInfo.InvariantCulture);
    }

    private static string FormatLapTime(float? totalSeconds)
    {
        if (!TryCreateLapTime(totalSeconds, out TimeSpan time))
        {
            return "N/A";
        }

        int minutes = (int)time.TotalMinutes;
        return
            $"{minutes.ToString(CultureInfo.InvariantCulture)}:" +
            $"{time.Seconds:00}.{time.Milliseconds:000}";
    }

    private static string FormatLapTimeShort(float? totalSeconds)
    {
        if (!TryCreateLapTime(totalSeconds, out TimeSpan time))
        {
            return "N/A";
        }

        int minutes = (int)time.TotalMinutes;
        int tenths = time.Milliseconds / 100;
        return
            $"{minutes.ToString(CultureInfo.InvariantCulture)}:" +
            $"{time.Seconds:00}.{tenths}";
    }

    private static string FormatLapSeconds(float? totalSeconds)
    {
        if (totalSeconds is not float value ||
            !float.IsFinite(value) ||
            value <= 0)
        {
            return "---";
        }

        return Math.Clamp(
                (int)Math.Round(value, MidpointRounding.AwayFromZero),
                0,
                999)
            .ToString(CultureInfo.InvariantCulture);
    }

    private static bool TryCreateLapTime(
        float? totalSeconds,
        out TimeSpan time)
    {
        if (totalSeconds is not float value ||
            !float.IsFinite(value) ||
            value <= 0 ||
            value >= 6000)
        {
            time = default;
            return false;
        }

        time = TimeSpan.FromSeconds(value);
        return true;
    }

    private static string FormatConnection(string state) =>
        state.ToUpperInvariant() switch
        {
            "WAITING" => "WAITING",
            "ERROR" => "ERROR",
            _ => "OFFLINE"
        };
}
