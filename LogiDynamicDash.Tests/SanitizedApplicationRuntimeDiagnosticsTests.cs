using System.Text.Json;
using LogiDynamicDash.Diagnostics;
using LogiDynamicDash.Models;

namespace LogiDynamicDash.Tests;

public sealed class SanitizedApplicationRuntimeDiagnosticsTests
{
    [Fact]
    public void RecordsRenderAndStopAsSanitizedJsonLines()
    {
        StringWriter writer = new();
        using SanitizedApplicationRuntimeDiagnostics diagnostics =
            new(writer);
        TelemetrySnapshot snapshot = new()
        {
            ConnectionState = "CONNECTED",
            IsOnTrack = true,
            Gear = 0,
            SpeedMetersPerSecond = 0,
            SessionIdentity = new IRacingSessionIdentity(
                IRacingDiscipline.DirtRoad,
                "DirtRoad",
                "dirt oval",
                null)
        };

        diagnostics.RecordRender(
            "telemetry",
            snapshot,
            DisplayMode.Normal);
        diagnostics.RecordStop(ApplicationLifecycleState.Stopped);

        string[] lines = writer.ToString().Split(
            Environment.NewLine,
            StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(2, lines.Length);

        using JsonDocument render = JsonDocument.Parse(lines[0]);
        JsonElement root = render.RootElement;
        Assert.Equal("render", root.GetProperty("event").GetString());
        Assert.Equal(
            "telemetry",
            root.GetProperty("trigger").GetString());
        Assert.Equal(
            "CONNECTED",
            root.GetProperty("connection_state").GetString());
        Assert.Equal("Normal", root.GetProperty("mode").GetString());
        Assert.True(root.GetProperty("is_on_track").GetBoolean());
        Assert.Equal(0, root.GetProperty("gear").GetInt32());
        Assert.Equal(
            0,
            root.GetProperty("speed_meters_per_second").GetSingle());
        Assert.True(
            root.GetProperty("has_session_identity").GetBoolean());
        Assert.False(root.TryGetProperty("session_identity", out _));

        using JsonDocument stop = JsonDocument.Parse(lines[1]);
        Assert.Equal(
            "stop",
            stop.RootElement.GetProperty("event").GetString());
        Assert.Equal(
            "Stopped",
            stop.RootElement.GetProperty("state").GetString());
    }
}
