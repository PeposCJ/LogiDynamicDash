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

    [Theory]
    [InlineData("""{"schemaVersion":2,"layout":"E","speedUnit":"KMH","maximumRpm":8000,"gaugeMaximumSpeed":300}""")]
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
