using LogiDynamicDash.Configuration;
using LogiDynamicDash.Controllers;
using LogiDynamicDash.Diagnostics;
using LogiDynamicDash.Models;
using LogiDynamicDash.Offline;
using LogiDynamicDash.Runtime;
using LogiDynamicDash.Services;
using SVappsLAB.iRacingTelemetrySDK;

namespace LogiDynamicDash;

[RequiredTelemetryVars([
    TelemetryVar.IsOnTrackCar,
    TelemetryVar.Gear,
    TelemetryVar.RPM,
    TelemetryVar.Speed,
    TelemetryVar.dcBrakeBias,
    TelemetryVar.LapLastLapTime
])]
internal class Program
{
    private static async Task<int> Main(string[] arguments)
    {
        if (Rs50ProductionRunOptions.TryParse(
                arguments,
                out Rs50ProductionRunOptions? production))
        {
            return await RunProductionAsync(production!);
        }

        if (OfflineCommandLine.TryParse(
                arguments,
                out OfflineCommand? offlineCommand))
        {
            return await RunOfflineAsync(offlineCommand!);
        }

        ApplicationDisplaySelection? selection;
        try
        {
            if (!ApplicationDisplayFactory.TryCreate(
                    arguments,
                    out selection))
            {
                Console.Error.WriteLine(OfflineCommandLine.Usage);
                Console.Error.WriteLine();
                Console.Error.WriteLine(Rs50StationaryTrialOptions.Usage);
                Console.Error.WriteLine();
                Console.Error.WriteLine(Rs50LowSpeedTrialOptions.Usage);
                Console.Error.WriteLine();
                Console.Error.WriteLine(Rs50DrivingTrialOptions.Usage);
                Console.Error.WriteLine();
                Console.Error.WriteLine(Rs50ProductionRunOptions.Usage);
                return 2;
            }
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(
                $"Configuration rejected: {exception.Message}");
            return 2;
        }

        using CancellationTokenSource cancellationSource = new();
        if (selection!.HardwareTrialDuration is TimeSpan trialDuration)
        {
            cancellationSource.CancelAfter(trialDuration);
        }

        Console.CancelKeyPress += (_, eventArgs) =>
        {
            eventArgs.Cancel = true;
            cancellationSource.Cancel();
        };

        using IApplicationRuntimeDiagnostics? diagnostics =
            selection.UsesPhysicalHardware
                ? SanitizedApplicationRuntimeDiagnostics.CreateLocal()
                : null;
        LogiDynamicDashApplication application = new(
            new IRacingTelemetryService(),
            selection.Display,
            new DisplayController(),
            diagnostics: diagnostics);
        try
        {
            await application.RunAsync(cancellationSource.Token);
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(
                $"LogiDynamicDash stopped safely: {exception.Message}");
            return 1;
        }
    }

    private static async Task<int> RunProductionAsync(
        Rs50ProductionRunOptions options)
    {
        using CancellationTokenSource cancellationSource = new();
        ConsoleCancelEventHandler handler = (_, eventArgs) =>
        {
            eventArgs.Cancel = true;
            cancellationSource.Cancel();
        };
        Console.CancelKeyPress += handler;
        try
        {
            DashboardRuntimeSettings settings = new(
                Rs50OledConfigurationFile.Load(options.ConfigurationPath),
                options.ProfileDirectory,
                options.AutomaticProfiles,
                options.LastLapDisplaySeconds);
            DashboardRuntime runtime = new();
            DashboardRuntimeStatus? previous = null;
            await runtime.RunAsync(
                settings,
                status =>
                {
                    if (status != previous)
                    {
                        Console.WriteLine(
                            $"OLED={status.Oled}; " +
                            $"iRacing={status.Telemetry}; " +
                            $"Car={status.Car}; " +
                            $"Category=" +
                            $"{IRacingDisciplineDisplay.Name(
                                status.Discipline)}; " +
                            status.Message);
                        previous = status;
                    }
                },
                cancellationSource.Token);
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(
                $"LogiDynamicDash stopped safely: {exception.Message}");
            return 1;
        }
        finally
        {
            Console.CancelKeyPress -= handler;
        }
    }

    private static async Task<int> RunOfflineAsync(OfflineCommand command)
    {
        try
        {
            if (command.Kind == OfflineCommandKind.RecordTelemetry)
            {
                Rs50TelemetryRecorder recorder = new(
                    new IRacingTelemetryService());
                using CancellationTokenSource recordingCancellation = new();
                ConsoleCancelEventHandler cancelHandler = (_, eventArgs) =>
                {
                    eventArgs.Cancel = true;
                    recordingCancellation.Cancel();
                };
                Console.CancelKeyPress += cancelHandler;
                try
                {
                    await recorder.RecordAsync(
                        command.OutputPath!,
                        TimeSpan.FromSeconds(command.DurationSeconds!.Value),
                        recordingCancellation.Token);
                }
                finally
                {
                    Console.CancelKeyPress -= cancelHandler;
                }

                Console.WriteLine(
                    $"Telemetry replay saved to '{command.OutputPath}'.");
                return 0;
            }

            Rs50OledConfiguration configuration =
                Rs50OledConfigurationFile.Load(command.ConfigurationPath!);
            switch (command.Kind)
            {
                case OfflineCommandKind.PreviewAll:
                    Rs50OledPreviewRunner.RunAll(
                        configuration,
                        Console.Out);
                    break;
                case OfflineCommandKind.SimulateAll:
                    Rs50OledSimulationRunner.RunAll(
                        configuration,
                        Console.Out);
                    break;
                case OfflineCommandKind.Replay:
                    await Rs50TelemetryReplayRunner.RunAsync(
                        configuration,
                        command.TelemetryPath!,
                        Console.Out);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(command));
            }

            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(
                $"Offline command failed: {exception.Message}");
            return 1;
        }
    }
}
