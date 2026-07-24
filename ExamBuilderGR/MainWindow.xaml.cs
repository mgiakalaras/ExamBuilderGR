using System.ComponentModel;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Threading;
using ExamBuilderGR.Models;
using ExamBuilderGR.Services;
using ExamBuilderGR.ViewModels;
using ExamBuilderGR.Views;

namespace ExamBuilderGR;

public partial class MainWindow : Window
{
    private readonly ExamDocumentRenderer _documentRenderer = new();
    private readonly ExamValidationService _validationService = new();
    private readonly DispatcherTimer _previewTimer;
    private readonly MainViewModel _viewModel;
    private bool _forceClose;

    public MainWindow()
    {
        InitializeComponent();

        _previewTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(180)
        };
        _previewTimer.Tick += PreviewTimer_Tick;

        _viewModel = new MainViewModel();
        DataContext = _viewModel;
        _viewModel.PreviewInvalidated += ViewModel_PreviewInvalidated;

        Loaded += MainWindow_Loaded;
        Closing += MainWindow_Closing;
        Closed += MainWindow_Closed;
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e) => RebuildPreview();


    private void ToolbarMenuButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not System.Windows.Controls.Button button || button.ContextMenu is null) return;

        button.ContextMenu.PlacementTarget = button;
        button.ContextMenu.Placement = PlacementMode.Bottom;
        button.ContextMenu.HorizontalOffset = 0;
        button.ContextMenu.VerticalOffset = 4;
        button.ContextMenu.DataContext = DataContext;
        button.ContextMenu.IsOpen = true;
    }

    private async void MainWindow_Closing(object? sender, CancelEventArgs e)
    {
        if (_forceClose || !_viewModel.HasUnsavedChanges) return;

        // Cancel the current close request first. After the user's choice we issue a second,
        // intentional close with _forceClose=true. This avoids async Closing race conditions.
        e.Cancel = true;

        var result = MessageBox.Show(this,
            "Υπάρχουν μη αποθηκευμένες αλλαγές. Να αποθηκευτεί το διαγώνισμα πριν κλείσει η εφαρμογή;",
            "Μη αποθηκευμένες αλλαγές",
            MessageBoxButton.YesNoCancel,
            MessageBoxImage.Warning);

        if (result == MessageBoxResult.Cancel) return;

        if (result == MessageBoxResult.Yes && !await _viewModel.SaveAsync()) return;

        _forceClose = true;
        Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(Close));
    }

    private void MainWindow_Closed(object? sender, EventArgs e)
    {
        _previewTimer.Stop();
        _viewModel.PreviewInvalidated -= ViewModel_PreviewInvalidated;
    }

    private void ViewModel_PreviewInvalidated(object? sender, EventArgs e)
    {
        if (!_viewModel.Exam.GenerateAnswerKey && AnswerKeyPreviewToggle.IsChecked == true)
            AnswerKeyPreviewToggle.IsChecked = false;

        _previewTimer.Stop();
        _previewTimer.Start();
    }

    private void PreviewTimer_Tick(object? sender, EventArgs e)
    {
        _previewTimer.Stop();
        RebuildPreview();
    }

    private void RebuildPreview()
    {
        try
        {
            PreviewViewer.Document = AnswerKeyPreviewToggle.IsChecked == true && _viewModel.Exam.GenerateAnswerKey
                ? _documentRenderer.CreateAnswerKeyDocument(_viewModel.Exam, _viewModel.School)
                : _documentRenderer.CreateDocument(_viewModel.Exam, _viewModel.School);
        }
        catch (Exception ex)
        {
            _viewModel.StatusMessage = $"Η προεπισκόπηση δεν ανανεώθηκε: {ex.Message}";
        }
    }

    private void AnswerKeyPreviewToggle_Changed(object sender, RoutedEventArgs e)
    {
        if (IsLoaded) RebuildPreview();
    }

    private void UseSelectedBlank_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not System.Windows.Controls.Button button ||
            button.DataContext is not FillBlankSentence sentence ||
            button.Tag is not System.Windows.Controls.TextBox editor) return;

        var selectedText = editor.SelectedText?.Trim();
        if (string.IsNullOrWhiteSpace(selectedText))
        {
            MessageBox.Show(this,
                "Επίλεξε πρώτα με το ποντίκι μία λέξη ή φράση μέσα στην πρόταση και πάτησε ξανά το κουμπί.",
                "Συμπλήρωση κενού", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (selectedText.Contains("[[", StringComparison.Ordinal) ||
            selectedText.Contains("]]", StringComparison.Ordinal))
        {
            MessageBox.Show(this,
                "Η επιλογή περιλαμβάνει ήδη δείκτη κενού. Επίλεξε μόνο καθαρό κείμενο που δεν έχει γίνει κενό.",
                "Συμπλήρωση κενού", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var start = editor.SelectionStart;
        var length = editor.SelectionLength;
        var current = editor.Text ?? string.Empty;
        var replacement = $"[[{current.Substring(start, length)}]]";
        sentence.MarkedText = current[..start] + replacement + current[(start + length)..];

        editor.Focus();
        editor.CaretIndex = Math.Min(sentence.MarkedText.Length, start + replacement.Length);
        _viewModel.StatusMessage = $"Δημιουργήθηκε νέο κενό με απάντηση «{selectedText}».";
        RebuildPreview();
    }

    private void RestoreFillBlankMarkers_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not System.Windows.Controls.Button button ||
            button.DataContext is not FillBlankSentence sentence) return;

        sentence.MarkedText = sentence.OriginalText;
        _viewModel.StatusMessage = "Επαναφέρθηκαν όλες οι λέξεις της πρότασης.";
        RebuildPreview();
    }

    private bool RunPreflight(bool forAnswerKey = false)
    {
        var issues = _validationService.Validate(_viewModel.Exam, _viewModel.School, forAnswerKey);
        var dialog = new PreflightWindow(issues)
        {
            Owner = this
        };
        return dialog.ShowDialog() == true;
    }

    private void Preflight_Click(object sender, RoutedEventArgs e)
    {
        var issues = _validationService.Validate(_viewModel.Exam, _viewModel.School,
            AnswerKeyPreviewToggle.IsChecked == true);
        var dialog = new PreflightWindow(issues)
        {
            Owner = this
        };
        dialog.ShowDialog();
    }

    private void Print_Click(object sender, RoutedEventArgs e)
    {
        if (!RunPreflight()) return;
        try
        {
            if (_documentRenderer.PrintExam(_viewModel.Exam, _viewModel.School))
                _viewModel.StatusMessage = "Το διαγώνισμα στάλθηκε για εκτύπωση.";
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Σφάλμα εκτύπωσης", MessageBoxButton.OK, MessageBoxImage.Error);
            _viewModel.StatusMessage = "Η εκτύπωση απέτυχε.";
        }
    }

    private void ExportPdf_Click(object sender, RoutedEventArgs e)
    {
        if (!RunPreflight()) return;

        try
        {
            if (_documentRenderer.ExportPdf(_viewModel.Exam, _viewModel.School, this))
                _viewModel.StatusMessage = "Το διαγώνισμα στάλθηκε στον Microsoft Print to PDF.";
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Σφάλμα εξαγωγής PDF", MessageBoxButton.OK, MessageBoxImage.Error);
            _viewModel.StatusMessage = "Η εξαγωγή PDF απέτυχε.";
        }
    }

    private void PrintAnswerKey_Click(object sender, RoutedEventArgs e)
    {
        if (!RunPreflight(forAnswerKey: true)) return;

        try
        {
            if (_documentRenderer.PrintAnswerKey(_viewModel.Exam, _viewModel.School))
                _viewModel.StatusMessage = "Το κλειδί λύσεων στάλθηκε για εκτύπωση.";
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Σφάλμα εκτύπωσης λύσεων", MessageBoxButton.OK, MessageBoxImage.Error);
            _viewModel.StatusMessage = "Η εκτύπωση του κλειδιού απέτυχε.";
        }
    }

    private void ExportAnswerKeyPdf_Click(object sender, RoutedEventArgs e)
    {
        if (!RunPreflight(forAnswerKey: true)) return;

        try
        {
            if (_documentRenderer.ExportAnswerKeyPdf(_viewModel.Exam, _viewModel.School, this))
                _viewModel.StatusMessage = "Το κλειδί λύσεων στάλθηκε στον Microsoft Print to PDF.";
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Σφάλμα PDF λύσεων", MessageBoxButton.OK, MessageBoxImage.Error);
            _viewModel.StatusMessage = "Η εξαγωγή του κλειδιού απέτυχε.";
        }
    }
    private void OpenSectionSettings_Click(object sender, RoutedEventArgs e)
    {
        ExamSection? section = null;

        if (sender is FrameworkElement element)
        {
            section = element.Tag as ExamSection ?? element.DataContext as ExamSection;
        }

        section ??= _viewModel.SelectedSection;
        if (section is null) return;

        _viewModel.SelectedSection = section;

        var dialog = new SectionSettingsWindow(section)
        {
            Owner = this
        };

        dialog.ShowDialog();
        RebuildPreview();
    }

}
