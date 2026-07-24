using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using ExamBuilderGR.Models;
using ExamBuilderGR.Services;
using Microsoft.Win32;

namespace ExamBuilderGR.Views;

public sealed record ImagePlacementOption(ExamImagePlacement Value, string Label);
public sealed record ImageAlignmentOption(ExamImageAlignment Value, string Label);
public sealed record ImageLayoutOption(ExamImageLayout Value, string Label);
public sealed record ImageLabelOption(ExamImageLabelStyle Value, string Label);

public partial class ImageEditorControl : UserControl
{
    public static readonly DependencyProperty ImagesProperty = DependencyProperty.Register(
        nameof(Images), typeof(ObservableCollection<ExamImageAsset>), typeof(ImageEditorControl),
        new PropertyMetadata(null, ImagesChanged));

    public static readonly DependencyProperty TitleProperty = DependencyProperty.Register(
        nameof(Title), typeof(string), typeof(ImageEditorControl), new PropertyMetadata("Εικόνες και διαγράμματα"));

    public ObservableCollection<ExamImageAsset>? Images
    {
        get => (ObservableCollection<ExamImageAsset>?)GetValue(ImagesProperty);
        set => SetValue(ImagesProperty, value);
    }

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public IReadOnlyList<ImagePlacementOption> PlacementOptions { get; } =
    [
        new(ExamImagePlacement.BeforeText, "Πριν από την εκφώνηση"),
        new(ExamImagePlacement.AfterText, "Μετά την εκφώνηση"),
        new(ExamImagePlacement.EndOfItem, "Στο τέλος")
    ];

    public IReadOnlyList<ImageAlignmentOption> AlignmentOptions { get; } =
    [
        new(ExamImageAlignment.Left, "Αριστερά"),
        new(ExamImageAlignment.Center, "Κέντρο"),
        new(ExamImageAlignment.Right, "Δεξιά")
    ];

    public IReadOnlyList<ImageLayoutOption> LayoutOptions { get; } =
    [
        new(ExamImageLayout.Standalone, "Μόνη της"),
        new(ExamImageLayout.InlineRow, "Δίπλα στην ίδια σειρά")
    ];

    public IReadOnlyList<ImageLabelOption> LabelOptions { get; } =
    [
        new(ExamImageLabelStyle.None, "Χωρίς αρίθμηση"),
        new(ExamImageLabelStyle.GreekLetters, "α), β), γ), δ)"),
        new(ExamImageLabelStyle.FigureNumbers, "Εικόνα 1, Εικόνα 2…")
    ];

    public ImageEditorControl()
    {
        InitializeComponent();
    }

    private static void ImagesChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not ImageEditorControl control || control.ImagesList is null) return;
        control.ImagesList.SelectedItem = control.Images?.FirstOrDefault();
    }

    private void AddImage_Click(object sender, RoutedEventArgs e)
    {
        if (Images is null)
        {
            MessageBox.Show(Window.GetWindow(this), "Δεν υπάρχει επιλεγμένο θέμα ή ερώτηση.",
                "Προσθήκη εικόνας", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var dialog = new OpenFileDialog
        {
            Title = "Επιλογή εικόνας ή διαγράμματος",
            Filter = "Εικόνες|*.png;*.jpg;*.jpeg;*.bmp;*.gif|PNG|*.png|JPEG|*.jpg;*.jpeg|Bitmap|*.bmp|GIF|*.gif",
            Multiselect = true
        };

        if (dialog.ShowDialog(Window.GetWindow(this)) != true) return;

        var failed = new List<string>();
        ExamImageAsset? lastAdded = null;
        var addAsRow = dialog.FileNames.Length > 1;
        var newRowGroup = Images.Count == 0 ? 1 : Images.Max(image => image.RowGroup) + 1;

        foreach (var file in dialog.FileNames)
        {
            try
            {
                lastAdded = ExamImageService.CreateFromFile(file);
                if (addAsRow)
                {
                    lastAdded.Layout = ExamImageLayout.InlineRow;
                    lastAdded.RowGroup = newRowGroup;
                    lastAdded.LabelStyle = ExamImageLabelStyle.GreekLetters;
                    lastAdded.WidthCm = 7.0;
                }
                Images.Add(lastAdded);
            }
            catch (Exception ex)
            {
                failed.Add($"{System.IO.Path.GetFileName(file)}: {ex.Message}");
            }
        }

        if (lastAdded is not null)
        {
            ImagesList.SelectedItem = lastAdded;
            ImagesList.ScrollIntoView(lastAdded);
        }

        if (failed.Count > 0)
        {
            MessageBox.Show(Window.GetWindow(this), string.Join(Environment.NewLine, failed),
                "Ορισμένες εικόνες δεν προστέθηκαν", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void DeleteImage_Click(object sender, RoutedEventArgs e)
    {
        if (Images is null || ImagesList.SelectedItem is not ExamImageAsset image) return;
        var index = Images.IndexOf(image);
        Images.Remove(image);
        if (Images.Count > 0)
            ImagesList.SelectedIndex = Math.Clamp(index, 0, Images.Count - 1);
    }

    private void MoveUp_Click(object sender, RoutedEventArgs e)
    {
        if (Images is null || ImagesList.SelectedItem is not ExamImageAsset image) return;
        var index = Images.IndexOf(image);
        if (index <= 0) return;
        Images.Move(index, index - 1);
        ImagesList.SelectedItem = image;
        ImagesList.ScrollIntoView(image);
    }

    private void MoveDown_Click(object sender, RoutedEventArgs e)
    {
        if (Images is null || ImagesList.SelectedItem is not ExamImageAsset image) return;
        var index = Images.IndexOf(image);
        if (index < 0 || index >= Images.Count - 1) return;
        Images.Move(index, index + 1);
        ImagesList.SelectedItem = image;
        ImagesList.ScrollIntoView(image);
    }

    private void ImagesList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        // The selected item's properties are edited in the compact panel below the gallery.
    }
}
