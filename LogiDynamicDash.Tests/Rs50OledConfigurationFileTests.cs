using LogiDynamicDash.Configuration;
using LogiDynamicDash.Models;

namespace LogiDynamicDash.Tests;

public sealed class Rs50OledConfigurationFileTests
{
    private const string ValidJson =
        """
        {
          "schemaVersion": 1,
          "layout": "E",
          "speedUnit": "KMH",
          "maximumRpm": 8000,
          "gaugeMaximumSpeed": 300
        }
        """;

    [Fact]
    public void Parse_AcceptsStrictVersionedConfiguration()
    {
        Rs50OledConfiguration configuration =
            Rs50OledConfigurationFile.Parse(ValidJson);

        Assert.Equal(Rs50OledLayout.E, configuration.Layout);
        Assert.Equal(
            SpeedUnit.KilometersPerHour,
            configuration.SpeedUnit);
        Assert.Equal(8000, configuration.MaximumRpm);
        Assert.Equal(300, configuration.GaugeMaximumSpeed);
    }

    [Fact]
    public void Parse_AcceptsVersionTwoPerModeLayouts()
    {
        const string json =
            """
            {
              "schemaVersion": 2,
              "layouts": {
                "normal": "E",
                "brakeBias": "H",
                "lastLap": "J",
                "connectionProblem": "A"
              },
              "speedUnit": "MPH",
              "maximumRpm": 9000,
              "gaugeMaximumSpeed": 200
            }
            """;

        Rs50OledConfiguration configuration =
            Rs50OledConfigurationFile.Parse(json);

        Assert.Equal(Rs50OledLayout.E, configuration.Layout);
        Assert.Equal(
            Rs50OledLayout.H,
            configuration.LayoutFor(DisplayMode.BrakeBias));
        Assert.Equal(
            Rs50OledLayout.J,
            configuration.LayoutFor(DisplayMode.LastLap));
        Assert.Equal(
            Rs50OledLayout.A,
            configuration.LayoutFor(DisplayMode.ConnectionProblem));
    }

    [Fact]
    public void Serialize_WritesStrictRoundTrippableVersionTwoConfiguration()
    {
        Rs50OledConfiguration original =
            DisciplineProfileRecommendations.Create(
                DrivingDiscipline.Oval,
                SpeedUnit.MilesPerHour).Configuration;

        string json = Rs50OledConfigurationFile.Serialize(original);
        Rs50OledConfiguration parsed =
            Rs50OledConfigurationFile.Parse(json);

        Assert.Contains("\"schemaVersion\": 2", json);
        Assert.Equal(
            original.LayoutFor(DisplayMode.Normal),
            parsed.LayoutFor(DisplayMode.Normal));
        Assert.Equal(original.SpeedUnit, parsed.SpeedUnit);
        Assert.Equal(original.MaximumRpm, parsed.MaximumRpm);
        Assert.Equal(
            original.GaugeMaximumSpeed,
            parsed.GaugeMaximumSpeed);
    }

    [Theory]
    [InlineData("""{"schemaVersion":2,"layout":"E","speedUnit":"KMH","maximumRpm":8000,"gaugeMaximumSpeed":300}""")]
    [InlineData("""{"schemaVersion":2,"layouts":{"normal":"E","brakeBias":"H","lastLap":"J"},"speedUnit":"KMH","maximumRpm":8000,"gaugeMaximumSpeed":300}""")]
    [InlineData("""{"schemaVersion":1,"layout":"e","speedUnit":"KMH","maximumRpm":8000,"gaugeMaximumSpeed":300}""")]
    [InlineData("""{"schemaVersion":1,"layout":"E","speedUnit":"kmh","maximumRpm":8000,"gaugeMaximumSpeed":300}""")]
    [InlineData("""{"schemaVersion":1,"layout":"E","speedUnit":"KMH","maximumRpm":999,"gaugeMaximumSpeed":300}""")]
    [InlineData("""{"schemaVersion":1,"layout":"E","speedUnit":"KMH","maximumRpm":8000,"gaugeMaximumSpeed":501}""")]
    [InlineData("""{"schemaVersion":1,"layout":"E","speedUnit":"KMH","maximumRpm":8000,"gaugeMaximumSpeed":300,"extra":true}""")]
    [InlineData("""{"schemaVersion":1,"schemaVersion":1,"layout":"E","speedUnit":"KMH","maximumRpm":8000,"gaugeMaximumSpeed":300}""")]
    public void Parse_RejectsInvalidOrAmbiguousConfiguration(string json)
    {
        Assert.ThrowsAny<Exception>(
            () => Rs50OledConfigurationFile.Parse(json));
    }

    [Fact]
    public void Parse_RejectsMissingPropertyAndOversizedInput()
    {
        Assert.Throws<InvalidDataException>(
            () => Rs50OledConfigurationFile.Parse(
                """{"schemaVersion":1}"""));
        Assert.Throws<InvalidDataException>(
            () => Rs50OledConfigurationFile.Parse(
                new string(' ', 16 * 1024 + 1)));
    }
}
