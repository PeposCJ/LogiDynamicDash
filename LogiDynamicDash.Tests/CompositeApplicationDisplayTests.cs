using LogiDynamicDash.Displays;
using LogiDynamicDash.Models;

namespace LogiDynamicDash.Tests;

public sealed class CompositeApplicationDisplayTests
{
    [Fact]
    public void InitializeFailure_StopsEarlierDisplaysAndPreservesFailure()
    {
        FakeDisplay first = new();
        FakeDisplay second = new()
        {
            InitializeException = new IOException("second failed")
        };
        CompositeApplicationDisplay composite = new(first, second);

        IOException exception =
            Assert.Throws<IOException>(() => composite.Initialize());

        Assert.Equal("second failed", exception.Message);
        Assert.Equal(1, first.StopCount);
        Assert.Equal(0, second.StopCount);
    }

    [Fact]
    public void CleanupFailure_DoesNotReplaceInitializationFailure()
    {
        FakeDisplay first = new()
        {
            StopException = new IOException("cleanup failed")
        };
        FakeDisplay second = new()
        {
            InitializeException = new InvalidOperationException(
                "initialization failed")
        };
        CompositeApplicationDisplay composite = new(first, second);

        InvalidOperationException exception =
            Assert.Throws<InvalidOperationException>(
                () => composite.Initialize());

        Assert.Equal("initialization failed", exception.Message);
    }

    [Fact]
    public void RenderAndStop_ReachEveryInitializedDisplay()
    {
        FakeDisplay first = new();
        FakeDisplay second = new();
        CompositeApplicationDisplay composite = new(first, second);
        composite.Initialize();
        TelemetrySnapshot snapshot = new();

        composite.Render(snapshot, DisplayMode.Normal);
        composite.Stop();
        composite.Stop();

        Assert.Equal(1, first.RenderCount);
        Assert.Equal(1, second.RenderCount);
        Assert.Equal(1, first.StopCount);
        Assert.Equal(1, second.StopCount);
    }

    private sealed class FakeDisplay : IApplicationDisplay
    {
        public Exception? InitializeException { get; set; }

        public Exception? StopException { get; set; }

        public int RenderCount { get; private set; }

        public int StopCount { get; private set; }

        public void Initialize()
        {
            if (InitializeException is not null)
            {
                throw InitializeException;
            }
        }

        public void Render(TelemetrySnapshot snapshot, DisplayMode mode) =>
            RenderCount++;

        public void Stop()
        {
            StopCount++;
            if (StopException is not null)
            {
                throw StopException;
            }
        }
    }
}
