using LogiDynamicDash.Configuration;
using LogiDynamicDash.Hidpp;
using LogiDynamicDash.Models;

namespace LogiDynamicDash.Tests;

public sealed class ApplicationDisplayFactoryTests
{
    [Fact]
    public void NoArguments_SelectsConsoleWithoutConstructingSession()
    {
        int sessionFactoryCalls = 0;

        bool accepted = ApplicationDisplayFactory.TryCreate(
            [],
            () =>
            {
                sessionFactoryCalls++;
                return new FakeSession();
            },
            out ApplicationDisplaySelection? selection);

        Assert.True(accepted);
        Assert.NotNull(selection);
        Assert.False(selection.IsBoundedHardwareTrial);
        Assert.Equal(0, sessionFactoryCalls);
    }

    [Fact]
    public void InvalidArguments_DoNotConstructSession()
    {
        int sessionFactoryCalls = 0;

        bool accepted = ApplicationDisplayFactory.TryCreate(
            ["--enable-rs50-oled"],
            () =>
            {
                sessionFactoryCalls++;
                return new FakeSession();
            },
            out ApplicationDisplaySelection? selection);

        Assert.False(accepted);
        Assert.Null(selection);
        Assert.Equal(0, sessionFactoryCalls);
    }

    [Fact]
    public void ValidArguments_DeferSessionConstructionUntilInitialization()
    {
        int sessionFactoryCalls = 0;
        FakeSession session = new();
        Assert.True(
            ApplicationDisplayFactory.TryCreate(
                Rs50StationaryTrialOptionsTests.ValidArguments(),
                () =>
                {
                    sessionFactoryCalls++;
                    return session;
                },
                () => new FakeDisplay(),
                out ApplicationDisplaySelection? selection));

        Assert.NotNull(selection);
        Assert.True(selection.IsBoundedHardwareTrial);
        Assert.Equal(0, sessionFactoryCalls);

        selection.Display.Initialize();
        Assert.Equal(1, sessionFactoryCalls);
        Assert.Equal(1, session.OpenCount);
        selection.Display.Stop();
        Assert.True(session.Disposed);
    }

    private sealed class FakeSession : IRs50OledSession
    {
        public int OpenCount { get; private set; }

        public bool Disposed { get; private set; }

        public void Open() =>
            OpenCount++;

        public Rs50OledSendResult Send(Rs50OledFrame frame) =>
            Rs50OledSendResult.Transmitted;

        public void Dispose() =>
            Disposed = true;
    }

    private sealed class FakeDisplay
        : LogiDynamicDash.Displays.IApplicationDisplay
    {
        public void Initialize()
        {
        }

        public void Render(TelemetrySnapshot snapshot, DisplayMode mode)
        {
        }

        public void Stop()
        {
        }
    }
}
