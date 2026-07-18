using System.Text.Json.Serialization;

namespace ExamBuilderGR.Models;

public sealed class ExamTemplate
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "Νέο πρότυπο";
    public string Description { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
    public ExamDocument Exam { get; set; } = new();

    [JsonIgnore]
    public int SectionCount => Exam.Sections.Count;

    [JsonIgnore]
    public int QuestionCount => Exam.Sections.Sum(section => section.Questions.Count);

    [JsonIgnore]
    public string Summary => $"{Exam.Subject} · {Exam.Grade} · {QuestionCount} ερωτήσεις";
}
