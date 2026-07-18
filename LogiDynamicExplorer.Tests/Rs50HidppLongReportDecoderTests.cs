using LogiDynamicExplorer.Decoders;

namespace LogiDynamicExplorer.Tests;

public sealed class Rs50HidppLongReportDecoderTests
{
    [Fact]
    public void Decode_FeatureSetResponse_DescribesPublicKnownFeature()
    {
        byte[] report =
        [
            0x11, 0x01, 0x01, 0x1D,
            0x80, 0x91, 0x00, 0x00
        ];

        string result = Rs50ReportDecoder.Decode(report);

        Assert.Equal(
            "HID++ FeatureSet: device 0x01, " +
            "feature 0x8091 (per-key/LED matrix), " +
            "flags 0x00 (public), version 0, SW-ID 0x0D",
            result);
    }

    [Fact]
    public void Decode_FeatureSetResponse_DescribesRestrictedUnknownFeature()
    {
        byte[] report =
        [
            0x11, 0x01, 0x01, 0x1D,
            0x93, 0x15, 0x70, 0x04
        ];

        string result = Rs50ReportDecoder.Decode(report);

        Assert.Equal(
            "HID++ FeatureSet: device 0x01, feature 0x9315, " +
            "flags 0x70 (hidden, engineering, manufacturing-deactivatable), " +
            "version 4, SW-ID 0x0D",
            result);
    }

    [Fact]
    public void Decode_NonFeatureSetReport_PreservesHeaderFields()
    {
        byte[] report = [0x11, 0x02, 0x07, 0x3A];

        string result = Rs50ReportDecoder.Decode(report);

        Assert.Equal(
            "HID++ long: device 0x02, feature index 0x07, " +
            "function 0x03, SW-ID 0x0A, parameters (none)",
            result);
    }

    [Fact]
    public void Decode_SanitizedStartupRequest_PreservesAllParameters()
    {
        byte[] report =
        [
            0x11, 0xFF, 0x0F, 0x2B,
            0x0A, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07,
            0x08, 0x09, 0x0A, 0x00, 0x00, 0x00, 0x00, 0x00
        ];

        string result = Rs50ReportDecoder.Decode(report);

        Assert.Equal(
            "HID++ long: device 0xFF, feature index 0x0F, " +
            "function 0x02, SW-ID 0x0B, parameters " +
            "0A 01 02 03 04 05 06 07 08 09 0A 00 00 00 00 00",
            result);
    }

    [Fact]
    public void Decode_IncompleteFeatureSetResponse_ReturnsDiagnosticMessage()
    {
        byte[] report = [0x11, 0x01, 0x01, 0x1D, 0x80, 0x93];

        string result = Rs50ReportDecoder.Decode(report);

        Assert.Equal(
            "HID++ FeatureSet response incomplete " +
            "(6 bytes, device 0x01, SW-ID 0x0D)",
            result);
    }

    [Fact]
    public void Decode_EmptyReport_ReturnsDiagnosticMessage()
    {
        string result = Rs50ReportDecoder.Decode([]);

        Assert.Equal("Report too short (0 bytes)", result);
    }

    [Fact]
    public void Decode_HeaderOnlyLongReport_ReturnsDiagnosticMessage()
    {
        byte[] report = [0x11];

        string result = Rs50ReportDecoder.Decode(report);

        Assert.Equal(
            "HID++ long report too short (1 bytes)",
            result);
    }

    [Fact]
    public void Decode_ThreeByteLongReport_ReturnsDiagnosticMessage()
    {
        byte[] report = [0x11, 0x01, 0x01];

        string result = Rs50ReportDecoder.Decode(report);

        Assert.Equal(
            "HID++ long report too short (3 bytes)",
            result);
    }
}
