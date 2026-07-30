using LogiDynamicDash.Configuration;

namespace LogiDynamicDash.Tests;

public sealed class Rs50DrivingTrialOptionsTests
{
    [Fact]
    public void TryParse_AcceptsOnlyCompleteOrderedArmingContract()
    {
        Assert.True(
            Rs50DrivingTrialOptions.TryParse(
                ValidArguments(),
                out Rs50DrivingTrialOptions? options));

        Assert.NotNull(options);
        Assert.Equal("settings.json", options.ConfigurationPath);
    }

    [Fact]
    public void TryParse_RejectsMissingReorderedOrExtraArgument()
    {
        string[] missing = ValidArguments()[..^1];
        Assert.False(Rs50DrivingTrialOptions.TryParse(missing, out _));

        string[] reordered = ValidArguments();
        (reordered[5], reordered[6]) =
            (reordered[6], reordered[5]);
        Assert.False(Rs50DrivingTrialOptions.TryParse(reordered, out _));

        string[] extra = [.. ValidArguments(), "--extra"];
        Assert.False(Rs50DrivingTrialOptions.TryParse(extra, out _));
    }

    [Fact]
    public void TryParse_RejectsEmptyConfigurationPath()
    {
        string[] arguments = ValidArguments();
        arguments[8] = " ";

        Assert.False(Rs50DrivingTrialOptions.TryParse(arguments, out _));
    }

    internal static string[] ValidArguments() =>
    [
        "--enable-rs50-oled-driving-trial",
        "--confirm-ghub-closed",
        "--confirm-iracing-running",
        "--confirm-controlled-driving-session",
        "--confirm-rs50-dynamic-selected",
        "--acknowledge-no-speed-limit",
        "--acknowledge-manual-stop-required",
        "--config",
        "settings.json",
        "--confirm-settings"
    ];
}
