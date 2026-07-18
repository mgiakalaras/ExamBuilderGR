namespace ExamBuilderGR.Models;

public enum ValidationSeverity
{
    Error,
    Warning,
    Information
}

public sealed class ValidationIssue
{
    public ValidationSeverity Severity { get; init; }
    public string Location { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;

    public string SeverityLabel => Severity switch
    {
        ValidationSeverity.Error => "Σφάλμα",
        ValidationSeverity.Warning => "Προειδοποίηση",
        _ => "Πληροφορία"
    };

    public string Icon => Severity switch
    {
        ValidationSeverity.Error => "✖",
        ValidationSeverity.Warning => "⚠",
        _ => "ℹ"
    };
}
