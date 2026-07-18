using LogiDynamicExplorer.Decoders;

namespace LogiDynamicExplorer.Tests;

public sealed class Rs50ReportDecoderTests
{
    [Fact]
    public void Decode_WhenReportIsTooShort_ReturnsDiagnosticMessage()
    {
        byte[] report = [0x12, 0xFF, 0x0A];

        string result = Rs50ReportDecoder.Decode(report);

        Assert.Equal("Report too short (3 bytes)", result);
    }

    [Fact]
    public void Decode_WhenHeaderIsUnrecognized_ReturnsDiagnosticMessage()
    {
        byte[] report = [0x13, 0xFF, 0x0A, 0x00, 0x00, 0x64];

        string result = Rs50ReportDecoder.Decode(report);

        Assert.Equal("Unrecognized report format (ID 0x13)", result);
    }

    [Fact]
    public void Decode_RpmBrightness_ReadsBigEndianValue()
    {
        byte[] report = CreateReport(0x0A, 0x00, 0x64);

        string result = Rs50ReportDecoder.Decode(report);

        Assert.Equal("RPM Bright: 100%", result);
    }

    [Theory]
    [InlineData(0x01, 0x00, "RPM Mode: Inside Out (likely)")]
    [InlineData(0x02, 0x00, "RPM Mode: Outside In")]
    public void Decode_RpmMode_ReadsLittleEndianValue(
        byte lowByte,
        byte highByte,
        string expected)
    {
        byte[] report = CreateReport(0x0B, lowByte, highByte);

        string result = Rs50ReportDecoder.Decode(report);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void Decode_NormalizedPercentage_UsesFullUnsignedRange()
    {
        byte[] report = CreateReport(0x15, 0xFF, 0xFF);

        string result = Rs50ReportDecoder.Decode(report);

        Assert.Equal("Brake Pressure: 100%", result);
    }

    [Fact]
    public void Decode_Strength_ConvertsPercentageToTorque()
    {
        byte[] report = CreateReport(0x16, 0x80, 0x00);

        string result = Rs50ReportDecoder.Decode(report);

        Assert.Equal("Strength: 4.0 Nm (50%)", result);
    }

    [Fact]
    public void Decode_WheelAngle_ReadsBigEndianDegrees()
    {
        byte[] report = CreateReport(0x18, 0x03, 0x84);

        string result = Rs50ReportDecoder.Decode(report);

        Assert.Equal("Wheel Angle: 900°", result);
    }

    [Theory]
    [InlineData(0x01, 0x00, "Settings: opened (observed)")]
    [InlineData(0x01, 0x01, "Settings: closed; HomeScreen restored (observed)")]
    [InlineData(0x12, 0x34, "Settings: unknown state 0x1234")]
    public void Decode_SettingsState_PreservesObservedMappings(
        byte highByte,
        byte lowByte,
        string expected)
    {
        byte[] report = CreateReport(0x17, highByte, lowByte);

        string result = Rs50ReportDecoder.Decode(report);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(0x01, 0x00, 0x0A, 0x00, "FFB Filter: 10")]
    [InlineData(0x05, 0x00, 0x0B, 0x00, "FFB Filter: Auto (internal value 11)")]
    public void Decode_FfbFilter_ReadsLittleEndianFields(
        byte modeLowByte,
        byte modeHighByte,
        byte valueLowByte,
        byte valueHighByte,
        string expected)
    {
        byte[] report = CreateReport(
            0x1A,
            modeLowByte,
            modeHighByte,
            valueLowByte,
            valueHighByte);

        string result = Rs50ReportDecoder.Decode(report);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void Decode_WhenFfbFilterPayloadIsIncomplete_ReturnsDiagnosticMessage()
    {
        byte[] report = [0x12, 0xFF, 0x1A, 0x00, 0x01, 0x00];

        string result = Rs50ReportDecoder.Decode(report);

        Assert.Equal("FFB Filter: incomplete report", result);
    }

    [Fact]
    public void Decode_UnknownParameter_PreservesBothEndianInterpretations()
    {
        byte[] report = CreateReport(0x09, 0x12, 0x34);

        string result = Rs50ReportDecoder.Decode(report);

        Assert.Equal(
            "Unknown parameter 0x09 (context 0x00, BE 4660, LE 13330)",
            result);
    }

    private static byte[] CreateReport(
        byte parameterId,
        params byte[] payload)
    {
        byte[] report = new byte[Math.Max(8, 4 + payload.Length)];

        report[0] = 0x12;
        report[1] = 0xFF;
        report[2] = parameterId;

        payload.CopyTo(report, 4);

        return report;
    }
}
