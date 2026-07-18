namespace LogiDynamicExplorer.Diagnostics;

internal static class HexReportParser
{
    private static readonly char[] Separators =
        [' ', '\t', '-', ':', ','];

    public static bool TryParse(
        string text,
        out byte[] report,
        out string? error)
    {
        ArgumentNullException.ThrowIfNull(text);

        string compact =
            string.Concat(text.Split(
                Separators,
                StringSplitOptions.RemoveEmptyEntries));

        if (compact.Length == 0)
        {
            report = [];
            error = "The report is empty.";
            return false;
        }

        if (compact.Length % 2 != 0)
        {
            report = [];
            error = "The hexadecimal report must contain complete bytes.";
            return false;
        }

        report = new byte[compact.Length / 2];

        for (int index = 0; index < report.Length; index++)
        {
            if (!byte.TryParse(
                    compact.AsSpan(index * 2, 2),
                    System.Globalization.NumberStyles.HexNumber,
                    provider: null,
                    out report[index]))
            {
                report = [];
                error = "The report contains a non-hexadecimal value.";
                return false;
            }
        }

        error = null;
        return true;
    }
}
