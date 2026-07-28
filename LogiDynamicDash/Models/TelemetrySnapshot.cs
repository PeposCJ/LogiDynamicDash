namespace LogiDynamicDash.Models;

internal sealed record TelemetrySnapshot
{
    public string ConnectionState { get; init; } = "WAITING";

    public bool? IsOnTrack { get; init; }

    public int? Gear { get; init; }

    public float? Rpm { get; init; }

    public float? SpeedMetersPerSecond { get; init; }
    public float? BrakeBiasPercent { get; init; }

    public float? LastLapTimeSeconds { get; init; }
}
