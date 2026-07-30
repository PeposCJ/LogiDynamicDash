using System.Diagnostics;

namespace LogiDynamicDash.Hidpp.Transport;

/// <summary>
/// Strict physical adapter for the two confirmed RS50 HID++ collections.
/// It accepts only transactions created by <see cref="Rs50OledProtocol"/>.
/// </summary>
internal sealed class Rs50OledDeviceExchange : IRs50OledExchange
{
    private const uint ShortCollectionUsage = 0xFF430701;
    private const uint VeryLongCollectionUsage = 0xFF430704;
    private const int MaximumReportsPerExchange = 256;
    private static readonly TimeSpan MaximumResponseWait =
        TimeSpan.FromMilliseconds(500);

    private readonly IRs50HidStream shortStream;
    private readonly IRs50HidStream veryLongStream;
    private readonly object synchronization = new();
    private bool disposed;

    private Rs50OledDeviceExchange(
        IRs50HidStream shortStream,
        IRs50HidStream veryLongStream)
    {
        this.shortStream = shortStream;
        this.veryLongStream = veryLongStream;
    }

    internal static Rs50OledDeviceExchange Open() =>
        Open(
            new HidSharpRs50HidCatalog(
                ConfirmedOledDeviceIdentity.Rs50),
            ConfirmedOledDeviceIdentity.Rs50);

    internal static Rs50OledDeviceExchange Open(IRs50HidCatalog catalog)
        => Open(catalog, ConfirmedOledDeviceIdentity.Rs50);

    private static Rs50OledDeviceExchange Open(
        IRs50HidCatalog catalog,
        ConfirmedOledDeviceIdentity identity)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        IReadOnlyList<IRs50HidCollection> collections =
            catalog.Enumerate();

        IRs50HidCollection shortCollection = SelectUniqueCollection(
            collections,
            identity,
            pathMarker: "mi_01&col01",
            ShortCollectionUsage,
            expectedReportLength: Rs50OledProtocol.ShortReportLength);
        IRs50HidCollection veryLongCollection = SelectUniqueCollection(
            collections,
            identity,
            pathMarker: "mi_01&col03",
            VeryLongCollectionUsage,
            expectedReportLength: Rs50OledProtocol.VeryLongReportLength);

        IRs50HidStream? openedShort = null;
        try
        {
            openedShort = shortCollection.Open();
            IRs50HidStream openedVeryLong = veryLongCollection.Open();
            return new Rs50OledDeviceExchange(
                openedShort,
                openedVeryLong);
        }
        catch
        {
            openedShort?.Dispose();
            throw;
        }
    }

    public byte[] Exchange(Rs50OledTransaction transaction)
    {
        ArgumentNullException.ThrowIfNull(transaction);

        lock (synchronization)
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            byte[] request = transaction.Request.ToArray();
            ValidateCanonicalTransaction(transaction.Kind, request);

            IRs50HidStream output =
                transaction.Kind ==
                Rs50OledTransactionKind.DiscoverDisplayFeature
                    ? shortStream
                    : veryLongStream;

            output.Write(request);
            return ReadMatchingResponse(transaction.Kind, request);
        }
    }

    public void Dispose()
    {
        lock (synchronization)
        {
            if (disposed)
            {
                return;
            }

            disposed = true;
            try
            {
                shortStream.Dispose();
            }
            finally
            {
                veryLongStream.Dispose();
            }
        }
    }

    private byte[] ReadMatchingResponse(
        Rs50OledTransactionKind kind,
        byte[] request)
    {
        long started = Stopwatch.GetTimestamp();
        int reportsRead = 0;
        for (int index = 0; index < MaximumReportsPerExchange; index++)
        {
            byte[] response =
                new byte[Rs50OledProtocol.VeryLongReportLength];
            int bytesRead = veryLongStream.Read(response);
            reportsRead++;
            if (bytesRead != response.Length)
            {
                throw new IOException(
                    $"Expected {response.Length} HID++ response bytes, " +
                    $"received {bytesRead}.");
            }

            if (Matches(kind, request, response))
            {
                return response;
            }

            if (Stopwatch.GetElapsedTime(started) >= MaximumResponseWait)
            {
                break;
            }
        }

        throw new IOException(
            "No matching Display Game Data response was received within " +
            $"{reportsRead} reports and the bounded response window.");
    }

    private static bool Matches(
        Rs50OledTransactionKind kind,
        byte[] request,
        byte[] response)
    {
        if (response[0] != 0x12 || response[1] != 0xFF)
        {
            return false;
        }

        byte expectedFeature =
            kind == Rs50OledTransactionKind.DiscoverDisplayFeature
                ? (byte)0
                : request[2];
        byte expectedFunction = request[3];

        bool exact =
            response[2] == expectedFeature &&
            response[3] == expectedFunction;
        bool matchingError =
            response[2] == 0xFF &&
            response[3] == 0x0A &&
            response[4] == expectedFeature &&
            response[5] == expectedFunction;

        return exact || matchingError;
    }

    private static void ValidateCanonicalTransaction(
        Rs50OledTransactionKind kind,
        byte[] request)
    {
        if (kind == Rs50OledTransactionKind.DiscoverDisplayFeature)
        {
            if (!request.SequenceEqual(
                    Rs50OledProtocol.CreateDiscovery().Request.Span))
            {
                throw new InvalidOperationException(
                    "The discovery request is not canonical.");
            }

            return;
        }

        int expectedLayout =
            kind - Rs50OledTransactionKind.SetLayoutA;
        if (kind is < Rs50OledTransactionKind.SetLayoutA or
                > Rs50OledTransactionKind.SetLayoutJ ||
            request.Length != Rs50OledProtocol.VeryLongReportLength ||
            request[0] != 0x12 ||
            request[1] != 0xFF ||
            request[2] is < 0x02 or >= 0xFF ||
            request[3] != 0x3A ||
            request[4] != expectedLayout ||
            request[63] != 0)
        {
            throw new InvalidOperationException(
                "The OLED layout request is not canonical.");
        }
    }

    private static IRs50HidCollection SelectUniqueCollection(
        IReadOnlyList<IRs50HidCollection> collections,
        ConfirmedOledDeviceIdentity identity,
        string pathMarker,
        uint usage,
        int expectedReportLength)
    {
        IRs50HidCollection[] matches = collections
            .Where(collection =>
                collection.VendorId == identity.VendorId &&
                collection.ProductId == identity.ProductId &&
                collection.DevicePath.Contains(
                    pathMarker,
                    StringComparison.OrdinalIgnoreCase) &&
                collection.Usages.Count == 1 &&
                collection.Usages.Contains(usage) &&
                collection.MaximumInputReportLength ==
                    expectedReportLength &&
                collection.MaximumOutputReportLength ==
                    expectedReportLength)
            .ToArray();

        if (matches.Length != 1)
        {
            throw new InvalidOperationException(
                $"Expected exactly one validated {identity.Model} " +
                $"{pathMarker} " +
                $"collection, found {matches.Length}.");
        }

        return matches[0];
    }
}
