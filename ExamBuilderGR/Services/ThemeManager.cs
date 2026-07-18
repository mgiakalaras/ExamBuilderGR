using System.Windows;

namespace ExamBuilderGR.Services;

public static class ThemeManager
{
    private static readonly Dictionary<string, string> Themes = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Classic Light"] = "Themes/ClassicLight.xaml",
        ["Obsidian"] = "Themes/Obsidian.xaml",
        ["Nord"] = "Themes/Nord.xaml",
        ["Monokai"] = "Themes/Monokai.xaml"
    };

    public static IReadOnlyCollection<string> AvailableThemes => Themes.Keys;

    public static void Apply(string themeName)
    {
        if (!Themes.TryGetValue(themeName, out var uri)) return;

        var dictionaries = Application.Current.Resources.MergedDictionaries;
        var oldTheme = dictionaries.FirstOrDefault(d => d.Source?.OriginalString.StartsWith("Themes/", StringComparison.OrdinalIgnoreCase) == true);
        if (oldTheme is not null) dictionaries.Remove(oldTheme);

        dictionaries.Insert(0, new ResourceDictionary { Source = new Uri(uri, UriKind.Relative) });
    }
}
