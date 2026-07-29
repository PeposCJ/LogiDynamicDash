using System.Globalization;
using LogiDynamicDash.Models;

namespace LogiDynamicDash.Configuration;

internal sealed record Rs50StationaryTrialOptions(
    Rs50OledConfiguration OledConfiguration)
{
    internal static readonly TimeSpan Duration = TimeSpan.FromSeconds(10);

    private const int ArgumentCount = 16;

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
            arguments[7] != "--layout" ||
            arguments[9] != "--speed-unit" ||
            arguments[11] != "--maximum-rpm" ||
            arguments[13] != "--gauge-maximum-speed" ||
            arguments[15] != "--confirm-settings")
        {
            return false;
        }

        if (arguments[8].Length != 1 ||
            arguments[8][0] is < 'A' or > 'J')
        {
            return false;
        }

        Rs50OledLayout layout =
            (Rs50OledLayout)(arguments[8][0] - 'A');

        SpeedUnit speedUnit = arguments[10] switch
        {
            "KMH" => SpeedUnit.KilometersPerHour,
            "MPH" => SpeedUnit.MilesPerHour,
            _ => (SpeedUnit)(-1)
        };
        if (!Enum.IsDefined(speedUnit))
        {
            return false;
        }

        if (!double.TryParse(
                arguments[12],
                NumberStyles.AllowDecimalPoint,
                CultureInfo.InvariantCulture,
                out double maximumRpm) ||
            !double.IsFinite(maximumRpm) ||
            maximumRpm is < 1000 or > 30000)
        {
            return false;
        }

        if (!double.TryParse(
                arguments[14],
                NumberStyles.AllowDecimalPoint,
                CultureInfo.InvariantCulture,
                out double maximumSpeed) ||
            !double.IsFinite(maximumSpeed) ||
            maximumSpeed is < 10 or > 500)
        {
            return false;
        }

        options = new Rs50StationaryTrialOptions(
            new Rs50OledConfiguration(
                layout,
                speedUnit,
                maximumRpm,
                maximumSpeed));
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
        "--layout <A-J> " +
        "--speed-unit <KMH|MPH> " +
        "--maximum-rpm <1000-30000> " +
        "--gauge-maximum-speed <10-500> " +
        "--confirm-settings";
}
