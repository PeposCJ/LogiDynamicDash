using System.Text;
using LogiDynamicDash.Models;

namespace LogiDynamicDash.Hidpp;

/// <summary>
/// Closed encoder and response validator for the confirmed public HID++
/// Display Game Data feature. This type has no device or stream access.
/// </summary>
internal static class Rs50OledProtocol
{
    internal const ushort DisplayFeatureId = 0x8130;
    internal const int ShortReportLength = 7;
    internal const int VeryLongReportLength = 64;

    private const byte SoftwareId = 0x0A;
    private const byte DeviceIndex = 0xFF;
    private const byte ShortReportId = 0x10;
    private const byte VeryLongReportId = 0x12;
    private const byte RootFeatureIndex = 0x00;
    private const byte RootGetFeatureFunction = 0x00;
    private const byte SetLayoutFunction = 0x03;

    internal static Rs50OledTransaction CreateDiscovery()
    {
        byte[] request =
        [
            ShortReportId,
            DeviceIndex,
            RootFeatureIndex,
            EncodeFunction(RootGetFeatureFunction),
            (byte)(DisplayFeatureId >> 8),
            (byte)(DisplayFeatureId & 0xFF),
            0
        ];

        return Rs50OledTransaction.Discovery(request);
    }

    internal static byte ParseDiscoveryResponse(
        ReadOnlySpan<byte> response)
    {
        RequireResponseLength(response);
        ThrowIfHidppError(
            response,
            RootFeatureIndex,
            EncodeFunction(RootGetFeatureFunction));
        RequireHeader(
            response,
            RootFeatureIndex,
            EncodeFunction(RootGetFeatureFunction));

        byte runtimeIndex = response[4];
        if (!IsRuntimeIndex(runtimeIndex))
        {
            throw new Rs50OledProtocolException(
                $"The device returned invalid runtime index " +
                $"0x{runtimeIndex:X2}.");
        }

        if (response[5] != 0)
        {
            throw new Rs50OledProtocolException(
                "Display Game Data is not a public feature.");
        }

        if (response[6] != 0)
        {
            throw new Rs50OledProtocolException(
                $"Unsupported Display Game Data version {response[6]}.");
        }

        RequireZero(response[7..], "discovery padding");
        return runtimeIndex;
    }

    internal static Rs50OledTransaction CreateLayout(
        byte runtimeIndex,
        Rs50OledFrame frame)
    {
        ArgumentNullException.ThrowIfNull(frame);
        RequireRuntimeIndex(runtimeIndex);

        byte[] request = new byte[VeryLongReportLength];
        request[0] = VeryLongReportId;
        request[1] = DeviceIndex;
        request[2] = runtimeIndex;
        request[3] = EncodeFunction(SetLayoutFunction);
        request[4] = (byte)frame.Layout;

        Rs50OledTransactionKind kind = frame switch
        {
            Rs50LayoutAFrame =>
                Rs50OledTransactionKind.SetLayoutA,
            Rs50LayoutBFrame =>
                Rs50OledTransactionKind.SetLayoutB,
            Rs50LayoutCFrame value =>
                EncodeLayoutC(request, value),
            Rs50LayoutDFrame value =>
                EncodeLayoutD(request, value),
            Rs50LayoutEFrame value =>
                EncodeLayoutE(request, value),
            Rs50LayoutFFrame value =>
                EncodeTwoText(
                    request,
                    value.LeftText,
                    1,
                    value.RightText,
                    3,
                    Rs50OledTransactionKind.SetLayoutF),
            Rs50LayoutGFrame value =>
                EncodeTwoText(
                    request,
                    value.LeftText,
                    1,
                    value.RightText,
                    3,
                    Rs50OledTransactionKind.SetLayoutG),
            Rs50LayoutHFrame value =>
                EncodeTwoText(
                    request,
                    value.TopText,
                    21,
                    value.BottomText,
                    10,
                    Rs50OledTransactionKind.SetLayoutH),
            Rs50LayoutIFrame value =>
                EncodeFourText(
                    request,
                    value.Line1,
                    value.Line2,
                    value.Line3,
                    value.Line4,
                    Rs50OledTransactionKind.SetLayoutI),
            Rs50LayoutJFrame value =>
                EncodeFourText(
                    request,
                    value.Line1,
                    value.Line2,
                    value.Line3,
                    value.Line4,
                    Rs50OledTransactionKind.SetLayoutJ),
            _ => throw new ArgumentOutOfRangeException(
                nameof(frame),
                "Unknown OLED frame type.")
        };

        return Rs50OledTransaction.Layout(kind, request);
    }

    internal static void ParseLayoutAcknowledgement(
        Rs50OledTransaction transaction,
        ReadOnlySpan<byte> response)
    {
        ArgumentNullException.ThrowIfNull(transaction);
        if (transaction.Kind is
            Rs50OledTransactionKind.DiscoverDisplayFeature)
        {
            throw new ArgumentException(
                "A discovery transaction cannot validate a layout response.",
                nameof(transaction));
        }

        ReadOnlySpan<byte> request = transaction.RequestSpan;
        RequireResponseLength(response);
        ThrowIfHidppError(response, request[2], request[3]);
        RequireHeader(response, request[2], request[3]);
        RequireZero(response[4..], "layout acknowledgement");
    }

