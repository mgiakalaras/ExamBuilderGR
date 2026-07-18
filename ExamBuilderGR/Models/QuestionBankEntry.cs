using System.Text.Json.Serialization;

namespace ExamBuilderGR.Models;

public sealed class QuestionBankEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Grade { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Tags { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
    public ExamQuestion Question { get; set; } = new();

    [JsonIgnore]
    public string StoragePath { get; set; } = string.Empty;

    [JsonIgnore]
    public string TypeLabel => Question.Type switch
    {
        QuestionType.Development => "Ανάπτυξης",
        QuestionType.TrueFalse => "Σωστό / Λάθος",
        QuestionType.Matching => "Αντιστοίχισης",
        QuestionType.FillBlank => "Συμπλήρωσης κενού",
        QuestionType.MultipleChoice => "Πολλαπλής επιλογής",
        _ => Question.Type.ToString()
    };

    [JsonIgnore]
    public string Summary
    {
        get
        {
            var text = Question.Text?.Replace("\r", " ").Replace("\n", " ").Trim() ?? string.Empty;
            return text.Length <= 130 ? text : text[..127] + "...";
        }
    }
}
