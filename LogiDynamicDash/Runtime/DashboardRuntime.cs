using LogiDynamicDash.Configuration;
using LogiDynamicDash.Controllers;
using LogiDynamicDash.Diagnostics;
using LogiDynamicDash.Displays;
using LogiDynamicDash.Models;
using LogiDynamicDash.Services;

namespace LogiDynamicDash.Runtime;

internal sealed class DashboardRuntime(
    Func<Hidpp.IRs50OledSession>? sessionFactory = null,
    Func<ITelemetrySource>? telemetryFactory = null)
{
    private readonly Func<Hidpp.IRs50OledSession> createSession =
        sessionFactory ?? Rs50OledSessionFactory.OpenPhysicalWithLocalDiagnostics;
    private readonly Func<ITelemetrySource> createTelemetry =
        telemetryFactory ?? (() => new IRacingTelemetryService());

    internal async Task RunAsync(
        DashboardRuntimeSettings settings,
        Action<DashboardRuntimeStatus> statusChanged,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(statusChanged);

        DashboardRuntimeStatus current = DashboardRuntimeStatus.Initial;
        object statusSynchronization = new();

        void Update(
            DashboardOledState? oled = null,
            TelemetrySnapshot? telemetry = null,
            string? message = null)
        {
            lock (statusSynchronization)
            {
                string telemetryState =
                    telemetry?.ConnectionState ?? current.Telemetry;
                IRacingSessionIdentity? identity =
                    telemetry?.SessionIdentity;
                current = current with
                {
                    Oled = oled ?? current.Oled,
                    Telemetry = telemetryState,
                    Car = identity?.Car?.DisplayName ??
                        identity?.Car?.ShortName ??
                        current.Car,
                    CarId = identity?.Car?.CarId ?? current.CarId,
                    Discipline = identity?.Discipline ?? current.Discipline,
                    Message = message ?? Message(
                        oled ?? current.Oled,
                        telemetryState)
                };
                statusChanged(current);
            }
        }

        statusChanged(current);
        Rs50ProfileStore profiles = new(settings.ProfileDirectory);
        AutomaticRs50TelemetryFrameFormatter formatter = new(
            settings.FallbackConfiguration,
            profiles,
            settings.AutomaticProfiles);
        RecoveringRs50OledDisplaySink oledDisplay = new(
            createSession,
            formatter,
            oled => Update(oled: oled));
        DashboardStatusDisplay telemetryDisplay = new(
            telemetry => Update(telemetry: telemetry));
        CompositeApplicationDisplay display = new(
            telemetryDisplay,
            oledDisplay);
        using IApplicationRuntimeDiagnostics diagnostics =
            SanitizedApplicationRuntimeDiagnostics.CreateLocal();
        LogiDynamicDashApplication application = new(
            createTelemetry(),
            display,
            new DisplayController(
                lastLapDuration: settings.LastLapDuration),
            diagnostics: diagnostics);

        try
        {
            await application.RunAsync(cancellationToken);
        }
        finally
        {
            Update(
                oled: DashboardOledState.Stopped,
                message: "Dashboard stopped.");
        }
    }

    private static string Message(
        DashboardOledState oled,
        string telemetry) =>
        (oled, telemetry.ToUpperInvariant()) switch
        {
            (DashboardOledState.Connected, "CONNECTED") =>
                "OLED and iRacing telemetry are active.",
            (DashboardOledState.Connected, _) =>
                "OLED connected; waiting for iRacing telemetry.",
            (DashboardOledState.Reconnecting, _) =>
                "RS50 disconnected; reconnecting automatically.",
            (DashboardOledState.Stopped, _) =>
                "Dashboard stopped.",
            _ => "Waiting for the validated RS50 OLED."
        };
}
