using System.Text;
using LogiDynamicDash.Hidpp;
using Rs50SharedHidppCoexistence;

string[] validArguments =
[
    "--arm-rs50-shared-hidpp-coexistence",
    "--confirm-ghub-closed",
    "--confirm-iracing-running",
    "--confirm-car-stationary-in-pits",
    "--confirm-rs50-awake",
    "--confirm-dynamic-selected",
    "--confirm-usbpcap-running",
    "--confirm-one-hz-five-fixed-frames"
];

VerifyInvalidArguments();
VerifyFixedSequence();
VerifyFrameFailure();
VerifyDelayFailure();

Console.WriteLine(
    "Build I fake-only verification passed: arming, five fixed frames, " +
    "timing, fail-closed behavior, and disposal.");
return;

void VerifyInvalidArguments()
{
    bool opened = false;
    int exitCode = BuildICoexistenceProgram.Run(
        [],
        () =>
        {
            opened = true;
            throw new InvalidOperationException();
        },
        new RecordingDelay(),
        TextWriter.Null,
        TextWriter.Null);

    Require(exitCode == 2, "Invalid arguments must return exit code 2.");
    Require(!opened, "Invalid arguments must not open an exchange.");
}

void VerifyFixedSequence()
{
    var exchange = new RecordingExchange();
    var delay = new RecordingDelay();

    int exitCode = BuildICoexistenceProgram.Run(
        validArguments,
        () => exchange,
        delay,
        TextWriter.Null,
        TextWriter.Null);

    Require(exitCode == 0, "The fixed sequence must succeed.");
    Require(exchange.Disposed, "The successful exchange must be disposed.");
    Require(delay.WaitCount == 4, "The sequence must wait exactly four times.");
    Require(
        exchange.Transactions.Count == 6,
        "The sequence must contain one discovery and five setters.");

    for (int index = 1; index <= 5; index++)
    {
        byte[] report = exchange.Transactions[index].Request.ToArray();
        Require(
            ReadText(report, 5, 19) == "IRACING COEXIST",
            "Line 1 changed.");
        Require(
            ReadText(report, 24, 10) == "BUILD I",
            "Line 2 changed.");
        Require(
            ReadText(report, 34, 19) == $"FRAME {index} OF 5",
            "The frame counter changed.");
        Require(
            ReadText(report, 53, 10) == "1 HZ",
            "Line 4 changed.");
    }
}

void VerifyFrameFailure()
{
    var exchange = new RecordingExchange
    {
        FailingLayoutNumber = 3
    };
    var delay = new RecordingDelay();

    int exitCode = BuildICoexistenceProgram.Run(
        validArguments,
        () => exchange,
        delay,
        TextWriter.Null,
        TextWriter.Null);

    Require(exitCode == 1, "A frame failure must return exit code 1.");
    Require(exchange.Disposed, "A failed exchange must be disposed.");
    Require(
        exchange.Transactions.Count == 4,
        "No frame may follow the third-frame failure.");
    Require(delay.WaitCount == 2, "No delay may follow a frame failure.");
}

void VerifyDelayFailure()
{
    var exchange = new RecordingExchange();
    var delay = new RecordingDelay
    {
        Failure = new IOException("delay rejected")
    };

    int exitCode = BuildICoexistenceProgram.Run(
        validArguments,
        () => exchange,
        delay,
        TextWriter.Null,
        TextWriter.Null);

    Require(exitCode == 1, "A delay failure must return exit code 1.");
    Require(exchange.Disposed, "A delay failure must dispose the exchange.");
    Require(
        exchange.Transactions.Count == 2,
        "No second frame may follow a delay failure.");
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

sealed class RecordingDelay : IBuildIDelay
{
    public Exception? Failure { get; init; }

    public int WaitCount { get; private set; }

    public void WaitOneSecond()
    {
        WaitCount++;
        if (Failure is not null)
        {
            throw Failure;
        }
    }
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
