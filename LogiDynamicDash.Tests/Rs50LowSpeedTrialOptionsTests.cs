using LogiDynamicDash.Configuration;

namespace LogiDynamicDash.Tests;

public sealed class Rs50LowSpeedTrialOptionsTests
{
    [Fact]
    public void TryParse_AcceptsOnlyCompleteOrderedArmingContract()
    {
        Assert.True(
            Rs50LowSpeedTrialOptions.TryParse(
                ValidArguments(),
                out Rs50LowSpeedTrialOptions? options));

        Assert.NotNull(options);
        Assert.Equal("settings.json", options.ConfigurationPath);
        Assert.Equal(
            20f / 3.6f,
            Rs50LowSpeedTrialOptions.MaximumSpeedMetersPerSecond);
        Assert.Equal(
            TimeSpan.FromSeconds(15),
            Rs50LowSpeedTrialOptions.Duration);
    }

    [Fact]
    public void TryParse_RejectsMissingReorderedOrExtraArgument()
    {
        string[] missing = ValidArguments()[..^1];
        Assert.False(Rs50LowSpeedTrialOptions.TryParse(missing, out _));

        string[] reordered = ValidArguments();
        (reordered[6], reordered[7]) =
            (reordered[7], reordered[6]);
        Assert.False(Rs50LowSpeedTrialOptions.TryParse(reordered, out _));

        string[] extra = [.. ValidArguments(), "--extra"];
        Assert.False(Rs50LowSpeedTrialOptions.TryParse(extra, out _));
    }

    [Fact]
    public void TryParse_RejectsEmptyConfigurationPath()
    {
        string[] arguments = ValidArguments();
        arguments[9] = " ";

        Assert.False(Rs50LowSpeedTrialOptions.TryParse(arguments, out _));
    }

    internal static string[] ValidArguments() =>
    [
        "--enable-rs50-oled-low-speed-trial",
        "--confirm-ghub-closed",
        "--confirm-iracing-running",
        "--confirm-controlled-pit-lane",
        "--confirm-rs50-dynamic-selected",
        "--confirm-15-second-limit",
        "--confirm-maximum-20-kmh",
        "--acknowledge-stop-on-speed-limit",
        "--config",
        "settings.json",
        "--confirm-settings"
    ];
}
