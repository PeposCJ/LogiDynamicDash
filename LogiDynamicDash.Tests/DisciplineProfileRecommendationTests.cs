using LogiDynamicDash.Models;

namespace LogiDynamicDash.Tests;

public sealed class DisciplineProfileRecommendationTests
{
    [Fact]
    public void Road_UsesGearFocusedLayoutEAndClearTemporaryPages()
    {
        DisciplineProfileRecommendation recommendation =
            DisciplineProfileRecommendations.Create(
                DrivingDiscipline.Road,
                SpeedUnit.KilometersPerHour);

        Assert.Equal(
            Rs50OledLayout.E,
            recommendation.Configuration.LayoutFor(DisplayMode.Normal));
        Assert.Equal(
            Rs50OledLayout.H,
            recommendation.Configuration.LayoutFor(DisplayMode.BrakeBias));
        Assert.Equal(
            Rs50OledLayout.J,
            recommendation.Configuration.LayoutFor(DisplayMode.LastLap));
        Assert.Equal(300, recommendation.Configuration.GaugeMaximumSpeed);
    }

    [Fact]
    public void Oval_UsesCompactLayoutDAndHigherSpeedScale()
    {
        DisciplineProfileRecommendation recommendation =
            DisciplineProfileRecommendations.Create(
                DrivingDiscipline.Oval,
                SpeedUnit.MilesPerHour);

        Assert.Equal(
            Rs50OledLayout.D,
            recommendation.Configuration.LayoutFor(DisplayMode.Normal));
        Assert.Equal(9000, recommendation.Configuration.MaximumRpm);
        Assert.Equal(225, recommendation.Configuration.GaugeMaximumSpeed);
    }
}
