using LogiDynamicDash.Configuration;
namespace LogiDynamicDash.Tests;

public sealed class Rs50StationaryTrialOptionsTests
{
    [Fact]
    public void TryParse_AcceptsOnlyCompleteOrderedArmingContract()
    {
        Assert.True(
            Rs50StationaryTrialOptions.TryParse(
                ValidArguments(),
                out Rs50StationaryTrialOptions? options));

        Assert.NotNull(options);
        Assert.Equal("settings.json", options.ConfigurationPath);
    }

    [Fact]
    public void TryParse_RejectsMissingReorderedOrExtraArgument()
    {
        string[] missing = ValidArguments()[..^1];
        Assert.False(
            Rs50StationaryTrialOptions.TryParse(missing, out _));

        string[] reordered = ValidArguments();
        (reordered[1], reordered[2]) = (reordered[2], reordered[1]);
        Assert.False(
            Rs50StationaryTrialOptions.TryParse(reordered, out _));

        string[] extra = [.. ValidArguments(), "--extra"];
        Assert.False(
            Rs50StationaryTrialOptions.TryParse(extra, out _));
    }

    [Fact]
    public void TryParse_RejectsEmptyConfigurationPath()
    {
        string[] arguments = ValidArguments();
        arguments[8] = " ";

        Assert.False(
            Rs50StationaryTrialOptions.TryParse(arguments, out _));
    }

    internal static string[] ValidArguments() =>
    [
        "--enable-rs50-oled-stationary-trial",
        "--confirm-ghub-closed",
        "--confirm-iracing-running",
        "--confirm-car-stationary-in-pits",
        "--confirm-rs50-dynamic-selected",
        "--confirm-10-second-limit",
        "--acknowledge-no-moving-car-use",
        "--config",
        "settings.json",
        "--confirm-settings"
    ];
}
