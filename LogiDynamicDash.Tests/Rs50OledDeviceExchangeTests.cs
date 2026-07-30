using LogiDynamicDash.Hidpp;
using LogiDynamicDash.Hidpp.Transport;
using LogiDynamicDash.Models;

namespace LogiDynamicDash.Tests;

public sealed class Rs50OledDeviceExchangeTests
{
    [Fact]
    public void Open_RequiresExactUniqueCollections()
    {
        FakeCollection shortCollection = ShortCollection();
        FakeCollection longCollection = LongCollection();

        using Rs50OledDeviceExchange exchange =
            Rs50OledDeviceExchange.Open(
                new FakeCatalog(shortCollection, longCollection));

        Assert.Equal(1, shortCollection.OpenCount);
        Assert.Equal(1, longCollection.OpenCount);
    }

    [Fact]
    public void Open_RejectsMissingDuplicateOrMalformedCollection()
    {
        Assert.Throws<InvalidOperationException>(
            () => Rs50OledDeviceExchange.Open(
                new FakeCatalog(LongCollection())));

        Assert.Throws<InvalidOperationException>(
            () => Rs50OledDeviceExchange.Open(
                new FakeCatalog(
                    ShortCollection(),
                    ShortCollection(),
                    LongCollection())));

        FakeCollection malformed = ShortCollection();
        malformed.MaximumOutputReportLength = 64;
        Assert.Throws<InvalidOperationException>(
            () => Rs50OledDeviceExchange.Open(
                new FakeCatalog(malformed, LongCollection())));
    }

    [Fact]
    public void Open_DisposesShortStreamWhenLongOpenFails()
    {
        FakeCollection shortCollection = ShortCollection();
        FakeCollection longCollection = LongCollection();
        longCollection.OpenException = new IOException("blocked");

        Assert.Throws<IOException>(
            () => Rs50OledDeviceExchange.Open(
                new FakeCatalog(shortCollection, longCollection)));

        Assert.True(shortCollection.Stream.Disposed);
    }

    [Fact]
    public void Discovery_WritesShortAndReadsMatchingLongResponse()
    {
        FakeCollection shortCollection = ShortCollection();
        FakeCollection longCollection = LongCollection();
        longCollection.Stream.Enqueue(UnrelatedResponse());
        byte[] expected = DiscoveryResponse();
        longCollection.Stream.Enqueue(expected);

        using Rs50OledDeviceExchange exchange =
            Rs50OledDeviceExchange.Open(
                new FakeCatalog(shortCollection, longCollection));
        byte[] actual =
            exchange.Exchange(Rs50OledProtocol.CreateDiscovery());

        Assert.Equal(expected, actual);
        Assert.Single(shortCollection.Stream.Writes);
        Assert.Empty(longCollection.Stream.Writes);
        Assert.Equal(
            [0x10, 0xFF, 0x00, 0x0A, 0x81, 0x30, 0x00],
            shortCollection.Stream.Writes[0]);
    }

    [Fact]
    public void Layout_WritesLongAndAcceptsOnlyMatchingResponse()
    {
        FakeCollection shortCollection = ShortCollection();
        FakeCollection longCollection = LongCollection();
        longCollection.Stream.Enqueue(UnrelatedResponse());
        byte[] expected = LayoutResponse();
        longCollection.Stream.Enqueue(expected);

        using Rs50OledDeviceExchange exchange =
            Rs50OledDeviceExchange.Open(
                new FakeCatalog(shortCollection, longCollection));
        Rs50OledTransaction transaction = Rs50OledProtocol.CreateLayout(
            0x12,
            new Rs50LayoutJFrame("SPEED", "0 KMH", "GEAR", "N"));
        byte[] actual = exchange.Exchange(transaction);

        Assert.Equal(expected, actual);
        Assert.Empty(shortCollection.Stream.Writes);
        Assert.Single(longCollection.Stream.Writes);
        Assert.Equal(transaction.Request, longCollection.Stream.Writes[0]);
    }

    [Fact]
    public void Exchange_AcceptsResponseAfterMoreThanSixteenReports()
    {
        FakeCollection shortCollection = ShortCollection();
        FakeCollection longCollection = LongCollection();
        for (int index = 0; index < 32; index++)
        {
            longCollection.Stream.Enqueue(UnrelatedResponse());
        }
        longCollection.Stream.Enqueue(DiscoveryResponse());

        using Rs50OledDeviceExchange exchange =
            Rs50OledDeviceExchange.Open(
                new FakeCatalog(shortCollection, longCollection));

        exchange.Exchange(Rs50OledProtocol.CreateDiscovery());

        Assert.Single(shortCollection.Stream.Writes);
        Assert.Equal(33, longCollection.Stream.ReadCount);
    }

