using LogiDynamicDash.Hidpp;
using LogiDynamicDash.Models;
using LogiDynamicDash.Native;

namespace LogiDynamicDash.Tests;

public sealed class Rs50SharedHidppDisplaySessionTests
{
    [Fact]
    public void OpenSendClose_UsesOnlyDiscoveryAndLayoutJ()
    {
        RecordingExchange exchange = new();
        exchange.Responses.Enqueue(ValidDiscoveryResponse());
        exchange.Responses.Enqueue(
            Rs50HidppDisplayProtocolTests
                .ValidLayoutJAcknowledgement());
        Rs50SharedHidppDisplaySession session = new(exchange);

        session.Open();
        Rs50FrameOutcome outcome =
            session.Send(new("SPEED", "0 KMH", "GEAR", "N"));
        session.Close();

        Assert.True(outcome.Transmitted);
        Assert.Equal(
            [
                Rs50HidppDisplayTransactionKind.DiscoverDisplayFeature,
                Rs50HidppDisplayTransactionKind.SetLayoutJ
            ],
            exchange.Kinds);
        Assert.Equal(2, exchange.Requests.Count);
    }

    [Fact]
    public void SendBeforeOpen_IsRejectedWithoutExchange()
    {
        RecordingExchange exchange = new();
        Rs50SharedHidppDisplaySession session = new(exchange);

        Assert.Throws<InvalidOperationException>(
            () => session.Send(new("", "", "", "")));

        Assert.Empty(exchange.Requests);
    }

    [Fact]
    public void RepeatedOpen_IsRejected()
    {
        RecordingExchange exchange = new();
        exchange.Responses.Enqueue(ValidDiscoveryResponse());
        Rs50SharedHidppDisplaySession session = new(exchange);

        session.Open();

        Assert.Throws<InvalidOperationException>(session.Open);
        Assert.Single(exchange.Requests);
    }

    [Fact]
    public void IdenticalFrame_IsSuppressed()
    {
        RecordingExchange exchange = OpenReadyExchange();
        Rs50SharedHidppDisplaySession session = new(exchange);
        LayoutJFrame frame = new("SPEED", "0 KMH", "GEAR", "N");

        session.Open();
        Rs50FrameOutcome first = session.Send(frame);
        Rs50FrameOutcome second = session.Send(frame);

        Assert.True(first.Transmitted);
        Assert.True(second.Unchanged);
        Assert.Equal(2, exchange.Requests.Count);
    }

    [Fact]
    public void ChangedFrameInsideTwoHundredMilliseconds_IsRateLimited()
    {
        ManualTimeProvider time = new();
        RecordingExchange exchange = OpenReadyExchange();
        Rs50SharedHidppDisplaySession session = new(exchange, time);

        session.Open();
        session.Send(new("SPEED", "0 KMH", "GEAR", "N"));
        time.Advance(TimeSpan.FromMilliseconds(199));

        Rs50FrameOutcome outcome =
            session.Send(new("SPEED", "1 KMH", "GEAR", "1"));

        Assert.True(outcome.RateLimited);
        Assert.Equal(2, exchange.Requests.Count);
    }

    [Fact]
    public void ChangedFrameAtTwoHundredMilliseconds_IsTransmitted()
    {
        ManualTimeProvider time = new();
        RecordingExchange exchange = OpenReadyExchange();
        exchange.Responses.Enqueue(
            Rs50HidppDisplayProtocolTests
                .ValidLayoutJAcknowledgement());
        Rs50SharedHidppDisplaySession session = new(exchange, time);

        session.Open();
        session.Send(new("SPEED", "0 KMH", "GEAR", "N"));
        time.Advance(TimeSpan.FromMilliseconds(200));

        Rs50FrameOutcome outcome =
            session.Send(new("SPEED", "1 KMH", "GEAR", "1"));

        Assert.True(outcome.Transmitted);
        Assert.Equal(3, exchange.Requests.Count);
    }

    [Fact]
    public void ProtocolFailure_FaultsSessionAndBlocksFurtherExchange()
    {
        RecordingExchange exchange = new();
        exchange.Responses.Enqueue(ValidDiscoveryResponse());
        exchange.Responses.Enqueue(new byte[64]);
        Rs50SharedHidppDisplaySession session = new(exchange);

        session.Open();
        Assert.Throws<Rs50HidppProtocolException>(
            () => session.Send(new("SPEED", "0 KMH", "GEAR", "N")));

        Assert.Throws<InvalidOperationException>(
            () => session.Send(new("SPEED", "1 KMH", "GEAR", "1")));
        Assert.Equal(2, exchange.Requests.Count);
    }

    [Fact]
    public void DiscoveryFailure_FaultsSession()
    {
        RecordingExchange exchange = new();
        exchange.Responses.Enqueue(new byte[64]);
        Rs50SharedHidppDisplaySession session = new(exchange);

        Assert.Throws<Rs50HidppProtocolException>(session.Open);
        Assert.Throws<InvalidOperationException>(session.Open);
        Assert.Single(exchange.Requests);
    }

    [Fact]
    public void Dispose_DisposesExchangeAndBlocksUse()
    {
        RecordingExchange exchange = new();
        Rs50SharedHidppDisplaySession session = new(exchange);

        session.Dispose();
        session.Dispose();

        Assert.True(exchange.Disposed);
        Assert.Throws<ObjectDisposedException>(session.Open);
    }

    private static RecordingExchange OpenReadyExchange()
    {
        RecordingExchange exchange = new();
        exchange.Responses.Enqueue(ValidDiscoveryResponse());
        exchange.Responses.Enqueue(
            Rs50HidppDisplayProtocolTests
                .ValidLayoutJAcknowledgement());
        return exchange;
    }

    private static byte[] ValidDiscoveryResponse()
    {
        byte[] response = new byte[64];
        response[0] = 0x12;
        response[1] = 0xFF;
        response[2] = 0x00;
        response[3] = 0x0A;
        response[4] = 0x12;
        return response;
    }

    private sealed class RecordingExchange
        : IRs50HidppDisplayExchange
    {
        public Queue<byte[]> Responses { get; } = [];

        public List<byte[]> Requests { get; } = [];

        public List<Rs50HidppDisplayTransactionKind> Kinds { get; } = [];

        public bool Disposed { get; private set; }

        public byte[] Exchange(Rs50HidppDisplayTransaction transaction)
        {
            Requests.Add(transaction.Request.ToArray());
            Kinds.Add(transaction.Kind);
            return Responses.Dequeue();
        }

        public void Dispose() =>
            Disposed = true;
    }

    private sealed class ManualTimeProvider : TimeProvider
    {
        private long timestamp;

        public override long TimestampFrequency =>
            TimeSpan.TicksPerSecond;

        public override long GetTimestamp() =>
            timestamp;

        public void Advance(TimeSpan duration) =>
            timestamp += duration.Ticks;
    }
}
