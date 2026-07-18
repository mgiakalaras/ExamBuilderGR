using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using ExamBuilderGR.Models;

namespace ExamBuilderGR.Services;

public sealed class ExamTemplateService
{
    private readonly JsonSerializerOptions _options = CreateJsonOptions();

    public string TemplatesFolder { get; }

    public ExamTemplateService()
    {
        var rootFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "ExamBuilder GR");
        TemplatesFolder = Path.Combine(rootFolder, "Πρότυπα");
        Directory.CreateDirectory(TemplatesFolder);
    }

    public async Task<string> SaveTemplateAsync(string name, string description, ExamDocument sourceExam)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Το πρότυπο χρειάζεται όνομα.", nameof(name));

        ArgumentNullException.ThrowIfNull(sourceExam);

        var now = DateTime.Now;
        var template = new ExamTemplate
        {
            Name = name.Trim(),
            Description = description?.Trim() ?? string.Empty,
            CreatedAt = now,
            UpdatedAt = now,
            Exam = CloneExam(sourceExam, preserveIdentity: true)
        };

        var path = Path.Combine(TemplatesFolder, $"{Sanitize(template.Name)}.template.json");

        if (File.Exists(path))
        {
            try
            {
                var existing = await LoadTemplateAsync(path);
                template.Id = existing.Id;
                template.CreatedAt = existing.CreatedAt;
            }
            catch
            {
                // Αν το παλιό αρχείο δεν διαβάζεται, αντικαθίσταται με έγκυρο πρότυπο.
            }
        }

        await File.WriteAllTextAsync(path, JsonSerializer.Serialize(template, _options));
        return path;
    }

    public async Task<IReadOnlyList<ExamTemplate>> LoadTemplatesAsync()
    {
        Directory.CreateDirectory(TemplatesFolder);
        var templates = new List<ExamTemplate>();

        foreach (var path in Directory.EnumerateFiles(TemplatesFolder, "*.template.json", SearchOption.TopDirectoryOnly))
        {
            try
            {
                templates.Add(await LoadTemplateAsync(path));
            }
            catch
            {
                // Ένα κατεστραμμένο πρότυπο δεν πρέπει να εμποδίζει την εμφάνιση των υπόλοιπων.
            }
        }

        return templates
            .OrderByDescending(template => template.UpdatedAt)
            .ThenBy(template => template.Name, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
    }

    public async Task<ExamTemplate> LoadTemplateAsync(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            throw new FileNotFoundException("Το αρχείο προτύπου δεν βρέθηκε.", path);

        var json = await File.ReadAllTextAsync(path);
        var template = JsonSerializer.Deserialize<ExamTemplate>(json, _options)
                       ?? throw new InvalidDataException("Το αρχείο δεν περιέχει έγκυρο πρότυπο.");
        NormalizeExam(template.Exam);
        return template;
    }

    public ExamDocument CreateExamFromTemplate(ExamTemplate template)
    {
        ArgumentNullException.ThrowIfNull(template);
        var exam = CloneExam(template.Exam, preserveIdentity: false);
        exam.Id = Guid.NewGuid();
        exam.ExamDate = DateTime.Today;
        exam.CreatedAt = DateTime.Now;
        exam.UpdatedAt = DateTime.Now;
        return exam;
    }

    public bool DeleteTemplate(ExamTemplate template)
    {
        ArgumentNullException.ThrowIfNull(template);
        var path = Path.Combine(TemplatesFolder, $"{Sanitize(template.Name)}.template.json");
        if (!File.Exists(path)) return false;
        File.Delete(path);
        return true;
    }

    public void OpenTemplatesFolder()
    {
        Directory.CreateDirectory(TemplatesFolder);
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName = "explorer.exe",
            Arguments = $"\"{TemplatesFolder}\"",
            UseShellExecute = true
        });
    }

    private ExamDocument CloneExam(ExamDocument exam, bool preserveIdentity)
    {
        var json = JsonSerializer.Serialize(exam, _options);
        var clone = JsonSerializer.Deserialize<ExamDocument>(json, _options)
                    ?? throw new InvalidOperationException("Δεν ήταν δυνατή η αντιγραφή του διαγωνίσματος.");

        if (!preserveIdentity)
        {
            clone.Id = Guid.NewGuid();
            clone.CreatedAt = DateTime.Now;
            clone.UpdatedAt = DateTime.Now;
        }

        NormalizeExam(clone);
        return clone;
    }

    private static void NormalizeExam(ExamDocument exam)
    {
        foreach (var section in exam.Sections)
            foreach (var question in section.Questions)
                question.NormalizeLegacyStructures();
    }

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

    private static string Sanitize(string value)
    {
        foreach (var c in Path.GetInvalidFileNameChars()) value = value.Replace(c, '-');
        var cleaned = string.Join("-", value.Split(' ', StringSplitOptions.RemoveEmptyEntries));
        return string.IsNullOrWhiteSpace(cleaned) ? "Χωρίς-όνομα" : cleaned;
    }
}
