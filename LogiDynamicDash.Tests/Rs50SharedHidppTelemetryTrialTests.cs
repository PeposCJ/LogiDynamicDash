using System.Text.Json;
using LogiDynamicDash.Hidpp;
using Rs50SharedHidppTelemetryTrial;

namespace LogiDynamicDash.Tests;

public sealed class Rs50SharedHidppTelemetryTrialTests
{
    private static readonly string[] ValidArguments =
    [
        "--arm-rs50-shared-hidpp-telemetry",
        "--confirm-ghub-closed",
        "--confirm-iracing-running",
        "--confirm-car-stationary-in-pits",
        "--confirm-rs50-awake",
        "--confirm-dynamic-selected",
        "--confirm-usbpcap-running",
        "--confirm-video-recording",
        "--confirm-10-second-telemetry-trial"
    ];

    [Fact]
    public async Task MissingArguments_RejectBeforeOpeningAnything()
    {
        bool opened = false;
        var source = new RecordingSource([]);

        int exitCode = await BuildJTelemetryTrialProgram.RunAsync(
            [],
            source,
            () =>
            {
                opened = true;
                throw new InvalidOperationException();
            },
            TimeProvider.System,
            new CancellationToken(canceled: true),
            TextWriter.Null,
            TextWriter.Null);

        Assert.Equal(2, exitCode);
        Assert.False(opened);
        Assert.False(source.Started);
    }

    [Fact]
    public async Task NeutralStationaryTelemetry_SendsExpectedLayout()
    {
        using var cancellationSource = new CancellationTokenSource();
        var source = new RecordingSource(
            [new(true, true, 0, 0.0f)],
            cancellationSource);
        var exchange = new RecordingExchange();

        int exitCode = await BuildJTelemetryTrialProgram.RunAsync(
            ValidArguments,
            source,
            () => exchange,
            TimeProvider.System,
            cancellationSource.Token,
            TextWriter.Null,
            TextWriter.Null);

        Assert.Equal(0, exitCode);
        Assert.True(exchange.Disposed);
        Assert.Equal(2, exchange.Transactions.Count);
        byte[] report = exchange.Transactions[1].Request.ToArray();
        Assert.Equal("SPEED", ReadText(report, 5, 19));
        Assert.Equal("0 KMH", ReadText(report, 24, 10));
        Assert.Equal("GEAR", ReadText(report, 34, 19));
        Assert.Equal("N", ReadText(report, 53, 10));
    }

    [Fact]
    public async Task Movement_StopsBeforeSetterAndDisposes()
    {
        var source = new RecordingSource(
            [new(true, true, 1, 0.51f)]);
        var exchange = new RecordingExchange();

        int exitCode = await BuildJTelemetryTrialProgram.RunAsync(
            ValidArguments,
            source,
            () => exchange,
            TimeProvider.System,
            CancellationToken.None,
            TextWriter.Null,
            TextWriter.Null);

        Assert.Equal(1, exitCode);
        Assert.True(exchange.Disposed);
        Assert.Single(exchange.Transactions);
    }

    [Fact]
    public async Task DisconnectAfterValidTelemetry_FailsWithoutAnotherSetter()
    {
        var source = new RecordingSource(
        [
            new(true, true, 0, 0.0f),
            new(false, null, null, null)
        ]);
        var exchange = new RecordingExchange();

        int exitCode = await BuildJTelemetryTrialProgram.RunAsync(
            ValidArguments,
            source,
            () => exchange,
            TimeProvider.System,
            CancellationToken.None,
            TextWriter.Null,
            TextWriter.Null);

        Assert.Equal(1, exitCode);
        Assert.True(exchange.Disposed);
        Assert.Equal(2, exchange.Transactions.Count);
    }

