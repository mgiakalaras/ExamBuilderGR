using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using ExamBuilderGR.Models;

namespace ExamBuilderGR.Services;

public sealed class QuestionBankService
{
    private readonly JsonSerializerOptions _options = CreateJsonOptions();

    public string LibraryFolder { get; }

    public QuestionBankService()
    {
        var rootFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "ExamBuilder GR");
        LibraryFolder = Path.Combine(rootFolder, "Βιβλιοθήκη Ερωτήσεων");
        Directory.CreateDirectory(LibraryFolder);
    }

    public async Task<QuestionBankEntry> SaveQuestionAsync(
        ExamQuestion question,
        string subject,
        string grade,
        string category,
        string? tags = null)
    {
        ArgumentNullException.ThrowIfNull(question);

        var text = question.Text?.Replace("\r", " ").Replace("\n", " ").Trim() ?? string.Empty;
        var shortText = text.Length <= 70 ? text : text[..67] + "...";
        var title = string.IsNullOrWhiteSpace(shortText)
            ? $"Ερώτηση {question.Code}"
            : $"{question.Code} — {shortText}";

        var entry = new QuestionBankEntry
        {
            Id = Guid.NewGuid(),
            Title = title,
            Subject = subject?.Trim() ?? string.Empty,
            Grade = grade?.Trim() ?? string.Empty,
            Category = category?.Trim() ?? string.Empty,
            Tags = tags?.Trim() ?? string.Empty,
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now,
            Question = CloneQuestion(question)
        };

        var path = GetPath(entry.Id);
        entry.StoragePath = path;
        await File.WriteAllTextAsync(path, JsonSerializer.Serialize(entry, _options));
        return entry;
    }

    public async Task<IReadOnlyList<QuestionBankEntry>> LoadEntriesAsync()
    {
        Directory.CreateDirectory(LibraryFolder);
        var entries = new List<QuestionBankEntry>();

        foreach (var path in Directory.EnumerateFiles(LibraryFolder, "*.question.json", SearchOption.TopDirectoryOnly))
        {
            try
            {
                var json = await File.ReadAllTextAsync(path);
                var entry = JsonSerializer.Deserialize<QuestionBankEntry>(json, _options);
                if (entry is null) continue;
                entry.Question.NormalizeLegacyStructures();
                entry.StoragePath = path;
                entries.Add(entry);
            }
            catch
            {
                // Ένα κατεστραμμένο αρχείο δεν πρέπει να εμποδίζει τη φόρτωση της βιβλιοθήκης.
            }
        }

        return entries
            .OrderByDescending(entry => entry.UpdatedAt)
            .ThenBy(entry => entry.Title, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
    }

    public ExamQuestion CreateQuestion(QuestionBankEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        var question = CloneQuestion(entry.Question);
        question.NormalizeLegacyStructures();
        return question;
    }

    public bool DeleteEntry(QuestionBankEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        var path = string.IsNullOrWhiteSpace(entry.StoragePath) ? GetPath(entry.Id) : entry.StoragePath;
        if (!File.Exists(path)) return false;
        File.Delete(path);
        return true;
    }

    public void OpenLibraryFolder()
    {
        Directory.CreateDirectory(LibraryFolder);
        Process.Start(new ProcessStartInfo
        {
            FileName = "explorer.exe",
            Arguments = $"\"{LibraryFolder}\"",
            UseShellExecute = true
        });
    }

    private ExamQuestion CloneQuestion(ExamQuestion question)
    {
        var json = JsonSerializer.Serialize(question, _options);
        var clone = JsonSerializer.Deserialize<ExamQuestion>(json, _options)
                    ?? throw new InvalidOperationException("Δεν ήταν δυνατή η αντιγραφή της ερώτησης.");
        clone.NormalizeLegacyStructures();
        return clone;
    }

    private string GetPath(Guid id) => Path.Combine(LibraryFolder, $"{id:N}.question.json");

    private static JsonSerializerOptions CreateJsonOptions()
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true
        };
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }
}
