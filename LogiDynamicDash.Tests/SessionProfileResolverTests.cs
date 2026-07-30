using LogiDynamicDash.Models;

namespace LogiDynamicDash.Tests;

public sealed class SessionProfileResolverTests
{
    [Theory]
    [InlineData("SportsCar", "E")]
    [InlineData("FormulaCar", "E")]
    [InlineData("Oval", "D")]
    [InlineData("DirtOval", "D")]
    [InlineData("DirtRoad", "E")]
    public void ExactCarAndCurrentCategory_SelectReviewedProfile(
        string disciplineName,
        string expectedLayoutName)
    {
        IRacingDiscipline discipline =
            Enum.Parse<IRacingDiscipline>(disciplineName);
        IRacingSessionIdentity identity = Identity(discipline, carId: 42);

        SessionProfileResolution resolution =
            SessionProfileResolver.Resolve(
                identity,
                SpeedUnit.KilometersPerHour);

        Assert.True(resolution.CanApply);
        Assert.Equal(42, resolution.CarKey?.CarId);
        Assert.Equal(discipline, resolution.CarKey?.Discipline);
        Assert.Equal(
            Enum.Parse<Rs50OledLayout>(expectedLayoutName),
            resolution.Recommendation?.Configuration.LayoutFor(
                DisplayMode.Normal));
    }

    [Theory]
    [InlineData("Unknown", 42)]
    [InlineData("LegacyRoad", 42)]
    [InlineData("SportsCar", null)]
    public void AmbiguousIdentity_RequiresManualProfile(
        string disciplineName,
        int? carId)
    {
        IRacingDiscipline discipline =
            Enum.Parse<IRacingDiscipline>(disciplineName);

        SessionProfileResolution resolution =
            SessionProfileResolver.Resolve(
                Identity(discipline, carId),
                SpeedUnit.KilometersPerHour);

        Assert.False(resolution.CanApply);
        Assert.Null(resolution.CarKey);
        Assert.Null(resolution.Recommendation);
        Assert.Contains("manual", resolution.Explanation);
    }

    [Fact]
    public void SameCategoryDifferentCar_ProducesDifferentStableKey()
    {
        SessionProfileResolution first = SessionProfileResolver.Resolve(
            Identity(IRacingDiscipline.SportsCar, 100),
            SpeedUnit.KilometersPerHour);
        SessionProfileResolution second = SessionProfileResolver.Resolve(
            Identity(IRacingDiscipline.SportsCar, 200),
            SpeedUnit.KilometersPerHour);

        Assert.NotEqual(first.CarKey, second.CarKey);
        Assert.Equal(
            first.Recommendation?.Configuration.LayoutFor(DisplayMode.Normal),
            second.Recommendation?.Configuration.LayoutFor(DisplayMode.Normal));
        Assert.Equal(
            first.Recommendation?.Configuration.MaximumRpm,
            second.Recommendation?.Configuration.MaximumRpm);
    }

    private static IRacingSessionIdentity Identity(
        IRacingDiscipline discipline,
        int? carId) =>
        new(
            discipline,
            discipline.ToString(),
            "road course",
            new CarIdentity(
                carId,
                "cars/example",
                "Example Car",
                "Example",
                12,
                "Example Class",
                false));
}
