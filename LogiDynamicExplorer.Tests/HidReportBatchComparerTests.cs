using LogiDynamicExplorer.Diagnostics;

namespace LogiDynamicExplorer.Tests;

public sealed class HidReportBatchComparerTests
{
    [Fact]
    public void Compare_ReportsOnlyPositiveExactCountDeltas()
    {
        string[] baseline =
        [
            "HOST\t1\t1000.0\t10ff052b010203",
            "DEVICE\t2\t1000.1\t11ff052b0102030000000000000000000000000000"
        ];
        string[] candidate =
        [
            "HOST\t3\t1005.0\t10ff052b010203",
            "HOST\t4\t1005.1\t10ff062b040506",
            "HOST\t5\t1005.2\t10ff062b040506",
            "DEVICE\t6\t1005.3\t11ff052b0102030000000000000000000000000000"
        ];

        IReadOnlyList<string> result =
            HidReportBatchComparer.Compare(baseline, candidate);

        Assert.Contains("Baseline HID++ reports: 2", result);
        Assert.Contains("Candidate HID++ reports: 4", result);
        Assert.Contains(
            "  +2 HOST ID 0x10, device 0xFF, feature 0x06, " +
            "function 0x02, SW-ID 0x0B, parameters 040506",
            result);
        Assert.DoesNotContain(
            result,
            line => line.StartsWith("  +") && line.Contains("feature 0x05"));
    }

    [Fact]
    public void Compare_DoesNotTreatTimestampsAsPartOfSignature()
    {
        IReadOnlyList<string> result = HidReportBatchComparer.Compare(
            ["HOST\t1\t1000.0\t10ff052b010203"],
            ["HOST\t99\t2000.0\t10ff052b010203"]);

        Assert.Contains("  (none)", result);
    }

    [Fact]
    public void Compare_RejectsNonDirectionalAndIgnoresNonHidppLines()
    {
        IReadOnlyList<string> result = HidReportBatchComparer.Compare(
            ["UNKNOWN\t1\t1000.0\t10ff052b010203"],
            ["DEVICE\t2\t1001.0\t01020304"]);

        Assert.Contains("Baseline HID++ reports: 0", result);
        Assert.Contains("Candidate HID++ reports: 0", result);
        Assert.Contains("Baseline invalid lines: 1", result);
        Assert.Contains("Candidate invalid lines: 0", result);
        Assert.Contains("Baseline non-HID++ reports ignored: 0", result);
        Assert.Contains("Candidate non-HID++ reports ignored: 1", result);
    }
}
