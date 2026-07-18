using System.IO;
using ExamBuilderGR.Models;

namespace ExamBuilderGR.Services;

public sealed class ExamValidationService
{
    public IReadOnlyList<ValidationIssue> Validate(ExamDocument exam, SchoolProfile school, bool forAnswerKey = false)
    {
        ArgumentNullException.ThrowIfNull(exam);
        ArgumentNullException.ThrowIfNull(school);

        var issues = new List<ValidationIssue>();

        AddRequired(issues, school.SchoolName, "Ρυθμίσεις σχολείου", "Δεν έχει οριστεί ονομασία σχολείου.");
        AddRequired(issues, school.TeacherName, "Ρυθμίσεις σχολείου", "Δεν έχει οριστεί ονοματεπώνυμο καθηγητή.", ValidationSeverity.Warning);
        if (school.ShowSchoolLogo && !string.IsNullOrWhiteSpace(school.SchoolLogoPath) && !File.Exists(school.SchoolLogoPath))
            issues.Add(Warning("Ρυθμίσεις σχολείου", "Το αρχείο λογοτύπου του σχολείου δεν βρέθηκε."));
        AddRequired(issues, exam.Title, "Στοιχεία διαγωνίσματος", "Δεν έχει οριστεί τίτλος διαγωνίσματος.");
        AddRequired(issues, exam.Subject, "Στοιχεία διαγωνίσματος", "Δεν έχει οριστεί μάθημα.");
        AddRequired(issues, exam.Grade, "Στοιχεία διαγωνίσματος", "Δεν έχει οριστεί τάξη.", ValidationSeverity.Warning);

        if (exam.Sections.Count == 0)
        {
            issues.Add(Error("Δομή διαγωνίσματος", "Το διαγώνισμα δεν περιέχει κανένα θέμα."));
            return issues;
        }

        if (exam.TotalPoints != 100)
            issues.Add(Error("Βαθμολογία", $"Το σύνολο των μονάδων είναι {exam.TotalPoints}/100."));

        foreach (var section in exam.Sections)
        {
            var sectionName = string.IsNullOrWhiteSpace(section.Title) ? "Θέμα χωρίς τίτλο" : section.Title;
            if (string.IsNullOrWhiteSpace(section.Title))
                issues.Add(Warning(sectionName, "Το θέμα δεν έχει τίτλο."));

            if (section.Questions.Count == 0)
            {
                issues.Add(Error(sectionName, "Το θέμα δεν περιέχει υποερωτήματα."));
                continue;
            }

            if (section.TotalPoints <= 0)
                issues.Add(Error(sectionName, "Το άθροισμα των μονάδων του θέματος πρέπει να είναι μεγαλύτερο από μηδέν."));

            var duplicateCodes = section.Questions
                .Where(question => !string.IsNullOrWhiteSpace(question.Code))
                .GroupBy(question => question.Code.Trim(), StringComparer.CurrentCultureIgnoreCase)
                .Where(group => group.Count() > 1)
                .Select(group => group.Key)
                .ToList();
            if (duplicateCodes.Count > 0)
                issues.Add(Warning(sectionName, $"Υπάρχουν διπλότυποι κωδικοί υποερωτημάτων: {string.Join(", ", duplicateCodes)}."));

            foreach (var question in section.Questions)
                ValidateQuestion(issues, question, sectionName, forAnswerKey || exam.GenerateAnswerKey);
        }

        if (issues.Count == 0)
        {
            issues.Add(new ValidationIssue
            {
                Severity = ValidationSeverity.Information,
                Location = "Προέλεγχος",
                Message = "Το διαγώνισμα είναι έτοιμο για εκτύπωση ή PDF."
            });
        }

        return issues;
    }

