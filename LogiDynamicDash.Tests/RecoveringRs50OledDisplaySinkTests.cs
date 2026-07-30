using LogiDynamicDash.Displays;
using LogiDynamicDash.Hidpp;
using LogiDynamicDash.Models;
using LogiDynamicDash.Runtime;

namespace LogiDynamicDash.Tests;

public sealed class RecoveringRs50OledDisplaySinkTests
{
    [Fact]
    public void MissingDeviceAtStartup_ReconnectsAndSendsLatestFrame()
    {
        ManualTimeProvider clock = new();
        Queue<FakeSession> sessions = new(
        [
            new FakeSession { OpenException = new IOException("missing") },
            new FakeSession()
        ]);
        List<DashboardOledState> states = [];
        RecoveringRs50OledDisplaySink sink = new(
            () => sessions.Dequeue(),
            new Rs50TelemetryFrameFormatter(
                new Rs50OledConfiguration(Rs50OledLayout.E)),
            states.Add,
            clock);

        sink.Initialize();
        sink.Render(ConnectedSnapshot(), DisplayMode.Normal);
        clock.Advance(TimeSpan.FromSeconds(2));
        sink.Flush();

        Assert.Contains(DashboardOledState.Waiting, states);
        Assert.Contains(DashboardOledState.Connected, states);
        Assert.Empty(sessions);
        sink.Stop();
        Assert.Equal(DashboardOledState.Stopped, states[^1]);
    }

    [Fact]
    public void SendFailure_IsContainedAndSchedulesReconnect()
    {
        FakeSession failing = new()
        {
            SendException = new IOException("disconnected")
        };
        List<DashboardOledState> states = [];
        RecoveringRs50OledDisplaySink sink = new(
            () => failing,
            new Rs50TelemetryFrameFormatter(
                new Rs50OledConfiguration(Rs50OledLayout.E)),
            states.Add);
        sink.Initialize();

        sink.Render(ConnectedSnapshot(), DisplayMode.Normal);

        Assert.True(failing.Disposed);
        Assert.Equal(DashboardOledState.Reconnecting, states[^1]);
        sink.Stop();
    }

    private static TelemetrySnapshot ConnectedSnapshot() =>
        new()
        {
            ConnectionState = "CONNECTED",
            IsOnTrack = true,
            SpeedMetersPerSecond = 0,
            Gear = 0,
            Rpm = 1000
        };

    private sealed class FakeSession : IRs50OledSession
    {
        internal Exception? OpenException { get; init; }
        internal Exception? SendException { get; init; }
        internal bool Disposed { get; private set; }

        public void Open()
        {
            if (OpenException is not null)
            {
                throw OpenException;
            }
        }

        public Rs50OledSendResult Send(Rs50OledFrame frame)
        {
            if (SendException is not null)
            {
                throw SendException;
            }

            return Rs50OledSendResult.Transmitted;
        }

        public void Dispose() =>
            Disposed = true;
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
