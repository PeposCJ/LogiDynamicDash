using LogiDynamicDash.Hidpp;
using LogiDynamicDash.Models;
using Rs50SharedHidppTransport;

namespace LogiDynamicDash.Tests;

public sealed class Rs50HidppDeviceExchangeTests
{
    [Fact]
    public void OpenSessionAndSend_RoutesOnlyAcrossCol01AndCol03()
    {
        FakeFixture fixture = FakeFixture.Valid();
        fixture.VeryLongStream.Reads.Enqueue(ValidDiscoveryResponse());
        fixture.VeryLongStream.Reads.Enqueue(ValidLayoutJAcknowledgement());

        using Rs50HidppDeviceExchange exchange =
            Rs50HidppDeviceExchange.Open(fixture.Catalog);
        using Rs50SharedHidppDisplaySession session = new(exchange);

        session.Open();
        session.Send(new("SPEED", "0 KMH", "GEAR", "N"));

        Assert.Single(fixture.ShortStream.Writes);
        Assert.Equal(
            [0x10, 0xFF, 0x00, 0x0A, 0x81, 0x30, 0x00],
            fixture.ShortStream.Writes[0]);
        Assert.Single(fixture.VeryLongStream.Writes);
        Assert.Equal(0x12, fixture.VeryLongStream.Writes[0][0]);
        Assert.Equal(0x3A, fixture.VeryLongStream.Writes[0][3]);
        Assert.Equal(0x09, fixture.VeryLongStream.Writes[0][4]);
    }

    [Fact]
    public void Exchange_SkipsUnrelatedReportsBeforeExactResponse()
    {
        FakeFixture fixture = FakeFixture.Valid();
        fixture.VeryLongStream.Reads.Enqueue(
            Report(0x12, 0x01, 0x03, 0x00));
        fixture.VeryLongStream.Reads.Enqueue(
            Report(0x12, 0xFF, 0x17, 0x00));
        fixture.VeryLongStream.Reads.Enqueue(ValidDiscoveryResponse());

        using Rs50HidppDeviceExchange exchange =
            Rs50HidppDeviceExchange.Open(fixture.Catalog);

        byte[] response =
            exchange.Exchange(
                Rs50HidppDisplayProtocol.CreateDiscovery());

        Assert.Equal(0x12, response[4]);
        Assert.Equal(3, fixture.VeryLongStream.ReadCount);
    }

    [Fact]
    public void Exchange_ReturnsMatchingHidppErrorForProtocolRejection()
    {
        FakeFixture fixture = FakeFixture.Valid();
        byte[] error = new byte[64];
        error[0] = 0x12;
        error[1] = 0xFF;
        error[2] = 0xFF;
        error[3] = 0x0A;
        error[4] = 0x00;
        error[5] = 0x0A;
        error[6] = 0x09;
        fixture.VeryLongStream.Reads.Enqueue(error);

        using Rs50HidppDeviceExchange exchange =
            Rs50HidppDeviceExchange.Open(fixture.Catalog);

        byte[] response =
            exchange.Exchange(
                Rs50HidppDisplayProtocol.CreateDiscovery());

        Assert.Equal(error, response);
    }

    [Fact]
    public void Exchange_RejectsShortVeryLongRead()
    {
        FakeFixture fixture = FakeFixture.Valid();
        fixture.VeryLongStream.Reads.Enqueue(new byte[63]);

        using Rs50HidppDeviceExchange exchange =
            Rs50HidppDeviceExchange.Open(fixture.Catalog);

        Assert.Throws<IOException>(
            () => exchange.Exchange(
                Rs50HidppDisplayProtocol.CreateDiscovery()));
    }

    [Fact]
    public void Exchange_StopsAfterSixteenUnrelatedReports()
    {
        FakeFixture fixture = FakeFixture.Valid();
        for (int index = 0; index < 16; index++)
        {
            fixture.VeryLongStream.Reads.Enqueue(
                Report(0x12, 0x01, 0x03, 0x00));
        }

        using Rs50HidppDeviceExchange exchange =
            Rs50HidppDeviceExchange.Open(fixture.Catalog);

        Assert.Throws<IOException>(
            () => exchange.Exchange(
                Rs50HidppDisplayProtocol.CreateDiscovery()));
        Assert.Equal(16, fixture.VeryLongStream.ReadCount);
    }

    [Fact]
    public void Dispose_DisposesBothStreamsAndBlocksExchange()
    {
        FakeFixture fixture = FakeFixture.Valid();
        Rs50HidppDeviceExchange exchange =
            Rs50HidppDeviceExchange.Open(fixture.Catalog);

        exchange.Dispose();
        exchange.Dispose();

        Assert.True(fixture.ShortStream.Disposed);
        Assert.True(fixture.VeryLongStream.Disposed);
        Assert.Throws<ObjectDisposedException>(
            () => exchange.Exchange(
                Rs50HidppDisplayProtocol.CreateDiscovery()));
    }

