using LogiDynamicDash.Displays;

namespace LogiDynamicDash.Tests;

public sealed class ProgramArmingTests
{
    [Fact]
    public void NoArguments_UsesSafeConsolePreview()
    {
        bool accepted = Program.TryCreateDashboard([], out IDisplaySink sink);

        Assert.True(accepted);
        Assert.IsType<DistinctDisplaySink>(sink);
    }

    [Theory]
    [InlineData("--enable-rs50-oled")]
    [InlineData(
        "--enable-rs50-oled",
        "--confirm-exclusive-layout-j-stream")]
    [InlineData(
        "--enable-rs50-oled",
        "--confirm-telemetry-transmission",
        "--confirm-exclusive-layout-j-stream")]
    [InlineData(
        "--enable-rs50-oled",
        "--confirm-exclusive-layout-j-stream",
        "--confirm-telemetry-transmission")]
    [InlineData("--unknown")]
    public void PartialWrongOrReorderedArguments_AreRejected(
        params string[] arguments)
    {
        bool accepted =
            Program.TryCreateDashboard(arguments, out _);

        Assert.False(accepted);
    }

    [Fact]
    public void ExactFourArguments_ArmsRs50CompositionWithoutOpeningHardware()
    {
        string[] arguments =
        [
            "--enable-rs50-oled",
            "--confirm-exclusive-layout-j-stream",
            "--confirm-telemetry-transmission",
            "--confirm-10-second-trial"
        ];

        bool accepted =
            Program.TryCreateDashboard(arguments, out IDisplaySink sink);

        Assert.True(accepted);
        Assert.IsType<DistinctDisplaySink>(sink);
    }
}
