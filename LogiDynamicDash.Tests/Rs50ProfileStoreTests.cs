using LogiDynamicDash.Configuration;
using LogiDynamicDash.Displays;
using LogiDynamicDash.Models;

namespace LogiDynamicDash.Tests;

public sealed class Rs50ProfileStoreTests
{
    [Fact]
    public void CarProfile_TakesPriorityOverDisciplineProfile()
    {
        using TemporaryDirectory directory = new();
        Rs50ProfileStore store = new(directory.Path);
        store.SaveForDiscipline(
            IRacingDiscipline.FormulaCar,
            new Rs50OledConfiguration(Rs50OledLayout.D));
        store.SaveForCar(
            42,
            new Rs50OledConfiguration(Rs50OledLayout.J));

        Rs50OledConfiguration? resolved = store.Resolve(
            Identity(IRacingDiscipline.FormulaCar, 42));

        Assert.NotNull(resolved);
        Assert.Equal(Rs50OledLayout.J, resolved.Layout);
    }

    [Fact]
    public void AutomaticFormatter_UsesRecommendationWithoutStoredOverride()
    {
        using TemporaryDirectory directory = new();
        AutomaticRs50TelemetryFrameFormatter formatter = new(
            new Rs50OledConfiguration(Rs50OledLayout.D),
            new Rs50ProfileStore(directory.Path));
        TelemetrySnapshot snapshot = new()
        {
            SessionIdentity = Identity(IRacingDiscipline.FormulaCar, 42),
            ConnectionState = "CONNECTED",
            SpeedMetersPerSecond = 0,
            Gear = 0,
            Rpm = 1000
        };

        Rs50OledFrame frame = formatter.Format(
            snapshot,
            DisplayMode.Normal);

        Assert.IsType<Rs50LayoutEFrame>(frame);
    }

    [Fact]
    public void ManualFormatter_AlwaysUsesFallback()
    {
        using TemporaryDirectory directory = new();
        AutomaticRs50TelemetryFrameFormatter formatter = new(
            new Rs50OledConfiguration(Rs50OledLayout.D),
            new Rs50ProfileStore(directory.Path),
            automaticProfiles: false);
        TelemetrySnapshot snapshot = new()
        {
            SessionIdentity = Identity(IRacingDiscipline.FormulaCar, 42),
            ConnectionState = "CONNECTED",
            SpeedMetersPerSecond = 0,
            Gear = 0,
            Rpm = 1000
        };

        Assert.IsType<Rs50LayoutDFrame>(
            formatter.Format(snapshot, DisplayMode.Normal));
    }

    private static IRacingSessionIdentity Identity(
        IRacingDiscipline discipline,
        int carId) =>
        new(
            discipline,
            discipline.ToString(),
            "Road",
            new CarIdentity(
                carId,
                "cars/test",
                "Test Car",
                "Test",
                1,
                "Test Class",
                false));

    private sealed class TemporaryDirectory : IDisposable
    {
        internal TemporaryDirectory()
        {
            Path = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                "LogiDynamicDash.Tests",
                Guid.NewGuid().ToString("N"));
        }

        internal string Path { get; }

        public void Dispose()
        {
            if (Directory.Exists(Path))
            {
                Directory.Delete(Path, recursive: true);
            }
        }
    }
}
