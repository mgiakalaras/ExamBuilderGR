using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ExamBuilderGR.Models;
using ExamBuilderGR.Services;

namespace ExamBuilderGR.Views;

public partial class QuestionLibraryWindow : Window
{
    private readonly QuestionBankService _service;
    private readonly ObservableCollection<QuestionBankEntry> _visibleEntries = new();
    private IReadOnlyList<QuestionBankEntry> _allEntries = Array.Empty<QuestionBankEntry>();

    public ExamQuestion? SelectedQuestion { get; private set; }

    public QuestionLibraryWindow(QuestionBankService service)
    {
        InitializeComponent();
        _service = service;
        QuestionsListBox.ItemsSource = _visibleEntries;
        Loaded += QuestionLibraryWindow_Loaded;
    }

    private async void QuestionLibraryWindow_Loaded(object sender, RoutedEventArgs e) => await ReloadAsync();

    private async Task ReloadAsync()
    {
        _allEntries = await _service.LoadEntriesAsync();

        var subjects = _allEntries
            .Select(entry => entry.Subject)
            .Where(subject => !string.IsNullOrWhiteSpace(subject))
            .Distinct(StringComparer.CurrentCultureIgnoreCase)
            .OrderBy(subject => subject, StringComparer.CurrentCultureIgnoreCase)
            .ToList();

        SubjectFilterComboBox.Items.Clear();
        SubjectFilterComboBox.Items.Add("Όλα τα μαθήματα");
        foreach (var subject in subjects) SubjectFilterComboBox.Items.Add(subject);
        SubjectFilterComboBox.SelectedIndex = 0;

        TypeFilterComboBox.Items.Clear();
        TypeFilterComboBox.Items.Add("Όλοι οι τύποι");
        foreach (var type in Enum.GetValues<QuestionType>()) TypeFilterComboBox.Items.Add(GetTypeLabel(type));
        TypeFilterComboBox.SelectedIndex = 0;

        ApplyFilter();
    }

    private void ApplyFilter()
    {
        if (SearchTextBox is null || SubjectFilterComboBox is null || TypeFilterComboBox is null) return;

        var search = SearchTextBox.Text.Trim();
        var subject = SubjectFilterComboBox.SelectedItem as string;
        var type = TypeFilterComboBox.SelectedItem as string;
        var filterSubject = !string.IsNullOrWhiteSpace(subject) && subject != "Όλα τα μαθήματα";
        var filterType = !string.IsNullOrWhiteSpace(type) && type != "Όλοι οι τύποι";

        var filtered = _allEntries.Where(entry =>
        {
            var matchesSubject = !filterSubject ||
                                 string.Equals(entry.Subject, subject, StringComparison.CurrentCultureIgnoreCase);
            var matchesType = !filterType ||
                              string.Equals(entry.TypeLabel, type, StringComparison.CurrentCultureIgnoreCase);
            var matchesSearch = string.IsNullOrWhiteSpace(search) ||
                                entry.Title.Contains(search, StringComparison.CurrentCultureIgnoreCase) ||
                                entry.Subject.Contains(search, StringComparison.CurrentCultureIgnoreCase) ||
                                entry.Grade.Contains(search, StringComparison.CurrentCultureIgnoreCase) ||
                                entry.Category.Contains(search, StringComparison.CurrentCultureIgnoreCase) ||
                                entry.Tags.Contains(search, StringComparison.CurrentCultureIgnoreCase) ||
                                entry.Question.Text.Contains(search, StringComparison.CurrentCultureIgnoreCase);
            return matchesSubject && matchesType && matchesSearch;
        });

        var previous = QuestionsListBox.SelectedItem as QuestionBankEntry;
        _visibleEntries.Clear();
        foreach (var entry in filtered) _visibleEntries.Add(entry);
        QuestionsListBox.SelectedItem = previous is not null && _visibleEntries.Contains(previous)
            ? previous
            : _visibleEntries.FirstOrDefault();

        CountTextBlock.Text = _visibleEntries.Count == 1
            ? "1 αποθηκευμένη ερώτηση"
            : $"{_visibleEntries.Count} αποθηκευμένες ερωτήσεις";
    }

    private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e) => ApplyFilter();
    private void FilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e) => ApplyFilter();
    private void QuestionsListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e) => UseSelected();
    private void Use_Click(object sender, RoutedEventArgs e) => UseSelected();

    private void UseSelected()
    {
        if (QuestionsListBox.SelectedItem is not QuestionBankEntry entry)
        {
            MessageBox.Show(this, "Επίλεξε πρώτα μία ερώτηση.", "Βιβλιοθήκη ερωτήσεων",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        SelectedQuestion = _service.CreateQuestion(entry);
        DialogResult = true;
    }

    private async void Delete_Click(object sender, RoutedEventArgs e)
    {
        if (QuestionsListBox.SelectedItem is not QuestionBankEntry entry) return;

        var answer = MessageBox.Show(this,
            $"Να διαγραφεί οριστικά η ερώτηση «{entry.Title}» από τη βιβλιοθήκη;",
            "Διαγραφή ερώτησης", MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (answer != MessageBoxResult.Yes) return;

        _service.DeleteEntry(entry);
        await ReloadAsync();
    }

    private void OpenFolder_Click(object sender, RoutedEventArgs e) => _service.OpenLibraryFolder();
    private void Close_Click(object sender, RoutedEventArgs e) => DialogResult = false;

    private static string GetTypeLabel(QuestionType type) => type switch
    {
        QuestionType.Development => "Ανάπτυξης",
        QuestionType.TrueFalse => "Σωστό / Λάθος",
        QuestionType.Matching => "Αντιστοίχισης",
        QuestionType.FillBlank => "Συμπλήρωσης κενού",
        QuestionType.MultipleChoice => "Πολλαπλής επιλογής",
        _ => type.ToString()
    };
}
