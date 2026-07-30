namespace LogiDynamicDash.Hidpp.Transport;

/// <summary>
/// A device identity may enter the physical transport only after its complete
/// collection contract has been confirmed. No speculative PRO identity is
/// included.
/// </summary>
internal sealed record ConfirmedOledDeviceIdentity(
    string Model,
    int VendorId,
    int ProductId)
{
    internal static ConfirmedOledDeviceIdentity Rs50 { get; } =
        new("RS50", 0x046D, 0xC276);
}
