using System.Collections.ObjectModel;
using System.Windows;
using ExamBuilderGR.Models;

namespace ExamBuilderGR.Views;

public partial class NewExamWindow : Window
{
    private static readonly string[] GreekLetters = ["Α", "Β", "Γ", "Δ", "Ε", "ΣΤ", "Ζ", "Η"];

    public ExamDocument? CreatedExam { get; private set; }

    public NewExamWindow()
    {
        InitializeComponent();
        SectionCountComboBox.ItemsSource = Enumerable.Range(1, 8);
        SectionCountComboBox.SelectedItem = 4;
        ExamDatePicker.SelectedDate = DateTime.Today;
        Loaded += (_, _) => TitleTextBox.Focus();
    }

    private void Create_Click(object sender, RoutedEventArgs e)
    {
        var title = TitleTextBox.Text.Trim();
        var subject = SubjectTextBox.Text.Trim();

        if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(subject))
        {
            MessageBox.Show("Συμπλήρωσε τίτλο και μάθημα.", "Νέο διαγώνισμα",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (!int.TryParse(DurationTextBox.Text, out var duration) || duration < 0)
        {
            MessageBox.Show("Η διάρκεια πρέπει να είναι μη αρνητικός ακέραιος αριθμός.", "Νέο διαγώνισμα",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var sectionCount = SectionCountComboBox.SelectedItem is int count ? count : 4;
        var exam = new ExamDocument
        {
            Title = title,
            Subject = subject,
            Grade = GradeTextBox.Text.Trim(),
            ClassSection = ClassSectionTextBox.Text.Trim(),
            Orientation = OrientationTextBox.Text.Trim(),
            ExamDate = ExamDatePicker.SelectedDate ?? DateTime.Today,
            DurationMinutes = duration,
            Sections = new ObservableCollection<ExamSection>()
        };

        var basePoints = 100 / sectionCount;
        var remainder = 100 % sectionCount;
        var createQuestions = CreateQuestionsCheckBox.IsChecked == true;

        for (var i = 0; i < sectionCount; i++)
        {
            var letter = GreekLetters[i];
            var section = new ExamSection { Title = $"ΘΕΜΑ {letter}" };
            if (createQuestions)
            {
                section.Questions.Add(new ExamQuestion
                {
                    Code = $"{letter}1",
                    Text = "Γράψε εδώ την εκφώνηση της ερώτησης.",
                    Points = basePoints + (i == sectionCount - 1 ? remainder : 0),
                    AnswerLines = 8
                });
            }
            exam.Sections.Add(section);
        }

        CreatedExam = exam;
        DialogResult = true;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;
}
