using System.IO;
using System.Windows;
using ExamBuilderGR.Models;
using ExamBuilderGR.Services;
using Microsoft.Win32;

namespace ExamBuilderGR.Views;

public partial class SchoolSettingsWindow : Window
{
    private readonly SchoolLogoService _logoService = new();

    public SchoolProfile Profile { get; }

    public SchoolSettingsWindow(SchoolProfile profile)
    {
        Profile = profile.Clone();
        InitializeComponent();

        ThemeComboBox.ItemsSource = ThemeManager.AvailableThemes;
        LogoPositionComboBox.ItemsSource = new[] { "Αριστερά", "Κέντρο" };
        LogoWidthComboBox.ItemsSource = new[] { 2.0d, 2.4d, 2.8d, 3.2d, 3.6d, 4.0d };
        DataContext = this;
        UpdateLogoPreview();
    }

    private void SelectLogo_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "Επιλογή λογοτύπου σχολείου",
            Filter = "Αρχεία εικόνας (*.png;*.jpg;*.jpeg;*.bmp)|*.png;*.jpg;*.jpeg;*.bmp|Όλα τα αρχεία (*.*)|*.*",
            CheckFileExists = true,
            Multiselect = false
        };

        if (dialog.ShowDialog(this) != true) return;

        try
        {
            Profile.SchoolLogoPath = _logoService.Import(dialog.FileName);
            Profile.ShowSchoolLogo = true;
            ShowLogoCheckBox.IsChecked = true;
            UpdateLogoPreview();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Λογότυπο σχολείου",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ClearLogo_Click(object sender, RoutedEventArgs e)
    {
        Profile.SchoolLogoPath = string.Empty;
        Profile.ShowSchoolLogo = false;
        ShowLogoCheckBox.IsChecked = false;
        UpdateLogoPreview();
    }

    private void UpdateLogoPreview()
    {
        var source = SchoolLogoService.LoadImage(Profile.SchoolLogoPath, Profile.SchoolLogoGrayscale);
        SchoolLogoPreview.Source = source;
        NoLogoText.Visibility = source is null ? Visibility.Visible : Visibility.Collapsed;
        LogoPathTextBlock.Text = Profile.SchoolLogoPath;
    }

    private void LogoPreviewOption_Changed(object sender, RoutedEventArgs e)
    {
        if (!IsLoaded) return;
        Profile.SchoolLogoGrayscale = GrayscaleLogoCheckBox.IsChecked == true;
        UpdateLogoPreview();
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(Profile.SchoolName) || string.IsNullOrWhiteSpace(Profile.SchoolYear))
        {
            MessageBox.Show("Η ονομασία σχολείου και το σχολικό έτος είναι υποχρεωτικά.",
                "Ρυθμίσεις σχολείου", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (Profile.ShowSchoolLogo && !string.IsNullOrWhiteSpace(Profile.SchoolLogoPath) &&
            !File.Exists(Profile.SchoolLogoPath))
        {
            MessageBox.Show(this,
                "Το αποθηκευμένο λογότυπο δεν βρέθηκε. Επίλεξε ξανά την εικόνα ή πάτησε Καθαρισμός.",
                "Λογότυπο σχολείου", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        DialogResult = true;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;
}
