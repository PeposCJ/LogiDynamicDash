using LogiDynamicDash.Configuration;
using LogiDynamicDash.Models;

namespace LogiDynamicDash.Displays;

internal sealed class AutomaticRs50TelemetryFrameFormatter(
    Rs50OledConfiguration fallback,
    Rs50ProfileStore profileStore,
    bool automaticProfiles = true) : IRs50TelemetryFrameFormatter
{
    private readonly Dictionary<Rs50OledConfiguration, Rs50TelemetryFrameFormatter>
        formatters = [];

    public Rs50OledFrame Format(
        TelemetrySnapshot snapshot,
        DisplayMode mode)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        Rs50OledConfiguration configuration = Select(snapshot);
        if (!formatters.TryGetValue(configuration, out var formatter))
        {
            formatter = new Rs50TelemetryFrameFormatter(configuration);
            formatters.Add(configuration, formatter);
        }

        return formatter.Format(snapshot, mode);
    }

    internal Rs50OledConfiguration Select(TelemetrySnapshot snapshot)
    {
        if (!automaticProfiles ||
            snapshot.SessionIdentity is not IRacingSessionIdentity identity)
        {
            return fallback;
        }

        Rs50OledConfiguration? stored = profileStore.Resolve(identity);
        if (stored is not null)
        {
            return stored;
        }

        if (identity.Discipline is
            IRacingDiscipline.Unknown or IRacingDiscipline.LegacyRoad)
        {
            return fallback;
        }

        return DisciplineProfileRecommendations.Create(
            identity.Discipline,
            fallback.SpeedUnit).Configuration;
    }
}
