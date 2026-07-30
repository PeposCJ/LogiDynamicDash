namespace LogiDynamicDash.Configuration;

internal sealed record Rs50DrivingTrialOptions(
    string ConfigurationPath)
{
    private const int ArgumentCount = 10;

    internal static bool TryParse(
        string[] arguments,
        out Rs50DrivingTrialOptions? options)
    {
        ArgumentNullException.ThrowIfNull(arguments);
        options = null;
        if (arguments.Length != ArgumentCount ||
            arguments[0] != "--enable-rs50-oled-driving-trial" ||
            arguments[1] != "--confirm-ghub-closed" ||
            arguments[2] != "--confirm-iracing-running" ||
            arguments[3] != "--confirm-controlled-driving-session" ||
            arguments[4] != "--confirm-rs50-dynamic-selected" ||
            arguments[5] != "--acknowledge-no-speed-limit" ||
            arguments[6] != "--acknowledge-manual-stop-required" ||
            arguments[7] != "--config" ||
            string.IsNullOrWhiteSpace(arguments[8]) ||
            arguments[9] != "--confirm-settings")
        {
            return false;
        }

        options = new Rs50DrivingTrialOptions(arguments[8]);
        return true;
    }

    internal static string Usage =>
        "Manually stopped, no-speed-limit RS50 OLED driving trial:\n" +
        "  LogiDynamicDash.exe " +
        "--enable-rs50-oled-driving-trial " +
        "--confirm-ghub-closed " +
        "--confirm-iracing-running " +
        "--confirm-controlled-driving-session " +
        "--confirm-rs50-dynamic-selected " +
        "--acknowledge-no-speed-limit " +
        "--acknowledge-manual-stop-required " +
        "--config <json-path> " +
        "--confirm-settings";
}
