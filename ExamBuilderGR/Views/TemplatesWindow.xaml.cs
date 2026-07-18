using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ExamBuilderGR.Models;
using ExamBuilderGR.Services;

namespace ExamBuilderGR.Views;

public partial class TemplatesWindow : Window
{
    private readonly ExamTemplateService _service;
    private readonly ObservableCollection<ExamTemplate> _visibleTemplates = new();
    private IReadOnlyList<ExamTemplate> _allTemplates = Array.Empty<ExamTemplate>();

    public ExamDocument? CreatedExam { get; private set; }

    public TemplatesWindow(ExamTemplateService service)
    {
        InitializeComponent();
        _service = service;
        TemplatesListBox.ItemsSource = _visibleTemplates;
        Loaded += TemplatesWindow_Loaded;
    }

    private async void TemplatesWindow_Loaded(object sender, RoutedEventArgs e) => await ReloadAsync();

    private async Task ReloadAsync()
    {
        _allTemplates = await _service.LoadTemplatesAsync();

        var subjects = _allTemplates
            .Select(template => template.Exam.Subject)
            .Where(subject => !string.IsNullOrWhiteSpace(subject))
            .Distinct(StringComparer.CurrentCultureIgnoreCase)
            .OrderBy(subject => subject, StringComparer.CurrentCultureIgnoreCase)
            .ToList();

        SubjectFilterComboBox.Items.Clear();
        SubjectFilterComboBox.Items.Add("Όλα τα μαθήματα");
        foreach (var subject in subjects) SubjectFilterComboBox.Items.Add(subject);
        SubjectFilterComboBox.SelectedIndex = 0;
        ApplyFilter();
    }

    private void ApplyFilter()
    {
        var search = SearchTextBox.Text.Trim();
        var selectedSubject = SubjectFilterComboBox.SelectedItem as string;
        var filterBySubject = !string.IsNullOrWhiteSpace(selectedSubject) && selectedSubject != "Όλα τα μαθήματα";

        var filtered = _allTemplates.Where(template =>
        {
            var matchesSubject = !filterBySubject ||
                                 string.Equals(template.Exam.Subject, selectedSubject, StringComparison.CurrentCultureIgnoreCase);
            var matchesSearch = string.IsNullOrWhiteSpace(search) ||
                                template.Name.Contains(search, StringComparison.CurrentCultureIgnoreCase) ||
                                template.Description.Contains(search, StringComparison.CurrentCultureIgnoreCase) ||
                                template.Exam.Subject.Contains(search, StringComparison.CurrentCultureIgnoreCase) ||
                                template.Exam.Grade.Contains(search, StringComparison.CurrentCultureIgnoreCase);
            return matchesSubject && matchesSearch;
        });

        var selected = TemplatesListBox.SelectedItem as ExamTemplate;
        _visibleTemplates.Clear();
        foreach (var template in filtered) _visibleTemplates.Add(template);

        TemplatesListBox.SelectedItem = selected is not null && _visibleTemplates.Contains(selected)
            ? selected
            : _visibleTemplates.FirstOrDefault();

        CountTextBlock.Text = _visibleTemplates.Count == 1
            ? "1 διαθέσιμο πρότυπο"
            : $"{_visibleTemplates.Count} διαθέσιμα πρότυπα";
    }

    private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e) => ApplyFilter();
    private void SubjectFilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e) => ApplyFilter();
    private void TemplatesListBox_SelectionChanged(object sender, SelectionChangedEventArgs e) { }

    private void TemplatesListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e) => UseSelectedTemplate();

    private void Use_Click(object sender, RoutedEventArgs e) => UseSelectedTemplate();

    private void UseSelectedTemplate()
    {
        if (TemplatesListBox.SelectedItem is not ExamTemplate template)
        {
            MessageBox.Show(this, "Επίλεξε πρώτα ένα πρότυπο.", "Πρότυπα",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        CreatedExam = _service.CreateExamFromTemplate(template);
        DialogResult = true;
    }

    private async void Delete_Click(object sender, RoutedEventArgs e)
    {
        if (TemplatesListBox.SelectedItem is not ExamTemplate template) return;

        var answer = MessageBox.Show(this,
            $"Να διαγραφεί οριστικά το πρότυπο «{template.Name}»;",
            "Διαγραφή προτύπου", MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (answer != MessageBoxResult.Yes) return;

        _service.DeleteTemplate(template);
        await ReloadAsync();
    }

    private void OpenFolder_Click(object sender, RoutedEventArgs e) => _service.OpenTemplatesFolder();
    private void Close_Click(object sender, RoutedEventArgs e) => DialogResult = false;
}
