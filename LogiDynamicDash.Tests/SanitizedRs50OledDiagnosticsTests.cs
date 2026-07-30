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

    [Fact]
    public void UnacknowledgedFrame_IsRecordedAsTypedNonFailure()
    {
        ManualTimeProvider clock = new();
        FakeSession inner = new(clock)
        {
            Result = Rs50OledSendResult.Unacknowledged
        };
        StringWriter writer = new();
        using DiagnosticRs50OledSession session = new(
            inner,
            new SanitizedRs50OledDiagnostics(writer, clock),
            clock);
        session.Open();

        Assert.Equal(
            Rs50OledSendResult.Unacknowledged,
            session.Send(new Rs50LayoutAFrame()));

        string text = writer.ToString();
        Assert.Contains("\"result\":\"unacknowledged\"", text);
        Assert.DoesNotContain("\"event\":\"failure\"", text);
    }

    [Fact]
    public void DiagnosticWriteFailure_PropagatesAndOriginalFailureIsNotMasked()
    {
        ManualTimeProvider clock = new();
        DiagnosticRs50OledSession writeFailure = new(
            new FakeSession(clock),
            new SanitizedRs50OledDiagnostics(new ThrowingWriter(), clock),
            clock);

        Assert.Throws<IOException>(() => writeFailure.Open());
        Assert.Throws<IOException>(() => writeFailure.Dispose());

        FakeSession failingInner = new(clock)
        {
            SendException = new InvalidOperationException("original")
        };
        DiagnosticRs50OledSession operationFailure = new(
            failingInner,
            new SanitizedRs50OledDiagnostics(
                new ThrowAfterWritesWriter(1),
                clock),
            clock);
        operationFailure.Open();
        InvalidOperationException exception =
            Assert.Throws<InvalidOperationException>(
                () => operationFailure.Send(new Rs50LayoutAFrame()));
        Assert.Equal("original", exception.Message);
        Assert.Throws<IOException>(() => operationFailure.Dispose());
    }

    private sealed class FakeSession(ManualTimeProvider clock)
        : IRs50OledSession
    {
        public Exception? SendException { get; set; }
        public Rs50OledSendResult Result { get; set; } =
            Rs50OledSendResult.Transmitted;

        public void Open() =>
            clock.Advance(TimeSpan.FromMilliseconds(2));

        public Rs50OledSendResult Send(Rs50OledFrame frame)
        {
            clock.Advance(TimeSpan.FromMilliseconds(3));
            if (SendException is not null)
            {
                throw SendException;
            }

            return Result;
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

    private sealed class ThrowingWriter : TextWriter
    {
        public override System.Text.Encoding Encoding =>
            System.Text.Encoding.UTF8;

        public override void WriteLine(string? value) =>
            throw new IOException("injected disk failure");
    }

    private sealed class ThrowAfterWritesWriter(int writesBeforeFailure)
        : TextWriter
    {
        private int writes;

        public override System.Text.Encoding Encoding =>
            System.Text.Encoding.UTF8;

        public override void WriteLine(string? value)
        {
            if (writes++ >= writesBeforeFailure)
            {
                throw new IOException("injected disk failure");
            }
        }
    }
}
