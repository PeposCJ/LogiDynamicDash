using LogiDynamicDash.Displays;
using LogiDynamicDash.Models;

namespace LogiDynamicDash.Tests;

public sealed class DistinctDisplaySinkTests
{
    [Fact]
    public void Render_ForwardsOnlyChanges()
    {
        RecordingDisplaySink inner = new();
        DistinctDisplaySink sink = new(inner);
        LayoutJFrame first = new("SPEED", "200 KMH", "GEAR", "4");
        LayoutJFrame equivalent = new("SPEED", "200 KMH", "GEAR", "4");
        LayoutJFrame changed = new("SPEED", "201 KMH", "GEAR", "4");

        sink.Initialize();
        sink.Render(first);
        sink.Render(equivalent);
        sink.Render(changed);

        Assert.Equal([first, changed], inner.Frames);
    }

    [Fact]
    public void Render_RetriesFrameRejectedByWrappedSink()
    {
        RecordingDisplaySink inner = new()
        {
            FailNextRender = true
        };
        DistinctDisplaySink sink = new(inner);
        LayoutJFrame frame = new("SPEED", "200 KMH", "GEAR", "4");

        Assert.Throws<InvalidOperationException>(() => sink.Render(frame));
        sink.Render(frame);

        Assert.Equal([frame], inner.Frames);
    }

    [Fact]
    public void InitializeAndStop_ResetRememberedFrame()
    {
        RecordingDisplaySink inner = new();
        DistinctDisplaySink sink = new(inner);
        LayoutJFrame frame = new("SPEED", "200 KMH", "GEAR", "4");

        sink.Render(frame);
        sink.Stop();
        sink.Render(frame);
        sink.Initialize();
        sink.Render(frame);

        Assert.Equal([frame, frame, frame], inner.Frames);
        Assert.Equal(1, inner.InitializeCount);
        Assert.Equal(1, inner.StopCount);
    }

    private sealed class RecordingDisplaySink : IDisplaySink
    {
        public List<LayoutJFrame> Frames { get; } = [];

        public bool FailNextRender { get; set; }

        public int InitializeCount { get; private set; }

        public int StopCount { get; private set; }

        public void Initialize() => InitializeCount++;

        public void Render(LayoutJFrame frame)
        {
            if (FailNextRender)
            {
                FailNextRender = false;
                throw new InvalidOperationException("Simulated sink failure.");
            }

            Frames.Add(frame);
        }

        public void Stop() => StopCount++;
    }
}
