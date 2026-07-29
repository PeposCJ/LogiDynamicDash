namespace LogiDynamicDash.Models;

internal enum DrivingDiscipline
{
    Road,
    Oval
}

internal sealed record DisciplineProfileRecommendation(
    DrivingDiscipline Discipline,
    string Summary,
    Rs50OledConfiguration Configuration);

internal static class DisciplineProfileRecommendations
{
    internal static DisciplineProfileRecommendation Create(
        DrivingDiscipline discipline,
        SpeedUnit speedUnit)
    {
        IReadOnlyDictionary<DisplayMode, Rs50OledLayout> layouts =
            discipline switch
            {
                DrivingDiscipline.Road =>
                    new Dictionary<DisplayMode, Rs50OledLayout>
                    {
                        [DisplayMode.Normal] = Rs50OledLayout.E,
                        [DisplayMode.BrakeBias] = Rs50OledLayout.H,
                        [DisplayMode.LastLap] = Rs50OledLayout.J,
                        [DisplayMode.ConnectionProblem] = Rs50OledLayout.H
                    },
                DrivingDiscipline.Oval =>
                    new Dictionary<DisplayMode, Rs50OledLayout>
                    {
                        [DisplayMode.Normal] = Rs50OledLayout.D,
                        [DisplayMode.BrakeBias] = Rs50OledLayout.H,
                        [DisplayMode.LastLap] = Rs50OledLayout.J,
                        [DisplayMode.ConnectionProblem] = Rs50OledLayout.H
                    },
                _ => throw new ArgumentOutOfRangeException(nameof(discipline))
            };

        double maximumRpm =
            discipline == DrivingDiscipline.Oval ? 9000 : 8000;
        double gaugeMaximumSpeed = (discipline, speedUnit) switch
        {
            (DrivingDiscipline.Road, SpeedUnit.KilometersPerHour) => 300,
            (DrivingDiscipline.Road, SpeedUnit.MilesPerHour) => 190,
            (DrivingDiscipline.Oval, SpeedUnit.KilometersPerHour) => 360,
            (DrivingDiscipline.Oval, SpeedUnit.MilesPerHour) => 225,
            _ => throw new ArgumentOutOfRangeException(nameof(speedUnit))
        };
        string summary = discipline switch
        {
            DrivingDiscipline.Road =>
                "Layout E emphasizes RPM, speed, and frequent gear changes. " +
                "H makes brake-bias adjustments legible; J gives lap time " +
                "maximum space.",
            DrivingDiscipline.Oval =>
                "Layout D keeps RPM and speed visible with compact gear/status " +
                "text. H favors quick setup checks; J keeps lap timing clear.",
            _ => throw new ArgumentOutOfRangeException(nameof(discipline))
        };

        return new DisciplineProfileRecommendation(
            discipline,
            summary,
            new Rs50OledConfiguration(
                layouts,
                speedUnit,
                maximumRpm,
                gaugeMaximumSpeed));
    }
}
