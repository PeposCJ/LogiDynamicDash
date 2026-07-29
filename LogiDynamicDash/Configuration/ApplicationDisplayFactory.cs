using LogiDynamicDash.Displays;
using LogiDynamicDash.Hidpp;
using LogiDynamicDash.Hidpp.Transport;

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
            () => new Rs50OledSession(Rs50OledDeviceExchange.Open()),
            out selection);

    internal static bool TryCreate(
        string[] arguments,
        Func<IRs50OledSession> sessionFactory,
        out ApplicationDisplaySelection? selection) =>
        TryCreate(
            arguments,
            sessionFactory,
            () => new ConsoleDashboard(),
            out selection);

    internal static bool TryCreate(
        string[] arguments,
        Func<IRs50OledSession> sessionFactory,
        Func<IApplicationDisplay> consoleFactory,
        out ApplicationDisplaySelection? selection)
    {
        ArgumentNullException.ThrowIfNull(arguments);
        ArgumentNullException.ThrowIfNull(sessionFactory);
        ArgumentNullException.ThrowIfNull(consoleFactory);

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

        Rs50TelemetryFrameFormatter formatter =
            new(trial!.OledConfiguration);
        selection = new(
            new CompositeApplicationDisplay(
                consoleFactory(),
                new Rs50OledDisplaySink(sessionFactory, formatter)),
            IsBoundedHardwareTrial: true);
        return true;
    }
}
