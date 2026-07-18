using LogiDynamicExplorer.Decoders;

namespace LogiDynamicExplorer.Tests;

public sealed class Rs50HidppShortReportDecoderTests
{
    [Fact]
    public void Decode_ShortReport_PreservesHeaderAndParameters()
    {
        byte[] report = [0x10, 0xFF, 0x05, 0x2B, 0x01, 0x02, 0x03];

        string result = Rs50ReportDecoder.Decode(report);

        Assert.Equal(
            "HID++ short: device 0xFF, feature index 0x05, " +
            "function 0x02, SW-ID 0x0B, parameters 01 02 03",
            result);
    }

    [Fact]
    public void Decode_HeaderOnlyShortReport_ReportsNoParameters()
    {
        byte[] report = [0x10, 0x01, 0x02, 0x3D];

        string result = Rs50ReportDecoder.Decode(report);

        Assert.Equal(
            "HID++ short: device 0x01, feature index 0x02, " +
            "function 0x03, SW-ID 0x0D, parameters (none)",
            result);
    }

    [Fact]
    public void Decode_IncompleteShortReport_ReturnsDiagnosticMessage()
    {
        byte[] report = [0x10, 0x01, 0x02];

        string result = Rs50ReportDecoder.Decode(report);

        Assert.Equal(
            "HID++ short report too short (3 bytes)",
            result);
    }
}
