using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using LogiDynamicDash.Configuration;
using LogiDynamicDash.Displays;
using LogiDynamicDash.Models;
using LogiDynamicDash.Offline;
using LogiDynamicDash.Runtime;
using Microsoft.Win32;

namespace LogiDynamicDash.Configurator;

public partial class MainWindow : Window
{
    private readonly ComboBox[] layoutBoxes;
    private bool updating;
    private IRacingSessionIdentity? detectedIdentity;
    private CancellationTokenSource? runtimeCancellation;
    private Task? runtimeTask;
    private int? liveCarId;
    private IRacingDiscipline liveDiscipline = IRacingDiscipline.Unknown;

    private static readonly string ApplicationDataDirectory = Path.Combine(
        Environment.GetFolderPath(
            Environment.SpecialFolder.LocalApplicationData),
        "LogiDynamicDash");
    private static readonly string ActiveConfigurationPath = Path.Combine(
        ApplicationDataDirectory,
        "active-dashboard.json");
    private static readonly string PreferencesPath = Path.Combine(
        ApplicationDataDirectory,
        "preferences.json");
    private static readonly string ProfileDirectory = Path.Combine(
        ApplicationDataDirectory,
        "profiles");

    public MainWindow()
    {
        InitializeComponent();
        layoutBoxes =
        [
            NormalLayoutBox,
            BrakeBiasLayoutBox,
            LastLapLayoutBox,
            ConnectionLayoutBox
        ];

        DisciplineBox.ItemsSource =
            DisciplineProfileRecommendations.SupportedDisciplines;
        SpeedUnitBox.ItemsSource = Enum.GetValues<SpeedUnit>();
        PreviewModeBox.ItemsSource = Enum.GetValues<DisplayMode>();
        foreach (ComboBox box in layoutBoxes)
        {
            box.ItemsSource = Enum.GetValues<Rs50OledLayout>();
            box.SelectionChanged += (_, _) => UpdatePreview();
        }

        DisciplineBox.SelectionChanged += (_, _) => UpdateRecommendation();
        SpeedUnitBox.SelectionChanged += (_, _) => UpdateRecommendation();
        MaximumRpmBox.TextChanged += (_, _) => UpdatePreview();
        GaugeSpeedBox.TextChanged += (_, _) => UpdatePreview();
        PreviewModeBox.SelectionChanged += (_, _) => UpdatePreview();

        DisciplineBox.SelectedItem = IRacingDiscipline.SportsCar;
        SpeedUnitBox.SelectedItem = SpeedUnit.KilometersPerHour;
        PreviewModeBox.SelectedItem = DisplayMode.Normal;
        ApplyRecommendation(IRacingDiscipline.SportsCar);
        LoadPersistedSettings();
        Closing += (_, _) =>
        {
            runtimeCancellation?.Cancel();
            PersistSettingsBestEffort();
        };
    }

    private void StartDashboard_Click(object sender, RoutedEventArgs e)
    {
        if (runtimeTask is { IsCompleted: false })
        {
            return;
        }

        try
        {
            Rs50OledConfiguration configuration = CreateConfiguration();
            double lastLapSeconds = ParseLastLapSeconds();
            Directory.CreateDirectory(ApplicationDataDirectory);
            File.WriteAllText(
                ActiveConfigurationPath,
                Rs50OledConfigurationFile.Serialize(configuration));
            SavePreferences();

            runtimeCancellation = new CancellationTokenSource();
            DashboardRuntime runtime = new();
            DashboardRuntimeSettings settings = new(
                configuration,
                ProfileDirectory,
                AutomaticProfilesBox.IsChecked == true,
                lastLapSeconds);
            StartDashboardButton.IsEnabled = false;
            StopDashboardButton.IsEnabled = true;
            RuntimeStateText.Text = "Starting dashboard";
            RuntimeDetailText.Text =
                "Waiting for the validated RS50 and iRacing.";
            runtimeTask = RunDashboardAsync(
                runtime,
                settings,
                runtimeCancellation);
        }
        catch (Exception exception)
        {
            ShowRuntimeError(exception);
        }
    }

