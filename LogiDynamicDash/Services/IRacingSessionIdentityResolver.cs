using LogiDynamicDash.Models;
using SVappsLAB.iRacingTelemetrySDK;

namespace LogiDynamicDash.Services;

internal static class IRacingSessionIdentityResolver
{
    internal static IRacingSessionIdentity Resolve(
        TelemetrySessionInfo session)
    {
        ArgumentNullException.ThrowIfNull(session);
        string rawCategory = Clean(session.WeekendInfo?.Category, 32);
        string trackType = Clean(session.WeekendInfo?.TrackType, 64);
        DriverInfo? driverInfo = session.DriverInfo;
        Driver? driver = driverInfo?.Drivers?.FirstOrDefault(
            candidate => candidate.CarIdx == driverInfo.DriverCarIdx);

        CarIdentity? car = driver is null
            ? null
            : new CarIdentity(
                PositiveOrNull(driver.CarID),
                Clean(driver.CarPath, 128),
                Clean(driver.CarScreenName, 128),
                Clean(driver.CarScreenNameShort, 64),
                PositiveOrNull(driver.CarClassID),
                Clean(driver.CarClassShortName, 64),
                driver.CarIsElectric != 0);

        return new IRacingSessionIdentity(
            IRacingDisciplineParser.Parse(rawCategory),
            rawCategory,
            trackType,
            car);
    }

    private static int? PositiveOrNull(int value) =>
        value > 0 ? value : null;

    private static string Clean(string? value, int maximumLength)
    {
        string cleaned = (value ?? string.Empty).Trim();
        return cleaned.Length <= maximumLength
            ? cleaned
            : cleaned[..maximumLength];
    }
}
