using System.Diagnostics;
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
    TelemetryVar.dcBrakeBias
])]
internal class Program
{
    private static readonly Stopwatch RefreshTimer =
        Stopwatch.StartNew();

    private static readonly TelemetrySnapshot Snapshot =
        new();

    private static readonly ConsoleDashboard Dashboard =
        new();

    private static readonly IRacingTelemetryService
        TelemetryService = new();

    private static async Task Main()
    {
        Dashboard.Initialize();
        Dashboard.Render(Snapshot);

        using var cancellationSource =
            new CancellationTokenSource();

        Console.CancelKeyPress += (_, eventArgs) =>
        {
            eventArgs.Cancel = true;
            cancellationSource.Cancel();
        };

        try
        {
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
        finally
        {
            Dashboard.Stop();
        }
    }

    private static void HandleTelemetryUpdated(
        TelemetrySnapshot snapshot)
    {
        if (RefreshTimer.ElapsedMilliseconds < 100)
        {
            return;
        }

        Dashboard.Render(snapshot);
        RefreshTimer.Restart();
    }

    private static void HandleStatusChanged(
        TelemetrySnapshot snapshot)
    {
        Dashboard.Render(snapshot);
    }
}