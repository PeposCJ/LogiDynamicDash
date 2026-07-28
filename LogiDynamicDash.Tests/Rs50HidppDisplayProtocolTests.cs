using System.Text;
using LogiDynamicDash.Hidpp;
using LogiDynamicDash.Models;

namespace LogiDynamicDash.Tests;

public sealed class Rs50HidppDisplayProtocolTests
{
    [Fact]
    public void CreateDiscovery_UsesOnlyRootGetFeatureFor8130()
    {
        Rs50HidppDisplayTransaction transaction =
            Rs50HidppDisplayProtocol.CreateDiscovery();

        Assert.Equal(
            Rs50HidppDisplayTransactionKind.DiscoverDisplayFeature,
            transaction.Kind);
        Assert.Equal(
            [0x10, 0xFF, 0x00, 0x0A, 0x81, 0x30, 0x00],
            transaction.Request.ToArray());
    }

    [Fact]
    public void ParseDiscoveryResponse_AcceptsCapturedShape()
    {
        byte[] response = ValidDiscoveryResponse();

        byte runtime =
            Rs50HidppDisplayProtocol.ParseDiscoveryResponse(response);

        Assert.Equal(0x12, runtime);
    }

    [Theory]
    [InlineData(0, 0x11)]
    [InlineData(1, 0x01)]
    [InlineData(2, 0x01)]
    [InlineData(3, 0x0B)]
    [InlineData(5, 0x40)]
    [InlineData(6, 0x01)]
    [InlineData(7, 0x01)]
    public void ParseDiscoveryResponse_RejectsAnyUnexpectedField(
        int offset,
        byte value)
    {
        byte[] response = ValidDiscoveryResponse();
        response[offset] = value;

        Assert.Throws<Rs50HidppProtocolException>(
            () => Rs50HidppDisplayProtocol.ParseDiscoveryResponse(response));
    }

    [Fact]
    public void ParseDiscoveryResponse_RejectsWrongLength()
    {
        Assert.Throws<Rs50HidppProtocolException>(
            () => Rs50HidppDisplayProtocol.ParseDiscoveryResponse(
                new byte[63]));
    }

    [Fact]
    public void ParseDiscoveryResponse_RejectsHidppError()
    {
        byte[] response = ErrorResponse(
            expectedFeature: 0x00,
            expectedFunction: 0x0A,
            error: 0x09);

        Rs50HidppProtocolException exception =
            Assert.Throws<Rs50HidppProtocolException>(
                () =>
                    Rs50HidppDisplayProtocol.ParseDiscoveryResponse(
                        response));

        Assert.Contains("0x09", exception.Message);
    }

    [Fact]
    public void CreateLayoutJ_MatchesSuccessfulWireFrame()
    {
        LayoutJFrame frame =
            new("SPEED", "0 KMH", "GEAR", "N");

        Rs50HidppDisplayTransaction transaction =
            Rs50HidppDisplayProtocol.CreateLayoutJ(0x12, frame);
        byte[] request = transaction.Request.ToArray();

        Assert.Equal(
            Rs50HidppDisplayTransactionKind.SetLayoutJ,
            transaction.Kind);
        Assert.Equal(64, request.Length);
        Assert.Equal([0x12, 0xFF, 0x12, 0x3A, 0x09], request[..5]);
        Assert.Equal("SPEED", ReadText(request, 5, 19));
        Assert.Equal("0 KMH", ReadText(request, 24, 10));
        Assert.Equal("GEAR", ReadText(request, 34, 19));
        Assert.Equal("N", ReadText(request, 53, 10));
        Assert.Equal(0, request[63]);
    }

    [Theory]
    [InlineData(0x00)]
    [InlineData(0x01)]
    [InlineData(0xFF)]
    public void CreateLayoutJ_RejectsInvalidRuntimeIndex(byte runtime)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => Rs50HidppDisplayProtocol.CreateLayoutJ(
                runtime,
                new("", "", "", "")));
    }

    [Fact]
    public void ParseLayoutJAcknowledgement_AcceptsExactZeroBody()
    {
        Rs50HidppDisplayProtocol.ParseLayoutJAcknowledgement(
            0x12,
            ValidLayoutJAcknowledgement());
    }

    [Theory]
    [InlineData(0, 0x11)]
    [InlineData(1, 0x01)]
    [InlineData(2, 0x13)]
    [InlineData(3, 0x3B)]
    [InlineData(4, 0x01)]
    [InlineData(63, 0x01)]
    public void ParseLayoutJAcknowledgement_RejectsUnexpectedData(
        int offset,
        byte value)
    {
        byte[] response = ValidLayoutJAcknowledgement();
        response[offset] = value;

        Assert.Throws<Rs50HidppProtocolException>(
            () => Rs50HidppDisplayProtocol.ParseLayoutJAcknowledgement(
                0x12,
                response));
    }

    [Fact]
    public void ParseLayoutJAcknowledgement_RejectsHidppError()
    {
        byte[] response = ErrorResponse(
            expectedFeature: 0x12,
            expectedFunction: 0x3A,
            error: 0x08);

        Assert.Throws<Rs50HidppProtocolException>(
            () => Rs50HidppDisplayProtocol.ParseLayoutJAcknowledgement(
                0x12,
                response));
    }

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

    internal static byte[] ValidLayoutJAcknowledgement(
        byte runtime = 0x12)
    {
        byte[] response = new byte[64];
        response[0] = 0x12;
        response[1] = 0xFF;
        response[2] = runtime;
        response[3] = 0x3A;
        return response;
    }

    private static byte[] ErrorResponse(
        byte expectedFeature,
        byte expectedFunction,
        byte error)
    {
        byte[] response = new byte[64];
        response[0] = 0x12;
        response[1] = 0xFF;
        response[2] = 0xFF;
        response[3] = Rs50HidppDisplayProtocol.SoftwareId;
        response[4] = expectedFeature;
        response[5] = expectedFunction;
        response[6] = error;
        return response;
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
}
