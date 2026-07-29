using System.Text;

namespace LogiDynamicDash.Hidpp;

/// <summary>
/// Exact offline codec for the recovered feature-0x8130 transactions.
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
    internal const byte LayoutJIndex = 0x09;

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

    internal static Rs50HidppDisplayTransaction CreateLayoutA(
        byte runtimeIndex) =>
        CreateLayoutRequest(
            runtimeIndex,
            layoutIndex: 0,
            Rs50HidppDisplayTransactionKind.SetLayoutA);

    internal static Rs50HidppDisplayTransaction CreateLayoutB(
        byte runtimeIndex) =>
        CreateLayoutRequest(
            runtimeIndex,
            layoutIndex: 1,
            Rs50HidppDisplayTransactionKind.SetLayoutB);

    internal static Rs50HidppDisplayTransaction CreateLayoutC(
        byte runtimeIndex,
        byte mainGaugeValue)
    {
        byte[] request = CreateLayoutRequestBytes(
            runtimeIndex,
            layoutIndex: 2);
        request[5] = mainGaugeValue;
        return Rs50HidppDisplayTransaction.CreateLayout(
            Rs50HidppDisplayTransactionKind.SetLayoutC,
            request);
    }

    internal static Rs50HidppDisplayTransaction CreateLayoutD(
        byte runtimeIndex,
        byte mainGaugeValue,
        byte thinIndicatorValue,
        string text)
    {
        ValidateText(text, 11, nameof(text));
        byte[] request = CreateLayoutRequestBytes(
            runtimeIndex,
            layoutIndex: 3);
        request[5] = mainGaugeValue;
        request[6] = thinIndicatorValue;
        WriteAscii(request.AsSpan(7, 11), text);
        return Rs50HidppDisplayTransaction.CreateLayout(
            Rs50HidppDisplayTransactionKind.SetLayoutD,
            request);
    }

    internal static Rs50HidppDisplayTransaction CreateLayoutE(
        byte runtimeIndex,
        byte mainGaugeValue,
        byte thinIndicatorValue,
        string rightText,
        string leftText)
    {
        ValidateText(rightText, 3, nameof(rightText));
        ValidateText(leftText, 7, nameof(leftText));
        byte[] request = CreateLayoutRequestBytes(
            runtimeIndex,
            layoutIndex: 4);
        request[5] = mainGaugeValue;
        request[6] = thinIndicatorValue;
        WriteAscii(request.AsSpan(7, 3), rightText);
        WriteAscii(request.AsSpan(10, 7), leftText);
        return Rs50HidppDisplayTransaction.CreateLayout(
            Rs50HidppDisplayTransactionKind.SetLayoutE,
            request);
    }

    internal static Rs50HidppDisplayTransaction CreateLayoutF(
        byte runtimeIndex,
        string leftText,
        string rightText) =>
        CreateTwoTextLayout(
            runtimeIndex,
            layoutIndex: 5,
            Rs50HidppDisplayTransactionKind.SetLayoutF,
            leftText,
            firstMaximumLength: 1,
            rightText,
            secondMaximumLength: 3);

    internal static Rs50HidppDisplayTransaction CreateLayoutG(
        byte runtimeIndex,
        string leftText,
        string rightText) =>
        CreateTwoTextLayout(
            runtimeIndex,
            layoutIndex: 6,
            Rs50HidppDisplayTransactionKind.SetLayoutG,
            leftText,
            firstMaximumLength: 1,
            rightText,
            secondMaximumLength: 3);

    internal static Rs50HidppDisplayTransaction CreateLayoutH(
        byte runtimeIndex,
        string topText,
        string bottomText) =>
        CreateTwoTextLayout(
            runtimeIndex,
            layoutIndex: 7,
            Rs50HidppDisplayTransactionKind.SetLayoutH,
            topText,
            firstMaximumLength: 21,
            bottomText,
            secondMaximumLength: 10);

    internal static Rs50HidppDisplayTransaction CreateLayoutI(
        byte runtimeIndex,
        string line1,
        string line2,
        string line3,
        string line4) =>
        CreateFourTextLayout(
            runtimeIndex,
            layoutIndex: 8,
            Rs50HidppDisplayTransactionKind.SetLayoutI,
            line1,
            line2,
            line3,
            line4);

    internal static Rs50HidppDisplayTransaction CreateLayoutJ(
        byte runtimeIndex,
        string line1,
        string line2,
        string line3,
        string line4)
    {
        return CreateFourTextLayout(
            runtimeIndex,
            LayoutJIndex,
            Rs50HidppDisplayTransactionKind.SetLayoutJ,
            line1,
            line2,
            line3,
            line4);
    }

    internal static void ParseLayoutAcknowledgement(
        byte runtimeIndex,
        ReadOnlySpan<byte> response)
    {
        ValidateRuntimeIndex(runtimeIndex);
        ValidateLength(response);
        byte function = EncodeFunction(SetLayoutFunction);
        ThrowIfError(response, runtimeIndex, function);
        ValidateHeader(response, runtimeIndex, function);
        RequireZero(response[4..], "layout acknowledgement body");
    }

    internal static void ParseLayoutJAcknowledgement(
        byte runtimeIndex,
        ReadOnlySpan<byte> response) =>
        ParseLayoutAcknowledgement(runtimeIndex, response);

    private static Rs50HidppDisplayTransaction CreateLayoutRequest(
        byte runtimeIndex,
        byte layoutIndex,
        Rs50HidppDisplayTransactionKind kind) =>
        Rs50HidppDisplayTransaction.CreateLayout(
            kind,
            CreateLayoutRequestBytes(runtimeIndex, layoutIndex));

    private static byte[] CreateLayoutRequestBytes(
        byte runtimeIndex,
        byte layoutIndex)
    {
        ValidateRuntimeIndex(runtimeIndex);
        if (layoutIndex > LayoutJIndex)
        {
            throw new ArgumentOutOfRangeException(nameof(layoutIndex));
        }

        byte[] request = new byte[VeryLongReportLength];
        request[0] = VeryLongReportId;
        request[1] = BaseDeviceIndex;
        request[2] = runtimeIndex;
        request[3] = EncodeFunction(SetLayoutFunction);
        request[4] = layoutIndex;
        return request;
    }

    private static Rs50HidppDisplayTransaction CreateTwoTextLayout(
        byte runtimeIndex,
        byte layoutIndex,
        Rs50HidppDisplayTransactionKind kind,
        string firstText,
        int firstMaximumLength,
        string secondText,
        int secondMaximumLength)
    {
        ValidateText(
            firstText,
            firstMaximumLength,
            nameof(firstText));
        ValidateText(
            secondText,
            secondMaximumLength,
            nameof(secondText));

        byte[] request = CreateLayoutRequestBytes(
            runtimeIndex,
            layoutIndex);
        WriteAscii(
            request.AsSpan(5, firstMaximumLength),
            firstText);
        WriteAscii(
            request.AsSpan(
                5 + firstMaximumLength,
                secondMaximumLength),
            secondText);
        return Rs50HidppDisplayTransaction.CreateLayout(kind, request);
    }

    private static Rs50HidppDisplayTransaction CreateFourTextLayout(
        byte runtimeIndex,
        byte layoutIndex,
        Rs50HidppDisplayTransactionKind kind,
        string line1,
        string line2,
        string line3,
        string line4)
    {
        ValidateText(line1, 19, nameof(line1));
        ValidateText(line2, 10, nameof(line2));
        ValidateText(line3, 19, nameof(line3));
        ValidateText(line4, 10, nameof(line4));

        byte[] request = CreateLayoutRequestBytes(
            runtimeIndex,
            layoutIndex);
        WriteAscii(request.AsSpan(5, 19), line1);
        WriteAscii(request.AsSpan(24, 10), line2);
        WriteAscii(request.AsSpan(34, 19), line3);
        WriteAscii(request.AsSpan(53, 10), line4);
        return Rs50HidppDisplayTransaction.CreateLayout(kind, request);
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
                "Display text did not encode to one byte per character.");
        }
    }

    private static void ValidateText(
        string value,
        int maximumLength,
        string parameterName)
    {
        ArgumentNullException.ThrowIfNull(value, parameterName);

        if (value.Length > maximumLength)
        {
            throw new ArgumentException(
                $"Text exceeds the layout limit of {maximumLength} " +
                "characters.",
                parameterName);
        }

        if (value.Any(character =>
                character is < (char)0x20 or > (char)0x7F))
        {
            throw new ArgumentException(
                "Text contains a character outside the firmware's " +
                "recovered display range.",
                parameterName);
        }
    }
}
