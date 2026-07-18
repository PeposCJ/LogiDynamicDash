using LogiDynamicExplorer.Diagnostics;

namespace LogiDynamicExplorer.Tests;

public sealed class HexReportParserTests
{
    [Theory]
    [InlineData("11 01 1D", new byte[] { 0x11, 0x01, 0x1D })]
    [InlineData("11011d", new byte[] { 0x11, 0x01, 0x1D })]
    [InlineData("11-01:1D", new byte[] { 0x11, 0x01, 0x1D })]
    public void TryParse_ValidHex_ReturnsBytes(
        string text,
        byte[] expected)
    {
        bool success = HexReportParser.TryParse(
            text,
            out byte[] report,
            out string? error);

        Assert.True(success);
        Assert.Null(error);
        Assert.Equal(expected, report);
    }

    [Theory]
    [InlineData("", "The report is empty.")]
    [InlineData("111", "The hexadecimal report must contain complete bytes.")]
    [InlineData("11 ZZ", "The report contains a non-hexadecimal value.")]
    public void TryParse_InvalidHex_ReturnsError(
        string text,
        string expectedError)
    {
        bool success = HexReportParser.TryParse(
            text,
            out byte[] report,
            out string? error);

        Assert.False(success);
        Assert.Empty(report);
        Assert.Equal(expectedError, error);
    }
}
