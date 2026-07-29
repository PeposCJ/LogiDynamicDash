using System.Text;
using LogiDynamicDash.Hidpp;
using Rs50SharedHidppTelemetryTrial;

string[] validArguments =
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

await VerifyInvalidArguments();
await VerifyFormattingDeduplicationAndRateLimit();
await VerifyMovementFailure();
await VerifyDisconnectAfterTelemetryFailure();
await VerifyEarlySourceEnd();
await VerifyProtocolFailure();

Console.WriteLine(
    "Build J fake-only verification passed: arming, telemetry " +
    "formatting, deduplication, 5 Hz rate limit, movement stop, " +
    "fail-closed behavior, and disposal.");

async Task VerifyInvalidArguments()
{
    bool opened = false;
    var source = new ScriptedTelemetrySource([], cancelAtEnd: true);

    int exitCode = await BuildJTelemetryTrialProgram.RunAsync(
        [],
        source,
        () =>
        {
            opened = true;
            throw new InvalidOperationException();
        },
        new ManualTimeProvider(),
        new CancellationToken(canceled: true),
        TextWriter.Null,
        TextWriter.Null);

    Require(exitCode == 2, "Invalid arguments must return exit code 2.");
    Require(!opened, "Invalid arguments must not open an exchange.");
    Require(!source.Started, "Invalid arguments must not start telemetry.");
}

async Task VerifyFormattingDeduplicationAndRateLimit()
{
    using var cancellationSource = new CancellationTokenSource();
    var time = new ManualTimeProvider();
    var source = new ScriptedTelemetrySource(
        [
            new(false, false, 0, 0.0f),
            new(true, true, 0, 0.0f),
            new(true, true, 0, 0.0f),
            new(true, true, 1, 0.1f),
            new(true, true, 1, 0.1f),
            new(true, true, -1, 0.2f)
        ],
        cancelAtEnd: true,
        cancellationSource,
        time,
        [
            TimeSpan.Zero,
            TimeSpan.Zero,
            TimeSpan.FromMilliseconds(50),
            TimeSpan.FromMilliseconds(100),
            TimeSpan.FromMilliseconds(100),
            TimeSpan.FromMilliseconds(200)
        ]);
    var exchange = new RecordingExchange();

    int exitCode = await BuildJTelemetryTrialProgram.RunAsync(
        validArguments,
        source,
        () => exchange,
        time,
        cancellationSource.Token,
        TextWriter.Null,
        TextWriter.Null);

    Require(exitCode == 0, "The stationary script must succeed.");
    Require(exchange.Disposed, "The successful exchange must be disposed.");
    Require(
        exchange.Transactions.Count == 4,
        "Expected one discovery and three rate-limited telemetry frames.");

    byte[] neutral = exchange.Transactions[1].Request.ToArray();
    Require(ReadText(neutral, 5, 19) == "SPEED", "Line 1 changed.");
    Require(ReadText(neutral, 24, 10) == "0 KMH", "Speed changed.");
    Require(ReadText(neutral, 34, 19) == "GEAR", "Line 3 changed.");
    Require(ReadText(neutral, 53, 10) == "N", "Neutral changed.");

    byte[] firstGear = exchange.Transactions[2].Request.ToArray();
    Require(ReadText(firstGear, 24, 10) == "0 KMH", "Speed changed.");
    Require(ReadText(firstGear, 53, 10) == "1", "First gear changed.");

    byte[] reverse = exchange.Transactions[3].Request.ToArray();
    Require(ReadText(reverse, 24, 10) == "1 KMH", "Rounding changed.");
    Require(ReadText(reverse, 53, 10) == "R", "Reverse changed.");
}

async Task VerifyMovementFailure()
{
    using var cancellationSource = new CancellationTokenSource();
    var exchange = new RecordingExchange();
    var source = new ScriptedTelemetrySource(
        [new(true, true, 1, 0.51f)],
        cancelAtEnd: false);

    int exitCode = await BuildJTelemetryTrialProgram.RunAsync(
        validArguments,
        source,
        () => exchange,
        new ManualTimeProvider(),
        cancellationSource.Token,
        TextWriter.Null,
        TextWriter.Null);

    Require(exitCode == 1, "Movement must fail closed.");
    Require(exchange.Disposed, "Movement failure must dispose.");
    Require(
        exchange.Transactions.Count == 1,
        "Movement must stop before a layout setter.");
}

