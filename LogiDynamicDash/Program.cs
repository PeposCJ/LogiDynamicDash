using System.Diagnostics;
using LogiDynamicDash.Displays;
using LogiDynamicDash.Models;
using Microsoft.Extensions.Logging.Abstractions;
using SVappsLAB.iRacingTelemetrySDK;

namespace LogiDynamicDash;

[RequiredTelemetryVars([
    TelemetryVar.IsOnTrackCar,
    TelemetryVar.Gear,
    TelemetryVar.RPM,
    TelemetryVar.Speed
])]
internal class Program
{
    private static readonly Stopwatch RefreshTimer =
        Stopwatch.StartNew();

    private static readonly TelemetrySnapshot Snapshot =
        new();

    private static readonly ConsoleDashboard Dashboard =
        new();

    private static async Task Main()
    {
        Dashboard.Initialize();

        await using var client =
            TelemetryClient<TelemetryData>.Create(
                NullLogger.Instance);

        using var cancellationSource =
            new CancellationTokenSource();

        Console.CancelKeyPress += (_, eventArgs) =>
        {
            eventArgs.Cancel = true;
            cancellationSource.Cancel();
        };

        var handlers =
            new TelemetryHandlers<TelemetryData>
            {
                OnConnectStateChanged = state =>
                {
                    Snapshot.ConnectionState =
                        state
                            .ToString()
                            .ToUpperInvariant();

                    Dashboard.Render(Snapshot);

                    return Task.CompletedTask;
                },

                OnTelemetryUpdate = data =>
                {
                    Snapshot.IsOnTrack =
                        data.IsOnTrackCar;

                    Snapshot.Gear =
                        data.Gear;

                    Snapshot.Rpm =
                        data.RPM;

                    Snapshot.SpeedMetersPerSecond =
                        data.Speed;

                    if (RefreshTimer.ElapsedMilliseconds
                        >= 100)
                    {
                        Dashboard.Render(Snapshot);
                        RefreshTimer.Restart();
                    }

                    return Task.CompletedTask;
                },

                OnError = _ =>
                {
                    Snapshot.ConnectionState =
                        "ERROR";

                    Dashboard.Render(Snapshot);

                    return Task.CompletedTask;
                }
            };

        Dashboard.Render(Snapshot);

        try
        {
            await client.Monitor(
                handlers,
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
}