    private async Task RunDashboardAsync(
        DashboardRuntime runtime,
        DashboardRuntimeSettings settings,
        CancellationTokenSource cancellation)
    {
        try
        {
            await runtime.RunAsync(
                settings,
                status => Dispatcher.BeginInvoke(
                    () => ApplyRuntimeStatus(status)),
                cancellation.Token);
        }
        catch (Exception exception)
        {
            await Dispatcher.BeginInvoke(
                () => ShowRuntimeError(exception));
        }
        finally
        {
            await Dispatcher.BeginInvoke(
                () =>
                {
                    if (ReferenceEquals(runtimeCancellation, cancellation))
                    {
                        runtimeCancellation.Dispose();
                        runtimeCancellation = null;
                    }

                    StartDashboardButton.IsEnabled = true;
                    StopDashboardButton.IsEnabled = false;
                    RuntimeStateText.Text = "Dashboard stopped";
                });
        }
    }

    private void StopDashboard_Click(object sender, RoutedEventArgs e)
    {
        StopDashboardButton.IsEnabled = false;
        RuntimeStateText.Text = "Stopping dashboard";
        runtimeCancellation?.Cancel();
    }

    private void ApplyRuntimeStatus(DashboardRuntimeStatus status)
    {
        RuntimeStateText.Text = status.Message;
        RuntimeDetailText.Text =
            $"OLED: {status.Oled} · iRacing: {status.Telemetry} · " +
            $"Car: {status.Car} · Category: " +
            IRacingDisciplineDisplay.Name(status.Discipline);
        liveCarId = status.CarId;
        liveDiscipline = status.Discipline;
        SaveCarProfileButton.IsEnabled =
            status.CarId is > 0 || detectedIdentity?.Car?.CarId is > 0;
    }

    private void ApplyRecommendation_Click(object sender, RoutedEventArgs e)
    {
        if (DisciplineBox.SelectedItem is IRacingDiscipline discipline)
        {
            ApplyRecommendation(discipline);
        }
    }

    private void ApplyRecommendation(IRacingDiscipline discipline)
    {
        SpeedUnit unit = SpeedUnitBox.SelectedItem is SpeedUnit selected
            ? selected
            : SpeedUnit.KilometersPerHour;
        DisciplineProfileRecommendation recommendation =
            DisciplineProfileRecommendations.Create(discipline, unit);
        LoadConfiguration(recommendation.Configuration);
        RecommendationText.Text = recommendation.Summary;
        StatusText.Text = $"{discipline} recommendation applied.";
    }

    private void UpdateRecommendation()
    {
        if (updating ||
            DisciplineBox.SelectedItem is not IRacingDiscipline discipline ||
            SpeedUnitBox.SelectedItem is not SpeedUnit unit)
        {
            return;
        }

        RecommendationText.Text =
            DisciplineProfileRecommendations.Create(discipline, unit).Summary;
        UpdateDetectedIdentity();
        UpdatePreview();
    }

    private void InspectReplay_Click(object sender, RoutedEventArgs e)
    {
        OpenFileDialog dialog = new()
        {
            Filter = "LogiDynamicDash telemetry replay (*.json)|*.json",
            CheckFileExists = true
        };
        if (dialog.ShowDialog(this) != true)
        {
            return;
        }

        try
        {
            IReadOnlyList<TelemetryReplayEvent> events =
                TelemetryReplayFile.Load(dialog.FileName);
            detectedIdentity = events
                .Select(replayEvent => replayEvent.SessionIdentity)
                .LastOrDefault(identity => identity is not null);
            if (detectedIdentity is null)
            {
                throw new InvalidDataException(
                    "This replay contains no schema 2 session identity.");
            }

            UpdateDetectedIdentity();
            StatusText.Text =
                $"Inspected {Path.GetFileName(dialog.FileName)}.";
        }
        catch (Exception exception)
        {
            detectedIdentity = null;
            UpdateDetectedIdentity();
            MessageBox.Show(
                this,
                exception.Message,
                "Replay identity unavailable",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
    }

    private void ApplyDetected_Click(object sender, RoutedEventArgs e)
    {
        if (detectedIdentity is null ||
            SpeedUnitBox.SelectedItem is not SpeedUnit unit)
        {
            return;
        }

        SessionProfileResolution resolution =
            SessionProfileResolver.Resolve(detectedIdentity, unit);
        if (resolution.Recommendation is null)
        {
            return;
        }

        DisciplineBox.SelectedItem = detectedIdentity.Discipline;
        ApplyRecommendation(detectedIdentity.Discipline);
        StatusText.Text =
            $"Applied {IRacingDisciplineDisplay.Name(
                detectedIdentity.Discipline)} for CarID " +
            $"{resolution.CarKey!.CarId}.";
    }

