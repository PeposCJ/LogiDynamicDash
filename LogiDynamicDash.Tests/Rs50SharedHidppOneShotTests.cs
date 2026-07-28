using System.Text;
using LogiDynamicDash.Hidpp;
using Rs50SharedHidppOneShot;

namespace LogiDynamicDash.Tests;

public sealed class Rs50SharedHidppOneShotTests
{
    private static readonly string[] ValidArguments =
    [
        "--arm-rs50-shared-hidpp",
        "--confirm-ghub-closed",
        "--confirm-rs50-awake",
        "--confirm-dynamic-selected",
        "--confirm-usbpcap-running",
        "--confirm-one-fixed-frame"
    ];

    [Fact]
    public void MissingArguments_RejectsWithoutOpeningExchange()
    {
        bool opened = false;

        int exitCode = BuildGOneShotProgram.Run(
            [],
            () =>
            {
                opened = true;
                throw new InvalidOperationException();
            },
            TextWriter.Null,
            TextWriter.Null);

        Assert.Equal(2, exitCode);
        Assert.False(opened);
    }

    [Theory]
    [InlineData(
        "--arm-rs50-shared-hidpp",
        "--confirm-ghub-closed",
        "--confirm-rs50-awake",
        "--confirm-dynamic-selected",
        "--confirm-usbpcap-running")]
    [InlineData(
        "--confirm-ghub-closed",
        "--arm-rs50-shared-hidpp",
        "--confirm-rs50-awake",
        "--confirm-dynamic-selected",
        "--confirm-usbpcap-running",
        "--confirm-one-fixed-frame")]
    [InlineData(
        "--arm-rs50-shared-hidpp",
        "--confirm-ghub-closed",
        "--confirm-rs50-awake",
        "--confirm-dynamic-selected",
        "--confirm-usbpcap-running",
        "--confirm-one-fixed-frame",
        "--extra")]
    public void PartialReorderedOrExtraArguments_RejectsWithoutOpening(
        params string[] arguments)
    {
        bool opened = false;

        int exitCode = BuildGOneShotProgram.Run(
            arguments,
            () =>
            {
                opened = true;
                throw new InvalidOperationException();
            },
            TextWriter.Null,
            TextWriter.Null);

        Assert.Equal(2, exitCode);
        Assert.False(opened);
    }

    [Fact]
    public void ExactArguments_SendOneFixedFrameAndDispose()
    {
        var exchange = new FakeExchange();

        int exitCode = BuildGOneShotProgram.Run(
            ValidArguments,
            () => exchange,
            TextWriter.Null,
            TextWriter.Null);

        Assert.Equal(0, exitCode);
        Assert.True(exchange.Disposed);
        Assert.Equal(2, exchange.Transactions.Count);
        Assert.Equal(
            Rs50HidppDisplayTransactionKind.DiscoverDisplayFeature,
            exchange.Transactions[0].Kind);

        Rs50HidppDisplayTransaction layout =
            exchange.Transactions[1];
        Assert.Equal(
            Rs50HidppDisplayTransactionKind.SetLayoutJ,
            layout.Kind);

        byte[] report = layout.Request.ToArray();
        Assert.Equal("RS50 SHARED HIDPP", ReadText(report, 5, 19));
        Assert.Equal("BUILD G", ReadText(report, 24, 10));
        Assert.Equal("ONE SHOT ONLY", ReadText(report, 34, 19));
        Assert.Equal("USBPCAP", ReadText(report, 53, 10));
    }

    [Fact]
    public void DiscoveryFailure_FailsClosedWithoutLayoutAndDisposes()
    {
        var exchange = new FakeExchange
        {
            DiscoveryResponse = new byte[64]
        };

        int exitCode = BuildGOneShotProgram.Run(
            ValidArguments,
            () => exchange,
            TextWriter.Null,
            TextWriter.Null);

        Assert.Equal(1, exitCode);
        Assert.True(exchange.Disposed);
        Assert.Single(exchange.Transactions);
    }

    [Fact]
    public void LayoutFailure_FailsClosedAndDisposes()
    {
        var exchange = new FakeExchange
        {
            LayoutResponse = new byte[64]
        };

        int exitCode = BuildGOneShotProgram.Run(
            ValidArguments,
            () => exchange,
            TextWriter.Null,
            TextWriter.Null);

        Assert.Equal(1, exitCode);
        Assert.True(exchange.Disposed);
        Assert.Equal(2, exchange.Transactions.Count);
    }

    [Fact]
    public void OpenFailure_IsReportedAsFailClosed()
    {
        using var error = new StringWriter();

        int exitCode = BuildGOneShotProgram.Run(
            ValidArguments,
            () => throw new IOException("open rejected"),
            TextWriter.Null,
            error);

        Assert.Equal(1, exitCode);
        Assert.Contains("failed closed", error.ToString());
        Assert.Contains("open rejected", error.ToString());
    }

    private static string ReadText(
        byte[] report,
        int offset,
        int length)
    {
        ReadOnlySpan<byte> field =
            report.AsSpan(offset, length);
        int terminator = field.IndexOf((byte)0);
        if (terminator >= 0)
        {
            field = field[..terminator];
        }

        return Encoding.ASCII.GetString(field);
    }

    private sealed class FakeExchange : IRs50HidppDisplayExchange
    {
        public byte[] DiscoveryResponse { get; init; } =
            ValidDiscoveryResponse();

        public byte[] LayoutResponse { get; init; } =
            ValidLayoutResponse();

        public List<Rs50HidppDisplayTransaction> Transactions { get; } =
            [];

        public bool Disposed { get; private set; }

        public byte[] Exchange(
            Rs50HidppDisplayTransaction transaction)
        {
            Transactions.Add(transaction);
            return transaction.Kind ==
                Rs50HidppDisplayTransactionKind.DiscoverDisplayFeature
                    ? DiscoveryResponse
                    : LayoutResponse;
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