    [Fact]
    public void Exchange_FailsAfterBoundedReportsWithoutRetryingWrite()
    {
        FakeCollection shortCollection = ShortCollection();
        FakeCollection longCollection = LongCollection();
        for (int index = 0; index < 256; index++)
        {
            longCollection.Stream.Enqueue(UnrelatedResponse());
        }

        using Rs50OledDeviceExchange exchange =
            Rs50OledDeviceExchange.Open(
                new FakeCatalog(shortCollection, longCollection));

        Rs50OledAcknowledgementTimeoutException exception =
            Assert.Throws<Rs50OledAcknowledgementTimeoutException>(
                () => exchange.Exchange(
                    Rs50OledProtocol.CreateDiscovery()));
        Assert.Equal(256, exception.ReportsRead);
        Assert.Single(shortCollection.Stream.Writes);
        Assert.Equal(256, longCollection.Stream.ReadCount);
    }

    [Fact]
    public void Exchange_RejectsShortReadAndDisposedUse()
    {
        FakeCollection shortCollection = ShortCollection();
        FakeCollection longCollection = LongCollection();
        longCollection.Stream.Enqueue(new byte[63]);
        Rs50OledDeviceExchange exchange = Rs50OledDeviceExchange.Open(
            new FakeCatalog(shortCollection, longCollection));

        Assert.Throws<IOException>(
            () => exchange.Exchange(Rs50OledProtocol.CreateDiscovery()));

        exchange.Dispose();
        Assert.True(shortCollection.Stream.Disposed);
        Assert.True(longCollection.Stream.Disposed);
        Assert.Throws<ObjectDisposedException>(
            () => exchange.Exchange(Rs50OledProtocol.CreateDiscovery()));
    }

    private static FakeCollection ShortCollection() =>
        new(
            @"\\?\hid#vid_046d&pid_c276&mi_01&col01#safe",
            usage: 0xFF430701,
            reportLength: 7);

    private static FakeCollection LongCollection() =>
        new(
            @"\\?\hid#vid_046d&pid_c276&mi_01&col03#safe",
            usage: 0xFF430704,
            reportLength: 64);

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

    private static byte[] UnrelatedResponse()
    {
        byte[] response = new byte[64];
        response[0] = 0x12;
        response[1] = 0xFF;
        response[2] = 0x0B;
        response[3] = 0x6E;
        return response;
    }

    private sealed class FakeCatalog(
        params IRs50HidCollection[] collections) : IRs50HidCatalog
    {
        public IReadOnlyList<IRs50HidCollection> Enumerate() =>
            collections;
    }

    private sealed class FakeCollection(
        string path,
        uint usage,
        int reportLength) : IRs50HidCollection
    {
        public int VendorId { get; set; } = 0x046D;

        public int ProductId { get; set; } = 0xC276;

        public string DevicePath { get; set; } = path;

        public IReadOnlySet<uint> Usages { get; set; } =
            new HashSet<uint> { usage };

        public int MaximumInputReportLength { get; set; } =
            reportLength;

        public int MaximumOutputReportLength { get; set; } =
            reportLength;

        public FakeStream Stream { get; } = new();

        public Exception? OpenException { get; set; }

        public int OpenCount { get; private set; }

        public IRs50HidStream Open()
        {
            OpenCount++;
            if (OpenException is not null)
            {
                throw OpenException;
            }

            return Stream;
        }
    }

    private sealed class FakeStream : IRs50HidStream
    {
        private readonly Queue<byte[]> reads = new();

        public List<byte[]> Writes { get; } = [];

        public int ReadCount { get; private set; }

        public bool Disposed { get; private set; }

        public void Enqueue(byte[] response) =>
            reads.Enqueue((byte[])response.Clone());

        public int Read(byte[] buffer)
        {
            ReadCount++;
            byte[] response = reads.Dequeue();
            response.CopyTo(buffer, 0);
            return response.Length;
        }

        public void Write(byte[] report) =>
            Writes.Add((byte[])report.Clone());

        public void Dispose() =>
            Disposed = true;
    }
}
