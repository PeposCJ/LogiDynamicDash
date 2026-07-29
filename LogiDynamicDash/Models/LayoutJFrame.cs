namespace LogiDynamicDash.Models;

/// <summary>
/// Four presentation lines that fit the recovered RS50 Layout J fields.
/// This is telemetry presentation data only and has no device transport.
/// </summary>
internal sealed record LayoutJFrame
{
    public const int Line1MaximumLength = 19;
    public const int Line2MaximumLength = 10;
    public const int Line3MaximumLength = 19;
    public const int Line4MaximumLength = 10;

    public LayoutJFrame(
        string line1,
        string line2,
        string line3,
        string line4)
    {
        Line1 = Validate(line1, Line1MaximumLength, nameof(line1));
        Line2 = Validate(line2, Line2MaximumLength, nameof(line2));
        Line3 = Validate(line3, Line3MaximumLength, nameof(line3));
        Line4 = Validate(line4, Line4MaximumLength, nameof(line4));
    }

    public string Line1 { get; }

    public string Line2 { get; }

    public string Line3 { get; }

    public string Line4 { get; }

    private static string Validate(
        string line,
        int maximumLength,
        string parameterName)
    {
        ArgumentNullException.ThrowIfNull(line, parameterName);

        if (line.Length > maximumLength)
        {
            throw new ArgumentException(
                $"Text exceeds the Layout J limit of {maximumLength} " +
                "characters.",
                parameterName);
        }

        if (line.Any(character => character is < (char)0x20 or > (char)0x7F))
        {
            throw new ArgumentException(
                "Text contains a character outside the firmware's " +
                "recovered display range.",
                parameterName);
        }

        return line;
    }
}
