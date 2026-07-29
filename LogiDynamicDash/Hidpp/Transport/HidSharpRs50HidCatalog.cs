using HidSharp;

namespace LogiDynamicDash.Hidpp.Transport;

internal sealed class HidSharpRs50HidCatalog : IRs50HidCatalog
{
    private const int LogitechVendorId = 0x046D;
    private const int Rs50ProductId = 0xC276;

    public IReadOnlyList<IRs50HidCollection> Enumerate() =>
        DeviceList.Local
            .GetHidDevices(LogitechVendorId, Rs50ProductId)
            .OrderBy(
                device => device.DevicePath,
                StringComparer.OrdinalIgnoreCase)
            .Select(
                device =>
                    (IRs50HidCollection)new HidSharpRs50HidCollection(
                        device))
            .ToArray();

    private sealed class HidSharpRs50HidCollection(HidDevice device)
        : IRs50HidCollection
    {
        private readonly HidDevice device = device;

        public int VendorId => device.VendorID;

        public int ProductId => device.ProductID;

        public string DevicePath => device.DevicePath;

        public IReadOnlySet<uint> Usages { get; } =
            device
                .GetReportDescriptor()
                .DeviceItems
                .SelectMany(item => item.Usages.GetAllValues())
                .ToHashSet();

        public int MaximumInputReportLength =>
            device.GetMaxInputReportLength();

        public int MaximumOutputReportLength =>
            device.GetMaxOutputReportLength();

        public IRs50HidStream Open()
        {
            if (!device.TryOpen(out HidStream stream))
            {
                throw new IOException(
                    "The validated RS50 HID++ collection could not be opened.");
            }

            stream.ReadTimeout = 1000;
            stream.WriteTimeout = 1000;
            return new HidSharpRs50HidStream(stream);
        }
    }

    private sealed class HidSharpRs50HidStream(HidStream stream)
        : IRs50HidStream
    {
        public int Read(byte[] buffer)
        {
            ArgumentNullException.ThrowIfNull(buffer);
            return stream.Read(buffer, 0, buffer.Length);
        }

        public void Write(byte[] report)
        {
            ArgumentNullException.ThrowIfNull(report);
            stream.Write(report);
        }

        public void Dispose() =>
            stream.Dispose();
    }
}
