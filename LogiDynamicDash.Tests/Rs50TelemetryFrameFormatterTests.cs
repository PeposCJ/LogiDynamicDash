using LogiDynamicDash.Displays;
using LogiDynamicDash.Hidpp;
using LogiDynamicDash.Models;

namespace LogiDynamicDash.Tests;

public sealed class Rs50TelemetryFrameFormatterTests
{
    [Theory]
    [InlineData(0, typeof(Rs50LayoutAFrame))]
    [InlineData(1, typeof(Rs50LayoutBFrame))]
    [InlineData(2, typeof(Rs50LayoutCFrame))]
    [InlineData(3, typeof(Rs50LayoutDFrame))]
    [InlineData(4, typeof(Rs50LayoutEFrame))]
    [InlineData(5, typeof(Rs50LayoutFFrame))]
    [InlineData(6, typeof(Rs50LayoutGFrame))]
    [InlineData(7, typeof(Rs50LayoutHFrame))]
    [InlineData(8, typeof(Rs50LayoutIFrame))]
    [InlineData(9, typeof(Rs50LayoutJFrame))]
    public void Format_SupportsEveryConfirmedLayout(
        int layoutValue,
        Type expectedType)
    {
        Rs50OledLayout layout = (Rs50OledLayout)layoutValue;
        Rs50TelemetryFrameFormatter formatter =
            new(new Rs50OledConfiguration(layout));

        Rs50OledFrame frame =
            formatter.Format(ConnectedSnapshot(), DisplayMode.Normal);

        Assert.IsType(expectedType, frame);
        Rs50OledProtocol.CreateLayout(0x12, frame);
    }

    [Fact]
    public void LayoutE_MapsGearSpeedRpmAndSpeedGauge()
    {
        TelemetrySnapshot snapshot = ConnectedSnapshot();
        snapshot.Gear = 3;
        snapshot.SpeedMetersPerSecond = 100f / 3.6f;
        snapshot.Rpm = 4000;
        Rs50TelemetryFrameFormatter formatter =
            new(new Rs50OledConfiguration(
                Rs50OledLayout.E,
                maximumRpm: 8000,
                gaugeMaximumSpeed: 200));

        Rs50LayoutEFrame frame = Assert.IsType<Rs50LayoutEFrame>(
            formatter.Format(snapshot, DisplayMode.Normal));

        Assert.Equal("100 KMH", frame.LeftText);
        Assert.Equal("3", frame.RightText);
        Assert.Equal(128, frame.MainGauge.WireValue);
        Assert.Equal(128, frame.ThinIndicator.WireValue);
        Rs50OledProtocol.CreateLayout(0x12, frame);
    }

    [Fact]
    public void MilesPerHour_UsesMphAndSelectedGaugeUnits()
    {
        TelemetrySnapshot snapshot = ConnectedSnapshot();
        snapshot.SpeedMetersPerSecond = 44.70401f;
        Rs50TelemetryFrameFormatter formatter =
            new(new Rs50OledConfiguration(
                Rs50OledLayout.E,
                SpeedUnit.MilesPerHour,
                gaugeMaximumSpeed: 200));

        Rs50LayoutEFrame frame = Assert.IsType<Rs50LayoutEFrame>(
            formatter.Format(snapshot, DisplayMode.Normal));

        Assert.Equal("100 MPH", frame.LeftText);
        Assert.Equal(128, frame.ThinIndicator.WireValue);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void EveryLayoutAndMode_ProducesEncodableBoundedFrame(
        int modeValue)
    {
        DisplayMode mode = (DisplayMode)modeValue;
        TelemetrySnapshot snapshot = ConnectedSnapshot();
        snapshot.SpeedMetersPerSecond = 1000;
        snapshot.Rpm = 50000;
        snapshot.Gear = 99;
        snapshot.BrakeBiasPercent = 100;
        snapshot.LastLapTimeSeconds = 5999;

        foreach (Rs50OledLayout layout in Enum.GetValues<Rs50OledLayout>())
        {
            Rs50TelemetryFrameFormatter formatter =
                new(new Rs50OledConfiguration(layout));
            Rs50OledFrame frame = formatter.Format(snapshot, mode);

            Rs50OledProtocol.CreateLayout(0x12, frame);
        }
    }

    [Fact]
    public void InvalidTelemetry_UsesSafePlaceholdersAndEmptyGauges()
    {
        TelemetrySnapshot snapshot = ConnectedSnapshot();
        snapshot.SpeedMetersPerSecond = float.NaN;
        snapshot.Rpm = float.PositiveInfinity;
        snapshot.Gear = 100;
        Rs50TelemetryFrameFormatter formatter =
            new(new Rs50OledConfiguration(Rs50OledLayout.E));

        Rs50LayoutEFrame frame = Assert.IsType<Rs50LayoutEFrame>(
            formatter.Format(snapshot, DisplayMode.Normal));

        Assert.Equal("--- KMH", frame.LeftText);
        Assert.Equal("?", frame.RightText);
        Assert.Equal(0, frame.MainGauge.WireValue);
        Assert.Equal(0, frame.ThinIndicator.WireValue);
    }

    [Fact]
    public void Configuration_RejectsInvalidGaugeScales()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new Rs50OledConfiguration(
                Rs50OledLayout.E,
                maximumRpm: 0));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new Rs50OledConfiguration(
                Rs50OledLayout.E,
                gaugeMaximumSpeed: double.NaN));
    }

    private static TelemetrySnapshot ConnectedSnapshot() =>
        new()
        {
            ConnectionState = "CONNECTED",
            Gear = 0,
            SpeedMetersPerSecond = 0,
            Rpm = 0,
            BrakeBiasPercent = 52.3f,
            LastLapTimeSeconds = 92.481f
        };
}
