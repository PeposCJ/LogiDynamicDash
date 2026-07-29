namespace LogiDynamicDash.Models;

internal enum SpeedUnit
{
    KilometersPerHour,
    MilesPerHour
}

internal sealed record Rs50OledConfiguration
{
    private readonly IReadOnlyDictionary<DisplayMode, Rs50OledLayout> layouts;

    internal Rs50OledConfiguration(
        Rs50OledLayout layout,
        SpeedUnit speedUnit = SpeedUnit.KilometersPerHour,
        double maximumRpm = 8000,
        double gaugeMaximumSpeed = 300)
        : this(
            AllModesForLayout(layout),
            speedUnit,
            maximumRpm,
            gaugeMaximumSpeed)
    {
    }

    internal Rs50OledConfiguration(
        IReadOnlyDictionary<DisplayMode, Rs50OledLayout> layouts,
        SpeedUnit speedUnit = SpeedUnit.KilometersPerHour,
        double maximumRpm = 8000,
        double gaugeMaximumSpeed = 300)
    {
        ArgumentNullException.ThrowIfNull(layouts);
        DisplayMode[] modes = Enum.GetValues<DisplayMode>();
        if (layouts.Count != modes.Length ||
            modes.Any(mode => !layouts.TryGetValue(mode, out _) ||
                              !Enum.IsDefined(layouts[mode])))
        {
            throw new ArgumentException(
                "Every display mode must map to one confirmed layout A-J.",
                nameof(layouts));
        }

        if (!Enum.IsDefined(speedUnit))
        {
            throw new ArgumentOutOfRangeException(
                nameof(speedUnit));
        }

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

        this.layouts = new Dictionary<DisplayMode, Rs50OledLayout>(layouts);
        SpeedUnit = speedUnit;
        MaximumRpm = maximumRpm;
        GaugeMaximumSpeed = gaugeMaximumSpeed;
    }

    internal Rs50OledLayout Layout => LayoutFor(DisplayMode.Normal);

    internal Rs50OledLayout LayoutFor(DisplayMode mode)
    {
        if (!Enum.IsDefined(mode))
        {
            throw new ArgumentOutOfRangeException(nameof(mode));
        }

        return layouts[mode];
    }

    internal SpeedUnit SpeedUnit { get; }

    internal double MaximumRpm { get; }

    /// <summary>
    /// Maximum value, in the selected speed unit, represented by a full
    /// secondary speed indicator.
    /// </summary>
    internal double GaugeMaximumSpeed { get; }

    private static IReadOnlyDictionary<DisplayMode, Rs50OledLayout>
        AllModesForLayout(Rs50OledLayout layout)
    {
        if (!Enum.IsDefined(layout))
        {
            throw new ArgumentOutOfRangeException(
                nameof(layout),
                "Only confirmed layouts A-J are supported.");
        }

        return Enum.GetValues<DisplayMode>()
            .ToDictionary(mode => mode, _ => layout);
    }
}
