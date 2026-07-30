using LogiDynamicDash.Models;

namespace LogiDynamicDash.Tests;

public sealed class DisciplineProfileRecommendationTests
{
    [Fact]
    public void SportsCar_UsesGearFocusedLayoutEAndClearTemporaryPages()
    {
        DisciplineProfileRecommendation recommendation =
            DisciplineProfileRecommendations.Create(
                IRacingDiscipline.SportsCar,
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
                IRacingDiscipline.Oval,
                SpeedUnit.MilesPerHour);

        Assert.Equal(
            Rs50OledLayout.D,
            recommendation.Configuration.LayoutFor(DisplayMode.Normal));
        Assert.Equal(9000, recommendation.Configuration.MaximumRpm);
        Assert.Equal(225, recommendation.Configuration.GaugeMaximumSpeed);
    }

    [Theory]
    [InlineData("SportsCar", "E", 8000, 300)]
    [InlineData("FormulaCar", "E", 12000, 350)]
    [InlineData("Oval", "D", 9000, 360)]
    [InlineData("DirtOval", "D", 8500, 180)]
    [InlineData("DirtRoad", "E", 9000, 220)]
    public void CurrentCategory_HasAnExplicitProfile(
        string disciplineName,
        string normalLayoutName,
        double maximumRpm,
        double maximumSpeed)
    {
        IRacingDiscipline discipline =
            Enum.Parse<IRacingDiscipline>(disciplineName);
        Rs50OledLayout normalLayout =
            Enum.Parse<Rs50OledLayout>(normalLayoutName);
        DisciplineProfileRecommendation recommendation =
            DisciplineProfileRecommendations.Create(
                discipline,
                SpeedUnit.KilometersPerHour);

        Assert.Equal(normalLayout, recommendation.Configuration.LayoutFor(
            DisplayMode.Normal));
        Assert.Equal(maximumRpm, recommendation.Configuration.MaximumRpm);
        Assert.Equal(maximumSpeed, recommendation.Configuration.GaugeMaximumSpeed);
    }

    [Theory]
    [InlineData("Unknown")]
    [InlineData("LegacyRoad")]
    public void NonCurrentCategory_RequiresManualFallback(string disciplineName)
    {
        IRacingDiscipline discipline =
            Enum.Parse<IRacingDiscipline>(disciplineName);
        Assert.Throws<ArgumentOutOfRangeException>(
            () => DisciplineProfileRecommendations.Create(
                discipline,
                SpeedUnit.KilometersPerHour));
    }
}
