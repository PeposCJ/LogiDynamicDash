using System.Text;
using LogiDynamicDash.Hidpp;
using Rs50SharedHidppCoexistence;

namespace LogiDynamicDash.Tests;

public sealed class Rs50SharedHidppCoexistenceTests
{
    private static readonly string[] ValidArguments =
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

    [Fact]
    public void MissingArguments_RejectBeforeOpenOrDelay()
    {
        bool opened = false;
        var delay = new RecordingDelay();

        int exitCode = BuildICoexistenceProgram.Run(
            [],
            () =>
            {
                opened = true;
                throw new InvalidOperationException();
            },
            delay,
            TextWriter.Null,
            TextWriter.Null);

        Assert.Equal(2, exitCode);
        Assert.False(opened);
        Assert.Equal(0, delay.WaitCount);
    }

    [Theory]
    [InlineData(
        "--arm-rs50-shared-hidpp-coexistence",
        "--confirm-ghub-closed",
        "--confirm-iracing-running",
        "--confirm-car-stationary-in-pits",
        "--confirm-rs50-awake",
        "--confirm-dynamic-selected",
        "--confirm-usbpcap-running")]
    [InlineData(
        "--confirm-ghub-closed",
        "--arm-rs50-shared-hidpp-coexistence",
        "--confirm-iracing-running",
        "--confirm-car-stationary-in-pits",
        "--confirm-rs50-awake",
        "--confirm-dynamic-selected",
        "--confirm-usbpcap-running",
        "--confirm-one-hz-five-fixed-frames")]
    [InlineData(
        "--arm-rs50-shared-hidpp-coexistence",
        "--confirm-ghub-closed",
        "--confirm-iracing-running",
        "--confirm-car-stationary-in-pits",
        "--confirm-rs50-awake",
        "--confirm-dynamic-selected",
        "--confirm-usbpcap-running",
        "--confirm-one-hz-five-fixed-frames",
        "--extra")]
    public void PartialReorderedOrExtraArguments_Reject(
        params string[] arguments)
    {
        bool opened = false;

        int exitCode = BuildICoexistenceProgram.Run(
            arguments,
            () =>
            {
                opened = true;
                throw new InvalidOperationException();
            },
            new RecordingDelay(),
            TextWriter.Null,
            TextWriter.Null);

        Assert.Equal(2, exitCode);
        Assert.False(opened);
    }

    [Fact]
    public void ExactArguments_SendFiveFixedFramesAndDispose()
    {
        var exchange = new RecordingExchange();
        var delay = new RecordingDelay();

        int exitCode = BuildICoexistenceProgram.Run(
            ValidArguments,
            () => exchange,
            delay,
            TextWriter.Null,
            TextWriter.Null);

        Assert.Equal(0, exitCode);
        Assert.True(exchange.Disposed);
        Assert.Equal(4, delay.WaitCount);
        Assert.Equal(6, exchange.Transactions.Count);

        for (int index = 1; index <= 5; index++)
        {
            byte[] report =
                exchange.Transactions[index].Request.ToArray();
            Assert.Equal("IRACING COEXIST", ReadText(report, 5, 19));
            Assert.Equal("BUILD I", ReadText(report, 24, 10));
            Assert.Equal(
                $"FRAME {index} OF 5",
                ReadText(report, 34, 19));
            Assert.Equal("1 HZ", ReadText(report, 53, 10));
        }
    }

    [Fact]
    public void ThirdFrameFailure_StopsAndDisposes()
    {
        var exchange = new RecordingExchange
        {
            FailingLayoutNumber = 3
        };
        var delay = new RecordingDelay();

        int exitCode = BuildICoexistenceProgram.Run(
            ValidArguments,
            () => exchange,
            delay,
            TextWriter.Null,
            TextWriter.Null);

        Assert.Equal(1, exitCode);
        Assert.True(exchange.Disposed);
        Assert.Equal(4, exchange.Transactions.Count);
        Assert.Equal(2, delay.WaitCount);
    }

    [Fact]
    public void DelayFailure_StopsBeforeSecondFrameAndDisposes()
    {
        var exchange = new RecordingExchange();
        var delay = new RecordingDelay
        {
            Failure = new IOException("delay rejected")
        };

        int exitCode = BuildICoexistenceProgram.Run(
            ValidArguments,
            () => exchange,
            delay,
            TextWriter.Null,
            TextWriter.Null);

        Assert.Equal(1, exitCode);
        Assert.True(exchange.Disposed);
        Assert.Equal(2, exchange.Transactions.Count);
        Assert.Equal(1, delay.WaitCount);
    }

    [Fact]
    public void OpenFailure_IsReportedFailClosed()
    {
        using var error = new StringWriter();

        int exitCode = BuildICoexistenceProgram.Run(
            ValidArguments,
            () => throw new IOException("open rejected"),
            new RecordingDelay(),
            TextWriter.Null,
            error);

        Assert.Equal(1, exitCode);
        Assert.Contains("failed closed", error.ToString());
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

        return Encoding.ASCII.GetString(field);
    }

    private sealed class RecordingDelay : IBuildIDelay
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

    private sealed class RecordingExchange : IRs50HidppDisplayExchange
    {
        private int layoutCount;

        public int? FailingLayoutNumber { get; init; }

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
                return ValidDiscoveryResponse();
            }

            layoutCount++;
            return layoutCount == FailingLayoutNumber
                ? new byte[64]
                : ValidLayoutResponse();
        }

        public void Dispose() =>
            Disposed = true;

        private static byte[] ValidDiscoveryResponse()
        {
            byte[] response = new byte[64];
            response[0] = 0x12;
            response[1] = 0xFF;
            response[2] = 0x00;
            response[3] = 0x0A;
            response[4] = 0x12;
            return response;
        }

        private static byte[] ValidLayoutResponse()
        {
            byte[] response = new byte[64];
            response[0] = 0x12;
            response[1] = 0xFF;
            response[2] = 0x12;
            response[3] = 0x3A;
            return response;
        }
    }
}
