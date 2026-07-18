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
}
