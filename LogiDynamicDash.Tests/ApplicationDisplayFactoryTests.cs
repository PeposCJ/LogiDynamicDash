using LogiDynamicDash.Configuration;
using LogiDynamicDash.Displays;
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
        Assert.False(selection.UsesPhysicalHardware);
        Assert.False(selection.IsBoundedHardwareTrial);
        Assert.Equal(0, sessionFactoryCalls);
    }

    [Fact]
    public void DrivingArguments_SelectUnboundedPhysicalSafetyEnvelope()
    {
        FakeSession session = new();
        Assert.True(
            ApplicationDisplayFactory.TryCreate(
                Rs50DrivingTrialOptionsTests.ValidArguments(),
                () => session,
                () => new FakeDisplay(),
                _ => new Rs50OledConfiguration(Rs50OledLayout.E),
                out ApplicationDisplaySelection? selection));

        Assert.NotNull(selection);
        Assert.True(selection.UsesPhysicalHardware);
        Assert.False(selection.IsBoundedHardwareTrial);
        Assert.Null(selection.HardwareTrialDuration);
        selection.Display.Initialize();

        selection.Display.Render(
            new TelemetrySnapshot
            {
                ConnectionState = "CONNECTED",
                IsOnTrack = true,
                SpeedMetersPerSecond = 120,
                Gear = 6,
                Rpm = 7000
            },
            DisplayMode.Normal);

        selection.Display.Stop();
        Assert.True(session.Disposed);
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
        int configurationLoaderCalls = 0;
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
                _ =>
                {
                    configurationLoaderCalls++;
                    return new Rs50OledConfiguration(Rs50OledLayout.E);
                },
                out ApplicationDisplaySelection? selection));

        Assert.NotNull(selection);
        Assert.True(selection.IsBoundedHardwareTrial);
        Assert.Equal(
            Rs50StationaryTrialOptions.Duration,
            selection.HardwareTrialDuration);
        Assert.Equal(1, configurationLoaderCalls);
        Assert.Equal(0, sessionFactoryCalls);

        selection.Display.Initialize();
        Assert.Equal(1, sessionFactoryCalls);
        Assert.Equal(1, session.OpenCount);
        selection.Display.Stop();
        Assert.True(session.Disposed);
    }

    [Fact]
    public void LowSpeedArguments_SelectDistinctBoundedSafetyEnvelope()
    {
        FakeSession session = new();
        Assert.True(
            ApplicationDisplayFactory.TryCreate(
                Rs50LowSpeedTrialOptionsTests.ValidArguments(),
                () => session,
                () => new FakeDisplay(),
                _ => new Rs50OledConfiguration(Rs50OledLayout.E),
                out ApplicationDisplaySelection? selection));

        Assert.NotNull(selection);
        Assert.Equal(
            Rs50LowSpeedTrialOptions.Duration,
            selection.HardwareTrialDuration);
        selection.Display.Initialize();

        selection.Display.Render(
            new TelemetrySnapshot
            {
                ConnectionState = "CONNECTED",
                IsOnTrack = true,
                SpeedMetersPerSecond = 5,
                Gear = 1,
                Rpm = 1500
            },
            DisplayMode.Normal);

        Assert.Throws<InvalidOperationException>(
            () => selection.Display.Render(
                new TelemetrySnapshot
                {
                    ConnectionState = "CONNECTED",
                    IsOnTrack = true,
                    SpeedMetersPerSecond = 6,
                    Gear = 1,
                    Rpm = 1500
                },
                DisplayMode.Normal));
        selection.Display.Stop();
    }

    [Fact]
    public void ConfigurationFailure_DoesNotConstructPhysicalSession()
    {
        int sessionFactoryCalls = 0;

        Assert.Throws<InvalidDataException>(
            () => ApplicationDisplayFactory.TryCreate(
                Rs50StationaryTrialOptionsTests.ValidArguments(),
                () =>
                {
                    sessionFactoryCalls++;
                    return new FakeSession();
                },
                () => new FakeDisplay(),
                _ => throw new InvalidDataException("invalid config"),
                out _));

        Assert.Equal(0, sessionFactoryCalls);
    }

    [Fact]
    public void RedirectedConsole_DoesNotBlockStationarySessionInitialization()
    {
        FakeSession session = new();
        Assert.True(
            ApplicationDisplayFactory.TryCreate(
                Rs50StationaryTrialOptionsTests.ValidArguments(),
                () => session,
                () => new ConsoleDashboard(interactiveOverride: false),
                _ => new Rs50OledConfiguration(Rs50OledLayout.E),
                out ApplicationDisplaySelection? selection));

        selection!.Display.Initialize();

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
