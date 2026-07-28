using System.Text.RegularExpressions;
using LogiDynamicDash.Hidpp;

namespace Rs50SharedHidppTransport;

/// <summary>
/// Physical adapter compiled as a separate, unreferenced library.
/// No application route constructs or loads this type.
/// </summary>
internal sealed partial class Rs50HidppDeviceExchange
    : IRs50HidppDisplayExchange
{
    private const int LogitechVendorId = 0x046D;
    private const int Rs50ProductId = 0xC276;
    private const uint ShortCollectionUsage = 0xFF430701;
    private const uint VeryLongCollectionUsage = 0xFF430704;
    private const int MaximumReportsPerExchange = 16;

    private readonly IRs50HidStream shortStream;
    private readonly IRs50HidStream veryLongStream;
    private readonly object exchangeLock = new();
    private bool disposed;

    private Rs50HidppDeviceExchange(
        IRs50HidStream shortStream,
        IRs50HidStream veryLongStream)
    {
        this.shortStream = shortStream;
        this.veryLongStream = veryLongStream;
    }

    internal static Rs50HidppDeviceExchange Open() =>
        Open(new HidSharpRs50CollectionCatalog());

    internal static Rs50HidppDeviceExchange Open(
        IRs50HidCollectionCatalog catalog)
    {
        ArgumentNullException.ThrowIfNull(catalog);

        IReadOnlyList<IRs50HidCollection> collections =
            catalog.Enumerate();

        IRs50HidCollection shortCollection =
            SelectOnlyCollection(
                collections,
                "mi_01&col01",
                ShortCollectionUsage,
                expectedInputLength: 7,
                expectedOutputLength: 7);

        IRs50HidCollection veryLongCollection =
            SelectOnlyCollection(
                collections,
                "mi_01&col03",
                VeryLongCollectionUsage,
                expectedInputLength: 64,
                expectedOutputLength: 64);

        if (!string.Equals(
                NormalizeCollectionPath(shortCollection.DevicePath),
                NormalizeCollectionPath(veryLongCollection.DevicePath),
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "The RS50 HID++ collections do not belong to the same " +
                "physical interface instance.");
        }

        IRs50HidStream? shortStream = null;
        try
        {
            shortStream = shortCollection.Open();
            IRs50HidStream veryLongStream = veryLongCollection.Open();
            return new(shortStream, veryLongStream);
        }
        catch
        {
            shortStream?.Dispose();
            throw;
        }
    }

    public byte[] Exchange(Rs50HidppDisplayTransaction transaction)
    {
        ArgumentNullException.ThrowIfNull(transaction);

        lock (exchangeLock)
        {
            ObjectDisposedException.ThrowIf(disposed, this);

            byte[] request = transaction.Request.ToArray();
            ValidateClosedTransaction(transaction.Kind, request);

            IRs50HidStream outputStream =
                transaction.Kind ==
                Rs50HidppDisplayTransactionKind.DiscoverDisplayFeature
                    ? shortStream
                    : veryLongStream;

            outputStream.Write(request);
            return ReadMatchingResponse(transaction.Kind, request);
        }
    }

    public void Dispose()
    {
        lock (exchangeLock)
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
        Rs50HidppDisplayTransactionKind kind,
        byte[] request)
    {
        for (int attempt = 0;
             attempt < MaximumReportsPerExchange;
             attempt++)
        {
            byte[] response =
                new byte[Rs50HidppDisplayProtocol.VeryLongReportLength];

            int bytesRead = veryLongStream.Read(response);
            if (bytesRead != response.Length)
            {
                throw new IOException(
                    $"Expected a {response.Length}-byte HID++ response, " +
                    $"received {bytesRead}.");
            }

            if (MatchesTransaction(kind, request, response))
            {
                return response;
            }
        }

        throw new IOException(
            "No matching RS50 Display Game Data response was received " +
            $"within {MaximumReportsPerExchange} reports.");
    }

    private static bool MatchesTransaction(
        Rs50HidppDisplayTransactionKind kind,
        byte[] request,
        byte[] response)
    {
        if (response[0] != 0x12 ||
            response[1] != Rs50HidppDisplayProtocol.BaseDeviceIndex)
        {
            return false;
        }

        byte expectedFeature =
            kind ==
            Rs50HidppDisplayTransactionKind.DiscoverDisplayFeature
                ? (byte)0
                : request[2];
        byte expectedFunction = request[3];

        bool exactResponse =
            response[2] == expectedFeature &&
            response[3] == expectedFunction;

        bool matchingError =
            response[2] == 0xFF &&
            response[3] == Rs50HidppDisplayProtocol.SoftwareId &&
            response[4] == expectedFeature &&
            response[5] == expectedFunction;

        return exactResponse || matchingError;
    }

    private static void ValidateClosedTransaction(
        Rs50HidppDisplayTransactionKind kind,
        byte[] request)
    {
        if (kind ==
            Rs50HidppDisplayTransactionKind.DiscoverDisplayFeature)
        {
            byte[] expected =
                Rs50HidppDisplayProtocol
                    .CreateDiscovery()
                    .Request
                    .ToArray();

            if (!request.SequenceEqual(expected))
            {
                throw new InvalidOperationException(
                    "The discovery transaction is not canonical.");
            }

            return;
        }

        if (kind != Rs50HidppDisplayTransactionKind.SetLayoutJ ||
            request.Length !=
                Rs50HidppDisplayProtocol.VeryLongReportLength ||
            request[0] != 0x12 ||
            request[1] != Rs50HidppDisplayProtocol.BaseDeviceIndex ||
            request[2] is < 0x02 or >= 0xFF ||
            request[3] != 0x3A ||
            request[4] != 0x09 ||
            request[63] != 0)
        {
            throw new InvalidOperationException(
                "The Layout J transaction is not canonical.");
        }
    }

    private static IRs50HidCollection SelectOnlyCollection(
        IReadOnlyList<IRs50HidCollection> collections,
        string pathMarker,
        uint expectedUsage,
        int expectedInputLength,
        int expectedOutputLength)
    {
        IRs50HidCollection[] matches = collections
            .Where(collection =>
                collection.VendorId == LogitechVendorId &&
                collection.ProductId == Rs50ProductId &&
                collection.DevicePath.Contains(
                    pathMarker,
                    StringComparison.OrdinalIgnoreCase) &&
                collection.Usages.Count == 1 &&
                collection.Usages.Contains(expectedUsage) &&
                collection.MaximumInputReportLength == expectedInputLength &&
                collection.MaximumOutputReportLength == expectedOutputLength)
            .ToArray();

        if (matches.Length != 1)
        {
            throw new InvalidOperationException(
                $"Expected exactly one validated RS50 {pathMarker} " +
                $"collection, found {matches.Length}.");
        }

        return matches[0];
    }

    private static string NormalizeCollectionPath(string path) =>
        CollectionSuffixRegex().Replace(path, "&colXX");

    [GeneratedRegex(
        "&col(?:01|03)",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex CollectionSuffixRegex();
}