async Task VerifyDisconnectAfterTelemetryFailure()
{
    var exchange = new RecordingExchange();
    var source = new ScriptedTelemetrySource(
        [
            new(true, true, 0, 0.0f),
            new(false, null, null, null)
        ],
        cancelAtEnd: false);

    int exitCode = await BuildJTelemetryTrialProgram.RunAsync(
        validArguments,
        source,
        () => exchange,
        new ManualTimeProvider(),
        CancellationToken.None,
        TextWriter.Null,
        TextWriter.Null);

    Require(exitCode == 1, "A telemetry disconnect must fail closed.");
    Require(exchange.Disposed, "A disconnect must dispose the exchange.");
    Require(
        exchange.Transactions.Count == 2,
        "A disconnect must not send an additional layout frame.");
}

async Task VerifyEarlySourceEnd()
{
    var exchange = new RecordingExchange();
    var source = new ScriptedTelemetrySource(
        [new(true, true, 0, 0.0f)],
        cancelAtEnd: false);

    int exitCode = await BuildJTelemetryTrialProgram.RunAsync(
        validArguments,
        source,
        () => exchange,
        new ManualTimeProvider(),
        CancellationToken.None,
        TextWriter.Null,
        TextWriter.Null);

    Require(exitCode == 1, "An early telemetry end must fail closed.");
    Require(exchange.Disposed, "An early end must dispose.");
}

async Task VerifyProtocolFailure()
{
    using var cancellationSource = new CancellationTokenSource();
    var exchange = new RecordingExchange
    {
        FailingLayoutNumber = 1
    };
    var source = new ScriptedTelemetrySource(
        [new(true, true, 0, 0.0f)],
        cancelAtEnd: true,
        cancellationSource);

    int exitCode = await BuildJTelemetryTrialProgram.RunAsync(
        validArguments,
        source,
        () => exchange,
        new ManualTimeProvider(),
        cancellationSource.Token,
        TextWriter.Null,
        TextWriter.Null);

    Require(exitCode == 1, "A bad ACK must fail closed.");
    Require(exchange.Disposed, "A bad ACK must dispose.");
    Require(exchange.Transactions.Count == 2, "Unexpected retry.");
}

static void Require(bool condition, string message)
{
    if (!condition)
    {
        throw new InvalidOperationException(message);
    }
}

static string ReadText(byte[] report, int offset, int length)
{
    ReadOnlySpan<byte> field = report.AsSpan(offset, length);
    int terminator = field.IndexOf((byte)0);
    if (terminator >= 0)
    {
        field = field[..terminator];
    }

    return Encoding.ASCII.GetString(field);
}

sealed class ScriptedTelemetrySource(
    IReadOnlyList<BuildJTelemetrySnapshot> snapshots,
    bool cancelAtEnd,
    CancellationTokenSource? cancellationSource = null,
    ManualTimeProvider? timeProvider = null,
    IReadOnlyList<TimeSpan>? advances = null)
    : IBuildJTelemetrySource
{
    public bool Started { get; private set; }

    public Task MonitorAsync(
        Action<BuildJTelemetrySnapshot> onTelemetry,
        CancellationToken cancellationToken)
    {
        Started = true;
        for (int index = 0; index < snapshots.Count; index++)
        {
            timeProvider?.Advance(
                advances is null ? TimeSpan.Zero : advances[index]);
            onTelemetry(snapshots[index]);
        }

        if (cancelAtEnd)
        {
            cancellationSource?.Cancel();
            cancellationToken.ThrowIfCancellationRequested();
        }

        return Task.CompletedTask;
    }
}

sealed class ManualTimeProvider : TimeProvider
{
    private long timestamp;

    public override long TimestampFrequency => TimeSpan.TicksPerSecond;

    public override long GetTimestamp() => timestamp;

    public void Advance(TimeSpan duration) =>
        timestamp += duration.Ticks;
}

sealed class RecordingExchange : IRs50HidppDisplayExchange
{
    private int layoutCount;

    public int? FailingLayoutNumber { get; init; }

    public List<Rs50HidppDisplayTransaction> Transactions { get; } = [];

    public bool Disposed { get; private set; }

    public byte[] Exchange(Rs50HidppDisplayTransaction transaction)
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

        layoutCount++;
        if (layoutCount == FailingLayoutNumber)
        {
            return new byte[64];
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
