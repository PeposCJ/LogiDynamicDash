using LogiDynamicDash.Displays;
using LogiDynamicDash.Hidpp;
using LogiDynamicDash.Models;

namespace LogiDynamicDash.Tests;

public sealed class Rs50OledDisplaySinkTests
{
    [Fact]
    public void Initialize_OpensInjectedSessionWithoutHardwareDependency()
    {
        FakeSession session = new();
        Rs50OledDisplaySink sink = CreateSink(session);

        sink.Initialize();

        Assert.Equal(1, session.OpenCount);
        Assert.Equal(OledDeviceState.Active, sink.State);
        sink.Stop();
        Assert.True(session.Disposed);
        Assert.Equal(OledDeviceState.Stopped, sink.State);
    }

    [Fact]
    public void InitializeFailure_DisposesCreatedSession()
    {
        FakeSession session = new()
        {
            OpenException = new IOException("blocked")
        };
        Rs50OledDisplaySink sink = CreateSink(session);

        Assert.Throws<IOException>(() => sink.Initialize());
        Assert.True(session.Disposed);
        Assert.Equal(OledDeviceState.Faulted, sink.State);
    }

    [Fact]
    public void Render_FormatsAndSendsStationaryTelemetry()
    {
        FakeSession session = new();
        Rs50OledDisplaySink sink = CreateSink(session);
        sink.Initialize();
        TelemetrySnapshot snapshot = ConnectedSnapshot();

        sink.Render(snapshot, DisplayMode.Normal);

        Rs50LayoutEFrame frame =
            Assert.IsType<Rs50LayoutEFrame>(Assert.Single(session.Frames));
        Assert.Equal("0 KMH", frame.LeftText);
        Assert.Equal("N", frame.RightText);
    }

    [Theory]
    [InlineData(0.5001f)]
    [InlineData(-0.1f)]
    [InlineData(float.NaN)]
    public void Render_FailsBeforeSendWhenOnTrackTelemetryIsNotStationary(
        float speed)
    {
        FakeSession session = new();
        Rs50OledDisplaySink sink = CreateSink(session);
        sink.Initialize();
        TelemetrySnapshot snapshot = ConnectedSnapshot();
        snapshot.SpeedMetersPerSecond = speed;

        Assert.Throws<InvalidOperationException>(
            () => sink.Render(snapshot, DisplayMode.Normal));
        Assert.Empty(session.Frames);
        Assert.Equal(OledDeviceState.Faulted, sink.State);
    }

    [Fact]
    public void ExplicitLowSpeedEnvelope_AcceptsBelowAndRejectsAboveLimit()
    {
        FakeSession session = new();
        Rs50OledDisplaySink sink = new(
            () => session,
            new Rs50TelemetryFrameFormatter(
                new Rs50OledConfiguration(Rs50OledLayout.E)),
            maximumPermittedSpeedMetersPerSecond: 20f / 3.6f);
        sink.Initialize();
        TelemetrySnapshot snapshot = ConnectedSnapshot();
        snapshot.SpeedMetersPerSecond = 5;

        sink.Render(snapshot, DisplayMode.Normal);

        snapshot.SpeedMetersPerSecond = 6;
        Assert.Throws<InvalidOperationException>(
            () => sink.Render(snapshot, DisplayMode.Normal));
        Assert.Single(session.Frames);
        Assert.Equal(OledDeviceState.Faulted, sink.State);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(float.NaN)]
    public void InvalidSpeedEnvelope_IsRejected(float maximumSpeed)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new Rs50OledDisplaySink(
                () => new FakeSession(),
                new Rs50TelemetryFrameFormatter(
                    new Rs50OledConfiguration(Rs50OledLayout.E)),
                maximumSpeed));
    }

    [Fact]
    public void RenderFailure_FaultsAndDoesNotReopenOrRetry()
    {
        FakeSession session = new()
        {
            SendException = new IOException("injected")
        };
        Rs50OledDisplaySink sink = CreateSink(session);
        sink.Initialize();

        Assert.Throws<IOException>(
            () => sink.Render(ConnectedSnapshot(), DisplayMode.Normal));
        Assert.Equal(OledDeviceState.Faulted, sink.State);
        Assert.Throws<InvalidOperationException>(
            () => sink.Render(ConnectedSnapshot(), DisplayMode.Normal));
        Assert.Throws<InvalidOperationException>(() => sink.Initialize());
        Assert.Single(session.Frames);
    }

    [Fact]
    public void Stop_IsIdempotentAndRejectsFurtherRendering()
    {
        FakeSession session = new();
        Rs50OledDisplaySink sink = CreateSink(session);
        sink.Initialize();

        sink.Stop();
        sink.Stop();

        Assert.Equal(1, session.DisposeCount);
        Assert.Throws<InvalidOperationException>(
            () => sink.Render(ConnectedSnapshot(), DisplayMode.Normal));
    }

    private static Rs50OledDisplaySink CreateSink(FakeSession session) =>
        new(
            () => session,
            new Rs50TelemetryFrameFormatter(
                new Rs50OledConfiguration(Rs50OledLayout.E)));

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
        public List<Rs50OledFrame> Frames { get; } = [];

        public Exception? OpenException { get; set; }
        public Exception? SendException { get; set; }

        public int OpenCount { get; private set; }

        public int DisposeCount { get; private set; }

        public bool Disposed => DisposeCount != 0;

        public void Open()
        {
            OpenCount++;
            if (OpenException is not null)
            {
                throw OpenException;
            }
        }

        public Rs50OledSendResult Send(Rs50OledFrame frame)
        {
            Frames.Add(frame);
            if (SendException is not null)
            {
                throw SendException;
            }

            return Rs50OledSendResult.Transmitted;
        }

        public void Dispose() =>
            DisposeCount++;
    }
}
