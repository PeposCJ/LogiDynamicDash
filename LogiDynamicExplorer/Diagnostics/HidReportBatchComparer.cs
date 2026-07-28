using LogiDynamicExplorer.Decoders;

namespace LogiDynamicExplorer.Diagnostics;

internal static class HidReportBatchComparer
{
    public static IReadOnlyList<string> Compare(
        IEnumerable<string> baselineLines,
        IEnumerable<string> candidateLines)
    {
        ParsedBatch baseline = Parse(baselineLines);
        ParsedBatch candidate = Parse(candidateLines);

        List<string> output =
        [
            "Offline HID report batch comparison (no HID access):",
            $"Baseline HID++ reports: {baseline.Reports.Count}",
            $"Candidate HID++ reports: {candidate.Reports.Count}",
            $"Baseline invalid lines: {baseline.InvalidLineCount}",
            $"Candidate invalid lines: {candidate.InvalidLineCount}",
            $"Baseline non-HID++ reports ignored: {baseline.IgnoredReportCount}",
            $"Candidate non-HID++ reports ignored: {candidate.IgnoredReportCount}",
            "Positive exact-report count deltas:"
        ];

        ReportDelta[] positiveDeltas = CalculateDeltas(
                baseline.Reports,
                candidate.Reports)
            .Where(delta => delta.CountDelta > 0)
            .OrderBy(delta => delta.Signature.Direction)
            .ThenBy(delta => delta.Signature.ReportId)
            .ThenBy(delta => delta.Signature.DeviceIndex)
            .ThenBy(delta => delta.Signature.FeatureIndex)
            .ThenBy(delta => delta.Signature.FunctionAndSoftwareId)
            .ThenBy(delta => delta.Signature.Parameters, StringComparer.Ordinal)
            .ToArray();

        if (positiveDeltas.Length == 0)
        {
            output.Add("  (none)");
        }
        else
        {
            foreach (ReportDelta delta in positiveDeltas)
            {
                byte functionId =
                    (byte)(delta.Signature.FunctionAndSoftwareId >> 4);
                byte softwareId =
                    (byte)(delta.Signature.FunctionAndSoftwareId & 0x0F);

                output.Add(
                    $"  +{delta.CountDelta} {delta.Signature.Direction} " +
                    $"ID 0x{delta.Signature.ReportId:X2}, " +
                    $"device 0x{delta.Signature.DeviceIndex:X2}, " +
                    $"feature 0x{delta.Signature.FeatureIndex:X2}, " +
                    $"function 0x{functionId:X2}, SW-ID 0x{softwareId:X2}, " +
                    $"parameters {FormatParameters(delta.Signature.Parameters)}");
            }
        }

        output.Add("Comparison boundary:");
        output.Add(
            "  Positive deltas are differences, not proof of causation.");
        output.Add(
            "  Background traffic and unequal capture durations can also differ.");

        return output;
    }

    private static ParsedBatch Parse(IEnumerable<string> lines)
    {
        List<ReportSignature> reports = [];
        int invalidLineCount = 0;
        int ignoredReportCount = 0;

        foreach (string line in lines)
        {
            string trimmed = line.Trim();
            if (trimmed.Length == 0 || trimmed.StartsWith('#'))
            {
                continue;
            }

            string[] fields = trimmed.Split('\t');
            if (fields.Length < 2)
            {
                invalidLineCount++;
                continue;
            }

            string direction = fields[0].Trim().ToUpperInvariant();
            if (direction is not ("HOST" or "DEVICE") ||
                !HexReportParser.TryParse(
                    fields[^1],
                    out byte[] bytes,
                    out _))
            {
                invalidLineCount++;
                continue;
            }

            if (bytes.Length < 4 ||
                bytes[0] is not (0x10 or 0x11 or 0x12))
            {
                ignoredReportCount++;
                continue;
            }

            reports.Add(new ReportSignature(
                direction,
                bytes[0],
                bytes[1],
                bytes[2],
                bytes[3],
                Convert.ToHexString(bytes.AsSpan(4))));
        }

        return new ParsedBatch(
            reports,
            invalidLineCount,
            ignoredReportCount);
    }

    private static IEnumerable<ReportDelta> CalculateDeltas(
        IEnumerable<ReportSignature> baseline,
        IEnumerable<ReportSignature> candidate)
    {
        Dictionary<ReportSignature, int> baselineCounts = baseline
            .GroupBy(signature => signature)
            .ToDictionary(group => group.Key, group => group.Count());
        Dictionary<ReportSignature, int> candidateCounts = candidate
            .GroupBy(signature => signature)
            .ToDictionary(group => group.Key, group => group.Count());

        foreach ((ReportSignature signature, int candidateCount) in
                 candidateCounts)
        {
            baselineCounts.TryGetValue(signature, out int baselineCount);
            yield return new ReportDelta(
                signature,
                candidateCount - baselineCount);
        }
    }

    private static string FormatParameters(string parameters)
    {
        const int MaximumDisplayedHexCharacters = 64;

        if (parameters.Length == 0)
        {
            return "(none)";
        }

        if (parameters.Length <= MaximumDisplayedHexCharacters)
        {
            return parameters;
        }

        return parameters[..MaximumDisplayedHexCharacters] +
               $"... ({parameters.Length / 2} bytes)";
    }

    private sealed record ParsedBatch(
        IReadOnlyList<ReportSignature> Reports,
        int InvalidLineCount,
        int IgnoredReportCount);

    private sealed record ReportSignature(
        string Direction,
        byte ReportId,
        byte DeviceIndex,
        byte FeatureIndex,
        byte FunctionAndSoftwareId,
        string Parameters);

    private sealed record ReportDelta(
        ReportSignature Signature,
        int CountDelta);
}
