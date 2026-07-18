using HidSharp.Reports;
using LogiDynamicExplorer.Diagnostics;

namespace LogiDynamicExplorer.Tests;

public sealed class Rs50DescriptorFormatterTests
{
    [Fact]
    public void Format_DescribesUsageAndReports()
    {
        byte[] descriptorBytes =
        [
            0x06, 0x43, 0xFF,
            0x0A, 0x04, 0x07,
            0xA1, 0x01,
            0x85, 0x12,
            0x09, 0x01,
            0x75, 0x08,
            0x95, 0x3F,
            0x81, 0x02,
            0x09, 0x01,
            0x95, 0x3F,
            0x91, 0x02,
            0xC0
        ];

        var descriptor =
            new ReportDescriptor(descriptorBytes);

        IReadOnlyList<string> result =
            Rs50DescriptorFormatter.Format(descriptor);

        Assert.Equal(
            [
                "Usages: FF43:0704",
                "Uses report IDs: yes",
                "Input: 0x12 (64 bytes)",
                "Output: 0x12 (64 bytes)",
                "Feature: none"
            ],
            result);
    }
}
