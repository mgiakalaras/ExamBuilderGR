using System.Windows;
using System.Windows.Controls;
using ExamBuilderGR.Services;

namespace ExamBuilderGR.Views;

public partial class HelpWindow : Window
{
    public string VersionText => $"ExamBuilder GR v{AppInfo.Version}";

    public HelpWindow()
    {
        InitializeComponent();
        DataContext = this;
    }

    private void NavigationList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!IsLoaded || NavigationList.SelectedItem is not ListBoxItem item || item.Tag is not string targetName)
            return;

        FrameworkElement? target = targetName switch
        {
            nameof(QuickStartSection) => QuickStartSection,
            nameof(FirstUseSection) => FirstUseSection,
            nameof(ExamSetupSection) => ExamSetupSection,
            nameof(StructureSection) => StructureSection,
            nameof(QuestionTypesSection) => QuestionTypesSection,
            nameof(ImagesSection) => ImagesSection,
            nameof(AnswerKeySection) => AnswerKeySection,
            nameof(PreviewSection) => PreviewSection,
            nameof(StorageSection) => StorageSection,
            nameof(ShortcutsSection) => ShortcutsSection,
            _ => null
        };

        target?.BringIntoView();
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();
}
