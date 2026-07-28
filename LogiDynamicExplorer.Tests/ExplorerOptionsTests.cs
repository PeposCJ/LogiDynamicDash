using LogiDynamicExplorer.Diagnostics;

namespace LogiDynamicExplorer.Tests;

public sealed class ExplorerOptionsTests
{
    [Fact]
    public void Parse_OfflineDecode_ReturnsReportWithoutHardwareMode()
    {
        ExplorerOptions result = ExplorerOptions.Parse(
            ["--decode-report", "11 01 01 1D 80 93 00 00"]);

        Assert.Null(result.Error);
        Assert.Equal(
            "11 01 01 1D 80 93 00 00",
            result.DecodeReport);
        Assert.False(result.InventoryOnly);
        Assert.Null(result.MonitorCollection);
    }

    [Fact]
    public void Parse_OfflineDecodeWithHardwareMode_ReturnsError()
    {
        ExplorerOptions result = ExplorerOptions.Parse(
            ["--decode-report", "11", "--inventory"]);

        Assert.Equal(
            "--decode-report cannot be combined with hardware modes.",
            result.Error);
    }

    [Fact]
    public void Parse_OfflineBatchAnalysis_ReturnsFileWithoutHardwareMode()
    {
        ExplorerOptions result = ExplorerOptions.Parse(
            ["--analyze-reports", "reports.tsv"]);

        Assert.Null(result.Error);
        Assert.Equal("reports.tsv", result.AnalyzeReportFile);
        Assert.False(result.InventoryOnly);
        Assert.Null(result.MonitorCollection);
    }

    [Fact]
    public void Parse_BatchAnalysisWithHardwareMode_ReturnsError()
    {
        ExplorerOptions result = ExplorerOptions.Parse(
            ["--analyze-reports", "reports.tsv", "--inventory"]);

        Assert.Equal(
            "--analyze-reports cannot be combined with hardware modes.",
            result.Error);
    }

    [Fact]
    public void Parse_OfflineComparison_ReturnsBothFiles()
    {
        ExplorerOptions result = ExplorerOptions.Parse(
            ["--compare-reports", "baseline.tsv", "query.tsv"]);

        Assert.Null(result.Error);
        Assert.Equal("baseline.tsv", result.CompareBaselineFile);
        Assert.Equal("query.tsv", result.CompareCandidateFile);
        Assert.Null(result.MonitorCollection);
    }

    [Fact]
    public void Parse_ComparisonWithoutCandidate_ReturnsError()
    {
        ExplorerOptions result = ExplorerOptions.Parse(
            ["--compare-reports", "baseline.tsv"]);

        Assert.Equal(
            "--compare-reports requires baseline and candidate file paths.",
            result.Error);
    }

    [Fact]
    public void Parse_ComparisonWithHardwareMode_ReturnsError()
    {
        ExplorerOptions result = ExplorerOptions.Parse(
            [
                "--compare-reports",
                "baseline.tsv",
                "query.tsv",
                "--inventory"
            ]);

        Assert.Equal(
            "--compare-reports cannot be combined with hardware modes.",
            result.Error);
    }

    [Fact]
    public void Parse_TimeLimitedMonitor_ReturnsNormalizedOptions()
    {
        ExplorerOptions result = ExplorerOptions.Parse(
            ["--monitor", "mi_01_col03", "--duration", "15"]);

        Assert.Null(result.Error);
        Assert.Equal("MI_01_COL03", result.MonitorCollection);
        Assert.Equal(TimeSpan.FromSeconds(15), result.MonitorDuration);
    }

    [Fact]
    public void Parse_MonitorWithoutDuration_ReturnsError()
    {
        ExplorerOptions result = ExplorerOptions.Parse(
            ["--monitor", "MI_01_COL03"]);

        Assert.Equal(
            "--monitor requires --duration for a time-limited session.",
            result.Error);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("301")]
    [InlineData("invalid")]
    public void Parse_InvalidDuration_ReturnsError(
        string duration)
    {
        ExplorerOptions result = ExplorerOptions.Parse(
            ["--monitor", "MI_01_COL03", "--duration", duration]);

        Assert.Equal(
            "--duration must be between 1 and 300 seconds.",
            result.Error);
    }
}
