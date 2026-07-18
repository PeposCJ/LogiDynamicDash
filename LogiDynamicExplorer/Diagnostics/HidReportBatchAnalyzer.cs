namespace LogiDynamicExplorer.Diagnostics;

internal static class HidReportBatchAnalyzer
{
    public static IReadOnlyList<string> Analyze(IEnumerable<string> lines)
    {
        List<BatchReport> reports = [];
        List<string> errors = [];
        int lineNumber = 0;

        foreach (string line in lines)
        {
            lineNumber++;

            if (!TryParseLine(line, out BatchReport? report, out string? error))
            {
                if (error is not null)
                {
                    errors.Add($"line {lineNumber}: {error}");
                }

                continue;
            }

            reports.Add(report!);
        }

        int hostCount = reports.Count(report => report.Direction == ReportDirection.Host);
        int deviceCount = reports.Count(report => report.Direction == ReportDirection.Device);
        int unknownCount = reports.Count(report => report.Direction == ReportDirection.Unknown);
        (int matched, int unmatched) = CountMatchingTransactions(reports);

        List<string> output =
        [
            "Offline HID report batch analysis (no HID access):",
            $"Parsed reports: {reports.Count}",
            $"Directions: HOST {hostCount}, DEVICE {deviceCount}, UNKNOWN {unknownCount}",
            $"Report IDs: {FormatReportIdCounts(reports)}",
            $"Exact HID++ response headers: {matched}",
            $"HOST HID++ requests without an exact header match: {unmatched}",
            $"Invalid lines: {errors.Count}",
            "Groups:"
        ];

        IEnumerable<IGrouping<ReportGroupKey, BatchReport>> groups = reports
            .Where(report => report.IsHidpp)
            .GroupBy(report => report.GroupKey)
            .OrderBy(group => group.Key.Direction)
            .ThenBy(group => group.Key.ReportId)
            .ThenBy(group => group.Key.DeviceIndex)
            .ThenBy(group => group.Key.FeatureIndex)
            .ThenBy(group => group.Key.FunctionAndSoftwareId);

        foreach (IGrouping<ReportGroupKey, BatchReport> group in groups)
        {
            ReportGroupKey key = group.Key;
            byte functionId = (byte)(key.FunctionAndSoftwareId >> 4);
            byte softwareId = (byte)(key.FunctionAndSoftwareId & 0x0F);
            int signatureCount = group.Select(report => report.ParameterSignature)
                .Distinct(StringComparer.Ordinal)
                .Count();

            output.Add(
                $"  {key.Direction.ToString().ToUpperInvariant()} " +
                $"ID 0x{key.ReportId:X2}, device 0x{key.DeviceIndex:X2}, " +
                $"feature 0x{key.FeatureIndex:X2}, function 0x{functionId:X2}, " +
                $"SW-ID 0x{softwareId:X2}: {group.Count()} reports, " +
                $"{signatureCount} parameter signatures");
        }

        if (!groups.Any())
        {
            output.Add("  (none)");
        }

        if (errors.Count > 0)
        {
            output.Add("Errors:");
            output.AddRange(errors.Select(error => $"  {error}"));
        }

        return output;
    }

    private static string FormatReportIdCounts(IEnumerable<BatchReport> reports)
    {
        string[] counts = reports
            .GroupBy(report => new { report.Direction, ReportId = report.Bytes[0] })
            .OrderBy(group => group.Key.Direction)
            .ThenBy(group => group.Key.ReportId)
            .Select(group =>
                $"{group.Key.Direction.ToString().ToUpperInvariant()} " +
                $"0x{group.Key.ReportId:X2}={group.Count()}")
            .ToArray();

        return counts.Length == 0 ? "(none)" : string.Join(", ", counts);
    }

    private static bool TryParseLine(
        string line,
        out BatchReport? report,
        out string? error)
    {
        report = null;
        error = null;

        string trimmed = line.Trim();
        if (trimmed.Length == 0 || trimmed.StartsWith('#'))
        {
            return false;
        }

        string[] fields = trimmed.Split('\t');
        ReportDirection direction = ReportDirection.Unknown;
        string hexText = fields[^1].Trim();

        if (fields.Length > 1)
        {
            direction = fields[0].Trim().ToUpperInvariant() switch
            {
                "HOST" => ReportDirection.Host,
                "DEVICE" => ReportDirection.Device,
                "UNKNOWN" => ReportDirection.Unknown,
                _ => ReportDirection.Invalid
            };

            if (direction == ReportDirection.Invalid)
            {
                error = $"unknown direction '{fields[0].Trim()}'";
                return false;
            }
        }

        if (!HexReportParser.TryParse(hexText, out byte[] bytes, out error))
        {
            return false;
        }

        report = new BatchReport(direction, bytes);
        return true;
    }

    private static (int Matched, int Unmatched) CountMatchingTransactions(
        IEnumerable<BatchReport> reports)
    {
        Dictionary<HeaderKey, Queue<BatchReport>> pending = [];
        int matched = 0;

        foreach (BatchReport report in reports.Where(report => report.IsHidpp))
        {
            HeaderKey key = report.HeaderKey;

            if (report.Direction == ReportDirection.Host)
            {
                if (!pending.TryGetValue(key, out Queue<BatchReport>? queue))
                {
                    queue = [];
                    pending.Add(key, queue);
                }

                queue.Enqueue(report);
            }
            else if (report.Direction == ReportDirection.Device &&
                     pending.TryGetValue(key, out Queue<BatchReport>? queue) &&
                     queue.Count > 0)
            {
                queue.Dequeue();
                matched++;
            }
        }

        return (matched, pending.Values.Sum(queue => queue.Count));
    }

    private enum ReportDirection
    {
        Host,
        Device,
        Unknown,
        Invalid
    }

    private sealed record BatchReport(ReportDirection Direction, byte[] Bytes)
    {
        public bool HasHidppHeader => Bytes.Length >= 4;

        public bool IsHidpp =>
            HasHidppHeader &&
            (Bytes[0] == 0x10 || Bytes[0] == 0x11);

        public ReportGroupKey GroupKey => new(
            Direction,
            Bytes[0],
            Bytes[1],
            Bytes[2],
            Bytes[3]);

        public HeaderKey HeaderKey => new(Bytes[1], Bytes[2], Bytes[3]);

        public string ParameterSignature =>
            Bytes.Length == 4 ? string.Empty : Convert.ToHexString(Bytes.AsSpan(4));
    }

    private sealed record ReportGroupKey(
        ReportDirection Direction,
        byte ReportId,
        byte DeviceIndex,
        byte FeatureIndex,
        byte FunctionAndSoftwareId);

    private sealed record HeaderKey(
        byte DeviceIndex,
        byte FeatureIndex,
        byte FunctionAndSoftwareId);
}
