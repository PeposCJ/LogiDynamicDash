namespace LogiDynamicDash.Models;

internal sealed record DisciplineProfileRecommendation(
    IRacingDiscipline Discipline,
    string Summary,
    Rs50OledConfiguration Configuration);

internal static class DisciplineProfileRecommendations
{
    internal static readonly IReadOnlyList<IRacingDiscipline>
        SupportedDisciplines =
        [
            IRacingDiscipline.SportsCar,
            IRacingDiscipline.FormulaCar,
            IRacingDiscipline.Oval,
            IRacingDiscipline.DirtOval,
            IRacingDiscipline.DirtRoad
        ];

    internal static DisciplineProfileRecommendation Create(
        IRacingDiscipline discipline,
        SpeedUnit speedUnit)
    {
        if (!SupportedDisciplines.Contains(discipline))
        {
            throw new ArgumentOutOfRangeException(
                nameof(discipline),
                "A current iRacing category is required.");
        }

        IReadOnlyDictionary<DisplayMode, Rs50OledLayout> layouts =
            discipline switch
            {
                IRacingDiscipline.SportsCar or
                IRacingDiscipline.FormulaCar or
                IRacingDiscipline.DirtRoad =>
                    new Dictionary<DisplayMode, Rs50OledLayout>
                    {
                        [DisplayMode.Normal] = Rs50OledLayout.E,
                        [DisplayMode.BrakeBias] = Rs50OledLayout.H,
                        [DisplayMode.LastLap] = Rs50OledLayout.J,
                        [DisplayMode.ConnectionProblem] = Rs50OledLayout.H
                    },
                IRacingDiscipline.Oval or
                IRacingDiscipline.DirtOval =>
                    new Dictionary<DisplayMode, Rs50OledLayout>
                    {
                        [DisplayMode.Normal] = Rs50OledLayout.D,
                        [DisplayMode.BrakeBias] = Rs50OledLayout.H,
                        [DisplayMode.LastLap] = Rs50OledLayout.J,
                        [DisplayMode.ConnectionProblem] = Rs50OledLayout.H
                    },
                _ => throw new ArgumentOutOfRangeException(nameof(discipline))
            };

        double maximumRpm = discipline switch
        {
            IRacingDiscipline.SportsCar => 8000,
            IRacingDiscipline.FormulaCar => 12000,
            IRacingDiscipline.Oval => 9000,
            IRacingDiscipline.DirtOval => 8500,
            IRacingDiscipline.DirtRoad => 9000,
            _ => throw new ArgumentOutOfRangeException(nameof(discipline))
        };
        double gaugeMaximumSpeed = (discipline, speedUnit) switch
        {
            (IRacingDiscipline.SportsCar, SpeedUnit.KilometersPerHour) => 300,
            (IRacingDiscipline.SportsCar, SpeedUnit.MilesPerHour) => 190,
            (IRacingDiscipline.FormulaCar, SpeedUnit.KilometersPerHour) => 350,
            (IRacingDiscipline.FormulaCar, SpeedUnit.MilesPerHour) => 220,
            (IRacingDiscipline.Oval, SpeedUnit.KilometersPerHour) => 360,
            (IRacingDiscipline.Oval, SpeedUnit.MilesPerHour) => 225,
            (IRacingDiscipline.DirtOval, SpeedUnit.KilometersPerHour) => 180,
            (IRacingDiscipline.DirtOval, SpeedUnit.MilesPerHour) => 110,
            (IRacingDiscipline.DirtRoad, SpeedUnit.KilometersPerHour) => 220,
            (IRacingDiscipline.DirtRoad, SpeedUnit.MilesPerHour) => 140,
            _ => throw new ArgumentOutOfRangeException(nameof(speedUnit))
        };
        string summary = discipline switch
        {
            IRacingDiscipline.SportsCar =>
                "Sports Car: layout E emphasizes RPM, speed, and frequent " +
                "gear changes. H makes brake-bias adjustments legible; J " +
                "gives lap time maximum space.",
            IRacingDiscipline.FormulaCar =>
                "Formula Car: layout E prioritizes the high-RPM band, gear, " +
                "and speed. H keeps brake-bias changes clear; J isolates lap " +
                "time.",
            IRacingDiscipline.Oval =>
                "Oval: layout D keeps RPM and speed visible with compact " +
                "gear/status text. H favors quick setup checks; J keeps lap " +
                "timing clear.",
            IRacingDiscipline.DirtOval =>
                "Dirt Oval: layout D favors a stable gear, RPM, and speed " +
                "readout while the car is sliding. H and J reserve setup and " +
                "lap-time pages.",
            IRacingDiscipline.DirtRoad =>
                "Dirt Road: layout E emphasizes rapid gear changes, RPM, and " +
                "speed. H makes brake-bias adjustments legible; J gives lap " +
                "time maximum space.",
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
