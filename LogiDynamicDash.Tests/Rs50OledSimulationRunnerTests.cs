using LogiDynamicDash.Hidpp;
using LogiDynamicDash.Models;
using LogiDynamicDash.Offline;

namespace LogiDynamicDash.Tests;

public sealed class Rs50OledSimulationRunnerTests
{
    [Fact]
    public void RunAll_ExercisesEveryLayoutThroughProtocolSession()
    {
        StringWriter output = new();

        IReadOnlyList<Rs50OledSimulationResult> results =
            Rs50OledSimulationRunner.RunAll(
                new Rs50OledConfiguration(Rs50OledLayout.E),
                output);

        Assert.Equal(10, results.Count);
        Assert.Equal(
            Enum.GetValues<Rs50OledLayout>(),
            results.Select(result => result.Layout));

        foreach (Rs50OledSimulationResult result in results)
        {
            Assert.Equal(601, result.Updates);
            Assert.Equal(1, result.DiscoveryTransactions);
            Assert.Equal(result.Transmitted, result.LayoutTransactions);
            Assert.InRange(result.Transmitted, 1, 151);
            Assert.Equal(
                result.Updates,
                result.Transmitted +
                result.Unchanged +
                result.RateLimited);
        }

        Assert.Contains("LAYOUT A:", output.ToString());
        Assert.Contains("LAYOUT J:", output.ToString());
    }

    [Fact]
    public void Session_RemainsBoundedAcrossTwentyThousandVirtualUpdates()
    {
        ManualTimeProvider clock = new();
        SimulatedRs50OledExchange exchange = new();
        using Rs50OledSession session = new(exchange, clock);
        session.Open();
        int transmitted = 0;

        for (int index = 0; index < 20_000; index++)
        {
            Rs50OledSendResult result = session.Send(
                new Rs50LayoutJFrame(
                    "SPEED",
                    $"{index % 1000} KMH",
                    "GEAR",
                    $"{index % 9 + 1}"));
            if (result == Rs50OledSendResult.Transmitted)
            {
                transmitted++;
            }

            clock.Advance(TimeSpan.FromMilliseconds(10));
        }

        Assert.Equal(transmitted, exchange.LayoutCount);
        Assert.InRange(transmitted, 999, 1000);
        Assert.Equal(1, exchange.DiscoveryCount);
    }

    private sealed class ManualTimeProvider : TimeProvider
    {
        private long timestamp;

        public override long TimestampFrequency => TimeSpan.TicksPerSecond;

        public override long GetTimestamp() => timestamp;

        internal void Advance(TimeSpan duration) =>
            timestamp += duration.Ticks;
    }
}
