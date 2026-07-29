using LogiDynamicDash.Displays;
using LogiDynamicDash.Diagnostics;
using LogiDynamicDash.Hidpp;
using LogiDynamicDash.Models;

namespace LogiDynamicDash.Configuration;

internal sealed record ApplicationDisplaySelection(
    IApplicationDisplay Display,
    bool IsBoundedHardwareTrial);

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
                IsBoundedHardwareTrial: false);
            return true;
        }

        if (!Rs50StationaryTrialOptions.TryParse(
                arguments,
                out Rs50StationaryTrialOptions? trial))
        {
            selection = null;
            return false;
        }

        Rs50TelemetryFrameFormatter formatter = new(
            configurationLoader(trial!.ConfigurationPath));
        selection = new(
            new CompositeApplicationDisplay(
                consoleFactory(),
                new Rs50OledDisplaySink(sessionFactory, formatter)),
            IsBoundedHardwareTrial: true);
        return true;
    }
}
