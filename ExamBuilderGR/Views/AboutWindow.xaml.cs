using System.Windows;
using ExamBuilderGR.Services;

namespace ExamBuilderGR.Views;

public partial class AboutWindow : Window
{
    public string ProductName => AppInfo.ProductName;
    public string Description => AppInfo.Description;
    public string Creator => AppInfo.Creator;
    public string DevelopmentYear => AppInfo.DevelopmentYear;
    public string VersionText => $"v{AppInfo.Version}";

    public AboutWindow()
    {
        InitializeComponent();
        DataContext = this;
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();
}
