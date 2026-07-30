namespace LogiDynamicDash.Configuration;

internal sealed record Rs50StationaryTrialOptions(
    string ConfigurationPath)
{
    internal static readonly TimeSpan Duration = TimeSpan.FromSeconds(10);

    private const int ArgumentCount = 10;

    internal static bool TryParse(
        string[] arguments,
        out Rs50StationaryTrialOptions? options)
    {
        ArgumentNullException.ThrowIfNull(arguments);
        options = null;

        if (arguments.Length != ArgumentCount ||
            arguments[0] != "--enable-rs50-oled-stationary-trial" ||
            arguments[1] != "--confirm-ghub-closed" ||
            arguments[2] != "--confirm-iracing-running" ||
            arguments[3] != "--confirm-car-stationary-in-pits" ||
            arguments[4] != "--confirm-rs50-dynamic-selected" ||
            arguments[5] != "--confirm-10-second-limit" ||
            arguments[6] != "--acknowledge-no-moving-car-use" ||
            arguments[7] != "--config" ||
            string.IsNullOrWhiteSpace(arguments[8]) ||
            arguments[9] != "--confirm-settings")
        {
            return false;
        }

        options = new Rs50StationaryTrialOptions(arguments[8]);
        return true;
    }

    internal static string Usage =>
        "  LogiDynamicDash.exe\n\n" +
        "Bounded stationary RS50 OLED validation only:\n" +
        "  LogiDynamicDash.exe " +
        "--enable-rs50-oled-stationary-trial " +
        "--confirm-ghub-closed " +
        "--confirm-iracing-running " +
        "--confirm-car-stationary-in-pits " +
        "--confirm-rs50-dynamic-selected " +
        "--confirm-10-second-limit " +
        "--acknowledge-no-moving-car-use " +
        "--config <json-path> " +
        "--confirm-settings";
}
