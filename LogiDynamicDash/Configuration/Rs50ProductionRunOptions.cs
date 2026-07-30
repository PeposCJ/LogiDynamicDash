namespace LogiDynamicDash.Configuration;

internal sealed record Rs50ProductionRunOptions(
    string ConfigurationPath,
    string ProfileDirectory,
    bool AutomaticProfiles,
    double LastLapDisplaySeconds)
{
    internal const string Usage =
        "Production: --run-rs50-oled --config <path> " +
        "[--profiles <directory>] [--manual-profile] " +
        "[--last-lap-seconds <1-15>]";

    internal static bool TryParse(
        string[] arguments,
        out Rs50ProductionRunOptions? options)
    {
        options = null;
        if (arguments.Length < 3 ||
            !string.Equals(
                arguments[0],
                "--run-rs50-oled",
                StringComparison.Ordinal) ||
            !string.Equals(arguments[1], "--config", StringComparison.Ordinal))
        {
            return false;
        }

        string configurationPath = arguments[2];
        string profileDirectory = Path.Combine(
            Environment.GetFolderPath(
                Environment.SpecialFolder.LocalApplicationData),
            "LogiDynamicDash",
            "profiles");
        bool automatic = true;
        double lastLapSeconds = 5;

        for (int index = 3; index < arguments.Length;)
        {
            if (arguments[index] == "--manual-profile")
            {
                automatic = false;
                index++;
                continue;
            }

            if (index + 1 >= arguments.Length)
            {
                return false;
            }

            if (arguments[index] == "--profiles")
            {
                profileDirectory = arguments[index + 1];
            }
            else if (arguments[index] == "--last-lap-seconds" &&
                     double.TryParse(
                         arguments[index + 1],
                         System.Globalization.NumberStyles.Float,
                         System.Globalization.CultureInfo.InvariantCulture,
                         out double parsed))
            {
                lastLapSeconds = parsed;
            }
            else
            {
                return false;
            }

            index += 2;
        }

        if (string.IsNullOrWhiteSpace(configurationPath) ||
            string.IsNullOrWhiteSpace(profileDirectory) ||
            !double.IsFinite(lastLapSeconds) ||
            lastLapSeconds is < 1 or > 15)
        {
            return false;
        }

        options = new(
            configurationPath,
            profileDirectory,
            automatic,
            lastLapSeconds);
        return true;
    }
}
