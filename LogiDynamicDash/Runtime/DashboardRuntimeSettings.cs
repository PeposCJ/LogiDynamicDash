using LogiDynamicDash.Models;

namespace LogiDynamicDash.Runtime;

internal sealed record DashboardRuntimeSettings(
    Rs50OledConfiguration FallbackConfiguration,
    string ProfileDirectory,
    bool AutomaticProfiles = true,
    double LastLapDisplaySeconds = 5)
{
    internal TimeSpan LastLapDuration
    {
        get
        {
            if (!double.IsFinite(LastLapDisplaySeconds) ||
                LastLapDisplaySeconds is < 1 or > 15)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(LastLapDisplaySeconds),
                    "Last-lap duration must be between 1 and 15 seconds.");
            }

            return TimeSpan.FromSeconds(LastLapDisplaySeconds);
        }
    }
}
