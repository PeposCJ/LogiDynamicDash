using System.Text;
using LogiDynamicDash.Hidpp;
using Rs50SharedHidppLayoutGallery;

namespace LogiDynamicDash.Tests;

public sealed class Rs50SharedHidppLayoutGalleryTests
{
    private static readonly string[] ValidArguments =
    [
        "--arm-rs50-shared-hidpp-layout-gallery",
        "--confirm-ghub-closed",
        "--confirm-iracing-closed",
        "--confirm-rs50-awake",
        "--confirm-dynamic-selected",
        "--confirm-usbpcap-running",
        "--confirm-video-recording",
        "--confirm-ten-layouts-three-seconds-each"
    ];

    [Fact]
    public void ExactFactories_EncodeRecoveredAThroughJShapes()
    {
        Rs50HidppDisplayTransaction[] layouts =
        [
            Rs50HidppDisplayProtocol.CreateLayoutA(0x12),
            Rs50HidppDisplayProtocol.CreateLayoutB(0x12),
            Rs50HidppDisplayProtocol.CreateLayoutC(0x12, 128),
            Rs50HidppDisplayProtocol.CreateLayoutD(
                0x12,
                64,
                191,
                "LAYOUT D"),
            Rs50HidppDisplayProtocol.CreateLayoutE(
                0x12,
                mainGaugeValue: 64,
                thinIndicatorValue: 191,
                rightText: "E1",
                leftText: "LAYOUTE"),
            Rs50HidppDisplayProtocol.CreateLayoutF(
                0x12,
                "F",
                "123"),
            Rs50HidppDisplayProtocol.CreateLayoutG(
                0x12,
                "G",
                "456"),
            Rs50HidppDisplayProtocol.CreateLayoutH(
                0x12,
                "LAYOUT H WIDE TEST",
                "H SECOND"),
            Rs50HidppDisplayProtocol.CreateLayoutI(
                0x12,
                "LAYOUT I TOP",
                "I SECOND",
                "LAYOUT I LOWER",
                "I FOURTH"),
            Rs50HidppDisplayProtocol.CreateLayoutJ(
                0x12,
                "LAYOUT J TOP",
                "J SECOND",
                "LAYOUT J LOWER",
                "J FOURTH")
        ];

        Assert.Equal(10, layouts.Length);
        for (int index = 0; index < layouts.Length; index++)
        {
            byte[] request = layouts[index].Request.ToArray();
            Assert.Equal(64, request.Length);
            Assert.Equal(
                [0x12, 0xFF, 0x12, 0x3A, (byte)index],
                request[..5]);
            Assert.Equal(
                (Rs50HidppDisplayTransactionKind)(index + 1),
                layouts[index].Kind);
        }

        Assert.All(layouts[0].Request.Span[5..].ToArray(), AssertZero);
        Assert.All(layouts[1].Request.Span[5..].ToArray(), AssertZero);
        Assert.Equal(128, layouts[2].Request.Span[5]);

        Assert.Equal(64, layouts[3].Request.Span[5]);
        Assert.Equal(191, layouts[3].Request.Span[6]);
        Assert.Equal("LAYOUT D", ReadText(layouts[3], 7, 11));

        Assert.Equal("E1", ReadText(layouts[4], 7, 3));
        Assert.Equal("LAYOUTE", ReadText(layouts[4], 10, 7));
        Assert.Equal("F", ReadText(layouts[5], 5, 1));
        Assert.Equal("123", ReadText(layouts[5], 6, 3));
        Assert.Equal("G", ReadText(layouts[6], 5, 1));
        Assert.Equal("456", ReadText(layouts[6], 6, 3));
        Assert.Equal(
            "LAYOUT H WIDE TEST",
            ReadText(layouts[7], 5, 21));
        Assert.Equal("H SECOND", ReadText(layouts[7], 26, 10));
        Assert.Equal("LAYOUT I TOP", ReadText(layouts[8], 5, 19));
        Assert.Equal("I SECOND", ReadText(layouts[8], 24, 10));
        Assert.Equal("LAYOUT I LOWER", ReadText(layouts[8], 34, 19));
        Assert.Equal("I FOURTH", ReadText(layouts[8], 53, 10));
        Assert.Equal("LAYOUT J TOP", ReadText(layouts[9], 5, 19));
        Assert.Equal("J SECOND", ReadText(layouts[9], 24, 10));
        Assert.Equal("LAYOUT J LOWER", ReadText(layouts[9], 34, 19));
        Assert.Equal("J FOURTH", ReadText(layouts[9], 53, 10));
    }

