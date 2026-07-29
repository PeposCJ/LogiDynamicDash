using LogiDynamicDash.Configuration;
using LogiDynamicDash.Models;

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
        Assert.Equal(
            Rs50OledLayout.E,
            options.OledConfiguration.Layout);
        Assert.Equal(
            SpeedUnit.KilometersPerHour,
            options.OledConfiguration.SpeedUnit);
        Assert.Equal(8000, options.OledConfiguration.MaximumRpm);
        Assert.Equal(
            300,
            options.OledConfiguration.GaugeMaximumSpeed);
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

    [Theory]
    [InlineData(8, "K")]
    [InlineData(8, "0")]
    [InlineData(10, "kmh")]
    [InlineData(12, "999")]
    [InlineData(12, "30001")]
    [InlineData(14, "9")]
    [InlineData(14, "501")]
    [InlineData(14, "NaN")]
    public void TryParse_RejectsInvalidTypedSetting(
        int offset,
        string value)
    {
        string[] arguments = ValidArguments();
        arguments[offset] = value;

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
        "--layout",
        "E",
        "--speed-unit",
        "KMH",
        "--maximum-rpm",
        "8000",
        "--gauge-maximum-speed",
        "300",
        "--confirm-settings"
    ];
}
