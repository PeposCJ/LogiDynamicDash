using System.Buffers.Binary;
using LogiDynamicExplorer.Decoders;

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
        IReadOnlyList<FeatureSetEntry> featureSetEntries =
            ExtractFeatureSetEntries(reports)
                .Concat(ExtractRootFeatureEntries(reports))
                .DistinctBy(entry => (
                    entry.DeviceIndex,
                    entry.RuntimeIndex,
                    entry.FeatureId))
                .ToArray();

        List<string> output =
        [
            "Offline HID report batch analysis (no HID access):",
            $"Parsed reports: {reports.Count}",
            $"Directions: HOST {hostCount}, DEVICE {deviceCount}, UNKNOWN {unknownCount}",
            $"Report IDs: {FormatReportIdCounts(reports)}",
            $"Exact HID++ response headers: {matched}",
            $"HOST HID++ requests without an exact header match: {unmatched}",
            $"Invalid lines: {errors.Count}",
            "Feature catalog (matched Root/FeatureSet request/response pairs):"
        ];

        if (featureSetEntries.Count == 0)
        {
            output.Add("  (none)");
        }
        else
        {
            foreach (FeatureSetEntry entry in featureSetEntries
                         .OrderBy(entry => entry.DeviceIndex)
                         .ThenBy(entry => entry.RuntimeIndex))
            {
                int operationalRequestCount = reports.Count(report =>
                    report.Direction == ReportDirection.Host &&
                    report.IsResolvedFeatureTransport &&
                    !report.IsFeatureSetGetFeatureIdRequest &&
                    report.Bytes[1] == entry.DeviceIndex &&
                    report.Bytes[2] == entry.RuntimeIndex);

                output.Add(
                    $"  device 0x{entry.DeviceIndex:X2}, " +
                    $"runtime 0x{entry.RuntimeIndex:X2} -> " +
                    $"feature 0x{entry.FeatureId:X4}" +
                    $"{HidppFeatureNames.Format(entry.FeatureId)}, " +
                    $"flags 0x{entry.Flags:X2}, version {entry.Version}; " +
                    $"HOST operational requests {operationalRequestCount}");
            }
        }

        AddDisplayGameDataActivity(output, reports, featureSetEntries);

        output.Add(
            "Groups:"
        );

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

        foreach (BatchReport report in
                 reports.Where(report => report.IsPotentialHidppTransport))
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

    private static IReadOnlyList<FeatureSetEntry> ExtractFeatureSetEntries(
        IEnumerable<BatchReport> reports)
    {
        Dictionary<HeaderKey, Queue<byte>> pendingRuntimeIndices = [];
        List<FeatureSetEntry> entries = [];

        foreach (BatchReport report in reports)
        {
            if (report.Direction == ReportDirection.Host &&
                report.IsFeatureSetGetFeatureIdRequest)
            {
                HeaderKey key = report.HeaderKey;

                if (!pendingRuntimeIndices.TryGetValue(
                        key,
                        out Queue<byte>? runtimeIndices))
                {
                    runtimeIndices = [];
                    pendingRuntimeIndices.Add(key, runtimeIndices);
                }

                runtimeIndices.Enqueue(report.Bytes[4]);
                continue;
            }

            if (report.Direction != ReportDirection.Device ||
                !report.IsFeatureSetGetFeatureIdResponse ||
                !pendingRuntimeIndices.TryGetValue(
                    report.HeaderKey,
                    out Queue<byte>? pending) ||
                pending.Count == 0)
            {
                continue;
            }

            byte runtimeIndex = pending.Dequeue();
            ushort featureId = BinaryPrimitives.ReadUInt16BigEndian(
                report.Bytes.AsSpan(4, 2));

            entries.Add(new FeatureSetEntry(
                report.Bytes[1],
                runtimeIndex,
                featureId,
                report.Bytes[6],
                report.Bytes[7]));
        }

        return entries;
    }

    private static IReadOnlyList<FeatureSetEntry> ExtractRootFeatureEntries(
        IEnumerable<BatchReport> reports)
    {
        Dictionary<HeaderKey, Queue<ushort>> pendingFeatureIds = [];
        List<FeatureSetEntry> entries = [];

        foreach (BatchReport report in reports)
        {
            if (report.Direction == ReportDirection.Host &&
                report.IsRootGetFeatureRequest)
            {
                HeaderKey key = report.HeaderKey;

                if (!pendingFeatureIds.TryGetValue(
                        key,
                        out Queue<ushort>? featureIds))
                {
                    featureIds = [];
                    pendingFeatureIds.Add(key, featureIds);
                }

                featureIds.Enqueue(BinaryPrimitives.ReadUInt16BigEndian(
                    report.Bytes.AsSpan(4, 2)));
                continue;
            }

            if (report.Direction != ReportDirection.Device ||
                !report.IsRootGetFeatureResponse ||
                !pendingFeatureIds.TryGetValue(
                    report.HeaderKey,
                    out Queue<ushort>? pending) ||
                pending.Count == 0)
            {
                continue;
            }

            entries.Add(new FeatureSetEntry(
                report.Bytes[1],
                report.Bytes[4],
                pending.Dequeue(),
                report.Bytes[5],
                report.Bytes[6]));
        }

        return entries;
    }

    private static void AddDisplayGameDataActivity(
        List<string> output,
        IReadOnlyList<BatchReport> reports,
        IReadOnlyList<FeatureSetEntry> featureSetEntries)
    {
        FeatureSetEntry[] displayEntries = featureSetEntries
            .Where(entry => entry.FeatureId == 0x8130)
            .ToArray();

        if (displayEntries.Length == 0)
        {
            return;
        }

        output.Add("Display Game Data activity (feature 0x8130):");

        foreach (FeatureSetEntry entry in displayEntries)
        {
            BatchReport[] activity = reports
                .Where(report =>
                    (report.Direction == ReportDirection.Host ||
                     report.Direction == ReportDirection.Device) &&
                    report.IsResolvedFeatureTransport &&
                    report.Bytes[1] == entry.DeviceIndex &&
                    report.Bytes[2] == entry.RuntimeIndex)
                .ToArray();

            if (activity.Length == 0)
            {
                output.Add(
                    $"  device 0x{entry.DeviceIndex:X2}, " +
                    $"runtime 0x{entry.RuntimeIndex:X2}: " +
                    "no operational reports");
                continue;
            }

            foreach (IGrouping<(ReportDirection Direction, byte FunctionId), BatchReport> group
                         in activity
                             .GroupBy(report => (
                                 report.Direction,
                                 FunctionId: (byte)(report.Bytes[3] >> 4)))
                             .OrderBy(group => group.Key.Direction)
                             .ThenBy(group => group.Key.FunctionId))
            {
                string[] signatures = group
                    .Select(report => report.ParameterSignature)
                    .Distinct(StringComparer.Ordinal)
                    .ToArray();

                output.Add(
                    $"  {group.Key.Direction.ToString().ToUpperInvariant()} " +
                    $"device 0x{entry.DeviceIndex:X2}, " +
                    $"runtime 0x{entry.RuntimeIndex:X2}, " +
                    $"function 0x{group.Key.FunctionId:X2}: " +
                    $"{group.Count()} reports, " +
                    $"{signatures.Length} parameter signatures");

                foreach (BatchReport report in group
                             .DistinctBy(report => report.ParameterSignature)
                             .Take(10))
                {
                    string decoded = report.Direction == ReportDirection.Host
                        ? Rs50DisplayGameDataDecoder.DecodeHostCommand(
                            group.Key.FunctionId,
                            report.Parameters)
                        : Rs50DisplayGameDataDecoder.DecodeDeviceResponse(
                            group.Key.FunctionId,
                            report.Parameters);

                    output.Add($"    {decoded}");
                }

                if (signatures.Length > 10)
                {
                    output.Add(
                        $"    ({signatures.Length - 10} additional " +
                        "signatures omitted)");
                }
            }
        }
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

        public bool IsPotentialHidppTransport =>
            HasHidppHeader &&
            Bytes[0] is 0x10 or 0x11 or 0x12;

        public bool IsHidppRequest =>
            IsHidpp;

        public bool IsFeatureSetGetFeatureIdRequest =>
            IsHidppRequest &&
            Bytes.Length >= 5 &&
            Bytes[2] == 0x01 &&
            (Bytes[3] >> 4) == 0x01;

        public bool IsFeatureSetGetFeatureIdResponse =>
            HasHidppHeader &&
            Bytes.Length >= 8 &&
            (Bytes[0] == 0x11 || Bytes[0] == 0x12) &&
            Bytes[2] == 0x01 &&
            (Bytes[3] >> 4) == 0x01;

        public bool IsRootGetFeatureRequest =>
            IsPotentialHidppTransport &&
            Bytes.Length >= 6 &&
            Bytes[2] == 0x00 &&
            (Bytes[3] >> 4) == 0x00;

        public bool IsRootGetFeatureResponse =>
            IsPotentialHidppTransport &&
            Bytes.Length >= 7 &&
            Bytes[2] == 0x00 &&
            (Bytes[3] >> 4) == 0x00;

        public bool IsResolvedFeatureTransport =>
            HasHidppHeader &&
            (Bytes[0] == 0x10 ||
             Bytes[0] == 0x11 ||
             (Bytes[0] == 0x12 && (Bytes[3] & 0x0F) != 0));

        public ReportGroupKey GroupKey => new(
            Direction,
            Bytes[0],
            Bytes[1],
            Bytes[2],
            Bytes[3]);

        public HeaderKey HeaderKey => new(Bytes[1], Bytes[2], Bytes[3]);

        public string ParameterSignature =>
            Bytes.Length == 4 ? string.Empty : Convert.ToHexString(Bytes.AsSpan(4));

        public ReadOnlySpan<byte> Parameters => Bytes.AsSpan(4);
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

    private sealed record FeatureSetEntry(
        byte DeviceIndex,
        byte RuntimeIndex,
        ushort FeatureId,
        byte Flags,
        byte Version);
}
