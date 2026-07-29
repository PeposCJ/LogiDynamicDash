using LogiDynamicExplorer.Protocol;

namespace LogiDynamicExplorer.Tests;

public sealed class Rs50DisplayGameDataPayloadEncoderTests
{
    [Fact]
    public void EncodeDataFreeLayouts_ReturnOnlyTheirLayoutIndex()
    {
        Assert.Equal([0], Rs50DisplayGameDataPayloadEncoder.EncodeLayoutA());
        Assert.Equal([1], Rs50DisplayGameDataPayloadEncoder.EncodeLayoutB());
    }

    [Theory]
    [InlineData(-1.0f, 0)]
    [InlineData(0.0f, 0)]
    [InlineData(0.5f, 128)]
    [InlineData(1.0f, 255)]
    [InlineData(2.0f, 255)]
    public void EncodeLayoutC_UsesRecoveredNormalizedConversion(
        float value,
        byte expected)
    {
        Assert.Equal(
            [2, expected],
            Rs50DisplayGameDataPayloadEncoder.EncodeLayoutC(value));
    }

    [Theory]
    [InlineData(float.NaN)]
    [InlineData(float.PositiveInfinity)]
    [InlineData(float.NegativeInfinity)]
    public void EncodeLayoutC_RejectsNonFiniteValues(float value)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => Rs50DisplayGameDataPayloadEncoder.EncodeLayoutC(value));
    }

    [Fact]
    public void EncodeLayoutD_UsesFixedFieldsAndFirmwareTextSanitization()
    {
        byte[] result = Rs50DisplayGameDataPayloadEncoder.EncodeLayoutD(
            0.25f,
            0.75f,
            "abé");

        Assert.Equal(14, result.Length);
        Assert.Equal(3, result[0]);
        Assert.Equal(64, result[1]);
        Assert.Equal(191, result[2]);
        Assert.Equal("AB?"u8.ToArray(), result[3..6]);
        Assert.All(result[6..], value => Assert.Equal(0, value));
    }

    [Fact]
    public void EncodeLayoutE_EncodesRightFieldBeforeLeftFieldOnWire()
    {
        byte[] result = Rs50DisplayGameDataPayloadEncoder.EncodeLayoutE(
            mainGaugeValue: 0.0f,
            thinIndicatorValue: 1.0f,
            rightText: "N",
            leftText: "speed");

        Assert.Equal(13, result.Length);
        Assert.Equal([4, 0, 255], result[..3]);
        Assert.Equal([.. "N"u8, 0, 0], result[3..6]);
        Assert.Equal([.. "SPEED"u8, 0, 0], result[6..13]);
    }

    [Theory]
    [InlineData('F', 5, 5)]
    [InlineData('G', 6, 5)]
    [InlineData('H', 7, 32)]
    public void EncodeTwoTextLayouts_UseRecoveredSizes(
        char layout,
        byte expectedIndex,
        int expectedLength)
    {
        byte[] result = layout switch
        {
            'F' => Rs50DisplayGameDataPayloadEncoder.EncodeLayoutF("N", "123"),
            'G' => Rs50DisplayGameDataPayloadEncoder.EncodeLayoutG("R", "456"),
            'H' => Rs50DisplayGameDataPayloadEncoder.EncodeLayoutH("TITLE", "VALUE"),
            _ => throw new InvalidOperationException()
        };

        Assert.Equal(expectedLength, result.Length);
        Assert.Equal(expectedIndex, result[0]);
    }

    [Fact]
    public void EncodeLayoutI_UsesMaximum59BytePayloadAndFourSlots()
    {
        byte[] result = Rs50DisplayGameDataPayloadEncoder.EncodeLayoutI(
            "speed",
            "kmh",
            "gear",
            "3");

        Assert.Equal(59, result.Length);
        Assert.Equal(8, result[0]);
        Assert.Equal("SPEED"u8.ToArray(), result[1..6]);
        Assert.Equal("KMH"u8.ToArray(), result[20..23]);
        Assert.Equal("GEAR"u8.ToArray(), result[30..34]);
        Assert.Equal((byte)'3', result[49]);
    }

    [Fact]
    public void EncodeLayoutJ_AllowsExactMaximumFieldLengths()
    {
        byte[] result = Rs50DisplayGameDataPayloadEncoder.EncodeLayoutJ(
            new string('A', 19),
            new string('B', 10),
            new string('C', 19),
            new string('D', 10));

        Assert.Equal(59, result.Length);
        Assert.Equal(9, result[0]);
        Assert.All(result[1..20], value => Assert.Equal((byte)'A', value));
        Assert.All(result[20..30], value => Assert.Equal((byte)'B', value));
        Assert.All(result[30..49], value => Assert.Equal((byte)'C', value));
        Assert.All(result[49..59], value => Assert.Equal((byte)'D', value));
    }

    [Fact]
    public void EncodeText_RejectsOversizeAndEmbeddedNul()
    {
        Assert.Throws<ArgumentException>(
            () => Rs50DisplayGameDataPayloadEncoder.EncodeLayoutF("NN", "123"));
        Assert.Throws<ArgumentException>(
            () => Rs50DisplayGameDataPayloadEncoder.EncodeLayoutH("A\0B", "OK"));
    }
}