    private static void ValidateQuestion(
        ICollection<ValidationIssue> issues,
        ExamQuestion question,
        string sectionName,
        bool answerKeyRequested)
    {
        var code = string.IsNullOrWhiteSpace(question.Code) ? "Ερώτηση χωρίς κωδικό" : question.Code;
        var location = $"{sectionName} / {code}";

        if (string.IsNullOrWhiteSpace(question.Code))
            issues.Add(Warning(location, "Δεν έχει οριστεί κωδικός ερώτησης."));
        if (string.IsNullOrWhiteSpace(question.Text))
            issues.Add(Error(location, "Η εκφώνηση είναι κενή."));
        if (question.Points <= 0)
            issues.Add(Error(location, "Οι μονάδες της ερώτησης πρέπει να είναι μεγαλύτερες από μηδέν."));

        switch (question.Type)
        {
            case QuestionType.Development:
                if (question.AnswerAreaType != AnswerAreaType.None && question.AnswerLines <= 0)
                    issues.Add(Warning(location, "Δεν έχει οριστεί χώρος απάντησης."));
                if (answerKeyRequested && string.IsNullOrWhiteSpace(question.ModelAnswer))
                    issues.Add(Warning(location, "Δεν έχει καταχωριστεί ενδεικτική απάντηση για το κλειδί λύσεων."));
                break;

            case QuestionType.TrueFalse:
                if (question.TrueFalseItems.Count == 0)
                    issues.Add(Error(location, "Δεν υπάρχουν προτάσεις Σωστού / Λάθους."));
                if (question.TrueFalseItems.Any(item => string.IsNullOrWhiteSpace(item.Statement)))
                    issues.Add(Error(location, "Υπάρχει κενή πρόταση Σωστού / Λάθους."));
                break;

            case QuestionType.Matching:
                question.NormalizeLegacyStructures();
                if (question.MatchingLeftItems.Count < 2)
                    issues.Add(Error(location, "Η Στήλη Α χρειάζεται τουλάχιστον δύο στοιχεία."));
                if (question.MatchingRightItems.Count < 2)
                    issues.Add(Error(location, "Η Στήλη Β χρειάζεται τουλάχιστον δύο στοιχεία."));
                if (question.MatchingLeftItems.Any(item => string.IsNullOrWhiteSpace(item.Text)))
                    issues.Add(Error(location, "Υπάρχει κενό στοιχείο στη Στήλη Α."));
                if (question.MatchingRightItems.Any(item => string.IsNullOrWhiteSpace(item.Text)))
                    issues.Add(Error(location, "Υπάρχει κενό στοιχείο στη Στήλη Β."));

                var leftIds = question.MatchingLeftItems.Select(item => item.Id).ToHashSet();
                var rightIds = question.MatchingRightItems.Select(item => item.Id).ToHashSet();
                if (question.MatchingRelations.Any(relation =>
                        !leftIds.Contains(relation.LeftItemId) || !rightIds.Contains(relation.RightItemId)))
                    issues.Add(Error(location, "Υπάρχει σχέση που αναφέρεται σε στοιχείο το οποίο έχει διαγραφεί."));

                var duplicateRelations = question.MatchingRelations
                    .GroupBy(relation => new { relation.LeftItemId, relation.RightItemId })
                    .Any(group => group.Count() > 1);
                if (duplicateRelations)
                    issues.Add(Warning(location, "Υπάρχουν διπλότυπες σωστές σχέσεις στην αντιστοίχιση."));

                foreach (var left in question.MatchingLeftItems)
                {
                    if (!question.MatchingRelations.Any(relation => relation.LeftItemId == left.Id))
                        issues.Add(Error(location, $"Το στοιχείο «{left.Text}» της Στήλης Α δεν έχει καμία σωστή αντιστοίχιση."));
                }

                var unusedRight = question.MatchingRightItems
                    .Where(right => !question.MatchingRelations.Any(relation => relation.RightItemId == right.Id))
                    .ToList();
                if (unusedRight.Count > 0)
                    issues.Add(Warning(location, $"Υπάρχουν {unusedRight.Count} στοιχεία της Στήλης Β χωρίς αντιστοίχιση. Θα λειτουργήσουν ως παραπλανητικές επιλογές."));
                break;

            case QuestionType.FillBlank:
                question.NormalizeLegacyStructures();
                if (question.FillBlankSentences.Count == 0)
                {
                    issues.Add(Error(location, "Δεν υπάρχουν προτάσεις συμπλήρωσης κενού."));
                    break;
                }

                for (var sentenceIndex = 0; sentenceIndex < question.FillBlankSentences.Count; sentenceIndex++)
                {
                    var sentence = question.FillBlankSentences[sentenceIndex];
                    var sentenceLocation = $"{location} / Πρόταση {sentenceIndex + 1}";
                    if (string.IsNullOrWhiteSpace(sentence.MarkedText))
                        issues.Add(Error(sentenceLocation, "Η πρόταση είναι κενή."));
                    if (sentence.HasMalformedMarkers)
                        issues.Add(Error(sentenceLocation, "Οι δείκτες κενού [[...]] δεν είναι σωστά κλεισμένοι ή περιέχουν κενή απάντηση."));
                    if (sentence.BlankCount == 0)
                        issues.Add(Error(sentenceLocation, "Δεν έχει οριστεί κανένα κενό στην πρόταση."));
                }
                break;

            case QuestionType.MultipleChoice:
                if (question.MultipleChoiceOptions.Count < 2)
                    issues.Add(Error(location, "Η πολλαπλή επιλογή χρειάζεται τουλάχιστον δύο επιλογές."));
                if (question.MultipleChoiceOptions.Any(option => string.IsNullOrWhiteSpace(option.Text)))
                    issues.Add(Error(location, "Υπάρχει κενή επιλογή απάντησης."));
                var correctCount = question.MultipleChoiceOptions.Count(option => option.IsCorrect);
                if (correctCount != 1)
                    issues.Add(Error(location, $"Πρέπει να υπάρχει ακριβώς μία σωστή επιλογή. Βρέθηκαν: {correctCount}."));
                break;
        }
    }

    private static void AddRequired(
        ICollection<ValidationIssue> issues,
        string? value,
        string location,
        string message,
        ValidationSeverity severity = ValidationSeverity.Error)
    {
        if (!string.IsNullOrWhiteSpace(value)) return;
        issues.Add(new ValidationIssue { Severity = severity, Location = location, Message = message });
    }

    private static ValidationIssue Error(string location, string message) =>
        new() { Severity = ValidationSeverity.Error, Location = location, Message = message };

    private static ValidationIssue Warning(string location, string message) =>
        new() { Severity = ValidationSeverity.Warning, Location = location, Message = message };
}