    [Fact]
    public void RecordedExchange_WritesExactRequestAndResponseEvents()
    {
        using var transcriptOutput = new StringWriter();
        using var transcript = new BuildJTransactionTranscript(
            transcriptOutput,
            ".tmp/test.jsonl");
        var inner = new RecordingExchange();
        using var exchange = new BuildJRecordedExchange(
            inner,
            transcript,
            TimeProvider.System);

        Rs50HidppDisplayTransaction discovery =
            Rs50HidppDisplayProtocol.CreateDiscovery();
        byte[] discoveryResponse = exchange.Exchange(discovery);
        byte runtimeIndex =
            Rs50HidppDisplayProtocol.ParseDiscoveryResponse(
                discoveryResponse);
        Rs50HidppDisplayTransaction layout =
            Rs50HidppDisplayProtocol.CreateLayoutJ(
                runtimeIndex,
                "SPEED",
                "0 KMH",
                "GEAR",
                "N");
        exchange.Exchange(layout);

        string[] lines = transcriptOutput
            .ToString()
            .Split(
                Environment.NewLine,
                StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(4, lines.Length);

        using JsonDocument discoveryRequest =
            JsonDocument.Parse(lines[0]);
        Assert.Equal(
            "request",
            discoveryRequest.RootElement
                .GetProperty("event_type")
                .GetString());
        Assert.Equal(
            Convert.ToHexString(discovery.Request.Span),
            discoveryRequest.RootElement
                .GetProperty("report_hex")
                .GetString());

        using JsonDocument layoutResponse =
            JsonDocument.Parse(lines[3]);
        Assert.Equal(
            "response",
            layoutResponse.RootElement
                .GetProperty("event_type")
                .GetString());
        Assert.True(
            layoutResponse.RootElement
                .GetProperty("exact_header_match")
                .GetBoolean());
        Assert.Equal(
            "SetLayoutJ",
            layoutResponse.RootElement
                .GetProperty("transaction")
                .GetString());
    }

    [Fact]
    public void Transcript_StopsBeforeExceedingBoundedCapacity()
    {
        using var transcriptOutput = new StringWriter();
        using var transcript = new BuildJTransactionTranscript(
            transcriptOutput,
            ".tmp/test.jsonl");
        Rs50HidppDisplayTransaction discovery =
            Rs50HidppDisplayProtocol.CreateDiscovery();

        for (int index = 0;
             index < BuildJTransactionTranscript.MaximumTransactions;
             index++)
        {
            transcript.RecordRequest(
                discovery,
                DateTimeOffset.UnixEpoch);
        }

        Assert.Throws<IOException>(
            () => transcript.RecordRequest(
                discovery,
                DateTimeOffset.UnixEpoch));
    }

    [Fact]
    public void RecordedExchange_LogsFailureTypeWithoutExceptionMessage()
    {
        using var transcriptOutput = new StringWriter();
        using var transcript = new BuildJTransactionTranscript(
            transcriptOutput,
            ".tmp/test.jsonl");
        using var exchange = new BuildJRecordedExchange(
            new FailingExchange(),
            transcript,
            TimeProvider.System);

        Assert.Throws<IOException>(
            () => exchange.Exchange(
                Rs50HidppDisplayProtocol.CreateDiscovery()));

        string transcriptText = transcriptOutput.ToString();
        Assert.Contains(
            "\"event_type\":\"failure\"",
            transcriptText);
        Assert.Contains(
            typeof(IOException).FullName!,
            transcriptText);
        Assert.DoesNotContain(
            FailingExchange.SensitiveMessage,
            transcriptText);
    }

    private static string ReadText(
        byte[] report,
        int offset,
        int length)
    {
        ReadOnlySpan<byte> field = report.AsSpan(offset, length);
        int terminator = field.IndexOf((byte)0);
        if (terminator >= 0)
        {
            field = field[..terminator];
        }

        return System.Text.Encoding.ASCII.GetString(field);
    }

    private sealed class RecordingSource(
        IReadOnlyList<BuildJTelemetrySnapshot> snapshots,
        CancellationTokenSource? cancellationSource = null)
        : IBuildJTelemetrySource
    {
        public bool Started { get; private set; }

        public Task MonitorAsync(
            Action<BuildJTelemetrySnapshot> onTelemetry,
            CancellationToken cancellationToken)
        {
            Started = true;
            foreach (BuildJTelemetrySnapshot snapshot in snapshots)
            {
                onTelemetry(snapshot);
            }

            cancellationSource?.Cancel();
            cancellationToken.ThrowIfCancellationRequested();
            return Task.CompletedTask;
        }
    }

    private sealed class RecordingExchange : IRs50HidppDisplayExchange
    {
        public List<Rs50HidppDisplayTransaction> Transactions { get; } =
            [];

        public bool Disposed { get; private set; }

        public byte[] Exchange(
            Rs50HidppDisplayTransaction transaction)
        {
            Transactions.Add(transaction);
            if (transaction.Kind ==
                Rs50HidppDisplayTransactionKind.DiscoverDisplayFeature)
            {
                byte[] discovery = new byte[64];
                discovery[0] = 0x12;
                discovery[1] = 0xFF;
                discovery[2] = 0x00;
                discovery[3] = 0x0A;
                discovery[4] = 0x12;
                return discovery;
            }

            byte[] acknowledgement = new byte[64];
            acknowledgement[0] = 0x12;
            acknowledgement[1] = 0xFF;
            acknowledgement[2] = 0x12;
            acknowledgement[3] = 0x3A;
            return acknowledgement;
        }

        public void Dispose() =>
            Disposed = true;
    }

    private sealed class FailingExchange : IRs50HidppDisplayExchange
    {
        internal const string SensitiveMessage =
            "Do not copy a local device path into the transcript.";

        public byte[] Exchange(
            Rs50HidppDisplayTransaction transaction) =>
            throw new IOException(SensitiveMessage);

        public void Dispose()
        {
        }
    }
}
