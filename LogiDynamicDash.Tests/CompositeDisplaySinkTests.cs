using LogiDynamicDash.Displays;
using LogiDynamicDash.Models;

namespace LogiDynamicDash.Tests;

public sealed class CompositeDisplaySinkTests
{
    [Fact]
    public void Lifecycle_InitializesInOrderAndStopsInReverse()
    {
        List<string> events = [];
        RecordingSink first = new("first", events);
        RecordingSink second = new("second", events);
        CompositeDisplaySink sink = new(first, second);
        LayoutJFrame frame = new("SPEED", "1 KMH", "GEAR", "1");

        sink.Initialize();
        sink.Render(frame);
        sink.Stop();

        Assert.Equal(
            ["first:init", "second:init", "first:render", "second:render",
             "second:stop", "first:stop"],
            events);
    }

    [Fact]
    public void InitializeFailure_StopsOnlyInitializedSinks()
    {
        List<string> events = [];
        RecordingSink first = new("first", events);
        RecordingSink second = new("second", events)
        {
            FailInitialize = true
        };
        RecordingSink third = new("third", events);
        CompositeDisplaySink sink = new(first, second, third);

        Assert.Throws<InvalidOperationException>(sink.Initialize);

        Assert.Equal(
            ["first:init", "second:init", "first:stop"],
            events);
    }

    private sealed class RecordingSink(
        string name,
        List<string> events) : IDisplaySink
    {
        public bool FailInitialize { get; set; }

        public void Initialize()
        {
            events.Add($"{name}:init");
            if (FailInitialize)
            {
                throw new InvalidOperationException("Simulated failure.");
            }
        }

        public void Render(LayoutJFrame frame) =>
            events.Add($"{name}:render");

        public void Stop() =>
            events.Add($"{name}:stop");
    }
}