    private void UpdateDetectedIdentity()
    {
        if (detectedIdentity is null ||
            SpeedUnitBox.SelectedItem is not SpeedUnit unit)
        {
            DetectedIdentityText.Text = "No session identity loaded.";
            ApplyDetectedButton.IsEnabled = false;
            return;
        }

        SessionProfileResolution resolution =
            SessionProfileResolver.Resolve(detectedIdentity, unit);
        CarIdentity? car = detectedIdentity.Car;
        string carName = !string.IsNullOrWhiteSpace(car?.DisplayName)
            ? car.DisplayName
            : "Unknown car";
        string carId = car?.CarId?.ToString(CultureInfo.InvariantCulture)
            ?? "missing";
        DetectedIdentityText.Text =
            $"Car: {carName} (CarID {carId}){Environment.NewLine}" +
            $"Class: {car?.CarClassShortName ?? "Unknown"}{Environment.NewLine}" +
            $"Category: " +
            $"{IRacingDisciplineDisplay.Name(detectedIdentity.Discipline)}" +
            $"{Environment.NewLine}Track type: " +
            $"{detectedIdentity.TrackType}{Environment.NewLine}" +
            $"Decision: {resolution.Explanation}";
        ApplyDetectedButton.IsEnabled = resolution.CanApply;
        SaveCarProfileButton.IsEnabled =
            detectedIdentity.Car?.CarId is > 0 || liveCarId is > 0;
    }

