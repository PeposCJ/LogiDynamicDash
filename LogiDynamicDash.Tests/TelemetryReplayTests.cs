using LogiDynamicDash.Offline;

namespace LogiDynamicDash.Tests;

public sealed class TelemetryReplayTests
{
    private const string ValidReplay =
        """
        {
          "schemaVersion": 1,
          "events": [{
            "atMilliseconds": 0,
            "statusChanged": true,
            "connectionState": "CONNECTED",
            "isOnTrack": false,
            "gear": 0,
            "rpm": 900,
            "speedMetersPerSecond": 0,
            "brakeBiasPercent": 52.3,
            "lastLapTimeSeconds": null
          }]
        }
        """;

    [Fact]
    public void Parse_AcceptsStrictReplay()
    {
        TelemetryReplayEvent replayEvent =
            Assert.Single(TelemetryReplayFile.Parse(ValidReplay));

        Assert.True(replayEvent.StatusChanged);
        Assert.Equal("CONNECTED", replayEvent.ConnectionState);
        Assert.Equal(0, replayEvent.Gear);
    }

    [Theory]
    [InlineData("""{"schemaVersion":2,"events":[]}""")]
    [InlineData("""{"schemaVersion":1,"events":[]}""")]
    [InlineData("""{"schemaVersion":1,"events":[{"atMilliseconds":0}]}""")]
    [InlineData("""{"schemaVersion":1,"events":[{"atMilliseconds":0,"statusChanged":true,"connectionState":"CONNECTED","isOnTrack":false,"gear":0,"rpm":900,"speedMetersPerSecond":0,"brakeBiasPercent":52,"lastLapTimeSeconds":null,"extra":1}]}""")]
    public void Parse_RejectsUnsupportedEmptyOrAmbiguousReplay(string json)
    {
        Assert.ThrowsAny<Exception>(() => TelemetryReplayFile.Parse(json));
    }

    [Fact]
    public void Parse_RejectsBackwardTimeAndNonFiniteNumbers()
    {
        const string backward =
            """
            {
              "schemaVersion": 1,
              "events": [
                {
                  "atMilliseconds": 1,
                  "statusChanged": true,
                  "connectionState": "CONNECTED",
                  "isOnTrack": false,
                  "gear": 0,
                  "rpm": 900,
                  "speedMetersPerSecond": 0,
                  "brakeBiasPercent": 52.3,
                  "lastLapTimeSeconds": null
                },
                {
                  "atMilliseconds": 0,
                  "statusChanged": false,
                  "connectionState": "CONNECTED",
                  "isOnTrack": false,
                  "gear": 0,
                  "rpm": 900,
                  "speedMetersPerSecond": 0,
                  "brakeBiasPercent": 52.3,
                  "lastLapTimeSeconds": null
                }
              ]
            }
            """;

        Assert.ThrowsAny<Exception>(() => TelemetryReplayFile.Parse(backward));
        Assert.ThrowsAny<Exception>(
            () => TelemetryReplayFile.Parse(
                ValidReplay.Replace(
                    "\"rpm\": 900",
                    "\"rpm\": 1e999",
                    StringComparison.Ordinal)));
    }
}
