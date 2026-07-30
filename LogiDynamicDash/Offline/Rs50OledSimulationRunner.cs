using LogiDynamicDash.Displays;
using LogiDynamicDash.Hidpp;
using LogiDynamicDash.Models;

namespace LogiDynamicDash.Offline;

internal sealed record Rs50OledSimulationResult(
    Rs50OledLayout Layout,
    int Updates,
    int Transmitted,
    int Unchanged,
    int RateLimited,
    int DiscoveryTransactions,
    int LayoutTransactions);

internal static class Rs50OledSimulationRunner
{
    private const int UpdateIntervalMilliseconds = 50;
    private const int SimulationDurationSeconds = 30;

    internal static IReadOnlyList<Rs50OledSimulationResult> RunAll(
        Rs50OledConfiguration configuration,
        TextWriter output)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(output);

        List<Rs50OledSimulationResult> results = [];
        foreach (Rs50OledLayout layout in Enum.GetValues<Rs50OledLayout>())
        {
            Rs50OledConfiguration layoutConfiguration = new(
                layout,
                configuration.SpeedUnit,
                configuration.MaximumRpm,
                configuration.GaugeMaximumSpeed);
            Rs50OledSimulationResult result =
                RunOne(layoutConfiguration);
            results.Add(result);
            output.WriteLine(
                $"LAYOUT {layout}: updates={result.Updates} " +
                $"transmitted={result.Transmitted} " +
                $"unchanged={result.Unchanged} " +
                $"rate_limited={result.RateLimited} " +
                $"discovery={result.DiscoveryTransactions} " +
                $"layout_transactions={result.LayoutTransactions}");
        }

        return results;
    }

    internal static Rs50OledSimulationResult RunOne(
        Rs50OledConfiguration configuration)
    {
        SimulationTimeProvider clock = new();
        SimulatedRs50OledExchange exchange = new();
        using Rs50OledSession session = new(exchange, clock);
        session.Open();
        Rs50TelemetryFrameFormatter formatter = new(configuration);

        int transmitted = 0;
        int unchanged = 0;
        int rateLimited = 0;
        int updates = 0;
        int totalSteps =
            SimulationDurationSeconds * 1000 /
            UpdateIntervalMilliseconds;

        for (int step = 0; step <= totalSteps; step++)
        {
            double seconds =
                step * UpdateIntervalMilliseconds / 1000d;
            (TelemetrySnapshot snapshot, DisplayMode mode) =
                CreateScenario(seconds);
            Rs50OledFrame frame = formatter.Format(snapshot, mode);
            Rs50OledSendResult result = session.Send(frame);
            updates++;

            switch (result)
            {
                case Rs50OledSendResult.Transmitted:
                    transmitted++;
                    break;
                case Rs50OledSendResult.Unchanged:
                    unchanged++;
                    break;
                case Rs50OledSendResult.RateLimited:
                    rateLimited++;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(result));
            }

            clock.Advance(
                TimeSpan.FromMilliseconds(UpdateIntervalMilliseconds));
        }

        return new Rs50OledSimulationResult(
            configuration.Layout,
            updates,
            transmitted,
            unchanged,
            rateLimited,
            exchange.DiscoveryCount,
            exchange.LayoutCount);
    }

    private static (TelemetrySnapshot Snapshot, DisplayMode Mode)
        CreateScenario(double seconds)
    {
        TelemetrySnapshot snapshot = new();
        if (seconds < 2)
        {
            snapshot.ConnectionState = "WAITING";
            return (snapshot, DisplayMode.ConnectionProblem);
        }

        if (seconds >= 28)
        {
            snapshot.ConnectionState = "ERROR";
            return (snapshot, DisplayMode.ConnectionProblem);
        }

        double drivingSeconds = seconds - 2;
        double speedKmh = Math.Clamp(drivingSeconds * 12, 0, 240);
        snapshot.ConnectionState = "CONNECTED";
        snapshot.IsOnTrack = true;
        snapshot.SpeedMetersPerSecond = (float)(speedKmh / 3.6);
        snapshot.Gear = Math.Clamp((int)(speedKmh / 40) + 1, 1, 6);
        snapshot.Rpm =
            (float)(2500 + (drivingSeconds * 1700) % 5500);
        snapshot.BrakeBiasPercent =
            (float)(52.0 + Math.Sin(drivingSeconds) * 0.5);
        snapshot.LastLapTimeSeconds = 92.481f;

        DisplayMode mode = seconds switch
        {
            >= 10 and < 12 => DisplayMode.BrakeBias,
            >= 20 and < 23 => DisplayMode.LastLap,
            _ => DisplayMode.Normal
        };
        return (snapshot, mode);
    }

    private sealed class SimulationTimeProvider : TimeProvider
    {
        private long timestamp;

        public override long TimestampFrequency => TimeSpan.TicksPerSecond;

        public override long GetTimestamp() => timestamp;

        internal void Advance(TimeSpan duration) =>
            timestamp += duration.Ticks;
    }
}