    [Fact]
    public void Open_WhenSecondStreamFails_DisposesFirstStream()
    {
        FakeFixture fixture = FakeFixture.Valid();
        fixture.VeryLongCollection.OpenFailure =
            new IOException("Simulated COL03 open failure.");

        Assert.Throws<IOException>(
            () => Rs50HidppDeviceExchange.Open(fixture.Catalog));

        Assert.True(fixture.ShortStream.Disposed);
    }

    [Theory]
    [InlineData(CollectionMutation.RemoveShort)]
    [InlineData(CollectionMutation.RemoveVeryLong)]
    [InlineData(CollectionMutation.DuplicateShort)]
    [InlineData(CollectionMutation.DuplicateVeryLong)]
    [InlineData(CollectionMutation.WrongShortVendor)]
    [InlineData(CollectionMutation.WrongVeryLongProduct)]
    [InlineData(CollectionMutation.WrongShortUsage)]
    [InlineData(CollectionMutation.ExtraVeryLongUsage)]
    [InlineData(CollectionMutation.WrongShortInputLength)]
    [InlineData(CollectionMutation.WrongShortOutputLength)]
    [InlineData(CollectionMutation.WrongVeryLongInputLength)]
    [InlineData(CollectionMutation.WrongVeryLongOutputLength)]
    [InlineData(CollectionMutation.WrongShortPath)]
    [InlineData(CollectionMutation.WrongVeryLongPath)]
    [InlineData(CollectionMutation.DifferentPhysicalInstance)]
    public void Open_RejectsAnyIdentityOrShapeMismatch(
        CollectionMutation mutation)
    {
        FakeFixture fixture = FakeFixture.Valid();
        fixture.Apply(mutation);

        Assert.Throws<InvalidOperationException>(
            () => Rs50HidppDeviceExchange.Open(fixture.Catalog));

        Assert.Equal(0, fixture.ShortCollection.OpenCount);
        Assert.Equal(0, fixture.VeryLongCollection.OpenCount);
        Assert.Empty(fixture.ShortStream.Writes);
        Assert.Empty(fixture.VeryLongStream.Writes);
    }

    public enum CollectionMutation
    {
        RemoveShort,
        RemoveVeryLong,
        DuplicateShort,
        DuplicateVeryLong,
        WrongShortVendor,
        WrongVeryLongProduct,
        WrongShortUsage,
        ExtraVeryLongUsage,
        WrongShortInputLength,
        WrongShortOutputLength,
        WrongVeryLongInputLength,
        WrongVeryLongOutputLength,
        WrongShortPath,
        WrongVeryLongPath,
        DifferentPhysicalInstance
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

    private static byte[] ValidLayoutJAcknowledgement()
    {
        byte[] response = new byte[64];
        response[0] = 0x12;
        response[1] = 0xFF;
        response[2] = 0x12;
        response[3] = 0x3A;
        return response;
    }

    private static byte[] Report(
        byte reportId,
        byte device,
        byte feature,
        byte function)
    {
        byte[] report = new byte[64];
        report[0] = reportId;
        report[1] = device;
        report[2] = feature;
        report[3] = function;
        return report;
    }

    private sealed class FakeFixture
    {
        private const string ShortPath =
            @"\\?\hid#vid_046d&pid_c276&mi_01&col01#8&abc#{guid}";
        private const string VeryLongPath =
            @"\\?\hid#vid_046d&pid_c276&mi_01&col03#8&abc#{guid}";

        private FakeFixture(
            FakeCatalog catalog,
            FakeCollection shortCollection,
            FakeCollection veryLongCollection,
            FakeStream shortStream,
            FakeStream veryLongStream)
        {
            Catalog = catalog;
            ShortCollection = shortCollection;
            VeryLongCollection = veryLongCollection;
            ShortStream = shortStream;
            VeryLongStream = veryLongStream;
        }

        public FakeCatalog Catalog { get; }

        public FakeCollection ShortCollection { get; }

        public FakeCollection VeryLongCollection { get; }

        public FakeStream ShortStream { get; }

        public FakeStream VeryLongStream { get; }

