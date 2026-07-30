using LogiDynamicDash.Displays;
using LogiDynamicDash.Diagnostics;
using LogiDynamicDash.Hidpp;
using LogiDynamicDash.Models;

namespace LogiDynamicDash.Configuration;

internal sealed record ApplicationDisplaySelection(
    IApplicationDisplay Display,
    TimeSpan? HardwareTrialDuration)
{
    internal bool IsBoundedHardwareTrial =>
        HardwareTrialDuration is not null;
}

internal static class ApplicationDisplayFactory
{
    internal static bool TryCreate(
        string[] arguments,
        out ApplicationDisplaySelection? selection) =>
        TryCreate(
            arguments,
            Rs50OledSessionFactory.OpenPhysicalWithLocalDiagnostics,
            out selection);

    internal static bool TryCreate(
        string[] arguments,
        Func<IRs50OledSession> sessionFactory,
        out ApplicationDisplaySelection? selection) =>
        TryCreate(
            arguments,
            sessionFactory,
            () => new ConsoleDashboard(),
            Rs50OledConfigurationFile.Load,
            out selection);

    internal static bool TryCreate(
        string[] arguments,
        Func<IRs50OledSession> sessionFactory,
        Func<IApplicationDisplay> consoleFactory,
        Func<string, Rs50OledConfiguration> configurationLoader,
        out ApplicationDisplaySelection? selection)
    {
        ArgumentNullException.ThrowIfNull(arguments);
        ArgumentNullException.ThrowIfNull(sessionFactory);
        ArgumentNullException.ThrowIfNull(consoleFactory);
        ArgumentNullException.ThrowIfNull(configurationLoader);

        if (arguments.Length == 0)
        {
            selection = new(
                consoleFactory(),
                HardwareTrialDuration: null);
            return true;
        }

        string configurationPath;
        TimeSpan duration;
        float maximumSpeedMetersPerSecond;
        if (Rs50StationaryTrialOptions.TryParse(
                arguments,
                out Rs50StationaryTrialOptions? stationary))
        {
            configurationPath = stationary!.ConfigurationPath;
            duration = Rs50StationaryTrialOptions.Duration;
            maximumSpeedMetersPerSecond =
                Rs50OledDisplaySink.MaximumStationarySpeedMetersPerSecond;
        }
        else if (Rs50LowSpeedTrialOptions.TryParse(
                     arguments,
                     out Rs50LowSpeedTrialOptions? lowSpeed))
        {
            configurationPath = lowSpeed!.ConfigurationPath;
            duration = Rs50LowSpeedTrialOptions.Duration;
            maximumSpeedMetersPerSecond =
                Rs50LowSpeedTrialOptions.MaximumSpeedMetersPerSecond;
        }
        else
        {
            selection = null;
            return false;
        }

        Rs50TelemetryFrameFormatter formatter = new(
            configurationLoader(configurationPath));
        selection = new(
            new CompositeApplicationDisplay(
                consoleFactory(),
                new Rs50OledDisplaySink(
                    sessionFactory,
                    formatter,
                    maximumSpeedMetersPerSecond)),
            HardwareTrialDuration: duration);
        return true;
    }
}
