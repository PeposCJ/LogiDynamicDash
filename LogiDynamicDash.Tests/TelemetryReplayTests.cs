using LogiDynamicDash.Offline;
using LogiDynamicDash.Models;

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
        Assert.Null(replayEvent.SessionIdentity);
    }

    [Fact]
    public void VersionTwo_RoundTripsSessionAndCarIdentity()
    {
        TelemetryReplayEvent expected = new(
            0,
            true,
            "CONNECTED",
            true,
            3,
            7500,
            42,
            51.5f,
            90.2f,
            new IRacingSessionIdentity(
                IRacingDiscipline.FormulaCar,
                "FormulaCar",
                "road course",
                new CarIdentity(
                    123,
                    "formulacar example",
                    "Example Formula",
                    "Formula",
                    456,
                    "Formula Class",
                    false)));
        string path = Path.Combine(
            Path.GetTempPath(),
            $"logidynamicdash-{Guid.NewGuid():N}.json");
        try
        {
            TelemetryReplayFile.Save(path, [expected]);

            string json = File.ReadAllText(path);
            TelemetryReplayEvent actual =
                Assert.Single(TelemetryReplayFile.Parse(json));

            Assert.Contains("\"schemaVersion\": 2", json);
            Assert.Equal(expected.SessionIdentity, actual.SessionIdentity);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Theory]
    [InlineData("NotACategory")]
    [InlineData("formulacar")]
    public void VersionTwo_RejectsUnknownOrAmbiguousCategory(
        string discipline)
    {
        string json =
            $$"""
            {
              "schemaVersion": 2,
              "events": [{
                "atMilliseconds": 0,
                "statusChanged": true,
                "connectionState": "CONNECTED",
                "isOnTrack": false,
                "gear": 0,
                "rpm": 900,
                "speedMetersPerSecond": 0,
                "brakeBiasPercent": 52.3,
                "lastLapTimeSeconds": null,
                "sessionIdentity": {
                  "discipline": "{{discipline}}",
                  "rawCategory": "FormulaCar",
                  "trackType": "road course",
                  "car": null
                }
              }]
            }
            """;

        Assert.Throws<InvalidDataException>(
            () => TelemetryReplayFile.Parse(json));
    }

    [Theory]
    [InlineData("""{"schemaVersion":3,"events":[]}""")]
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
