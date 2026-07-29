using LogiDynamicDash.Configuration;
using LogiDynamicDash.Controllers;
using LogiDynamicDash.Models;
using LogiDynamicDash.Offline;
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
        if (selection!.IsBoundedHardwareTrial)
        {
            cancellationSource.CancelAfter(
                Rs50StationaryTrialOptions.Duration);
        }

        Console.CancelKeyPress += (_, eventArgs) =>
        {
            eventArgs.Cancel = true;
            cancellationSource.Cancel();
        };

        LogiDynamicDashApplication application = new(
            new IRacingTelemetryService(),
            selection.Display,
            new DisplayController());
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
