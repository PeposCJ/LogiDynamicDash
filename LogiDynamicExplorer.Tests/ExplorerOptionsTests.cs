using LogiDynamicExplorer.Diagnostics;

namespace LogiDynamicExplorer.Tests;

public sealed class ExplorerOptionsTests
{
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
