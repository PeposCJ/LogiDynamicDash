using LogiDynamicDash.Models;
using Microsoft.Extensions.Logging.Abstractions;
using SVappsLAB.iRacingTelemetrySDK;

namespace LogiDynamicDash.Services;

internal sealed class IRacingTelemetryService : ITelemetrySource
{
    public async Task MonitorAsync(
        TelemetrySnapshot snapshot,
        Action<TelemetrySnapshot> onTelemetryUpdated,
        Action<TelemetrySnapshot> onStatusChanged,
        CancellationToken cancellationToken)
    {
        await using var client =
            TelemetryClient<TelemetryData>.Create(
                NullLogger.Instance);

        var handlers =
            new TelemetryHandlers<TelemetryData>
            {
                OnConnectStateChanged = state =>
                {
                    snapshot.ConnectionState =
                        state
                            .ToString()
                            .ToUpperInvariant();

                    onStatusChanged(snapshot);

                    return Task.CompletedTask;
                },

                OnTelemetryUpdate = data =>
                {
                    snapshot.IsOnTrack =
                        data.IsOnTrackCar;

                    snapshot.Gear =
                        data.Gear;

                    snapshot.Rpm =
                        data.RPM;

                    snapshot.SpeedMetersPerSecond =
                        data.Speed;

                    snapshot.BrakeBiasPercent =
                        data.dcBrakeBias;

                    snapshot.LastLapTimeSeconds =
                        data.LapLastLapTime;

                    onTelemetryUpdated(snapshot);

                    return Task.CompletedTask;
                },

                OnError = _ =>
                {
                    snapshot.ConnectionState =
                        "ERROR";

                    onStatusChanged(snapshot);

                    return Task.CompletedTask;
                }
            };

        await client.Monitor(
            handlers,
            cancellationToken);
    }
}
