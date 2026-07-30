namespace LogiDynamicDash.Models;

internal enum Rs50OledLayout : byte
{
    A = 0,
    B = 1,
    C = 2,
    D = 3,
    E = 4,
    F = 5,
    G = 6,
    H = 7,
    I = 8,
    J = 9
}

internal readonly record struct Rs50GaugeLevel
{
    private Rs50GaugeLevel(byte wireValue)
    {
        WireValue = wireValue;
    }

    internal byte WireValue { get; }

    internal static Rs50GaugeLevel FromRatio(double ratio)
    {
        if (!double.IsFinite(ratio))
        {
            throw new ArgumentOutOfRangeException(
                nameof(ratio),
                "A gauge ratio must be finite.");
        }

        double clamped = Math.Clamp(ratio, 0d, 1d);
        byte wireValue = checked((byte)Math.Round(
            clamped * byte.MaxValue,
            MidpointRounding.AwayFromZero));
        return new Rs50GaugeLevel(wireValue);
    }
}

internal abstract record Rs50OledFrame(Rs50OledLayout Layout);

internal sealed record Rs50LayoutAFrame()
    : Rs50OledFrame(Rs50OledLayout.A);

internal sealed record Rs50LayoutBFrame()
    : Rs50OledFrame(Rs50OledLayout.B);

internal sealed record Rs50LayoutCFrame(Rs50GaugeLevel MainGauge)
    : Rs50OledFrame(Rs50OledLayout.C);

internal sealed record Rs50LayoutDFrame(
    Rs50GaugeLevel MainGauge,
    Rs50GaugeLevel ThinIndicator,
    string Text)
    : Rs50OledFrame(Rs50OledLayout.D);

internal sealed record Rs50LayoutEFrame(
    Rs50GaugeLevel MainGauge,
    Rs50GaugeLevel ThinIndicator,
    string LeftText,
    string RightText)
    : Rs50OledFrame(Rs50OledLayout.E);

internal sealed record Rs50LayoutFFrame(string LeftText, string RightText)
    : Rs50OledFrame(Rs50OledLayout.F);

internal sealed record Rs50LayoutGFrame(string LeftText, string RightText)
    : Rs50OledFrame(Rs50OledLayout.G);

internal sealed record Rs50LayoutHFrame(string TopText, string BottomText)
    : Rs50OledFrame(Rs50OledLayout.H);

internal sealed record Rs50LayoutIFrame(
    string Line1,
    string Line2,
    string Line3,
    string Line4)
    : Rs50OledFrame(Rs50OledLayout.I);

internal sealed record Rs50LayoutJFrame(
    string Line1,
    string Line2,
    string Line3,
    string Line4)
    : Rs50OledFrame(Rs50OledLayout.J);
