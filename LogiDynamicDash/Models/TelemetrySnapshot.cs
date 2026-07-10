namespace LogiDynamicDash.Models;

internal sealed class TelemetrySnapshot
{
    public string ConnectionState { get; set; } = "WAITING";

    public bool? IsOnTrack { get; set; }

    public int? Gear { get; set; }

    public float? Rpm { get; set; }

    public float? SpeedMetersPerSecond { get; set; }
    public float? BrakeBiasPercent { get; set; }

    public float? LastLapTimeSeconds { get; set; }
}