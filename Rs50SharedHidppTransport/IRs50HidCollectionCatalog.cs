namespace Rs50SharedHidppTransport;

internal interface IRs50HidCollectionCatalog
{
    IReadOnlyList<IRs50HidCollection> Enumerate();
}

internal interface IRs50HidCollection
{
    int VendorId { get; }

    int ProductId { get; }

    string DevicePath { get; }

    IReadOnlySet<uint> Usages { get; }

    int MaximumInputReportLength { get; }

    int MaximumOutputReportLength { get; }

    IRs50HidStream Open();
}

internal interface IRs50HidStream : IDisposable
{
    int Read(byte[] buffer);

    void Write(byte[] report);
}
