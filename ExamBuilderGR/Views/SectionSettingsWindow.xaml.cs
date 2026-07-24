using System.Windows;
using ExamBuilderGR.Models;

namespace ExamBuilderGR.Views;

public partial class SectionSettingsWindow : Window
{
    public SectionSettingsWindow(ExamSection section)
    {
        InitializeComponent();
        DataContext = section;
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
    }
}
