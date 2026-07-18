using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Text.Json.Serialization;

namespace ExamBuilderGR.Models;

public sealed partial class FillBlankSentence : INotifyPropertyChanged
{
    private string _markedText = "Γράψε εδώ την πρόταση και επίλεξε τις λέξεις που θα γίνουν κενά.";

    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Το κείμενο του καθηγητή. Κάθε απάντηση που γίνεται κενό αποθηκεύεται ως [[απάντηση]].
    /// Η μορφή είναι ευανάγνωστη, σταθερή σε μελλοντικές διορθώσεις και επιτρέπει πολλά κενά.
    /// </summary>
    public string MarkedText
    {
        get => _markedText;
        set
        {
            if (Set(ref _markedText, value ?? string.Empty))
            {
                Raise(nameof(StudentText));
                Raise(nameof(OriginalText));
                Raise(nameof(Answers));
                Raise(nameof(AnswerSummary));
                Raise(nameof(BlankCount));
                Raise(nameof(HasMalformedMarkers));
            }
        }
    }

    [JsonIgnore]
    public string StudentText => BlankMarkerRegex().Replace(MarkedText ?? string.Empty, match =>
    {
        var answer = match.Groups[1].Value.Trim();
        var length = Math.Clamp(answer.Length * 3, 24, 60);
        return new string('_', length);
    });

    [JsonIgnore]
    public string OriginalText => BlankMarkerRegex().Replace(MarkedText ?? string.Empty, "$1");

    [JsonIgnore]
    public IReadOnlyList<string> Answers => BlankMarkerRegex()
        .Matches(MarkedText ?? string.Empty)
        .Select(match => match.Groups[1].Value.Trim())
        .Where(answer => !string.IsNullOrWhiteSpace(answer))
        .ToList();

    [JsonIgnore]
    public int BlankCount => Answers.Count;

    [JsonIgnore]
    public string AnswerSummary => BlankCount == 0
        ? "Δεν έχουν οριστεί κενά."
        : string.Join("  |  ", Answers.Select((answer, index) => $"{index + 1}ο κενό: {answer}"));

    [JsonIgnore]
    public bool HasMalformedMarkers
    {
        get
        {
            var text = MarkedText ?? string.Empty;
            var openCount = CountOccurrences(text, "[[");
            var closeCount = CountOccurrences(text, "]]" );
            return openCount != closeCount || EmptyMarkerRegex().IsMatch(text);
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private static int CountOccurrences(string text, string token)
    {
        if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(token)) return 0;
        var count = 0;
        var index = 0;
        while ((index = text.IndexOf(token, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += token.Length;
        }
        return count;
    }

    private bool Set<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        Raise(name);
        return true;
    }

    private void Raise([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    [GeneratedRegex(@"\[\[(.*?)\]\]", RegexOptions.Singleline)]
    private static partial Regex BlankMarkerRegex();

    [GeneratedRegex(@"\[\[\s*\]\]", RegexOptions.Singleline)]
    private static partial Regex EmptyMarkerRegex();
}
