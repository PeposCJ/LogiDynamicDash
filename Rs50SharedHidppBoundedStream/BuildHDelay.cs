namespace Rs50SharedHidppBoundedStream;

internal interface IBuildHDelay
{
    void WaitOneSecond();
}

internal sealed class BuildHSystemDelay : IBuildHDelay
{
    public void WaitOneSecond() =>
        Thread.Sleep(TimeSpan.FromSeconds(1));
}
