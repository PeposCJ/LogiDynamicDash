namespace LogiDynamicDash.Configuration;

internal sealed record Rs50LowSpeedTrialOptions(
    string ConfigurationPath)
{
    internal static readonly TimeSpan Duration = TimeSpan.FromSeconds(15);

    internal const float MaximumSpeedMetersPerSecond = 20f / 3.6f;

    private const int ArgumentCount = 11;

    internal static bool TryParse(
        string[] arguments,
        out Rs50LowSpeedTrialOptions? options)
    {
        ArgumentNullException.ThrowIfNull(arguments);
        options = null;
        if (arguments.Length != ArgumentCount ||
            arguments[0] != "--enable-rs50-oled-low-speed-trial" ||
            arguments[1] != "--confirm-ghub-closed" ||
            arguments[2] != "--confirm-iracing-running" ||
            arguments[3] != "--confirm-controlled-pit-lane" ||
            arguments[4] != "--confirm-rs50-dynamic-selected" ||
            arguments[5] != "--confirm-15-second-limit" ||
            arguments[6] != "--confirm-maximum-20-kmh" ||
            arguments[7] != "--acknowledge-stop-on-speed-limit" ||
            arguments[8] != "--config" ||
            string.IsNullOrWhiteSpace(arguments[9]) ||
            arguments[10] != "--confirm-settings")
        {
            return false;
        }

        options = new Rs50LowSpeedTrialOptions(arguments[9]);
        return true;
    }

    internal static string Usage =>
        "Bounded low-speed RS50 OLED validation only:\n" +
        "  LogiDynamicDash.exe " +
        "--enable-rs50-oled-low-speed-trial " +
        "--confirm-ghub-closed " +
        "--confirm-iracing-running " +
        "--confirm-controlled-pit-lane " +
        "--confirm-rs50-dynamic-selected " +
        "--confirm-15-second-limit " +
        "--confirm-maximum-20-kmh " +
        "--acknowledge-stop-on-speed-limit " +
        "--config <json-path> " +
        "--confirm-settings";
}
