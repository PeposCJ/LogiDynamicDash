using LogiDynamicDash.Models;
using Microsoft.Extensions.Logging.Abstractions;
using SVappsLAB.iRacingTelemetrySDK;

namespace LogiDynamicDash.Services;

internal sealed class IRacingTelemetryService
{
    public async Task MonitorAsync(
        TelemetrySnapshot snapshot,
        Action<TelemetrySnapshot> onTelemetryUpdated,
        Action<TelemetrySnapshot> onStatusChanged,
        CancellationToken cancellationToken)
    {
        object snapshotGate = new();
        TelemetrySnapshot currentSnapshot = snapshot;

        await using var client =
            TelemetryClient<TelemetryData>.Create(
                NullLogger.Instance);

        var handlers =
            new TelemetryHandlers<TelemetryData>
            {
                OnConnectStateChanged = state =>
                {
                    lock (snapshotGate)
                    {
                        currentSnapshot = currentSnapshot with
                        {
                            ConnectionState = state
                                .ToString()
                                .ToUpperInvariant()
                        };

                        onStatusChanged(currentSnapshot);
                    }

                    return Task.CompletedTask;
                },

                OnTelemetryUpdate = data =>
                {
                    lock (snapshotGate)
                    {
                        currentSnapshot = currentSnapshot with
                        {
                            IsOnTrack = data.IsOnTrackCar,
                            Gear = data.Gear,
                            Rpm = data.RPM,
                            SpeedMetersPerSecond = data.Speed,
                            BrakeBiasPercent = data.dcBrakeBias,
                            LastLapTimeSeconds = data.LapLastLapTime
                        };

                        onTelemetryUpdated(currentSnapshot);
                    }

                    return Task.CompletedTask;
                },

                OnError = _ =>
                {
                    lock (snapshotGate)
                    {
                        currentSnapshot = currentSnapshot with
                        {
                            ConnectionState = "ERROR"
                        };

                        onStatusChanged(currentSnapshot);
                    }

                    return Task.CompletedTask;
                }
            };

        await client.Monitor(
            handlers,
            cancellationToken);
    }
}
