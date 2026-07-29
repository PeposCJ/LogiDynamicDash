namespace LogiDynamicDash.Models;

internal enum IRacingDiscipline
{
    Unknown,
    SportsCar,
    FormulaCar,
    Oval,
    DirtOval,
    DirtRoad,
    LegacyRoad
}

internal sealed record CarIdentity(
    int? CarId,
    string CarPath,
    string DisplayName,
    string ShortName,
    int? CarClassId,
    string CarClassShortName,
    bool IsElectric);

internal sealed record IRacingSessionIdentity(
    IRacingDiscipline Discipline,
    string RawCategory,
    string TrackType,
    CarIdentity? Car);

internal static class IRacingDisciplineParser
{
    internal static IRacingDiscipline Parse(string? category)
    {
        string normalized = new(
            (category ?? string.Empty)
            .Where(char.IsLetterOrDigit)
            .Select(char.ToUpperInvariant)
            .ToArray());
        return normalized switch
        {
            "SPORTSCAR" or "SPORTSCARS" =>
                IRacingDiscipline.SportsCar,
            "FORMULA" or "FORMULACAR" or "FORMULACARS" =>
                IRacingDiscipline.FormulaCar,
            "OVAL" => IRacingDiscipline.Oval,
            "DIRTOVAL" => IRacingDiscipline.DirtOval,
            "DIRTROAD" => IRacingDiscipline.DirtRoad,
            "ROAD" => IRacingDiscipline.LegacyRoad,
            _ => IRacingDiscipline.Unknown
        };
    }
}

internal static class IRacingDisciplineDisplay
{
    internal static string Name(IRacingDiscipline discipline) =>
        discipline switch
        {
            IRacingDiscipline.SportsCar => "Sports Car",
            IRacingDiscipline.FormulaCar => "Formula Car",
            IRacingDiscipline.Oval => "Oval",
            IRacingDiscipline.DirtOval => "Dirt Oval",
            IRacingDiscipline.DirtRoad => "Dirt Road",
            IRacingDiscipline.LegacyRoad => "Legacy Road",
            _ => "Unknown"
        };
}
