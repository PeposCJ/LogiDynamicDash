using LogiDynamicDash.Displays;
using LogiDynamicDash.Hidpp;
using LogiDynamicDash.Models;

namespace LogiDynamicDash.Tests;

public sealed class Rs50OledFrameSchedulerTests
{
    [Fact]
    public void Submit_RetainsLatestOrdinaryFrameUntilRateLimitClears()
    {
        FakeSession session = new(
            Rs50OledSendResult.RateLimited,
            Rs50OledSendResult.RateLimited,
            Rs50OledSendResult.Transmitted);
        Rs50OledFrameScheduler scheduler = new(session);
        Rs50OledFrame first = new Rs50LayoutFFrame("1", "10");
        Rs50OledFrame latest = new Rs50LayoutFFrame("2", "20");

        scheduler.Submit(first, isCritical: false);
        scheduler.Submit(latest, isCritical: false);
        scheduler.Submit(latest, isCritical: false);

        Assert.Equal([first, first, latest], session.Frames);
        Assert.False(scheduler.HasPendingFrame);
    }

    [Fact]
    public void Submit_DoesNotReplacePendingCriticalFrameWithTelemetry()
    {
        FakeSession session = new(
            Rs50OledSendResult.RateLimited,
            Rs50OledSendResult.RateLimited,
            Rs50OledSendResult.Transmitted,
            Rs50OledSendResult.RateLimited);
        Rs50OledFrameScheduler scheduler = new(session);
        Rs50OledFrame critical = new Rs50LayoutHFrame("IRACING", "ERROR");
        Rs50OledFrame normal = new Rs50LayoutHFrame("SPEED 0 KMH", "GEAR N");

        scheduler.Submit(critical, isCritical: true);
        scheduler.Submit(normal, isCritical: false);
        scheduler.Submit(normal, isCritical: false);

        Assert.Equal([critical, critical, critical, normal], session.Frames);
        Assert.True(scheduler.HasPendingFrame);
    }

    [Fact]
    public void Submit_RemainsSingleConsumerAcrossOneMillionVirtualUpdates()
    {
        CountingSession session = new();
        Rs50OledFrameScheduler scheduler = new(session);
        Rs50OledFrame frame = new Rs50LayoutFFrame("6", "299");

        for (int index = 0; index < 1_000_000; index++)
        {
            scheduler.Submit(frame, isCritical: false);
        }

        Assert.Equal(1_000_000, session.SendCount);
        Assert.False(scheduler.HasPendingFrame);
    }

    private sealed class FakeSession(params Rs50OledSendResult[] results)
        : IRs50OledSession
    {
        private readonly Queue<Rs50OledSendResult> results = new(results);

        public List<Rs50OledFrame> Frames { get; } = [];

        public void Open()
        {
        }

        public Rs50OledSendResult Send(Rs50OledFrame frame)
        {
            Frames.Add(frame);
            return results.Dequeue();
        }

        public void Dispose()
        {
        }
    }

    private sealed class CountingSession : IRs50OledSession
    {
        public int SendCount { get; private set; }

        public void Open()
        {
        }

        public Rs50OledSendResult Send(Rs50OledFrame frame)
        {
            SendCount++;
            return Rs50OledSendResult.Unchanged;
        }

        public void Dispose()
        {
        }
    }
}
