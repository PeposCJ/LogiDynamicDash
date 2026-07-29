using LogiDynamicDash.Offline;

namespace LogiDynamicDash.Tests;

public sealed class OfflineCommandLineTests
{
    [Theory]
    [InlineData("--preview-all", 0)]
    [InlineData("--simulate-all", 1)]
    public void TryParse_AcceptsExactOfflineCommands(
        string verb,
        int expectedKind)
    {
        Assert.True(
            OfflineCommandLine.TryParse(
                [verb, "--config", "settings.json"],
                out OfflineCommand? command));

        Assert.NotNull(command);
        Assert.Equal((OfflineCommandKind)expectedKind, command.Kind);
        Assert.Equal("settings.json", command.ConfigurationPath);
    }

    [Fact]
    public void TryParse_AcceptsExactReplayCommand()
    {
        Assert.True(
            OfflineCommandLine.TryParse(
                [
                    "--replay",
                    "--config",
                    "settings.json",
                    "--telemetry",
                    "scenario.json"
                ],
                out OfflineCommand? command));

        Assert.NotNull(command);
        Assert.Equal(OfflineCommandKind.Replay, command.Kind);
        Assert.Equal("settings.json", command.ConfigurationPath);
        Assert.Equal("scenario.json", command.TelemetryPath);
    }

    [Theory]
    [InlineData("--preview")]
    [InlineData("--simulate")]
    [InlineData("--preview-all")]
    public void TryParse_RejectsIncompleteOrUnknownCommands(string verb)
    {
        Assert.False(
            OfflineCommandLine.TryParse([verb], out _));
        Assert.False(
            OfflineCommandLine.TryParse(
                [verb, "--wrong", "settings.json"],
                out _));
    }
}
