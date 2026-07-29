using LogiDynamicDash.Displays;
using LogiDynamicDash.Models;
using LogiDynamicDash.Native;

namespace LogiDynamicDash.Tests;

public sealed class Rs50DisplaySinkTests
{
    [Fact]
    public void InitializeRenderStop_OwnsCompleteStreamLifecycle()
    {
        List<string> events = [];
        RecordingBridge bridge = new(events);
        RecordingOwnerWindow window = new(events);
        Rs50DisplaySink sink = new(bridge, window);
        LayoutJFrame frame =
            new("SPEED", "123 KMH", "GEAR", "4");

        sink.Initialize();
        sink.Render(frame);
        sink.Stop();

        Assert.Equal(
            ["window:create", "bridge:open:123", "bridge:begin",
             "bridge:send", "bridge:end", "bridge:dispose",
             "window:dispose"],
            events);
        Assert.Same(frame, bridge.LastFrame);
    }

    [Fact]
    public void InitializeFailure_ReleasesBridgeAndWindow()
    {
        List<string> events = [];
        RecordingBridge bridge = new(events)
        {
            FailBegin = true
        };
        RecordingOwnerWindow window = new(events);
        Rs50DisplaySink sink = new(bridge, window);

        Assert.Throws<InvalidOperationException>(sink.Initialize);

        Assert.Contains("bridge:dispose", events);
        Assert.Equal("window:dispose", events[^1]);
    }

    [Fact]
    public void RenderBeforeInitialize_IsRejected()
    {
        Rs50DisplaySink sink =
            new(new RecordingBridge([]), new RecordingOwnerWindow([]));

        Assert.Throws<InvalidOperationException>(
            () => sink.Render(new("", "", "", "")));
    }

    [Fact]
    public void RateLimitedFrame_IsDelayedAndRetriedOnce()
    {
        List<string> events = [];
        RecordingBridge bridge = new(events);
        bridge.Outcomes.Enqueue(new(false, false, true));
        bridge.Outcomes.Enqueue(new(true, false, false));
        RecordingOwnerWindow window = new(events);
        List<TimeSpan> delays = [];
        Rs50DisplaySink sink =
            new(bridge, window, delays.Add);

        sink.Initialize();
        sink.Render(new("SPEED", "123 KMH", "GEAR", "4"));

        Assert.Equal(2, bridge.SendCount);
        Assert.Equal([TimeSpan.FromMilliseconds(200)], delays);
    }

    [Fact]
    public void RepeatedRateLimit_IsRejectedSoDistinctSinkCanRetry()
    {
        List<string> events = [];
        RecordingBridge bridge = new(events);
        bridge.Outcomes.Enqueue(new(false, false, true));
        bridge.Outcomes.Enqueue(new(false, false, true));
        Rs50DisplaySink sink =
            new(bridge, new RecordingOwnerWindow(events), _ => { });

        sink.Initialize();

        Assert.Throws<InvalidOperationException>(
            () => sink.Render(new("SPEED", "123 KMH", "GEAR", "4")));
    }

    private sealed class RecordingOwnerWindow(
        List<string> events) : IRs50OwnerWindow
    {
        public nint Handle { get; private set; }

        public void Create()
        {
            Handle = 123;
            events.Add("window:create");
        }

        public void Dispose()
        {
            events.Add("window:dispose");
            Handle = 0;
        }
    }

    private sealed class RecordingBridge(
        List<string> events) : IRs50DisplayBridge
    {
        public bool FailBegin { get; set; }

        public LayoutJFrame? LastFrame { get; private set; }

        public Queue<Rs50FrameOutcome> Outcomes { get; } = [];

        public int SendCount { get; private set; }

        public void Open(nint ownerWindow)
        {
            events.Add($"bridge:open:{ownerWindow}");
        }

        public void BeginLayoutJStream()
        {
            events.Add("bridge:begin");
            if (FailBegin)
            {
                throw new InvalidOperationException("Simulated begin failure.");
            }
        }

        public Rs50FrameOutcome Send(LayoutJFrame frame)
        {
            events.Add("bridge:send");
            LastFrame = frame;
            SendCount++;
            return Outcomes.TryDequeue(out Rs50FrameOutcome outcome)
                ? outcome
                : new(true, false, false);
        }

        public void EndLayoutJStream() =>
            events.Add("bridge:end");

        public void Dispose() =>
            events.Add("bridge:dispose");
    }
}
