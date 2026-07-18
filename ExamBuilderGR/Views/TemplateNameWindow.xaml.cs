using System.Windows;

namespace ExamBuilderGR.Views;

public partial class TemplateNameWindow : Window
{
    public string TemplateName => TemplateNameTextBox.Text.Trim();
    public string Description => DescriptionTextBox.Text.Trim();

    public TemplateNameWindow(string suggestedName)
    {
        InitializeComponent();
        TemplateNameTextBox.Text = string.IsNullOrWhiteSpace(suggestedName) ? "Νέο πρότυπο" : suggestedName;
        Loaded += (_, _) =>
        {
            TemplateNameTextBox.Focus();
            TemplateNameTextBox.SelectAll();
        };
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(TemplateName))
        {
            MessageBox.Show(this, "Γράψε ένα όνομα για το πρότυπο.", "Αποθήκευση προτύπου",
                MessageBoxButton.OK, MessageBoxImage.Information);
            TemplateNameTextBox.Focus();
            return;
        }

        DialogResult = true;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;
}
