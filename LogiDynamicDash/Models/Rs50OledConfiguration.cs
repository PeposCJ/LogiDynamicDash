namespace LogiDynamicDash.Models;

internal enum SpeedUnit
{
    KilometersPerHour,
    MilesPerHour
}

internal sealed record Rs50OledConfiguration
{
    internal Rs50OledConfiguration(
        Rs50OledLayout layout,
        SpeedUnit speedUnit = SpeedUnit.KilometersPerHour,
        double maximumRpm = 8000,
        double gaugeMaximumSpeed = 300)
    {
        if (!double.IsFinite(maximumRpm) || maximumRpm <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maximumRpm),
                "Maximum RPM must be finite and greater than zero.");
        }

        if (!double.IsFinite(gaugeMaximumSpeed) ||
            gaugeMaximumSpeed <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(gaugeMaximumSpeed),
                "Gauge maximum speed must be finite and greater than zero.");
        }

        Layout = layout;
        SpeedUnit = speedUnit;
        MaximumRpm = maximumRpm;
        GaugeMaximumSpeed = gaugeMaximumSpeed;
    }

    internal Rs50OledLayout Layout { get; }

    internal SpeedUnit SpeedUnit { get; }

    internal double MaximumRpm { get; }

    /// <summary>
    /// Maximum value, in the selected speed unit, represented by a full
    /// secondary speed indicator.
    /// </summary>
    internal double GaugeMaximumSpeed { get; }
}
