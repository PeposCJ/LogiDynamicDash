namespace Rs50SharedHidppLayoutGallery;

internal interface IBuildKDelay
{
    void WaitThreeSeconds();
}

internal sealed class BuildKSystemDelay : IBuildKDelay
{
    public void WaitThreeSeconds() =>
        Thread.Sleep(TimeSpan.FromSeconds(3));
}
