using LogiDynamicDash.Controllers;
using LogiDynamicDash.Displays;
using LogiDynamicDash.Models;
using LogiDynamicDash.Services;

namespace LogiDynamicDash.Tests;

public sealed class LogiDynamicDashApplicationTests
{
    [Fact]
    public async Task RunAsync_UsesInjectedSourceAndStopsCleanly()
    {
        ManualTimeProvider clock = new();
        RecordingDisplay display = new();
        ScriptedSource source = new(clock);
        LogiDynamicDashApplication application = new(
            source,
            display,
            new DisplayController(clock),
            clock);

        await application.RunAsync(CancellationToken.None);

        Assert.Equal(ApplicationLifecycleState.Stopped, application.State);
        Assert.Equal(1, display.InitializeCount);
        Assert.Equal(1, display.StopCount);
        Assert.Equal(3, display.Modes.Count);
        Assert.Equal(DisplayMode.ConnectionProblem, display.Modes[0]);
        Assert.Equal(DisplayMode.Normal, display.Modes[1]);
    }

    [Fact]
    public async Task RunAsync_FaultsPermanentlyAndDoesNotRestart()
    {
        RecordingDisplay display = new()
        {
            RenderException = new IOException("injected")
        };
        LogiDynamicDashApplication application = new(
            new EmptySource(),
            display,
            new DisplayController());

        await Assert.ThrowsAsync<IOException>(
            () => application.RunAsync(CancellationToken.None));

        Assert.Equal(ApplicationLifecycleState.Faulted, application.State);
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => application.RunAsync(CancellationToken.None));
    }

    [Fact]
    public async Task RunAsync_SerializesConcurrentStatusCallbacks()
    {
        ConcurrentRecordingDisplay display = new();
        LogiDynamicDashApplication application = new(
            new ConcurrentSource(),
            display,
            new DisplayController());

        await application.RunAsync(CancellationToken.None);

        Assert.Equal(101, display.RenderCount);
        Assert.Equal(1, display.MaximumConcurrentRenders);
    }

    [Fact]
    public async Task RunAsync_TreatsRequestedCancellationAsCleanStop()
    {
        RecordingDisplay display = new();
        LogiDynamicDashApplication application = new(
            new CancellationSource(),
            display,
            new DisplayController());
        using CancellationTokenSource cancellation = new();
        cancellation.Cancel();

        await application.RunAsync(cancellation.Token);

        Assert.Equal(ApplicationLifecycleState.Stopped, application.State);
        Assert.Equal(1, display.StopCount);
    }

    private sealed class ScriptedSource(ManualTimeProvider clock)
        : ITelemetrySource
    {
        public Task MonitorAsync(
            TelemetrySnapshot snapshot,
            Action<TelemetrySnapshot> onTelemetryUpdated,
            Action<TelemetrySnapshot> onStatusChanged,
            CancellationToken cancellationToken)
        {
            snapshot.ConnectionState = "CONNECTED";
            snapshot.SpeedMetersPerSecond = 0;
            onStatusChanged(snapshot);
            clock.Advance(TimeSpan.FromMilliseconds(100));
            snapshot.Gear = 1;
            onTelemetryUpdated(snapshot);
            clock.Advance(TimeSpan.FromMilliseconds(100));
            snapshot.Gear = 2;
            onTelemetryUpdated(snapshot);
            return Task.CompletedTask;
        }
    }

    private sealed class EmptySource : ITelemetrySource
    {
        public Task MonitorAsync(
            TelemetrySnapshot snapshot,
            Action<TelemetrySnapshot> onTelemetryUpdated,
            Action<TelemetrySnapshot> onStatusChanged,
            CancellationToken cancellationToken) =>
            Task.CompletedTask;
    }

    private sealed class ConcurrentSource : ITelemetrySource
    {
        public Task MonitorAsync(
            TelemetrySnapshot snapshot,
            Action<TelemetrySnapshot> onTelemetryUpdated,
            Action<TelemetrySnapshot> onStatusChanged,
            CancellationToken cancellationToken)
        {
            snapshot.ConnectionState = "CONNECTED";
            Parallel.For(0, 100, _ => onStatusChanged(snapshot));
            return Task.CompletedTask;
        }
    }

    private sealed class CancellationSource : ITelemetrySource
    {
        public Task MonitorAsync(
            TelemetrySnapshot snapshot,
            Action<TelemetrySnapshot> onTelemetryUpdated,
            Action<TelemetrySnapshot> onStatusChanged,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.CompletedTask;
        }
    }

    private sealed class RecordingDisplay : IApplicationDisplay
    {
        public List<DisplayMode> Modes { get; } = [];
        public int InitializeCount { get; private set; }
        public int StopCount { get; private set; }
        public Exception? RenderException { get; set; }

        public void Initialize() => InitializeCount++;

        public void Render(TelemetrySnapshot snapshot, DisplayMode mode)
        {
            if (RenderException is not null)
            {
                throw RenderException;
            }

            Modes.Add(mode);
        }

        public void Stop() => StopCount++;
    }

    private sealed class ConcurrentRecordingDisplay : IApplicationDisplay
    {
        private int concurrentRenders;
        private int maximumConcurrentRenders;
        private int renderCount;

        public int RenderCount => renderCount;
        public int MaximumConcurrentRenders => maximumConcurrentRenders;

        public void Initialize()
        {
        }

        public void Render(TelemetrySnapshot snapshot, DisplayMode mode)
        {
            int concurrent = Interlocked.Increment(ref concurrentRenders);
            int observed;
            do
            {
                observed = maximumConcurrentRenders;
                if (observed >= concurrent)
                {
                    break;
                }
            }
            while (Interlocked.CompareExchange(
                       ref maximumConcurrentRenders,
                       concurrent,
                       observed) != observed);

            Thread.SpinWait(10_000);
            Interlocked.Increment(ref renderCount);
            Interlocked.Decrement(ref concurrentRenders);
        }

        public void Stop()
        {
        }
    }

    private sealed class ManualTimeProvider : TimeProvider
    {
        private long timestamp;
        private readonly DateTimeOffset epoch =
            new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

        public override long TimestampFrequency => TimeSpan.TicksPerSecond;
        public override long GetTimestamp() => timestamp;
        public override DateTimeOffset GetUtcNow() => epoch.AddTicks(timestamp);

        public void Advance(TimeSpan duration) => timestamp += duration.Ticks;
    }
}
