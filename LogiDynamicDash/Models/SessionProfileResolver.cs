namespace LogiDynamicDash.Models;

internal sealed record IRacingCarProfileKey(
    int CarId,
    IRacingDiscipline Discipline);

internal sealed record SessionProfileResolution(
    IRacingSessionIdentity Identity,
    IRacingCarProfileKey? CarKey,
    DisciplineProfileRecommendation? Recommendation,
    string Explanation)
{
    internal bool CanApply => Recommendation is not null;
}

internal static class SessionProfileResolver
{
    internal static SessionProfileResolution Resolve(
        IRacingSessionIdentity identity,
        SpeedUnit speedUnit)
    {
        ArgumentNullException.ThrowIfNull(identity);

        if (identity.Discipline is
            IRacingDiscipline.Unknown or IRacingDiscipline.LegacyRoad)
        {
            return new SessionProfileResolution(
                identity,
                null,
                null,
                "The event category is unknown or legacy. Keep the current " +
                "manual profile.");
        }

        if (identity.Car?.CarId is not int carId)
        {
            return new SessionProfileResolution(
                identity,
                null,
                null,
                "The driver car has no exact CarID. Keep the current manual " +
                "profile.");
        }

        DisciplineProfileRecommendation recommendation =
            DisciplineProfileRecommendations.Create(
                identity.Discipline,
                speedUnit);
        return new SessionProfileResolution(
            identity,
            new IRacingCarProfileKey(carId, identity.Discipline),
            recommendation,
            $"Exact CarID {carId} in " +
            $"{IRacingDisciplineDisplay.Name(identity.Discipline)} maps to " +
            "the reviewed category profile.");
    }
}
