using LogiDynamicDash.Models;

namespace LogiDynamicDash.Configuration;

internal sealed class Rs50ProfileStore(string directoryPath)
{
    internal string DirectoryPath { get; } =
        Path.GetFullPath(
            string.IsNullOrWhiteSpace(directoryPath)
                ? throw new ArgumentException(
                    "A profile directory is required.",
                    nameof(directoryPath))
                : directoryPath);

    internal Rs50OledConfiguration? Resolve(IRacingSessionIdentity identity)
    {
        ArgumentNullException.ThrowIfNull(identity);

        if (identity.Car?.CarId is int carId)
        {
            string carPath = Path.Combine(
                DirectoryPath,
                $"car-{carId}.json");
            if (File.Exists(carPath))
            {
                return Rs50OledConfigurationFile.Load(carPath);
            }
        }

        string? disciplineName = FileName(identity.Discipline);
        if (disciplineName is null)
        {
            return null;
        }

        string disciplinePath = Path.Combine(
            DirectoryPath,
            $"discipline-{disciplineName}.json");
        return File.Exists(disciplinePath)
            ? Rs50OledConfigurationFile.Load(disciplinePath)
            : null;
    }

    internal string SaveForDiscipline(
        IRacingDiscipline discipline,
        Rs50OledConfiguration configuration)
    {
        string disciplineName = FileName(discipline) ??
            throw new ArgumentOutOfRangeException(
                nameof(discipline),
                "A supported discipline is required.");
        return Save(
            $"discipline-{disciplineName}.json",
            configuration);
    }

    internal string SaveForCar(
        int carId,
        Rs50OledConfiguration configuration)
    {
        if (carId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(carId));
        }

        return Save($"car-{carId}.json", configuration);
    }

    private string Save(
        string fileName,
        Rs50OledConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        Directory.CreateDirectory(DirectoryPath);
        string path = Path.Combine(DirectoryPath, fileName);
        File.WriteAllText(
            path,
            Rs50OledConfigurationFile.Serialize(configuration));
        return path;
    }

    private static string? FileName(IRacingDiscipline discipline) =>
        discipline switch
        {
            IRacingDiscipline.SportsCar => "sports-car",
            IRacingDiscipline.FormulaCar => "formula-car",
            IRacingDiscipline.Oval => "oval",
            IRacingDiscipline.DirtOval => "dirt-oval",
            IRacingDiscipline.DirtRoad => "dirt-road",
            _ => null
        };
}
