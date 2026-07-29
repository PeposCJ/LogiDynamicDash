using LogiDynamicExplorer.Diagnostics;

namespace LogiDynamicExplorer.Tests;

public sealed class HidReportBatchAnalyzerTests
{
    [Fact]
    public void Analyze_GroupsReportsAndMatchesResponseHeaders()
    {
        string[] lines =
        [
            "# direction, optional timestamp, report",
            "HOST\t1.000\t10ff052b010203",
            "DEVICE\t1.001\t11ff052b0102030000000000000000000000000000",
            "HOST\t1.002\t10ff062b040506",
            "HOST\t1.003\t10ff062b040506",
            "",
            "DEVICE\t1.004\t12ff001700000000"
        ];

        IReadOnlyList<string> result = HidReportBatchAnalyzer.Analyze(lines);

        Assert.Contains("Parsed reports: 5", result);
        Assert.Contains("Directions: HOST 3, DEVICE 2, UNKNOWN 0", result);
        Assert.Contains(
            "Report IDs: HOST 0x10=3, DEVICE 0x11=1, DEVICE 0x12=1",
            result);
        Assert.Contains("Exact HID++ response headers: 1", result);
        Assert.Contains(
            "HOST HID++ requests without an exact header match: 2",
            result);
        Assert.Contains(
            "  HOST ID 0x10, device 0xFF, feature 0x06, " +
            "function 0x02, SW-ID 0x0B: 2 reports, 1 parameter signatures",
            result);
        Assert.DoesNotContain(result, line => line.Contains("ID 0x12"));
    }

    [Fact]
    public void Analyze_HexOnlyLineUsesUnknownDirection()
    {
        IReadOnlyList<string> result = HidReportBatchAnalyzer.Analyze(
            ["10ff052b010203"]);

        Assert.Contains("Directions: HOST 0, DEVICE 0, UNKNOWN 1", result);
        Assert.Contains(
            "  UNKNOWN ID 0x10, device 0xFF, feature 0x05, " +
            "function 0x02, SW-ID 0x0B: 1 reports, 1 parameter signatures",
            result);
    }

    [Fact]
    public void Analyze_InvalidLineIsReportedWithoutStoppingAnalysis()
    {
        IReadOnlyList<string> result = HidReportBatchAnalyzer.Analyze(
            ["SIDEWAYS\t1.000\t10ff052b010203", "HOST\t1.001\t10ff052b010203"]);

        Assert.Contains("Parsed reports: 1", result);
        Assert.Contains("Invalid lines: 1", result);
        Assert.Contains("  line 1: unknown direction 'SIDEWAYS'", result);
    }

    [Fact]
    public void Analyze_ReconstructsFeatureSetCatalogAcrossReportSizes()
    {
        string longResponse =
            "12ff011b81300000" + new string('0', 112);

        IReadOnlyList<string> result = HidReportBatchAnalyzer.Analyze(
        [
            "HOST\t1.000\t10ff011b120000",
            $"DEVICE\t1.001\t{longResponse}",
            "HOST\t1.002\t10ff122b000000"
        ]);

        Assert.Contains(
            "  device 0xFF, runtime 0x12 -> feature 0x8130 " +
            "(Display Game Data; G HUB static name), flags 0x00, version 0; " +
            "HOST operational requests 1",
            result);
    }

    [Fact]
    public void Analyze_DoesNotTreatFeatureSetEnumerationAsOperationalUse()
    {
        IReadOnlyList<string> result = HidReportBatchAnalyzer.Analyze(
        [
            "HOST\t10ff011b120000",
            "DEVICE\t11ff011b8130000000000000000000000000000000"
        ]);

        Assert.Contains(
            "  device 0xFF, runtime 0x12 -> feature 0x8130 " +
            "(Display Game Data; G HUB static name), flags 0x00, version 0; " +
            "HOST operational requests 0",
            result);
        Assert.Contains(
            "  device 0xFF, runtime 0x12: no operational reports",
            result);
    }

    [Fact]
    public void Analyze_DecodesResolvedDisplayGameDataActivity()
    {
        string layoutCReport =
            "12ff123b022a" + new string('0', 116);

        IReadOnlyList<string> result = HidReportBatchAnalyzer.Analyze(
        [
            "HOST\t10ff011b120000",
            "DEVICE\t11ff011b8130000000000000000000000000000000",
            $"HOST\t{layoutCReport}"
        ]);

        Assert.Contains(
            "  HOST device 0xFF, runtime 0x12, function 0x03: " +
            "1 reports, 1 parameter signatures",
            result);
        Assert.Contains("    set layout C: value 42/255 (16.5%)", result);
    }

    [Fact]
    public void Analyze_DecodesDisplayDescriptorIndexAndOneBasedId()
    {
        string descriptorResponse =
            "11ff121b090a130a130a" + new string('0', 20);

        IReadOnlyList<string> result = HidReportBatchAnalyzer.Analyze(
        [
            "HOST\t10ff011b120000",
            "DEVICE\t11ff011b8130000000000000000000000000000000",
            "HOST\t10ff121b090000",
            $"DEVICE\t{descriptorResponse}"
        ]);

        Assert.Contains(
            "    layout J, index 9, ID 10, capabilities 13 0A 13 0A",
            result);
    }

    [Fact]
    public void Analyze_CountsLongDisplayGameDataRequestAsOperationalUse()
    {
        string layoutIReport =
            "12ff123b08" + new string('0', 118);

        IReadOnlyList<string> result = HidReportBatchAnalyzer.Analyze(
        [
            "HOST\t10ff011b120000",
            "DEVICE\t11ff011b8130000000000000000000000000000000",
            $"HOST\t{layoutIReport}"
        ]);

        Assert.Contains(
            "  device 0xFF, runtime 0x12 -> feature 0x8130 " +
            "(Display Game Data; G HUB static name), flags 0x00, version 0; " +
            "HOST operational requests 1",
            result);
        Assert.Contains(
            "  HOST device 0xFF, runtime 0x12, function 0x03: " +
            "1 reports, 1 parameter signatures",
            result);
    }

    [Fact]
    public void Analyze_MatchesVeryLongRootResponseAndDiscoversDisplayFeature()
    {
        string response =
            "12ff000e120000" + new string('0', 114);

        IReadOnlyList<string> result = HidReportBatchAnalyzer.Analyze(
        [
            "HOST\t29\t1785212625.021284\t10ff000e813000",
            $"DEVICE\t31\t1785212625.024044\t{response}"
        ]);

        Assert.Contains("Exact HID++ response headers: 1", result);
        Assert.Contains(
            "  device 0xFF, runtime 0x12 -> feature 0x8130 " +
            "(Display Game Data; G HUB static name), flags 0x00, version 0; " +
            "HOST operational requests 0",
            result);
        Assert.Contains(
            "  device 0xFF, runtime 0x12: no operational reports",
            result);
    }
}