    private void SaveCategoryProfile_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            IRacingDiscipline discipline =
                DisciplineBox.SelectedItem is IRacingDiscipline selected
                    ? selected
                    : liveDiscipline;
            string path = new Rs50ProfileStore(ProfileDirectory)
                .SaveForDiscipline(discipline, CreateConfiguration());
            StatusText.Text =
                $"Saved category profile {Path.GetFileName(path)}.";
        }
        catch (Exception exception)
        {
            ShowConfigurationError(
                exception,
                "Category profile not saved");
        }
    }

    private void SaveCarProfile_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            int? carId = liveCarId ?? detectedIdentity?.Car?.CarId;
            if (carId is not > 0)
            {
                throw new InvalidOperationException(
                    "Run the dashboard or inspect a replay with an exact " +
                    "CarID first.");
            }

            string path = new Rs50ProfileStore(ProfileDirectory)
                .SaveForCar(carId.Value, CreateConfiguration());
            StatusText.Text =
                $"Saved CarID {carId} profile as {Path.GetFileName(path)}.";
        }
        catch (Exception exception)
        {
            ShowConfigurationError(exception, "Car profile not saved");
        }
    }

    private void LoadConfiguration(Rs50OledConfiguration configuration)
    {
        updating = true;
        try
        {
            SpeedUnitBox.SelectedItem = configuration.SpeedUnit;
            MaximumRpmBox.Text =
                configuration.MaximumRpm.ToString(CultureInfo.InvariantCulture);
            GaugeSpeedBox.Text = configuration.GaugeMaximumSpeed.ToString(
                CultureInfo.InvariantCulture);
            NormalLayoutBox.SelectedItem =
                configuration.LayoutFor(DisplayMode.Normal);
            BrakeBiasLayoutBox.SelectedItem =
                configuration.LayoutFor(DisplayMode.BrakeBias);
            LastLapLayoutBox.SelectedItem =
                configuration.LayoutFor(DisplayMode.LastLap);
            ConnectionLayoutBox.SelectedItem =
                configuration.LayoutFor(DisplayMode.ConnectionProblem);
        }
        finally
        {
            updating = false;
        }

        UpdateDetectedIdentity();
        UpdatePreview();
    }

    private Rs50OledConfiguration CreateConfiguration()
    {
        if (SpeedUnitBox.SelectedItem is not SpeedUnit speedUnit ||
            layoutBoxes.Any(box => box.SelectedItem is not Rs50OledLayout) ||
            !double.TryParse(
                MaximumRpmBox.Text,
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out double maximumRpm) ||
            !double.TryParse(
                GaugeSpeedBox.Text,
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out double maximumSpeed))
        {
            throw new InvalidDataException(
                "Complete every field using invariant numeric values.");
        }

        return new Rs50OledConfiguration(
            new Dictionary<DisplayMode, Rs50OledLayout>
            {
                [DisplayMode.Normal] =
                    (Rs50OledLayout)NormalLayoutBox.SelectedItem,
                [DisplayMode.BrakeBias] =
                    (Rs50OledLayout)BrakeBiasLayoutBox.SelectedItem,
                [DisplayMode.LastLap] =
                    (Rs50OledLayout)LastLapLayoutBox.SelectedItem,
                [DisplayMode.ConnectionProblem] =
                    (Rs50OledLayout)ConnectionLayoutBox.SelectedItem
            },
            speedUnit,
            maximumRpm,
            maximumSpeed);
    }

    private double ParseLastLapSeconds()
    {
        if (!double.TryParse(
                LastLapSecondsBox.Text,
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out double seconds) ||
            !double.IsFinite(seconds) ||
            seconds is < 1 or > 15)
        {
            throw new InvalidDataException(
                "Last-lap display duration must be between 1 and 15 seconds.");
        }

        return seconds;
    }

    private void UpdatePreview()
    {
        if (updating)
        {
            return;
        }

        try
        {
            Rs50OledConfiguration configuration = CreateConfiguration();
            Rs50TelemetryFrameFormatter formatter = new(configuration);
            TelemetrySnapshot sample = new()
            {
                ConnectionState = "ERROR",
                IsOnTrack = true,
                Gear = 3,
                Rpm = 6500,
                SpeedMetersPerSecond = 123f / 3.6f,
                BrakeBiasPercent = 52.3f,
                LastLapTimeSeconds = 92.481f
            };
            DisplayMode previewMode =
                PreviewModeBox.SelectedItem is DisplayMode selected
                    ? selected
                    : DisplayMode.Normal;
            Rs50OledFrame frame = formatter.Format(sample, previewMode);
            PreviewText.Text =
                $"{previewMode} [{configuration.LayoutFor(previewMode)}] " +
                Rs50OledFrameDescription.Describe(frame);
            RenderOledPreview(frame);
            StatusText.Text = "Configuration is valid.";
        }
        catch (Exception exception)
        {
            PreviewText.Text = "Preview unavailable.";
            StatusText.Text = exception.Message;
        }
    }

    private void Open_Click(object sender, RoutedEventArgs e)
    {
        OpenFileDialog dialog = new()
        {
            Filter = "LogiDynamicDash configuration (*.json)|*.json",
            CheckFileExists = true
        };
        if (dialog.ShowDialog(this) != true)
        {
            return;
        }

        try
        {
            LoadConfiguration(Rs50OledConfigurationFile.Load(dialog.FileName));
            StatusText.Text = $"Opened {Path.GetFileName(dialog.FileName)}.";
        }
        catch (Exception exception)
        {
            MessageBox.Show(
                this,
                exception.Message,
                "Configuration rejected",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            Rs50OledConfiguration configuration = CreateConfiguration();
            SaveFileDialog dialog = new()
            {
                Filter = "LogiDynamicDash configuration (*.json)|*.json",
                DefaultExt = ".json",
                AddExtension = true,
                FileName = "logidynamicdash.json"
            };
            if (dialog.ShowDialog(this) != true)
            {
                return;
            }

            File.WriteAllText(
                dialog.FileName,
                Rs50OledConfigurationFile.Serialize(configuration));
            StatusText.Text = $"Saved {Path.GetFileName(dialog.FileName)}.";
        }
        catch (Exception exception)
        {
            MessageBox.Show(
                this,
                exception.Message,
                "Configuration not saved",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private void LoadPersistedSettings()
    {
        try
        {
            if (File.Exists(ActiveConfigurationPath))
            {
                LoadConfiguration(
                    Rs50OledConfigurationFile.Load(
                        ActiveConfigurationPath));
            }

            if (!File.Exists(PreferencesPath))
            {
                return;
            }

            using JsonDocument document = JsonDocument.Parse(
                File.ReadAllText(PreferencesPath));
            JsonElement root = document.RootElement;
            if (root.TryGetProperty("automaticProfiles", out var automatic) &&
                automatic.ValueKind is JsonValueKind.True or
                    JsonValueKind.False)
            {
                AutomaticProfilesBox.IsChecked = automatic.GetBoolean();
            }

            if (root.TryGetProperty("lastLapDisplaySeconds", out var duration) &&
                duration.TryGetDouble(out double seconds) &&
                double.IsFinite(seconds) &&
                seconds is >= 1 and <= 15)
            {
                LastLapSecondsBox.Text =
                    seconds.ToString(CultureInfo.InvariantCulture);
            }
        }
        catch (Exception exception)
        {
            StatusText.Text =
                $"Saved settings were ignored: {exception.Message}";
        }
    }

    private void RenderOledPreview(Rs50OledFrame frame)
    {
        OledPreviewSurface.Children.Clear();
        OledPreviewSurface.RowDefinitions.Clear();

        switch (frame)
        {
            case Rs50LayoutAFrame:
                AddCenteredText("LAYOUT A", 22);
                AddCenteredText("NO DATA FIELDS", 12, secondary: true);
                break;
            case Rs50LayoutBFrame:
                AddCenteredText("LAYOUT B", 22);
                AddCenteredText("NO DATA FIELDS", 12, secondary: true);
                break;
            case Rs50LayoutCFrame layout:
                AddCenteredText("RPM", 12, secondary: true);
                AddGauge(layout.MainGauge, 28);
                break;
            case Rs50LayoutDFrame layout:
                AddCenteredText(layout.Text, 28);
                AddGauge(layout.MainGauge, 24);
                AddGauge(layout.ThinIndicator, 7);
                break;
            case Rs50LayoutEFrame layout:
                AddDualText(layout.LeftText, layout.RightText, 30);
                AddGauge(layout.MainGauge, 24);
                AddGauge(layout.ThinIndicator, 7);
                break;
            case Rs50LayoutFFrame layout:
                AddDualText(layout.LeftText, layout.RightText, 42);
                break;
            case Rs50LayoutGFrame layout:
                AddDualText(layout.LeftText, layout.RightText, 32);
                break;
            case Rs50LayoutHFrame layout:
                AddCenteredText(layout.TopText, 19, secondary: true);
                AddCenteredText(layout.BottomText, 34);
                break;
            case Rs50LayoutIFrame layout:
                AddFourRows(
                    layout.Line1,
                    layout.Line2,
                    layout.Line3,
                    layout.Line4,
                    20);
                break;
            case Rs50LayoutJFrame layout:
                AddFourRows(
                    layout.Line1,
                    layout.Line2,
                    layout.Line3,
                    layout.Line4,
                    23);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(frame));
        }
    }

    private void AddCenteredText(
        string text,
        double size,
        bool secondary = false)
    {
        int row = AddPreviewRow();
        TextBlock block = PreviewTextBlock(text, size, secondary);
        block.HorizontalAlignment = HorizontalAlignment.Center;
        Grid.SetRow(block, row);
        OledPreviewSurface.Children.Add(block);
    }

    private void AddDualText(string left, string right, double size)
    {
        int row = AddPreviewRow();
        Grid line = new();
        line.ColumnDefinitions.Add(new ColumnDefinition());
        line.ColumnDefinitions.Add(new ColumnDefinition());
        TextBlock leftBlock = PreviewTextBlock(left, size);
        TextBlock rightBlock = PreviewTextBlock(right, size);
        rightBlock.HorizontalAlignment = HorizontalAlignment.Right;
        Grid.SetColumn(rightBlock, 1);
        line.Children.Add(leftBlock);
        line.Children.Add(rightBlock);
        Grid.SetRow(line, row);
        OledPreviewSurface.Children.Add(line);
    }

    private void AddGauge(Rs50GaugeLevel gauge, double height)
    {
        int row = AddPreviewRow();
        ProgressBar bar = new()
        {
            Minimum = 0,
            Maximum = byte.MaxValue,
            Value = gauge.WireValue,
            Height = height,
            Margin = new Thickness(0, 5, 0, 2),
            Foreground = OledBlue(),
            Background = new SolidColorBrush(Color.FromRgb(20, 43, 52))
        };
        Grid.SetRow(bar, row);
        OledPreviewSurface.Children.Add(bar);
    }

    private void AddFourRows(
        string line1,
        string line2,
        string line3,
        string line4,
        double size)
    {
        string[] lines = [line1, line2, line3, line4];
        foreach ((string line, int index) in lines.Select(
                     (line, index) => (line, index)))
        {
            AddCenteredText(
                string.IsNullOrEmpty(line) ? " " : line,
                index % 2 == 0 ? size * 0.72 : size,
                secondary: index % 2 == 0);
        }
    }

    private int AddPreviewRow()
    {
        int row = OledPreviewSurface.RowDefinitions.Count;
        OledPreviewSurface.RowDefinitions.Add(
            new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        return row;
    }

    private static TextBlock PreviewTextBlock(
        string text,
        double size,
        bool secondary = false) =>
        new()
        {
            Text = text,
            FontFamily = new FontFamily("Consolas"),
            FontSize = size,
            FontWeight = secondary ? FontWeights.Normal : FontWeights.SemiBold,
            Foreground = secondary
                ? new SolidColorBrush(Color.FromRgb(142, 221, 234))
                : OledBlue(),
            VerticalAlignment = VerticalAlignment.Center,
            TextTrimming = TextTrimming.CharacterEllipsis
        };

    private static SolidColorBrush OledBlue() =>
        new(Color.FromRgb(93, 235, 255));

    private void SavePreferences()
    {
        Directory.CreateDirectory(ApplicationDataDirectory);
        File.WriteAllText(
            PreferencesPath,
            JsonSerializer.Serialize(
                new
                {
                    schemaVersion = 1,
                    automaticProfiles =
                        AutomaticProfilesBox.IsChecked == true,
                    lastLapDisplaySeconds = ParseLastLapSeconds()
                },
                new JsonSerializerOptions { WriteIndented = true }) +
            Environment.NewLine);
    }

    private void PersistSettingsBestEffort()
    {
        try
        {
            Directory.CreateDirectory(ApplicationDataDirectory);
            File.WriteAllText(
                ActiveConfigurationPath,
                Rs50OledConfigurationFile.Serialize(
                    CreateConfiguration()));
            SavePreferences();
        }
        catch
        {
            // Closing must not be blocked by an invalid draft field.
        }
    }

    private void ShowRuntimeError(Exception exception)
    {
        RuntimeStateText.Text = "Dashboard stopped safely";
        RuntimeDetailText.Text = exception.Message;
        StartDashboardButton.IsEnabled = true;
        StopDashboardButton.IsEnabled = false;
        MessageBox.Show(
            this,
            exception.Message,
            "Dashboard stopped safely",
            MessageBoxButton.OK,
            MessageBoxImage.Error);
    }

    private void ShowConfigurationError(
        Exception exception,
        string title) =>
        MessageBox.Show(
            this,
            exception.Message,
            title,
            MessageBoxButton.OK,
            MessageBoxImage.Error);
}
