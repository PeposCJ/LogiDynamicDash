using System.Runtime.InteropServices;
using LogiDynamicExplorer.Protocol;

namespace LogiDynamicExplorer.Tests;

public sealed class Rs50DisplayGameDataDirectInputAbiTests
{
    [Fact]
    public void DirectInputEscape32_MatchesPublicNativeLayout()
    {
        Assert.Equal(
            24,
            Marshal.SizeOf<
                Rs50DisplayGameDataDirectInputAbi.DirectInputEscape32Layout>());
        Assert.Equal(
            4,
            OffsetOf<
                Rs50DisplayGameDataDirectInputAbi.DirectInputEscape32Layout>(
                    "Command"));
        Assert.Equal(
            8,
            OffsetOf<
                Rs50DisplayGameDataDirectInputAbi.DirectInputEscape32Layout>(
                    "InputPointer"));
        Assert.Equal(
            12,
            OffsetOf<
                Rs50DisplayGameDataDirectInputAbi.DirectInputEscape32Layout>(
                    "InputSize"));
        Assert.Equal(
            16,
            OffsetOf<
                Rs50DisplayGameDataDirectInputAbi.DirectInputEscape32Layout>(
                    "OutputPointer"));
        Assert.Equal(
            20,
            OffsetOf<
                Rs50DisplayGameDataDirectInputAbi.DirectInputEscape32Layout>(
                    "OutputSize"));
    }

    [Fact]
    public void DirectInputEscape64_MatchesPublicNativeLayout()
    {
        Assert.Equal(
            40,
            Marshal.SizeOf<
                Rs50DisplayGameDataDirectInputAbi.DirectInputEscape64Layout>());
        Assert.Equal(
            4,
            OffsetOf<
                Rs50DisplayGameDataDirectInputAbi.DirectInputEscape64Layout>(
                    "Command"));
        Assert.Equal(
            8,
            OffsetOf<
                Rs50DisplayGameDataDirectInputAbi.DirectInputEscape64Layout>(
                    "InputPointer"));
        Assert.Equal(
            16,
            OffsetOf<
                Rs50DisplayGameDataDirectInputAbi.DirectInputEscape64Layout>(
                    "InputSize"));
        Assert.Equal(
            24,
            OffsetOf<
                Rs50DisplayGameDataDirectInputAbi.DirectInputEscape64Layout>(
                    "OutputPointer"));
        Assert.Equal(
            32,
            OffsetOf<
                Rs50DisplayGameDataDirectInputAbi.DirectInputEscape64Layout>(
                    "OutputSize"));
    }

    [Fact]
    public void Header_UsesRecoveredTwelveByteEnvelope()
    {
        Assert.Equal(12, Marshal.SizeOf<Rs50DisplayGameDataDirectInputAbi.Header>());
        Assert.Equal(0, OffsetOf<Rs50DisplayGameDataDirectInputAbi.Header>("Size"));
        Assert.Equal(4, OffsetOf<Rs50DisplayGameDataDirectInputAbi.Header>("Version"));
        Assert.Equal(8, OffsetOf<Rs50DisplayGameDataDirectInputAbi.Header>("Command"));
    }

    [Fact]
    public void OpaqueMsvcString_MatchesInstalledX64AbiSize()
    {
        Assert.Equal(
            32,
            Marshal.SizeOf<
                Rs50DisplayGameDataDirectInputAbi.MsvcString64Layout>());
    }

    [Fact]
    public void LayoutC_HasFloatAtOffsetTwelveAndSizeSixteen()
    {
        Assert.Equal(
            16,
            Marshal.SizeOf<Rs50DisplayGameDataDirectInputAbi.LayoutCInput>());
        Assert.Equal(
            12,
            OffsetOf<Rs50DisplayGameDataDirectInputAbi.LayoutCInput>("Value"));
    }

    [Fact]
    public void LayoutD_MatchesTwoFloatsAndPackedString()
    {
        Assert.Equal(
            52,
            Marshal.SizeOf<Rs50DisplayGameDataDirectInputAbi.LayoutDInput>());
        Assert.Equal(
            12,
            OffsetOf<Rs50DisplayGameDataDirectInputAbi.LayoutDInput>("FirstValue"));
        Assert.Equal(
            16,
            OffsetOf<Rs50DisplayGameDataDirectInputAbi.LayoutDInput>("SecondValue"));
        Assert.Equal(
            20,
            OffsetOf<Rs50DisplayGameDataDirectInputAbi.LayoutDInput>("Text"));
    }

    [Fact]
    public void LayoutE_MatchesTwoFloatsAndTwoPackedStrings()
    {
        Assert.Equal(
            84,
            Marshal.SizeOf<Rs50DisplayGameDataDirectInputAbi.LayoutEInput>());
        Assert.Equal(
            20,
            OffsetOf<Rs50DisplayGameDataDirectInputAbi.LayoutEInput>("FirstText"));
        Assert.Equal(
            52,
            OffsetOf<Rs50DisplayGameDataDirectInputAbi.LayoutEInput>("SecondText"));
    }

    [Fact]
    public void TwoTextLayouts_MatchPackedStringOffsetsAndSize()
    {
        Assert.Equal(
            76,
            Marshal.SizeOf<Rs50DisplayGameDataDirectInputAbi.TwoTextInput>());
        Assert.Equal(
            12,
            OffsetOf<Rs50DisplayGameDataDirectInputAbi.TwoTextInput>("FirstText"));
        Assert.Equal(
            44,
            OffsetOf<Rs50DisplayGameDataDirectInputAbi.TwoTextInput>("SecondText"));
    }

    [Fact]
    public void FourTextLayouts_MatchPackedStringOffsetsAndSize()
    {
        Assert.Equal(
            140,
            Marshal.SizeOf<Rs50DisplayGameDataDirectInputAbi.FourTextInput>());
        Assert.Equal(
            12,
            OffsetOf<Rs50DisplayGameDataDirectInputAbi.FourTextInput>("FirstText"));
        Assert.Equal(
            44,
            OffsetOf<Rs50DisplayGameDataDirectInputAbi.FourTextInput>("SecondText"));
        Assert.Equal(
            76,
            OffsetOf<Rs50DisplayGameDataDirectInputAbi.FourTextInput>("ThirdText"));
        Assert.Equal(
            108,
            OffsetOf<Rs50DisplayGameDataDirectInputAbi.FourTextInput>("FourthText"));
    }

    private static int OffsetOf<T>(string fieldName) =>
        checked((int)Marshal.OffsetOf<T>(fieldName));
}
