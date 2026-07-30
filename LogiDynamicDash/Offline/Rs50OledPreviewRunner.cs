using LogiDynamicDash.Displays;
using LogiDynamicDash.Models;

namespace LogiDynamicDash.Offline;

internal static class Rs50OledPreviewRunner
{
    internal static void RunAll(
        Rs50OledConfiguration configuration,
        TextWriter output)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(output);

        TelemetrySnapshot sample = CreateSample();
        foreach (Rs50OledLayout layout in Enum.GetValues<Rs50OledLayout>())
        {
            Rs50OledConfiguration layoutConfiguration = new(
                layout,
                configuration.SpeedUnit,
                configuration.MaximumRpm,
                configuration.GaugeMaximumSpeed);
            Rs50TelemetryFrameFormatter formatter =
                new(layoutConfiguration);

            output.WriteLine($"LAYOUT {layout}");
            foreach (DisplayMode mode in Enum.GetValues<DisplayMode>())
            {
                Rs50OledFrame frame = formatter.Format(sample, mode);
                output.WriteLine(
                    $"  {mode}: {Rs50OledFrameDescription.Describe(frame)}");
            }
        }
    }

    private static TelemetrySnapshot CreateSample() =>
        new()
        {
            ConnectionState = "ERROR",
            IsOnTrack = true,
            Gear = 3,
            Rpm = 6500,
            SpeedMetersPerSecond = 123f / 3.6f,
            BrakeBiasPercent = 52.3f,
            LastLapTimeSeconds = 92.481f
        };
}
