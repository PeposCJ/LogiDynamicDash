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
    private static int _telemetryUpdateCount;

    private static async Task Main()
    {
        Console.Title = "LogiDynamicDash";

        Console.WriteLine("================================");
        Console.WriteLine("       LogiDynamicDash");
        Console.WriteLine("================================");
        Console.WriteLine();
        Console.WriteLine("Starting iRacing telemetry monitor...");
        Console.WriteLine("Press Ctrl+C to stop.");
        Console.WriteLine();

        await using var client =
            TelemetryClient<TelemetryData>.Create(NullLogger.Instance);

        using var cancellationSource = new CancellationTokenSource();

        Console.CancelKeyPress += (_, eventArgs) =>
        {
            eventArgs.Cancel = true;
            cancellationSource.Cancel();
        };

        var handlers = new TelemetryHandlers<TelemetryData>
        {
            OnConnectStateChanged = state =>
            {
                Console.WriteLine($"Connection state: {state}");
                return Task.CompletedTask;
            },

            OnTelemetryUpdate = data =>
            {
                _telemetryUpdateCount++;

                if (_telemetryUpdateCount % 60 != 0)
                {
                    return Task.CompletedTask;
                }

                float? speedKph = data.Speed * 3.6f;
                float? speedMph = data.Speed * 2.23694f;

                string gear = data.Gear switch
                {
                    -1 => "R",
                    0 => "N",
                    int value => value.ToString(),
                    _ => "N/A"
                };

                string rpm = data.RPM?.ToString("F0") ?? "N/A";
                string kph = speedKph?.ToString("F0") ?? "N/A";
                string mph = speedMph?.ToString("F0") ?? "N/A";
                string onTrack = data.IsOnTrackCar == true ? "Yes" : "No";

                Console.WriteLine(
                    $"On track: {onTrack} | Gear: {gear} | RPM: {rpm} | " +
                    $"Speed: {kph} km/h ({mph} mph)");

                return Task.CompletedTask;
            },

            OnError = error =>
            {
                Console.WriteLine($"Telemetry error: {error.Message}");
                return Task.CompletedTask;
            }
        };

        try
        {
            await client.Monitor(handlers, cancellationSource.Token);
        }
        catch (OperationCanceledException)
        {
            // Expected when the user presses Ctrl+C.
        }

        Console.WriteLine();
        Console.WriteLine("Telemetry monitoring stopped.");
    }
}