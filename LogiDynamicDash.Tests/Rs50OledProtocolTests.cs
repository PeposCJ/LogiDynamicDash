using System.Text;
using LogiDynamicDash.Hidpp;
using LogiDynamicDash.Models;

namespace LogiDynamicDash.Tests;

public sealed class Rs50OledProtocolTests
{
    [Fact]
    public void Discovery_UsesOnlyRootGetFeatureForDisplayGameData()
    {
        Rs50OledTransaction transaction =
            Rs50OledProtocol.CreateDiscovery();

        Assert.Equal(
            Rs50OledTransactionKind.DiscoverDisplayFeature,
            transaction.Kind);
        Assert.Equal(
            [0x10, 0xFF, 0x00, 0x0A, 0x81, 0x30, 0x00],
            transaction.Request.ToArray());
    }

    [Fact]
    public void DiscoveryResponse_ReturnsCapturedRuntimeIndex()
    {
        byte[] response = Response(featureIndex: 0, function: 0x0A);
        response[4] = 0x12;

        Assert.Equal(
            0x12,
            Rs50OledProtocol.ParseDiscoveryResponse(response));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(255)]
    public void DiscoveryResponse_RejectsInvalidRuntimeIndex(byte value)
    {
        byte[] response = Response(featureIndex: 0, function: 0x0A);
        response[4] = value;

        Assert.Throws<Rs50OledProtocolException>(
            () => Rs50OledProtocol.ParseDiscoveryResponse(response));
    }

    [Fact]
    public void GaugeLevel_ClampsAndRoundsToWireByte()
    {
        Assert.Equal(0, Rs50GaugeLevel.FromRatio(-1).WireValue);
        Assert.Equal(128, Rs50GaugeLevel.FromRatio(0.5).WireValue);
        Assert.Equal(255, Rs50GaugeLevel.FromRatio(2).WireValue);
        Assert.Throws<ArgumentOutOfRangeException>(
            () => Rs50GaugeLevel.FromRatio(double.NaN));
    }

    [Fact]
    public void LayoutsAThroughJ_UseOnlyConfirmedLayoutIndices()
    {
        Rs50GaugeLevel low = Rs50GaugeLevel.FromRatio(0.25);
        Rs50GaugeLevel high = Rs50GaugeLevel.FromRatio(0.75);
        Rs50OledFrame[] frames =
        [
            new Rs50LayoutAFrame(),
            new Rs50LayoutBFrame(),
            new Rs50LayoutCFrame(low),
            new Rs50LayoutDFrame(low, high, "LAYOUT D"),
            new Rs50LayoutEFrame(low, high, "LAYOUTE", "E1"),
            new Rs50LayoutFFrame("F", "123"),
            new Rs50LayoutGFrame("G", "456"),
            new Rs50LayoutHFrame("LAYOUT H", "SECOND"),
            new Rs50LayoutIFrame("I1", "I2", "I3", "I4"),
            new Rs50LayoutJFrame("J1", "J2", "J3", "J4")
        ];

        for (int index = 0; index < frames.Length; index++)
        {
            byte[] request =
                Rs50OledProtocol.CreateLayout(0x12, frames[index])
                    .Request
                    .ToArray();

            Assert.Equal(64, request.Length);
            Assert.Equal([0x12, 0xFF, 0x12, 0x3A], request[..4]);
            Assert.Equal(index, request[4]);
        }
    }

    [Fact]
    public void LayoutE_EncodesVisualRightFieldBeforeVisualLeftField()
    {
        Rs50LayoutEFrame frame = new(
            Rs50GaugeLevel.FromRatio(64d / 255d),
            Rs50GaugeLevel.FromRatio(191d / 255d),
            LeftText: "123 KMH",
            RightText: "N");

        byte[] request =
            Rs50OledProtocol.CreateLayout(0x12, frame).Request.ToArray();

        Assert.Equal(64, request[5]);
        Assert.Equal(191, request[6]);
        Assert.Equal("N", ReadText(request, 7, 3));
        Assert.Equal("123 KMH", ReadText(request, 10, 7));
    }

    [Fact]
    public void LayoutJ_MatchesAcceptedStationaryTelemetryFrame()
    {
        Rs50LayoutJFrame frame =
            new("SPEED", "0 KMH", "GEAR", "N");

        Rs50OledTransaction transaction =
            Rs50OledProtocol.CreateLayout(0x12, frame);
        byte[] request = transaction.Request.ToArray();

        Assert.Equal(
            Rs50OledTransactionKind.SetLayoutJ,
            transaction.Kind);
        Assert.Equal("SPEED", ReadText(request, 5, 19));
        Assert.Equal("0 KMH", ReadText(request, 24, 10));
        Assert.Equal("GEAR", ReadText(request, 34, 19));
        Assert.Equal("N", ReadText(request, 53, 10));
        Assert.Equal(0, request[63]);
    }

    [Fact]
    public void TextFields_RejectOverflowAndUnsupportedCharacters()
    {
        Assert.Throws<ArgumentException>(
            () => Rs50OledProtocol.CreateLayout(
                0x12,
                new Rs50LayoutFFrame("AB", "123")));
        Assert.Throws<ArgumentException>(
            () => Rs50OledProtocol.CreateLayout(
                0x12,
                new Rs50LayoutHFrame(new string('A', 22), "")));
        Assert.Throws<ArgumentException>(
            () => Rs50OledProtocol.CreateLayout(
                0x12,
                new Rs50LayoutJFrame("", "", "", "KM/H \u00E9")));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(255)]
    public void Layout_RejectsRuntimeIndexThatWasNotDiscoverable(byte value)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => Rs50OledProtocol.CreateLayout(
                value,
                new Rs50LayoutAFrame()));
    }

    [Fact]
    public void LayoutAcknowledgement_RequiresExactHeaderAndZeroBody()
    {
        Rs50OledTransaction transaction =
            Rs50OledProtocol.CreateLayout(
                0x12,
                new Rs50LayoutJFrame("SPEED", "0 KMH", "GEAR", "N"));
        byte[] valid = Response(featureIndex: 0x12, function: 0x3A);

        Rs50OledProtocol.ParseLayoutAcknowledgement(transaction, valid);

        valid[63] = 1;
        Assert.Throws<Rs50OledProtocolException>(
            () => Rs50OledProtocol.ParseLayoutAcknowledgement(
                transaction,
                valid));
    }

    [Fact]
    public void LayoutAcknowledgement_RejectsHidppError()
    {
        Rs50OledTransaction transaction =
            Rs50OledProtocol.CreateLayout(
                0x12,
                new Rs50LayoutAFrame());
        byte[] error = Response(featureIndex: 0xFF, function: 0x0A);
        error[4] = 0x12;
        error[5] = 0x3A;
        error[6] = 0x08;

        Assert.Throws<Rs50OledProtocolException>(
            () => Rs50OledProtocol.ParseLayoutAcknowledgement(
                transaction,
                error));
    }

    [Fact]
    public void TransactionRequest_DoesNotExposeMutableStorage()
    {
        Rs50OledTransaction transaction =
            Rs50OledProtocol.CreateLayout(
                0x12,
                new Rs50LayoutAFrame());

        byte[] first = transaction.Request.ToArray();
        first[2] = 0x99;

        Assert.Equal(0x12, transaction.Request.Span[2]);
    }

    private static byte[] Response(byte featureIndex, byte function)
    {
        byte[] response = new byte[64];
        response[0] = 0x12;
        response[1] = 0xFF;
        response[2] = featureIndex;
        response[3] = function;
        return response;
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
}