    private static Rs50OledTransactionKind EncodeLayoutC(
        Span<byte> request,
        Rs50LayoutCFrame frame)
    {
        request[5] = frame.MainGauge.WireValue;
        return Rs50OledTransactionKind.SetLayoutC;
    }

    private static Rs50OledTransactionKind EncodeLayoutD(
        Span<byte> request,
        Rs50LayoutDFrame frame)
    {
        request[5] = frame.MainGauge.WireValue;
        request[6] = frame.ThinIndicator.WireValue;
        WriteText(request.Slice(7, 11), frame.Text, nameof(frame.Text));
        return Rs50OledTransactionKind.SetLayoutD;
    }

    private static Rs50OledTransactionKind EncodeLayoutE(
        Span<byte> request,
        Rs50LayoutEFrame frame)
    {
        request[5] = frame.MainGauge.WireValue;
        request[6] = frame.ThinIndicator.WireValue;

        // Layout E's confirmed visual order is the reverse of its wire order.
        WriteText(
            request.Slice(7, 3),
            frame.RightText,
            nameof(frame.RightText));
        WriteText(
            request.Slice(10, 7),
            frame.LeftText,
            nameof(frame.LeftText));
        return Rs50OledTransactionKind.SetLayoutE;
    }

    private static Rs50OledTransactionKind EncodeTwoText(
        Span<byte> request,
        string first,
        int firstLength,
        string second,
        int secondLength,
        Rs50OledTransactionKind kind)
    {
        WriteText(request.Slice(5, firstLength), first, nameof(first));
        WriteText(
            request.Slice(5 + firstLength, secondLength),
            second,
            nameof(second));
        return kind;
    }

    private static Rs50OledTransactionKind EncodeFourText(
        Span<byte> request,
        string line1,
        string line2,
        string line3,
        string line4,
        Rs50OledTransactionKind kind)
    {
        WriteText(request.Slice(5, 19), line1, nameof(line1));
        WriteText(request.Slice(24, 10), line2, nameof(line2));
        WriteText(request.Slice(34, 19), line3, nameof(line3));
        WriteText(request.Slice(53, 10), line4, nameof(line4));
        return kind;
    }

    private static void WriteText(
        Span<byte> destination,
        string value,
        string parameterName)
    {
        ArgumentNullException.ThrowIfNull(value, parameterName);
        if (value.Length > destination.Length)
        {
            throw new ArgumentException(
                $"Text exceeds this layout field's " +
                $"{destination.Length}-character limit.",
                parameterName);
        }

        if (value.Any(character =>
                character is < (char)0x20 or > (char)0x7F))
        {
            throw new ArgumentException(
                "OLED text must use the confirmed single-byte display range.",
                parameterName);
        }

        int encoded = Encoding.ASCII.GetBytes(value, destination);
        if (encoded != value.Length)
        {
            throw new ArgumentException(
                "OLED text must encode to exactly one byte per character.",
                parameterName);
        }
    }

    private static byte EncodeFunction(byte function) =>
        checked((byte)((function << 4) | SoftwareId));

    private static bool IsRuntimeIndex(byte runtimeIndex) =>
        runtimeIndex is >= 0x02 and < 0xFF;

    private static void RequireRuntimeIndex(byte runtimeIndex)
    {
        if (!IsRuntimeIndex(runtimeIndex))
        {
            throw new ArgumentOutOfRangeException(
                nameof(runtimeIndex),
                runtimeIndex,
                "A discovered runtime index must be between 0x02 and 0xFE.");
        }
    }

    private static void RequireResponseLength(ReadOnlySpan<byte> response)
    {
        if (response.Length != VeryLongReportLength)
        {
            throw new Rs50OledProtocolException(
                $"Expected {VeryLongReportLength} response bytes, received " +
                $"{response.Length}.");
        }
    }

    private static void RequireHeader(
        ReadOnlySpan<byte> response,
        byte featureIndex,
        byte function)
    {
        if (response[0] != VeryLongReportId ||
            response[1] != DeviceIndex ||
            response[2] != featureIndex ||
            response[3] != function)
        {
            throw new Rs50OledProtocolException(
                "The response header does not exactly match its request.");
        }
    }

    private static void ThrowIfHidppError(
        ReadOnlySpan<byte> response,
        byte featureIndex,
        byte function)
    {
        if (response[0] == VeryLongReportId &&
            response[1] == DeviceIndex &&
            response[2] == 0xFF &&
            response[4] == featureIndex &&
            response[5] == function)
        {
            throw new Rs50OledProtocolException(
                $"The device rejected the OLED request with HID++ error " +
                $"0x{response[6]:X2}.");
        }
    }

    private static void RequireZero(
        ReadOnlySpan<byte> bytes,
        string field)
    {
        if (bytes.IndexOfAnyExcept((byte)0) >= 0)
        {
            throw new Rs50OledProtocolException(
                $"Unexpected nonzero byte in {field}.");
        }
    }
}
