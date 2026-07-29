using LogiDynamicDash.Models;
using LogiDynamicDash.Services;
using SVappsLAB.iRacingTelemetrySDK;

namespace LogiDynamicDash.Tests;

public sealed class IRacingSessionIdentityTests
{
    [Theory]
    [InlineData("SportsCar", "SportsCar")]
    [InlineData("sports car", "SportsCar")]
    [InlineData("FormulaCar", "FormulaCar")]
    [InlineData("formula_car", "FormulaCar")]
    [InlineData("Oval", "Oval")]
    [InlineData("DirtOval", "DirtOval")]
    [InlineData("dirt-road", "DirtRoad")]
    [InlineData("Road", "LegacyRoad")]
    [InlineData("FutureCategory", "Unknown")]
    public void CategoryParser_IsTolerantButDoesNotGuess(
        string rawCategory,
        string expectedName)
    {
        Assert.Equal(
            expectedName,
            IRacingDisciplineParser.Parse(rawCategory).ToString());
    }

    [Fact]
    public void Resolver_UsesOfficialCategoryAndCapturesDriverCarIdentity()
    {
        TelemetrySessionInfo session = new()
        {
            WeekendInfo = new WeekendInfo
            {
                Category = "SportsCar",
                TrackType = "oval"
            },
            DriverInfo = new DriverInfo
            {
                DriverCarIdx = 7,
                Drivers =
                [
                    new Driver
                    {
                        CarIdx = 3,
                        CarID = 111,
                        CarScreenName = "Other car"
                    },
                    new Driver
                    {
                        CarIdx = 7,
                        CarID = 222,
                        CarPath = "stockcars example",
                        CarScreenName = "Example GT",
                        CarScreenNameShort = "GT",
                        CarClassID = 333,
                        CarClassShortName = "GT3",
                        CarIsElectric = 1
                    }
                ]
            }
        };

        IRacingSessionIdentity identity =
            IRacingSessionIdentityResolver.Resolve(session);

        Assert.Equal(IRacingDiscipline.SportsCar, identity.Discipline);
        Assert.Equal("oval", identity.TrackType);
        Assert.NotNull(identity.Car);
        Assert.Equal(222, identity.Car.CarId);
        Assert.Equal("stockcars example", identity.Car.CarPath);
        Assert.Equal("Example GT", identity.Car.DisplayName);
        Assert.Equal("GT3", identity.Car.CarClassShortName);
        Assert.True(identity.Car.IsElectric);
    }

    [Fact]
    public void Resolver_MissingDriverFailsClosedWithoutInventingACar()
    {
        TelemetrySessionInfo session = new()
        {
            WeekendInfo = new WeekendInfo
            {
                Category = "DirtRoad",
                TrackType = "dirt road"
            },
            DriverInfo = new DriverInfo
            {
                DriverCarIdx = 9,
                Drivers = []
            }
        };

        IRacingSessionIdentity identity =
            IRacingSessionIdentityResolver.Resolve(session);

        Assert.Equal(IRacingDiscipline.DirtRoad, identity.Discipline);
        Assert.Null(identity.Car);
    }
}
