using LogiDynamicDash.Hidpp;
using LogiDynamicDash.Models;

namespace LogiDynamicDash.Tests;

public sealed class Rs50OledSessionTests
{
    [Fact]
    public void Open_DiscoversFeatureExactlyOnce()
    {
        FakeExchange exchange = new();
        exchange.Enqueue(DiscoveryResponse());
        using Rs50OledSession session = new(exchange);

        session.Open();

        Assert.Single(exchange.Transactions);
        Assert.Equal(
            Rs50OledTransactionKind.DiscoverDisplayFeature,
            exchange.Transactions[0].Kind);
        Assert.Throws<InvalidOperationException>(() => session.Open());
    }

    [Fact]
    public void Send_RequiresOpenSession()
    {
        using Rs50OledSession session = new(new FakeExchange());

        Assert.Throws<InvalidOperationException>(
            () => session.Send(new Rs50LayoutAFrame()));
    }

    [Fact]
    public void Send_TransmitsAnyTypedLayoutAndValidatesAcknowledgement()
    {
        FakeExchange exchange = new();
        exchange.Enqueue(DiscoveryResponse());
        exchange.Enqueue(LayoutResponse());
        using Rs50OledSession session = new(exchange);
        session.Open();

        Rs50OledSendResult result =
            session.Send(new Rs50LayoutEFrame(
                Rs50GaugeLevel.FromRatio(0.5),
                Rs50GaugeLevel.FromRatio(0.25),
                "99 KMH",
                "3"));

        Assert.Equal(Rs50OledSendResult.Transmitted, result);
        Assert.Equal(2, exchange.Transactions.Count);
        Assert.Equal(
            Rs50OledTransactionKind.SetLayoutE,
            exchange.Transactions[1].Kind);
    }

    [Fact]
    public void Send_SuppressesLastAcknowledgedFrame()
    {
        FakeExchange exchange = new();
        exchange.Enqueue(DiscoveryResponse());
        exchange.Enqueue(LayoutResponse());
        using Rs50OledSession session = new(exchange);
        session.Open();
        Rs50LayoutJFrame frame =
            new("SPEED", "0 KMH", "GEAR", "N");

        Assert.Equal(
            Rs50OledSendResult.Transmitted,
            session.Send(frame));
        Assert.Equal(
            Rs50OledSendResult.Unchanged,
            session.Send(frame));
        Assert.Equal(2, exchange.Transactions.Count);
    }

    [Fact]
    public void Send_RateLimitsChangedFramesToFiveHertz()
    {
        ManualTimeProvider clock = new();
        FakeExchange exchange = new();
        exchange.Enqueue(DiscoveryResponse());
        exchange.Enqueue(LayoutResponse());
        exchange.Enqueue(LayoutResponse());
        using Rs50OledSession session = new(exchange, clock);
        session.Open();

        Assert.Equal(
            Rs50OledSendResult.Transmitted,
            session.Send(new Rs50LayoutFFrame("N", "000")));

        clock.Advance(TimeSpan.FromMilliseconds(199));
        Assert.Equal(
            Rs50OledSendResult.RateLimited,
            session.Send(new Rs50LayoutFFrame("1", "001")));
        Assert.Equal(2, exchange.Transactions.Count);

        clock.Advance(TimeSpan.FromMilliseconds(1));
        Assert.Equal(
            Rs50OledSendResult.Transmitted,
            session.Send(new Rs50LayoutFFrame("1", "001")));
        Assert.Equal(3, exchange.Transactions.Count);
    }

    [Fact]
    public void ProtocolFailure_PermanentlyFaultsSession()
    {
        FakeExchange exchange = new();
        exchange.Enqueue(DiscoveryResponse());
        byte[] invalidAcknowledgement = LayoutResponse();
        invalidAcknowledgement[63] = 1;
        exchange.Enqueue(invalidAcknowledgement);
        using Rs50OledSession session = new(exchange);
        session.Open();

        Assert.Throws<Rs50OledProtocolException>(
            () => session.Send(new Rs50LayoutAFrame()));
        Assert.Throws<InvalidOperationException>(
            () => session.Send(new Rs50LayoutAFrame()));
        Assert.Equal(2, exchange.Transactions.Count);
    }

    [Fact]
    public void DiscoveryFailure_PermanentlyFaultsSession()
    {
        FakeExchange exchange = new();
        exchange.Exception =
            new IOException("The device is unavailable.");
        using Rs50OledSession session = new(exchange);

        Assert.Throws<IOException>(() => session.Open());
        exchange.Exception = null;
        Assert.Throws<InvalidOperationException>(() => session.Open());
        Assert.Single(exchange.Transactions);
    }

    [Fact]
    public void Dispose_ClosesExchangeAndRejectsFurtherUse()
    {
        FakeExchange exchange = new();
        exchange.Enqueue(DiscoveryResponse());
        Rs50OledSession session = new(exchange);
        session.Open();

        session.Dispose();
        session.Dispose();

        Assert.True(exchange.Disposed);
        Assert.Throws<ObjectDisposedException>(() => session.Open());
        Assert.Throws<ObjectDisposedException>(
            () => session.Send(new Rs50LayoutAFrame()));
    }

    private static byte[] DiscoveryResponse()
    {
        byte[] response = new byte[64];
        response[0] = 0x12;
        response[1] = 0xFF;
        response[3] = 0x0A;
        response[4] = 0x12;
        return response;
    }

    private static byte[] LayoutResponse()
    {
        byte[] response = new byte[64];
        response[0] = 0x12;
        response[1] = 0xFF;
        response[2] = 0x12;
        response[3] = 0x3A;
        return response;
    }

    private sealed class FakeExchange : IRs50OledExchange
    {
        private readonly Queue<byte[]> responses = new();

        public List<Rs50OledTransaction> Transactions { get; } = [];

        public Exception? Exception { get; set; }

        public bool Disposed { get; private set; }

        public void Enqueue(byte[] response) =>
            responses.Enqueue((byte[])response.Clone());

        public byte[] Exchange(Rs50OledTransaction transaction)
        {
            Transactions.Add(transaction);
            if (Exception is not null)
            {
                throw Exception;
            }

            return responses.Dequeue();
        }

        public void Dispose() =>
            Disposed = true;
    }

    private sealed class ManualTimeProvider : TimeProvider
    {
        private long timestamp;

        public override long TimestampFrequency => TimeSpan.TicksPerSecond;

        public override long GetTimestamp() => timestamp;

        public void Advance(TimeSpan duration) =>
            timestamp += duration.Ticks;
    }
}
