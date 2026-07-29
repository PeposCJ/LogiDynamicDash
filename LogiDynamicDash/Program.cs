using System.Diagnostics;
using LogiDynamicDash.Configuration;
using LogiDynamicDash.Controllers;
using LogiDynamicDash.Displays;
using LogiDynamicDash.Models;
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
    private const int RefreshIntervalMilliseconds = 200;

    private static readonly Stopwatch RefreshTimer =
        Stopwatch.StartNew();

    private static readonly TelemetrySnapshot Snapshot =
        new();

    private static IApplicationDisplay? dashboard;

    private static readonly DisplayController Controller =
        new();

    private static readonly IRacingTelemetryService
        TelemetryService = new();

    private static async Task<int> Main(string[] arguments)
    {
        if (!ApplicationDisplayFactory.TryCreate(
                arguments,
                out ApplicationDisplaySelection? selection))
        {
            Console.Error.WriteLine(Rs50StationaryTrialOptions.Usage);
            return 2;
        }

        using var cancellationSource =
            new CancellationTokenSource();

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

        dashboard = selection.Display;
        bool initialized = false;
        int exitCode = 0;
        try
        {
            dashboard.Initialize();
            initialized = true;
            RenderCurrentDisplay(Snapshot);

            await TelemetryService.MonitorAsync(
                Snapshot,
                HandleTelemetryUpdated,
                HandleStatusChanged,
                cancellationSource.Token);
        }
        catch (OperationCanceledException)
        {
            // Expected when the user presses Ctrl+C.
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(
                $"LogiDynamicDash stopped safely: {exception.Message}");
            exitCode = 1;
        }
        finally
        {
            if (initialized)
            {
                try
                {
                    dashboard.Stop();
                }
                catch (Exception exception)
                {
                    Console.Error.WriteLine(
                        "LogiDynamicDash could not close a display cleanly: " +
                        exception.Message);
                    exitCode = 1;
                }
            }
        }

        return exitCode;
    }

    private static void HandleTelemetryUpdated(
        TelemetrySnapshot snapshot)
    {
        if (RefreshTimer.ElapsedMilliseconds <
            RefreshIntervalMilliseconds)
        {
            return;
        }

        RenderCurrentDisplay(snapshot);
        RefreshTimer.Restart();
    }

    private static void HandleStatusChanged(
        TelemetrySnapshot snapshot)
    {
        RenderCurrentDisplay(snapshot);
    }

    private static void RenderCurrentDisplay(
        TelemetrySnapshot snapshot)
    {
        DisplayMode mode =
            Controller.SelectMode(snapshot);

        dashboard!.Render(
            snapshot,
            mode);
    }
}
