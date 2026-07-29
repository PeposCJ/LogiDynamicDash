namespace LogiDynamicExplorer.Protocol;

/// <summary>
/// Builds function-3 parameters for the RS50 Display Game Data feature.
/// This type has no HID transport and cannot transmit a report.
/// </summary>
internal static class Rs50DisplayGameDataPayloadEncoder
{
    public const ushort PublicFeatureId = 0x8130;
    public const byte FunctionId = 0x03;

    public static byte[] EncodeLayoutA() => [0];

    public static byte[] EncodeLayoutB() => [1];

    public static byte[] EncodeLayoutC(float mainGaugeValue) =>
        [2, EncodeNormalizedValue(mainGaugeValue, nameof(mainGaugeValue))];

    public static byte[] EncodeLayoutD(
        float mainGaugeValue,
        float thinIndicatorValue,
        string text)
    {
        byte[] parameters = new byte[14];
        parameters[0] = 3;
        parameters[1] = EncodeNormalizedValue(
            mainGaugeValue,
            nameof(mainGaugeValue));
        parameters[2] = EncodeNormalizedValue(
            thinIndicatorValue,
            nameof(thinIndicatorValue));
        WriteText(parameters.AsSpan(3, 11), text, nameof(text));
        return parameters;
    }

    public static byte[] EncodeLayoutE(
        float mainGaugeValue,
        float thinIndicatorValue,
        string rightText,
        string leftText)
    {
        byte[] parameters = new byte[13];
        parameters[0] = 4;
        parameters[1] = EncodeNormalizedValue(
            mainGaugeValue,
            nameof(mainGaugeValue));
        parameters[2] = EncodeNormalizedValue(
            thinIndicatorValue,
            nameof(thinIndicatorValue));
        WriteText(parameters.AsSpan(3, 3), rightText, nameof(rightText));
        WriteText(parameters.AsSpan(6, 7), leftText, nameof(leftText));
        return parameters;
    }

    public static byte[] EncodeLayoutF(string leftText, string rightText) =>
        EncodeTwoTextLayout(5, 1, 3, leftText, rightText);

    public static byte[] EncodeLayoutG(string leftText, string rightText) =>
        EncodeTwoTextLayout(6, 1, 3, leftText, rightText);

    public static byte[] EncodeLayoutH(string topText, string bottomText) =>
        EncodeTwoTextLayout(7, 21, 10, topText, bottomText);

    public static byte[] EncodeLayoutI(
        string firstText,
        string secondText,
        string thirdText,
        string fourthText) =>
        EncodeFourTextLayout(
            8,
            firstText,
            secondText,
            thirdText,
            fourthText);

    public static byte[] EncodeLayoutJ(
        string firstText,
        string secondText,
        string thirdText,
        string fourthText) =>
        EncodeFourTextLayout(
            9,
            firstText,
            secondText,
            thirdText,
            fourthText);

    private static byte[] EncodeTwoTextLayout(
        byte layoutIndex,
        int firstLength,
        int secondLength,
        string firstText,
        string secondText)
    {
        byte[] parameters = new byte[1 + firstLength + secondLength];
        parameters[0] = layoutIndex;
        WriteText(
            parameters.AsSpan(1, firstLength),
            firstText,
            nameof(firstText));
        WriteText(
            parameters.AsSpan(1 + firstLength, secondLength),
            secondText,
            nameof(secondText));
        return parameters;
    }

    private static byte[] EncodeFourTextLayout(
        byte layoutIndex,
        string firstText,
        string secondText,
        string thirdText,
        string fourthText)
    {
        byte[] parameters = new byte[59];
        parameters[0] = layoutIndex;
        WriteText(parameters.AsSpan(1, 19), firstText, nameof(firstText));
        WriteText(parameters.AsSpan(20, 10), secondText, nameof(secondText));
        WriteText(parameters.AsSpan(30, 19), thirdText, nameof(thirdText));
        WriteText(parameters.AsSpan(49, 10), fourthText, nameof(fourthText));
        return parameters;
    }

    private static byte EncodeNormalizedValue(float value, string parameterName)
    {
        if (!float.IsFinite(value))
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                value,
                "The normalized display value must be finite.");
        }

        float scaled = Math.Clamp(value, 0.0f, 1.0f) * byte.MaxValue;
        return checked((byte)MathF.Round(scaled, MidpointRounding.AwayFromZero));
    }

    private static void WriteText(
        Span<byte> destination,
        string text,
        string parameterName)
    {
        ArgumentNullException.ThrowIfNull(text, parameterName);

        if (text.Contains('\0'))
        {
            throw new ArgumentException(
                "Display text cannot contain an embedded NUL.",
                parameterName);
        }

        if (text.Length > destination.Length)
        {
            throw new ArgumentException(
                $"Display text exceeds the {destination.Length}-character " +
                "layout field.",
                parameterName);
        }

        for (int index = 0; index < text.Length; index++)
        {
            char character = text[index];

            if (character is >= 'a' and <= 'z')
            {
                character = (char)(character - ('a' - 'A'));
            }

            destination[index] = character is >= (char)0x20 and <= (char)0x7F
                ? (byte)character
                : (byte)'?';
        }
    }
}
