using System.IO;
using System.IO.Compression;
using System.Text.Json;
using System.Text.Json.Serialization;
using ExamBuilderGR.Models;

namespace ExamBuilderGR.Services;

public sealed class ExamStorageService
{
    private readonly JsonSerializerOptions _options = CreateJsonOptions();

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

    public string RootFolder { get; }
    public string ExamsFolder { get; }
    public string BackupsFolder { get; }
    public string SettingsFile { get; }

    public ExamStorageService()
    {
        RootFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "ExamBuilder GR");
        ExamsFolder = Path.Combine(RootFolder, "Διαγωνίσματα");
        BackupsFolder = Path.Combine(RootFolder, "Αντίγραφα Ασφαλείας");
        SettingsFile = Path.Combine(RootFolder, "school-settings.json");
        Directory.CreateDirectory(ExamsFolder);
        Directory.CreateDirectory(BackupsFolder);
    }

    public async Task<string> SaveExamAsync(ExamDocument exam, SchoolProfile school)
    {
        ArgumentNullException.ThrowIfNull(exam);
        ArgumentNullException.ThrowIfNull(school);

        var schoolYear = string.IsNullOrWhiteSpace(school.SchoolYear) ? "Χωρίς σχολικό έτος" : school.SchoolYear;
        var schoolYearFolder = Path.Combine(ExamsFolder, Sanitize(schoolYear));
        Directory.CreateDirectory(schoolYearFolder);

        var subject = string.IsNullOrWhiteSpace(exam.Subject) ? "Μάθημα" : exam.Subject;
        var grade = string.IsNullOrWhiteSpace(exam.Grade) ? "Τάξη" : exam.Grade;
        var title = string.IsNullOrWhiteSpace(exam.Title) ? "Διαγώνισμα" : exam.Title;
        var fileName = $"{exam.ExamDate:yyyy-MM-dd}_{Sanitize(subject)}_{Sanitize(grade)}_{Sanitize(title)}.exam.json";
        var path = Path.Combine(schoolYearFolder, fileName);

        exam.UpdatedAt = DateTime.Now;
        await File.WriteAllTextAsync(path, JsonSerializer.Serialize(exam, _options));
        return path;
    }

    public async Task<ExamDocument> LoadExamAsync(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("Δεν επιλέχθηκε αρχείο.", nameof(path));
        if (!File.Exists(path)) throw new FileNotFoundException("Το αρχείο διαγωνίσματος δεν βρέθηκε.", path);

        var json = await File.ReadAllTextAsync(path);
        var exam = JsonSerializer.Deserialize<ExamDocument>(json, _options)
                   ?? throw new InvalidDataException("Το αρχείο δεν περιέχει έγκυρο διαγώνισμα.");
        NormalizeExam(exam);
        return exam;
    }

    public async Task SaveSchoolAsync(SchoolProfile school)
    {
        ArgumentNullException.ThrowIfNull(school);
        Directory.CreateDirectory(RootFolder);
        await File.WriteAllTextAsync(SettingsFile, JsonSerializer.Serialize(school, _options));
    }

    public async Task<SchoolProfile> LoadSchoolAsync()
    {
        if (!File.Exists(SettingsFile)) return new SchoolProfile();

        try
        {
            var json = await File.ReadAllTextAsync(SettingsFile);
            return JsonSerializer.Deserialize<SchoolProfile>(json, _options) ?? new SchoolProfile();
        }
        catch (JsonException)
        {
            return new SchoolProfile();
        }
    }

    public async Task<string> CreateBackupAsync()
    {
        Directory.CreateDirectory(RootFolder);
        Directory.CreateDirectory(BackupsFolder);

        var backupPath = Path.Combine(BackupsFolder,
            $"ExamBuilderGR_Backup_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.zip");

        await using var output = new FileStream(backupPath, FileMode.CreateNew, FileAccess.Write, FileShare.None);
        using var archive = new ZipArchive(output, ZipArchiveMode.Create, leaveOpen: false);

        foreach (var file in Directory.EnumerateFiles(RootFolder, "*", SearchOption.AllDirectories))
        {
            if (file.StartsWith(BackupsFolder + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                continue;

            var relativePath = Path.GetRelativePath(RootFolder, file);
            var entry = archive.CreateEntry(relativePath, CompressionLevel.Optimal);
            await using var input = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            await using var entryStream = entry.Open();
            await input.CopyToAsync(entryStream);
        }

        return backupPath;
    }

    private static void NormalizeExam(ExamDocument exam)
    {
        foreach (var section in exam.Sections)
            foreach (var question in section.Questions)
                question.NormalizeLegacyStructures();
    }

    private static string Sanitize(string value)
    {
        foreach (var c in Path.GetInvalidFileNameChars()) value = value.Replace(c, '-');
        var cleaned = string.Join("-", value.Split(' ', StringSplitOptions.RemoveEmptyEntries));
        return string.IsNullOrWhiteSpace(cleaned) ? "Χωρίς-τίτλο" : cleaned;
    }
}
