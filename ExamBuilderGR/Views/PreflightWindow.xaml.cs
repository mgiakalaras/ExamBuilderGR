using System.Collections.ObjectModel;
using System.Windows;
using ExamBuilderGR.Models;

namespace ExamBuilderGR.Views;

public partial class PreflightWindow : Window
{
    public ObservableCollection<ValidationIssue> Issues { get; }
    public int ErrorCount { get; }
    public int WarningCount { get; }
    public bool CanProceed => ErrorCount == 0;
    public string SummaryText => ErrorCount > 0
        ? "Ο προέλεγχος εντόπισε σφάλματα που χρειάζονται διόρθωση."
        : WarningCount > 0
            ? "Μπορείς να συνεχίσεις, αλλά αξίζει να ελέγξεις τις προειδοποιήσεις."
            : "Ο προέλεγχος ολοκληρώθηκε χωρίς προβλήματα.";

    public PreflightWindow(IEnumerable<ValidationIssue> issues)
    {
        InitializeComponent();
        Issues = new ObservableCollection<ValidationIssue>(issues);
        ErrorCount = Issues.Count(issue => issue.Severity == ValidationSeverity.Error);
        WarningCount = Issues.Count(issue => issue.Severity == ValidationSeverity.Warning);
        DataContext = this;
    }

    private void Continue_Click(object sender, RoutedEventArgs e)
    {
        if (!CanProceed) return;
        DialogResult = true;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;
}
