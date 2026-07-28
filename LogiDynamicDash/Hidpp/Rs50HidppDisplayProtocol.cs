using System.Text;
using LogiDynamicDash.Models;

namespace LogiDynamicDash.Hidpp;

/// <summary>
/// Exact offline codec for the two authorized feature-0x8130 transactions.
/// It contains no device enumeration, stream, handle, read, or write API.
/// </summary>
internal static class Rs50HidppDisplayProtocol
{
    internal const ushort DisplayFeatureId = 0x8130;
    internal const byte SoftwareId = 0x0A;
    internal const byte BaseDeviceIndex = 0xFF;
    internal const int ShortReportLength = 7;
    internal const int VeryLongReportLength = 64;

    private const byte ShortReportId = 0x10;
    private const byte VeryLongReportId = 0x12;
    private const byte RootFeatureIndex = 0x00;
    private const byte RootGetFeatureFunction = 0x00;
    private const byte SetLayoutFunction = 0x03;
    private const byte LayoutJIndex = 0x09;

    internal static Rs50HidppDisplayTransaction CreateDiscovery()
    {
        byte[] request =
        [
            ShortReportId,
            BaseDeviceIndex,
            RootFeatureIndex,
            EncodeFunction(RootGetFeatureFunction),
            (byte)(DisplayFeatureId >> 8),
            (byte)(DisplayFeatureId & 0xFF),
            0
        ];

        return Rs50HidppDisplayTransaction.CreateDiscovery(request);
    }

    internal static byte ParseDiscoveryResponse(ReadOnlySpan<byte> response)
    {
        ValidateLength(response);
        ThrowIfError(
            response,
            RootFeatureIndex,
            EncodeFunction(RootGetFeatureFunction));
        ValidateHeader(
            response,
            RootFeatureIndex,
            EncodeFunction(RootGetFeatureFunction));

        byte runtimeIndex = response[4];
        byte featureFlags = response[5];
        byte featureVersion = response[6];

        if (runtimeIndex is < 0x02 or >= 0xFF)
        {
            throw new Rs50HidppProtocolException(
                $"Display feature returned invalid runtime index " +
                $"0x{runtimeIndex:X2}.");
        }

        if (featureFlags != 0)
        {
            throw new Rs50HidppProtocolException(
                $"Display feature is not public (flags 0x{featureFlags:X2}).");
        }

        if (featureVersion != 0)
        {
            throw new Rs50HidppProtocolException(
                $"Unsupported Display Game Data version {featureVersion}.");
        }

        RequireZero(response[7..], "discovery response padding");
        return runtimeIndex;
    }

    internal static Rs50HidppDisplayTransaction CreateLayoutJ(
        byte runtimeIndex,
        LayoutJFrame frame)
    {
        ArgumentNullException.ThrowIfNull(frame);
        ValidateRuntimeIndex(runtimeIndex);

        byte[] request = new byte[VeryLongReportLength];
        request[0] = VeryLongReportId;
        request[1] = BaseDeviceIndex;
        request[2] = runtimeIndex;
        request[3] = EncodeFunction(SetLayoutFunction);
        request[4] = LayoutJIndex;
        WriteAscii(request.AsSpan(5, 19), frame.Line1);
        WriteAscii(request.AsSpan(24, 10), frame.Line2);
        WriteAscii(request.AsSpan(34, 19), frame.Line3);
        WriteAscii(request.AsSpan(53, 10), frame.Line4);

        return Rs50HidppDisplayTransaction.CreateLayoutJ(request);
    }

    internal static void ParseLayoutJAcknowledgement(
        byte runtimeIndex,
        ReadOnlySpan<byte> response)
    {
        ValidateRuntimeIndex(runtimeIndex);
        ValidateLength(response);
        byte function = EncodeFunction(SetLayoutFunction);
        ThrowIfError(response, runtimeIndex, function);
        ValidateHeader(response, runtimeIndex, function);
        RequireZero(response[4..], "Layout J acknowledgement body");
    }

    private static byte EncodeFunction(byte functionId) =>
        checked((byte)((functionId << 4) | SoftwareId));

    private static void ValidateRuntimeIndex(byte runtimeIndex)
    {
        if (runtimeIndex is < 0x02 or >= 0xFF)
        {
            throw new ArgumentOutOfRangeException(
                nameof(runtimeIndex),
                runtimeIndex,
                "The discovered runtime index must be between 0x02 and 0xFE.");
        }
    }

    private static void ValidateLength(ReadOnlySpan<byte> response)
    {
        if (response.Length != VeryLongReportLength)
        {
            throw new Rs50HidppProtocolException(
                $"Expected a {VeryLongReportLength}-byte response, received " +
                $"{response.Length}.");
        }
    }

    private static void ValidateHeader(
        ReadOnlySpan<byte> response,
        byte expectedFeatureIndex,
        byte expectedFunction)
    {
        if (response[0] != VeryLongReportId ||
            response[1] != BaseDeviceIndex ||
            response[2] != expectedFeatureIndex ||
            response[3] != expectedFunction)
        {
            throw new Rs50HidppProtocolException(
                "Response header does not exactly match the request.");
        }
    }

    private static void ThrowIfError(
        ReadOnlySpan<byte> response,
        byte expectedFeatureIndex,
        byte expectedFunction)
    {
        if (response[0] == VeryLongReportId &&
            response[1] == BaseDeviceIndex &&
            response[2] == 0xFF &&
            response[4] == expectedFeatureIndex &&
            response[5] == expectedFunction)
        {
            throw new Rs50HidppProtocolException(
                $"HID++ rejected the display request with error " +
                $"0x{response[6]:X2}.");
        }
    }

    private static void RequireZero(
        ReadOnlySpan<byte> bytes,
        string description)
    {
        if (bytes.IndexOfAnyExcept((byte)0) >= 0)
        {
            throw new Rs50HidppProtocolException(
                $"Unexpected nonzero byte in {description}.");
        }
    }

    private static void WriteAscii(Span<byte> destination, string value)
    {
        int bytesWritten = Encoding.ASCII.GetBytes(value, destination);
        if (bytesWritten != value.Length)
        {
            throw new InvalidOperationException(
                "Layout J text did not encode to one byte per character.");
        }
    }
}
