using System.Text.Json;
using LogiDynamicDash.Diagnostics;
using LogiDynamicDash.Hidpp;
using LogiDynamicDash.Models;

namespace LogiDynamicDash.Tests;

public sealed class SanitizedRs50OledDiagnosticsTests
{
    [Fact]
    public void DiagnosticSession_RecordsOnlySanitizedTypedFields()
    {
        ManualTimeProvider clock = new();
        FakeSession inner = new(clock);
        StringWriter writer = new();
        SanitizedRs50OledDiagnostics diagnostics =
            new(writer, clock);
        using DiagnosticRs50OledSession session =
            new(inner, diagnostics, clock);

        session.Open();
        session.Send(new Rs50LayoutEFrame(
            Rs50GaugeLevel.FromRatio(0.5),
            Rs50GaugeLevel.FromRatio(0.25),
            "100 KMH",
            "3"));

        string[] lines = writer
            .ToString()
            .Split(
                Environment.NewLine,
                StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(2, lines.Length);

        using JsonDocument open = JsonDocument.Parse(lines[0]);
        Assert.Equal(
            "open",
            open.RootElement.GetProperty("event").GetString());
        Assert.Equal(
            2000,
            open.RootElement
                .GetProperty("elapsed_microseconds")
                .GetInt64());

        using JsonDocument frame = JsonDocument.Parse(lines[1]);
        Assert.Equal(
            "E",
            frame.RootElement.GetProperty("layout").GetString());
        Assert.Equal(
            "acknowledged",
            frame.RootElement.GetProperty("result").GetString());
        Assert.Equal(
            3000,
            frame.RootElement
                .GetProperty("elapsed_microseconds")
                .GetInt64());

        Assert.DoesNotContain("report", writer.ToString());
        Assert.DoesNotContain("device", writer.ToString());
        Assert.DoesNotContain("path", writer.ToString());
    }

    [Fact]
    public void Failure_RecordsTypeButNeverExceptionMessage()
    {
        ManualTimeProvider clock = new();
        FakeSession inner = new(clock)
        {
            SendException = new IOException(
                @"secret path \\?\hid#serial-private")
        };
        StringWriter writer = new();
        using DiagnosticRs50OledSession session = new(
            inner,
            new SanitizedRs50OledDiagnostics(writer, clock),
            clock);
        session.Open();

        Assert.Throws<IOException>(
            () => session.Send(new Rs50LayoutAFrame()));

        string text = writer.ToString();
        Assert.Contains("\"error_type\":\"IOException\"", text);
        Assert.DoesNotContain("secret", text);
        Assert.DoesNotContain("serial-private", text);
    }

    private sealed class FakeSession(ManualTimeProvider clock)
        : IRs50OledSession
    {
        public Exception? SendException { get; set; }

        public void Open() =>
            clock.Advance(TimeSpan.FromMilliseconds(2));

        public Rs50OledSendResult Send(Rs50OledFrame frame)
        {
            clock.Advance(TimeSpan.FromMilliseconds(3));
            if (SendException is not null)
            {
                throw SendException;
            }

            return Rs50OledSendResult.Transmitted;
        }

        public void Dispose()
        {
        }
    }

    private sealed class ManualTimeProvider : TimeProvider
    {
        private long timestamp;
        private readonly DateTimeOffset origin =
            new(2026, 7, 29, 0, 0, 0, TimeSpan.Zero);

        public override long TimestampFrequency => TimeSpan.TicksPerSecond;

        public override long GetTimestamp() => timestamp;

        public override DateTimeOffset GetUtcNow() =>
            origin.AddTicks(timestamp);

        internal void Advance(TimeSpan duration) =>
            timestamp += duration.Ticks;
    }
}