    [Fact]
    public void LayoutFactories_RejectOversizedOrInvalidText()
    {
        Assert.Throws<ArgumentException>(
            () => Rs50HidppDisplayProtocol.CreateLayoutD(
                0x12,
                0,
                0,
                new string('D', 12)));
        Assert.Throws<ArgumentException>(
            () => Rs50HidppDisplayProtocol.CreateLayoutE(
                0x12,
                0,
                0,
                "1234",
                ""));
        Assert.Throws<ArgumentException>(
            () => Rs50HidppDisplayProtocol.CreateLayoutF(
                0x12,
                "FF",
                ""));
        Assert.Throws<ArgumentException>(
            () => Rs50HidppDisplayProtocol.CreateLayoutH(
                0x12,
                "",
                "\u0080"));
        Assert.Throws<ArgumentException>(
            () => Rs50HidppDisplayProtocol.CreateLayoutI(
                0x12,
                "",
                "",
                "",
                new string('I', 11)));
    }

    [Fact]
    public void InvalidArguments_RejectBeforeOpenOrDelay()
    {
        bool opened = false;
        var delay = new RecordingDelay();

        int exitCode = BuildKLayoutGalleryProgram.Run(
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

    [Fact]
    public void ExactArguments_SendAThroughJWithTenDelays()
    {
        var exchange = new RecordingExchange();
        var delay = new RecordingDelay();
        using var output = new StringWriter();

        int exitCode = BuildKLayoutGalleryProgram.Run(
            ValidArguments,
            () => exchange,
            delay,
            output,
            TextWriter.Null);

        Assert.Equal(0, exitCode);
        Assert.True(exchange.Disposed);
        Assert.Equal(10, delay.WaitCount);
        Assert.Equal(11, exchange.Transactions.Count);
        Assert.Equal(
            Rs50HidppDisplayTransactionKind.DiscoverDisplayFeature,
            exchange.Transactions[0].Kind);

        for (int index = 0; index < 10; index++)
        {
            Assert.Equal(
                (Rs50HidppDisplayTransactionKind)(index + 1),
                exchange.Transactions[index + 1].Kind);
            Assert.Contains(
                $"showing Layout {(char)('A' + index)}",
                output.ToString());
        }
    }

    [Fact]
    public void LayoutEFailure_StopsAndDisposes()
    {
        var exchange = new RecordingExchange
        {
            FailingKind = Rs50HidppDisplayTransactionKind.SetLayoutE
        };
        var delay = new RecordingDelay();

        int exitCode = BuildKLayoutGalleryProgram.Run(
            ValidArguments,
            () => exchange,
            delay,
            TextWriter.Null,
            TextWriter.Null);

        Assert.Equal(1, exitCode);
        Assert.True(exchange.Disposed);
        Assert.Equal(6, exchange.Transactions.Count);
        Assert.Equal(4, delay.WaitCount);
    }

    private static void AssertZero(byte value) =>
        Assert.Equal(0, value);

    private static string ReadText(
        Rs50HidppDisplayTransaction transaction,
        int offset,
        int length)
    {
        ReadOnlySpan<byte> field =
            transaction.Request.Span.Slice(offset, length);
        int terminator = field.IndexOf((byte)0);
        if (terminator >= 0)
        {
            field = field[..terminator];
        }

        return Encoding.ASCII.GetString(field);
    }

    private sealed class RecordingDelay : IBuildKDelay
    {
        public int WaitCount { get; private set; }

        public void WaitThreeSeconds() =>
            WaitCount++;
    }

    private sealed class RecordingExchange : IRs50HidppDisplayExchange
    {
        public Rs50HidppDisplayTransactionKind? FailingKind { get; init; }

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

            if (transaction.Kind == FailingKind)
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
}
