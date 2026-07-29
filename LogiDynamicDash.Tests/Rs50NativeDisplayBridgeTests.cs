using System.Runtime.InteropServices;
using System.Text;
using LogiDynamicDash.Models;
using LogiDynamicDash.Native;

namespace LogiDynamicDash.Tests;

public sealed class Rs50NativeDisplayBridgeTests
{
    [Fact]
    public void NativeLayoutJFrame_MatchesAuditedAbi()
    {
        Assert.Equal(
            68,
            Marshal.SizeOf<
                Rs50NativeDisplayBridge.NativeLayoutJFrame>());
        Assert.Equal(
            8,
            OffsetOf<
                Rs50NativeDisplayBridge.NativeLayoutJFrame>("Row1"));
        Assert.Equal(
            27,
            OffsetOf<
                Rs50NativeDisplayBridge.NativeLayoutJFrame>("Row2"));
        Assert.Equal(
            37,
            OffsetOf<
                Rs50NativeDisplayBridge.NativeLayoutJFrame>("Row3"));
        Assert.Equal(
            56,
            OffsetOf<
                Rs50NativeDisplayBridge.NativeLayoutJFrame>("Row4"));
    }

    [Fact]
    public void NativeStreamResult_MatchesAuditedAbi()
    {
        Assert.Equal(
            560,
            Marshal.SizeOf<
                Rs50NativeDisplayBridge.NativeStreamResult>());
        Assert.Equal(
            40,
            OffsetOf<
                Rs50NativeDisplayBridge.NativeStreamResult>("ProductName"));
    }

    [Fact]
    public void From_EncodesVisualRowsWithoutApplyingDriverPermutation()
    {
        LayoutJFrame frame =
            new("SPEED", "123 KMH", "GEAR", "4");

        Rs50NativeDisplayBridge.NativeLayoutJFrame native =
            Rs50NativeDisplayBridge.NativeLayoutJFrame.From(frame);

        Assert.Equal(68u, native.StructSize);
        Assert.Equal(5, native.Row1Length);
        Assert.Equal(7, native.Row2Length);
        Assert.Equal(4, native.Row3Length);
        Assert.Equal(1, native.Row4Length);
        Assert.Equal("SPEED", Decode(native.Row1, native.Row1Length));
        Assert.Equal("123 KMH", Decode(native.Row2, native.Row2Length));
        Assert.Equal("GEAR", Decode(native.Row3, native.Row3Length));
        Assert.Equal("4", Decode(native.Row4, native.Row4Length));
        Assert.All(
            native.Row1[native.Row1Length..],
            value => Assert.Equal(0, value));
        Assert.All(native.Reserved, value => Assert.Equal(0, value));
    }

    private static int OffsetOf<T>(string fieldName) =>
        checked((int)Marshal.OffsetOf<T>(fieldName));

    private static string Decode(byte[] value, int length) =>
        Encoding.ASCII.GetString(value, 0, length);
}
