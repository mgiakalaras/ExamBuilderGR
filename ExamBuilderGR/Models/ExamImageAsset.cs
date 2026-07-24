using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using System.Windows.Media;
using ExamBuilderGR.Services;

namespace ExamBuilderGR.Models;

public enum ExamImagePlacement
{
    BeforeText,
    AfterText,
    EndOfItem
}

public enum ExamImageAlignment
{
    Left,
    Center,
    Right
}

/// <summary>
/// Controls whether an image is printed as a full-width item or shares a row
/// with other images that have the same placement and row-group number.
/// </summary>
public enum ExamImageLayout
{
    Standalone,
    InlineRow
}

/// <summary>
/// Optional automatic labels that are added below the image in the generated
/// exam, PDF and answer key.
/// </summary>
public enum ExamImageLabelStyle
{
    None,
    GreekLetters,
    FigureNumbers
}

public sealed class ExamImageAsset : INotifyPropertyChanged
{
    private Guid _id = Guid.NewGuid();
    private string _fileName = string.Empty;
    private string _mimeType = "image/png";
    private string _dataBase64 = string.Empty;
    private string _caption = string.Empty;
    private double _widthCm = 8.0;
    private ExamImagePlacement _placement = ExamImagePlacement.AfterText;
    private ExamImageAlignment _alignment = ExamImageAlignment.Center;
    private ExamImageLayout _layout = ExamImageLayout.Standalone;
    private ExamImageLabelStyle _labelStyle = ExamImageLabelStyle.None;
    private int _rowGroup = 1;
    private bool _showInAnswerKey = true;
    private int _pixelWidth;
    private int _pixelHeight;
    private ImageSource? _previewSource;

    public Guid Id { get => _id; set => Set(ref _id, value == Guid.Empty ? Guid.NewGuid() : value); }
    public string FileName { get => _fileName; set => Set(ref _fileName, value ?? string.Empty); }
    public string MimeType { get => _mimeType; set => Set(ref _mimeType, value ?? "image/png"); }

    public string DataBase64
    {
        get => _dataBase64;
        set
        {
            if (!Set(ref _dataBase64, value ?? string.Empty)) return;
            _previewSource = null;
            Raise(nameof(PreviewSource));
            Raise(nameof(Details));
        }
    }

    public string Caption { get => _caption; set => Set(ref _caption, value ?? string.Empty); }

    public double WidthCm
    {
        get => _widthCm;
        set => Set(ref _widthCm, Math.Clamp(value, 2.0, 17.5));
    }

    public ExamImagePlacement Placement { get => _placement; set => Set(ref _placement, value); }
    public ExamImageAlignment Alignment { get => _alignment; set => Set(ref _alignment, value); }
    public ExamImageLayout Layout { get => _layout; set => Set(ref _layout, value); }
    public ExamImageLabelStyle LabelStyle { get => _labelStyle; set => Set(ref _labelStyle, value); }

    /// <summary>
    /// Images with InlineRow layout, the same placement and the same RowGroup
    /// are rendered next to each other. Up to four images are placed per line.
    /// </summary>
    public int RowGroup
    {
        get => _rowGroup;
        set => Set(ref _rowGroup, Math.Clamp(value, 1, 20));
    }

    public bool ShowInAnswerKey { get => _showInAnswerKey; set => Set(ref _showInAnswerKey, value); }

    public int PixelWidth
    {
        get => _pixelWidth;
        set
        {
            if (!Set(ref _pixelWidth, Math.Max(0, value))) return;
            Raise(nameof(Details));
        }
    }

    public int PixelHeight
    {
        get => _pixelHeight;
        set
        {
            if (!Set(ref _pixelHeight, Math.Max(0, value))) return;
            Raise(nameof(Details));
        }
    }

    [JsonIgnore]
    public ImageSource? PreviewSource => _previewSource ??= ExamImageService.CreateBitmapSource(DataBase64);

    [JsonIgnore]
    public string Details
    {
        get
        {
            var encodedBytes = string.IsNullOrWhiteSpace(DataBase64) ? 0 : DataBase64.Length * 3L / 4L;
            var sizeText = encodedBytes < 1024 * 1024
                ? $"{Math.Max(1L, encodedBytes / 1024)} KB"
                : $"{encodedBytes / 1024d / 1024d:0.0} MB";
            var pixelText = PixelWidth > 0 && PixelHeight > 0 ? $"{PixelWidth}×{PixelHeight} px" : "Άγνωστη ανάλυση";
            return $"{pixelText} · {sizeText}";
        }
    }

    [JsonIgnore]
    public string LayoutSummary => Layout == ExamImageLayout.InlineRow
        ? $"Δίπλα · ομάδα {RowGroup}"
        : "Μόνη της";

    public event PropertyChangedEventHandler? PropertyChanged;

    private bool Set<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        Raise(name);
        if (name is nameof(Layout) or nameof(RowGroup)) Raise(nameof(LayoutSummary));
        return true;
    }

    private void Raise([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
