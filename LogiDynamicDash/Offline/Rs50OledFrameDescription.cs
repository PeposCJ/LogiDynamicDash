using LogiDynamicDash.Models;

namespace LogiDynamicDash.Offline;

internal static class Rs50OledFrameDescription
{
    internal static string Describe(Rs50OledFrame frame) => frame switch
    {
        Rs50LayoutAFrame =>
            "blank",
        Rs50LayoutBFrame =>
            "firmware-test",
        Rs50LayoutCFrame value =>
            $"main={value.MainGauge.WireValue}",
        Rs50LayoutDFrame value =>
            $"main={value.MainGauge.WireValue} " +
            $"thin={value.ThinIndicator.WireValue} " +
            $"text=\"{value.Text}\"",
        Rs50LayoutEFrame value =>
            $"main={value.MainGauge.WireValue} " +
            $"thin={value.ThinIndicator.WireValue} " +
            $"left=\"{value.LeftText}\" right=\"{value.RightText}\"",
        Rs50LayoutFFrame value =>
            $"left=\"{value.LeftText}\" right=\"{value.RightText}\"",
        Rs50LayoutGFrame value =>
            $"left=\"{value.LeftText}\" right=\"{value.RightText}\"",
        Rs50LayoutHFrame value =>
            $"top=\"{value.TopText}\" bottom=\"{value.BottomText}\"",
        Rs50LayoutIFrame value =>
            DescribeFourRows(value.Line1, value.Line2, value.Line3, value.Line4),
        Rs50LayoutJFrame value =>
            DescribeFourRows(value.Line1, value.Line2, value.Line3, value.Line4),
        _ => throw new ArgumentOutOfRangeException(nameof(frame))
    };

    private static string DescribeFourRows(
        string line1,
        string line2,
        string line3,
        string line4) =>
        $"line1=\"{line1}\" line2=\"{line2}\" " +
        $"line3=\"{line3}\" line4=\"{line4}\"";
}
