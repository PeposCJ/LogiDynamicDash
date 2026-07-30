using LogiDynamicDash.Configuration;

namespace LogiDynamicDash.Tests;

public sealed class Rs50ProductionRunOptionsTests
{
    [Fact]
    public void MinimalArguments_EnableAutomaticProductionRuntime()
    {
        Assert.True(
            Rs50ProductionRunOptions.TryParse(
                ["--run-rs50-oled", "--config", "dash.json"],
                out Rs50ProductionRunOptions? options));

        Assert.NotNull(options);
        Assert.True(options.AutomaticProfiles);
        Assert.Equal(5, options.LastLapDisplaySeconds);
        Assert.EndsWith(
            Path.Combine("LogiDynamicDash", "profiles"),
            options.ProfileDirectory);
    }

    [Fact]
    public void OptionalArguments_SelectManualProfileAndDuration()
    {
        Assert.True(
            Rs50ProductionRunOptions.TryParse(
                [
                    "--run-rs50-oled",
                    "--config",
                    "dash.json",
                    "--profiles",
                    "profiles",
                    "--manual-profile",
                    "--last-lap-seconds",
                    "8.5"
                ],
                out Rs50ProductionRunOptions? options));

        Assert.NotNull(options);
        Assert.False(options.AutomaticProfiles);
        Assert.Equal("profiles", options.ProfileDirectory);
        Assert.Equal(8.5, options.LastLapDisplaySeconds);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("16")]
    [InlineData("NaN")]
    public void InvalidLastLapDuration_IsRejected(string value)
    {
        Assert.False(
            Rs50ProductionRunOptions.TryParse(
                [
                    "--run-rs50-oled",
                    "--config",
                    "dash.json",
                    "--last-lap-seconds",
                    value
                ],
                out _));
    }
}
