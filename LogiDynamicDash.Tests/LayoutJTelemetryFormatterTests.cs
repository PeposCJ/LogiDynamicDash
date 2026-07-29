using System.Globalization;
using LogiDynamicDash.Displays;
using LogiDynamicDash.Models;

namespace LogiDynamicDash.Tests;

public sealed class LayoutJTelemetryFormatterTests
{
    [Theory]
    [InlineData(-1, "R")]
    [InlineData(0, "N")]
    [InlineData(1, "1")]
    [InlineData(9, "9")]
    [InlineData(10, "?")]
    public void NormalMode_MapsSpeedAndGearToFourRows(
        int gear,
        string expectedGear)
    {
        TelemetrySnapshot snapshot = new()
        {
            SpeedMetersPerSecond = 55.5f,
            Gear = gear
        };

        LayoutJFrame frame = LayoutJTelemetryFormatter.Format(
            snapshot,
            DisplayMode.Normal);

        Assert.Equal("SPEED", frame.Line1);
        Assert.Equal("200 KMH", frame.Line2);
        Assert.Equal("GEAR", frame.Line3);
        Assert.Equal(expectedGear, frame.Line4);
        AssertFitsLayoutJ(frame);
    }

    [Fact]
    public void NormalMode_RejectsInvalidTelemetryWithoutInvalidText()
    {
        TelemetrySnapshot snapshot = new()
        {
            SpeedMetersPerSecond = float.NaN,
            Gear = null
        };

        LayoutJFrame frame = LayoutJTelemetryFormatter.Format(
            snapshot,
            DisplayMode.Normal);

        Assert.Equal("N/A", frame.Line2);
        Assert.Equal("?", frame.Line4);
        AssertFitsLayoutJ(frame);
    }

    [Theory]
    [InlineData(-1.0f, "N/A")]
    [InlineData(float.PositiveInfinity, "N/A")]
    [InlineData(1000.0f, "999 KMH")]
    public void NormalMode_BoundsInvalidOrExtremeSpeed(
        float metersPerSecond,
        string expected)
    {
        TelemetrySnapshot snapshot = new()
        {
            SpeedMetersPerSecond = metersPerSecond,
            Gear = 1
        };

        LayoutJFrame frame = LayoutJTelemetryFormatter.Format(
            snapshot,
            DisplayMode.Normal);

        Assert.Equal(expected, frame.Line2);
        AssertFitsLayoutJ(frame);
    }

    [Fact]
    public void BrakeBiasMode_UsesRecoveredSecondRowLimit()
    {
        TelemetrySnapshot snapshot = new()
        {
            BrakeBiasPercent = 54.25f
        };

        LayoutJFrame frame = LayoutJTelemetryFormatter.Format(
            snapshot,
            DisplayMode.BrakeBias);

        Assert.Equal("BRAKE BIAS", frame.Line1);
        Assert.Equal("54.2%", frame.Line2);
        AssertFitsLayoutJ(frame);
    }

    [Fact]
    public void LastLapMode_FormatsTimeWithinTenCharacters()
    {
        TelemetrySnapshot snapshot = new()
        {
            LastLapTimeSeconds = 83.456f
        };

        LayoutJFrame frame = LayoutJTelemetryFormatter.Format(
            snapshot,
            DisplayMode.LastLap);

        Assert.Equal("LAST LAP", frame.Line1);
        Assert.Equal("1:23.456", frame.Line2);
        AssertFitsLayoutJ(frame);
    }

    [Theory]
    [InlineData(0.0f)]
    [InlineData(6000.0f)]
    [InlineData(float.NaN)]
    public void LastLapMode_RejectsInvalidOrOversizedTimes(float seconds)
    {
        TelemetrySnapshot snapshot = new()
        {
            LastLapTimeSeconds = seconds
        };

        LayoutJFrame frame = LayoutJTelemetryFormatter.Format(
            snapshot,
            DisplayMode.LastLap);

        Assert.Equal("N/A", frame.Line2);
        AssertFitsLayoutJ(frame);
    }

    [Theory]
    [InlineData("WAITING", "WAITING")]
    [InlineData("ERROR", "ERROR")]
    [InlineData("unexpected long status", "OFFLINE")]
    public void ConnectionProblemMode_UsesBoundedStatus(
        string state,
        string expected)
    {
        TelemetrySnapshot snapshot = new()
        {
            ConnectionState = state
        };

        LayoutJFrame frame = LayoutJTelemetryFormatter.Format(
            snapshot,
            DisplayMode.ConnectionProblem);

        Assert.Equal("IRACING", frame.Line1);
        Assert.Equal(expected, frame.Line2);
        AssertFitsLayoutJ(frame);
    }

    [Fact]
    public void Frame_RejectsOversizedOrUnsupportedText()
    {
        Assert.Throws<ArgumentException>(
            () => new LayoutJFrame(
                new string('A', LayoutJFrame.Line1MaximumLength + 1),
                string.Empty,
                string.Empty,
                string.Empty));
        Assert.Throws<ArgumentException>(
            () => new LayoutJFrame(
                "SPEED 🏁",
                string.Empty,
                string.Empty,
                string.Empty));
    }

    [Fact]
    public void Formatter_IsIndependentOfCurrentCulture()
    {
        CultureInfo previousCulture = CultureInfo.CurrentCulture;

        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("es-MX");
            TelemetrySnapshot snapshot = new()
            {
                BrakeBiasPercent = 54.2f
            };

            LayoutJFrame frame = LayoutJTelemetryFormatter.Format(
                snapshot,
                DisplayMode.BrakeBias);

            Assert.Equal("54.2%", frame.Line2);
        }
        finally
        {
            CultureInfo.CurrentCulture = previousCulture;
        }
    }

    private static void AssertFitsLayoutJ(LayoutJFrame frame)
    {
        Assert.True(frame.Line1.Length <= LayoutJFrame.Line1MaximumLength);
        Assert.True(frame.Line2.Length <= LayoutJFrame.Line2MaximumLength);
        Assert.True(frame.Line3.Length <= LayoutJFrame.Line3MaximumLength);
        Assert.True(frame.Line4.Length <= LayoutJFrame.Line4MaximumLength);

        Assert.All(
            new[] { frame.Line1, frame.Line2, frame.Line3, frame.Line4 },
            line => Assert.All(
                line,
                character => Assert.InRange(character, (char)0x20, (char)0x7F)));
    }
}
