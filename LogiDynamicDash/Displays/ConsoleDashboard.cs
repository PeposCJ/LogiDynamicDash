using LogiDynamicDash.Models;

namespace LogiDynamicDash.Displays;

internal sealed class ConsoleDashboard : IDisplaySink
{
    private const int DashboardWidth = 44;

    public void Initialize()
    {
        Console.Title = "LogiDynamicDash";
        Console.CursorVisible = false;
        Console.Clear();
    }

    public void Render(LayoutJFrame frame)
    {
        ArgumentNullException.ThrowIfNull(frame);
        Console.SetCursorPosition(0, 0);

        WriteDashboardLine(
            new string('=', DashboardWidth));

        WriteCentered(
            "LOGIDYNAMICDASH OLED PREVIEW");

        WriteDashboardLine(
            new string('=', DashboardWidth));

        WriteDashboardLine();

        WriteCentered(frame.Line1);
        WriteCentered(frame.Line2);
        WriteCentered(frame.Line3);
        WriteCentered(frame.Line4);

        WriteDashboardLine(
            new string('-', DashboardWidth));

        WriteDashboardLine(
            "Press Ctrl+C to stop.");
    }

    public void Stop()
    {
        Console.CursorVisible = true;
        Console.Clear();

        Console.WriteLine(
            "Telemetry monitoring stopped.");
    }

    private static void WriteCentered(
        string text)
    {
        if (text.Length > DashboardWidth)
        {
            text =
                text[..DashboardWidth];
        }

        int leftPadding =
            Math.Max(
                0,
                (DashboardWidth - text.Length) / 2);

        WriteDashboardLine(
            new string(' ', leftPadding) + text);
    }

    private static void WriteDashboardLine(
        string text = "")
    {
        if (text.Length > DashboardWidth)
        {
            text =
                text[..DashboardWidth];
        }

        Console.WriteLine(
            text.PadRight(DashboardWidth));
    }
}