        public static FakeFixture Valid()
        {
            FakeStream shortStream = new();
            FakeStream veryLongStream = new();
            FakeCollection shortCollection = new(
                0x046D,
                0xC276,
                ShortPath,
                new HashSet<uint> { 0xFF430701 },
                7,
                7,
                shortStream);
            FakeCollection veryLongCollection = new(
                0x046D,
                0xC276,
                VeryLongPath,
                new HashSet<uint> { 0xFF430704 },
                64,
                64,
                veryLongStream);
            FakeCatalog catalog =
                new([shortCollection, veryLongCollection]);

            return new(
                catalog,
                shortCollection,
                veryLongCollection,
                shortStream,
                veryLongStream);
        }

        public void Apply(CollectionMutation mutation)
        {
            switch (mutation)
            {
                case CollectionMutation.RemoveShort:
                    Catalog.Collections.Remove(ShortCollection);
                    break;
                case CollectionMutation.RemoveVeryLong:
                    Catalog.Collections.Remove(VeryLongCollection);
                    break;
                case CollectionMutation.DuplicateShort:
                    Catalog.Collections.Add(ShortCollection.Clone());
                    break;
                case CollectionMutation.DuplicateVeryLong:
                    Catalog.Collections.Add(VeryLongCollection.Clone());
                    break;
                case CollectionMutation.WrongShortVendor:
                    ShortCollection.VendorId = 0x0001;
                    break;
                case CollectionMutation.WrongVeryLongProduct:
                    VeryLongCollection.ProductId = 0x0001;
                    break;
                case CollectionMutation.WrongShortUsage:
                    ShortCollection.Usages = new HashSet<uint> { 0xFF430702 };
                    break;
                case CollectionMutation.ExtraVeryLongUsage:
                    VeryLongCollection.Usages =
                        new HashSet<uint> { 0xFF430704, 0xFF430701 };
                    break;
                case CollectionMutation.WrongShortInputLength:
                    ShortCollection.MaximumInputReportLength = 8;
                    break;
                case CollectionMutation.WrongShortOutputLength:
                    ShortCollection.MaximumOutputReportLength = 8;
                    break;
                case CollectionMutation.WrongVeryLongInputLength:
                    VeryLongCollection.MaximumInputReportLength = 65;
                    break;
                case CollectionMutation.WrongVeryLongOutputLength:
                    VeryLongCollection.MaximumOutputReportLength = 65;
                    break;
                case CollectionMutation.WrongShortPath:
                    ShortCollection.DevicePath =
                        ShortPath.Replace("col01", "col02");
                    break;
                case CollectionMutation.WrongVeryLongPath:
                    VeryLongCollection.DevicePath =
                        VeryLongPath.Replace("col03", "col02");
                    break;
                case CollectionMutation.DifferentPhysicalInstance:
                    VeryLongCollection.DevicePath =
                        VeryLongPath.Replace("8&abc", "9&other");
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(mutation));
            }
        }
    }

    private sealed class FakeCatalog(
        IEnumerable<IRs50HidCollection> collections)
        : IRs50HidCollectionCatalog
    {
        public List<IRs50HidCollection> Collections { get; } =
            [.. collections];

        public IReadOnlyList<IRs50HidCollection> Enumerate() =>
            Collections;
    }

    private sealed class FakeCollection(
        int vendorId,
        int productId,
        string devicePath,
        IReadOnlySet<uint> usages,
        int maximumInputReportLength,
        int maximumOutputReportLength,
        FakeStream stream) : IRs50HidCollection
    {
        public int VendorId { get; set; } = vendorId;

        public int ProductId { get; set; } = productId;

        public string DevicePath { get; set; } = devicePath;

        public IReadOnlySet<uint> Usages { get; set; } = usages;

        public int MaximumInputReportLength { get; set; } =
            maximumInputReportLength;

        public int MaximumOutputReportLength { get; set; } =
            maximumOutputReportLength;

        public Exception? OpenFailure { get; set; }

        public int OpenCount { get; private set; }

        public IRs50HidStream Open()
        {
            OpenCount++;
            if (OpenFailure is not null)
            {
                throw OpenFailure;
            }

            return stream;
        }

        public FakeCollection Clone() =>
            new(
                VendorId,
                ProductId,
                DevicePath,
                new HashSet<uint>(Usages),
                MaximumInputReportLength,
                MaximumOutputReportLength,
                new FakeStream());
    }

    private sealed class FakeStream : IRs50HidStream
    {
        public Queue<byte[]> Reads { get; } = [];

        public List<byte[]> Writes { get; } = [];

        public int ReadCount { get; private set; }

        public bool Disposed { get; private set; }

        public int Read(byte[] buffer)
        {
            ReadCount++;
            byte[] report = Reads.Dequeue();
            report.CopyTo(buffer, 0);
            return report.Length;
        }

        public void Write(byte[] report) =>
            Writes.Add((byte[])report.Clone());

        public void Dispose() =>
            Disposed = true;
    }
}
