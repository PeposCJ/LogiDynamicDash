namespace Rs50SharedHidppCoexistence;

internal interface IBuildIDelay
{
    void WaitOneSecond();
}

internal sealed class BuildISystemDelay : IBuildIDelay
{
    public void WaitOneSecond() =>
        Thread.Sleep(TimeSpan.FromSeconds(1));
}
