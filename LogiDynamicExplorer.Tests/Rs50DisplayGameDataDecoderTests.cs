using LogiDynamicExplorer.Decoders;

namespace LogiDynamicExplorer.Tests;

public sealed class Rs50DisplayGameDataDecoderTests
{
    [Fact]
    public void DecodeDeviceResponse_DescribesFirmwareLayoutDescriptor()
    {
        string result = Rs50DisplayGameDataDecoder.DecodeDeviceResponse(
            0x01,
            [0x08, 0x09, 0x13, 0x0A, 0x13, 0x0A]);

        Assert.Equal(
            "layout I, index 8, ID 9, capabilities 13 0A 13 0A",
            result);
    }

    [Fact]
    public void DecodeDeviceResponse_FlagsInconsistentIndexAndId()
    {
        string result = Rs50DisplayGameDataDecoder.DecodeDeviceResponse(
            0x01,
            [0x08, 0x0A, 0x13, 0x0A, 0x13, 0x0A]);

        Assert.EndsWith(" (inconsistent index/ID)", result);
    }

    [Fact]
    public void DecodeHostCommand_DecodesFourTextLayoutAndPadding()
    {
        byte[] parameters = new byte[60];
        parameters[0] = 8;
        "SPEED"u8.CopyTo(parameters.AsSpan(1));
        "KMH"u8.CopyTo(parameters.AsSpan(20));
        "GEAR"u8.CopyTo(parameters.AsSpan(30));
        "3"u8.CopyTo(parameters.AsSpan(49));

        string result = Rs50DisplayGameDataDecoder.DecodeHostCommand(
            0x03,
            parameters);

        Assert.Equal(
            "set layout I: texts \"SPEED\", \"KMH\", \"GEAR\", \"3\"",
            result);
    }

    [Fact]
    public void DecodeHostCommand_IncompleteLayoutDoesNotThrow()
    {
        string result = Rs50DisplayGameDataDecoder.DecodeHostCommand(
            0x03,
            [0x04, 0x01]);

        Assert.Equal(
            "set layout E: values 1/255 (0.4%) (incomplete 1/2), " +
            "texts (missing), (missing)",
            result);
    }

    [Fact]
    public void DecodeHostCommand_InterpretsRecoveredNormalizedValue()
    {
        string result = Rs50DisplayGameDataDecoder.DecodeHostCommand(
            0x03,
            [0x02, 0x80]);

        Assert.Equal("set layout C: value 128/255 (50.2%)", result);
    }
}
