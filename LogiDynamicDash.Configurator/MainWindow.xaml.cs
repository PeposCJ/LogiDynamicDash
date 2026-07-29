using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using LogiDynamicDash.Configuration;
using LogiDynamicDash.Displays;
using LogiDynamicDash.Models;
using LogiDynamicDash.Offline;
using Microsoft.Win32;

namespace LogiDynamicDash.Configurator;

public partial class MainWindow : Window
{
    private readonly ComboBox[] layoutBoxes;
    private bool updating;

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

        DisciplineBox.ItemsSource = Enum.GetValues<DrivingDiscipline>();
        SpeedUnitBox.ItemsSource = Enum.GetValues<SpeedUnit>();
        foreach (ComboBox box in layoutBoxes)
        {
            box.ItemsSource = Enum.GetValues<Rs50OledLayout>();
            box.SelectionChanged += (_, _) => UpdatePreview();
        }

        DisciplineBox.SelectionChanged += (_, _) => UpdateRecommendation();
        SpeedUnitBox.SelectionChanged += (_, _) => UpdateRecommendation();
        MaximumRpmBox.TextChanged += (_, _) => UpdatePreview();
        GaugeSpeedBox.TextChanged += (_, _) => UpdatePreview();

        DisciplineBox.SelectedItem = DrivingDiscipline.Road;
        SpeedUnitBox.SelectedItem = SpeedUnit.KilometersPerHour;
        ApplyRecommendation(DrivingDiscipline.Road);
    }

    private void ApplyRecommendation_Click(object sender, RoutedEventArgs e)
    {
        if (DisciplineBox.SelectedItem is DrivingDiscipline discipline)
        {
            ApplyRecommendation(discipline);
        }
    }

    private void ApplyRecommendation(DrivingDiscipline discipline)
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
            DisciplineBox.SelectedItem is not DrivingDiscipline discipline ||
            SpeedUnitBox.SelectedItem is not SpeedUnit unit)
        {
            return;
        }

        RecommendationText.Text =
            DisciplineProfileRecommendations.Create(discipline, unit).Summary;
        UpdatePreview();
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
            PreviewText.Text = string.Join(
                Environment.NewLine,
                Enum.GetValues<DisplayMode>().Select(mode =>
                    $"{mode,-18} [{configuration.LayoutFor(mode)}] " +
                    Rs50OledFrameDescription.Describe(
                        formatter.Format(sample, mode))));
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
}
