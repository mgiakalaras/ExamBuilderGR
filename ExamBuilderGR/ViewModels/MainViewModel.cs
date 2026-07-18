using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Windows;
using System.Windows.Input;
using ExamBuilderGR.Models;
using ExamBuilderGR.Services;
using ExamBuilderGR.Views;
using Microsoft.Win32;

namespace ExamBuilderGR.ViewModels;

public sealed record QuestionTypeOption(QuestionType Value, string Label);
public sealed record AnswerAreaTypeOption(AnswerAreaType Value, string Label);
public sealed record BooleanAnswerOption(bool Value, string Label);

public sealed class MainViewModel : INotifyPropertyChanged
{
    private static readonly string[] GreekLetters = ["Α", "Β", "Γ", "Δ", "Ε", "ΣΤ", "Ζ", "Η"];

    private readonly ExamStorageService _storage = new();
    private readonly ExamTemplateService _templateService = new();
    private readonly QuestionBankService _questionBankService = new();
    private SchoolProfile _school = new();
    private ExamDocument _exam = CreateSampleExam();
    private ExamSection? _selectedSection;
    private ExamQuestion? _selectedQuestion;
    private string _statusMessage = "Έτοιμο";
    private string _selectedTheme = "Classic Light";
    private bool _isDirty;
    private string _cleanExamSnapshot = string.Empty;

    public SchoolProfile School
    {
        get => _school;
        private set
        {
            if (!Set(ref _school, value)) return;
            InvalidatePreview();
        }
    }

    public ExamDocument Exam
    {
        get => _exam;
        private set
        {
            if (ReferenceEquals(_exam, value)) return;
            UnwireExam(_exam);
            NormalizeExam(value);
            _exam = value;
            WireExam(_exam);
            Raise();
            Raise(nameof(TotalStatus));
            InvalidatePreview();
        }
    }

    public ObservableCollection<string> Themes { get; } = new(ThemeManager.AvailableThemes);

    public IReadOnlyList<QuestionTypeOption> QuestionTypes { get; } =
    [
        new(QuestionType.Development, "Ανάπτυξης"),
        new(QuestionType.TrueFalse, "Σωστό / Λάθος"),
        new(QuestionType.Matching, "Αντιστοίχισης"),
        new(QuestionType.FillBlank, "Συμπλήρωσης κενού"),
        new(QuestionType.MultipleChoice, "Πολλαπλής επιλογής")
    ];

    public IReadOnlyList<AnswerAreaTypeOption> AnswerAreaTypes { get; } =
    [
        new(AnswerAreaType.Lines, "Γραμμές απάντησης"),
        new(AnswerAreaType.BlankBox, "Κενό πλαίσιο"),
        new(AnswerAreaType.None, "Χωρίς χώρο απάντησης")
    ];

    public IReadOnlyList<BooleanAnswerOption> BooleanAnswers { get; } =
    [
        new(true, "Σωστό"),
        new(false, "Λάθος")
    ];

    public ExamSection? SelectedSection
    {
        get => _selectedSection;
        set => Set(ref _selectedSection, value);
    }

    public ExamQuestion? SelectedQuestion
    {
        get => _selectedQuestion;
        set
        {
            if (!Set(ref _selectedQuestion, value)) return;
            SelectedSection = value is null ? SelectedSection : Exam.Sections.FirstOrDefault(s => s.Questions.Contains(value));
            Raise(nameof(HasSelectedQuestion));
        }
    }

    public bool HasSelectedQuestion => SelectedQuestion is not null;
    public string TotalStatus => Exam.TotalPoints == 100 ? "Οι μονάδες είναι σωστές." : $"Απομένουν {100 - Exam.TotalPoints:+#;-#;0} μονάδες για το 100.";
    public string StatusMessage { get => _statusMessage; set => Set(ref _statusMessage, value); }
    public string AppSignature => AppInfo.ShortSignature;
    public bool IsDirty
    {
        get => _isDirty;
        private set
        {
            if (!Set(ref _isDirty, value)) return;
            Raise(nameof(DocumentState));
            Raise(nameof(WindowTitle));
        }
    }
    public bool HasUnsavedChanges
    {
        get
        {
            if (IsDirty) return true;
            try
            {
                return !string.Equals(CreateExamSnapshot(Exam), _cleanExamSnapshot, StringComparison.Ordinal);
            }
            catch
            {
                return IsDirty;
            }
        }
    }

    public string DocumentState => IsDirty ? "● Μη αποθηκευμένες αλλαγές" : "✓ Αποθηκευμένο";
    public string WindowTitle => $"{AppInfo.ProductName} v{AppInfo.Version}{(IsDirty ? " *" : string.Empty)}";

    public string SelectedTheme
    {
        get => _selectedTheme;
        set
        {
            if (string.IsNullOrWhiteSpace(value) || !Set(ref _selectedTheme, value)) return;
            ThemeManager.Apply(value);
            School.ThemeName = value;
            StatusMessage = $"Εφαρμόστηκε το θέμα: {value}";
            _ = SaveThemePreferenceAsync();
        }
    }

    public ICommand SaveCommand { get; }
    public ICommand OpenExamCommand { get; }
    public ICommand OpenExamsFolderCommand { get; }
    public ICommand OpenDataFolderCommand { get; }
    public ICommand CreateBackupCommand { get; }
    public ICommand AddSectionCommand { get; }
    public ICommand DeleteSectionCommand { get; }
    public ICommand AddQuestionCommand { get; }
    public ICommand DeleteQuestionCommand { get; }
    public ICommand AddTrueFalseItemCommand { get; }
    public ICommand DeleteTrueFalseItemCommand { get; }
    public ICommand AddMatchingLeftItemCommand { get; }
    public ICommand DeleteMatchingLeftItemCommand { get; }
    public ICommand AddMatchingRightItemCommand { get; }
    public ICommand DeleteMatchingRightItemCommand { get; }
    public ICommand AddMatchingRelationCommand { get; }
    public ICommand DeleteMatchingRelationCommand { get; }
    public ICommand ShuffleMatchingCommand { get; }
    public ICommand AddFillBlankSentenceCommand { get; }
    public ICommand DeleteFillBlankSentenceCommand { get; }
    public ICommand AddMultipleChoiceOptionCommand { get; }
    public ICommand DeleteMultipleChoiceOptionCommand { get; }
    public ICommand SetCorrectMultipleChoiceOptionCommand { get; }
    public ICommand SelectQuestionCommand { get; }
    public ICommand NewExamCommand { get; }
    public ICommand SchoolSettingsCommand { get; }
    public ICommand SaveAsTemplateCommand { get; }
    public ICommand OpenTemplatesCommand { get; }
    public ICommand SaveQuestionToLibraryCommand { get; }
    public ICommand OpenQuestionLibraryCommand { get; }
    public ICommand AboutCommand { get; }

    public event EventHandler? PreviewInvalidated;

    public MainViewModel()
    {
        SaveCommand = new RelayCommand(async _ => await SaveAsync());
        OpenExamCommand = new RelayCommand(async _ => await OpenExamAsync());
        OpenExamsFolderCommand = new RelayCommand(_ => OpenExamsFolder());
        OpenDataFolderCommand = new RelayCommand(_ => OpenDataFolder());
        CreateBackupCommand = new RelayCommand(async _ => await CreateBackupAsync());
        AddSectionCommand = new RelayCommand(_ => AddSection());
        DeleteSectionCommand = new RelayCommand(section => DeleteSection(section as ExamSection));
        AddQuestionCommand = new RelayCommand(section => AddQuestion(section as ExamSection));
        DeleteQuestionCommand = new RelayCommand(_ => DeleteQuestion());
        AddTrueFalseItemCommand = new RelayCommand(_ => AddTrueFalseItem());
        DeleteTrueFalseItemCommand = new RelayCommand(item => DeleteTrueFalseItem(item as TrueFalseItem));
        AddMatchingLeftItemCommand = new RelayCommand(_ => AddMatchingLeftItem());
        DeleteMatchingLeftItemCommand = new RelayCommand(item => DeleteMatchingLeftItem(item as MatchingLeftItem));
        AddMatchingRightItemCommand = new RelayCommand(_ => AddMatchingRightItem());
        DeleteMatchingRightItemCommand = new RelayCommand(item => DeleteMatchingRightItem(item as MatchingRightItem));
        AddMatchingRelationCommand = new RelayCommand(_ => AddMatchingRelation());
        DeleteMatchingRelationCommand = new RelayCommand(item => DeleteMatchingRelation(item as MatchingRelation));
        ShuffleMatchingCommand = new RelayCommand(_ => ShuffleMatching());
        AddFillBlankSentenceCommand = new RelayCommand(_ => AddFillBlankSentence());
        DeleteFillBlankSentenceCommand = new RelayCommand(item => DeleteFillBlankSentence(item as FillBlankSentence));
        AddMultipleChoiceOptionCommand = new RelayCommand(_ => AddMultipleChoiceOption());
        DeleteMultipleChoiceOptionCommand = new RelayCommand(item => DeleteMultipleChoiceOption(item as MultipleChoiceOption));
        SetCorrectMultipleChoiceOptionCommand = new RelayCommand(item => SetCorrectMultipleChoiceOption(item as MultipleChoiceOption));
        SelectQuestionCommand = new RelayCommand(q => SelectQuestion(q as ExamQuestion));
        NewExamCommand = new RelayCommand(_ => CreateNewExam());
        SchoolSettingsCommand = new RelayCommand(async _ => await EditSchoolSettingsAsync());
        SaveAsTemplateCommand = new RelayCommand(async _ => await SaveAsTemplateAsync());
        OpenTemplatesCommand = new RelayCommand(_ => OpenTemplates());
        SaveQuestionToLibraryCommand = new RelayCommand(async _ => await SaveQuestionToLibraryAsync());
        OpenQuestionLibraryCommand = new RelayCommand(_ => OpenQuestionLibrary());
        AboutCommand = new RelayCommand(_ => OpenAbout());

        NormalizeExam(Exam);
        WireExam(Exam);
        SelectedSection = Exam.Sections.FirstOrDefault();
        SelectedQuestion = SelectedSection?.Questions.FirstOrDefault();
        CaptureCleanState();
        _ = LoadSchoolAsync();
    }

    private async Task LoadSchoolAsync()
    {
        School = await _storage.LoadSchoolAsync();
        var savedTheme = Themes.Contains(School.ThemeName) ? School.ThemeName : "Classic Light";
        SelectedTheme = savedTheme;
        StatusMessage = "Έτοιμο";
        InvalidatePreview();
    }

    private async Task SaveThemePreferenceAsync()
    {
        try
        {
            await _storage.SaveSchoolAsync(School);
        }
        catch
        {
            // Η αποτυχία αποθήκευσης προτίμησης δεν πρέπει να διακόψει την εργασία του χρήστη.
        }
    }

    public async Task<bool> SaveAsync()
    {
        try
        {
            await _storage.SaveSchoolAsync(School);
            var path = await _storage.SaveExamAsync(Exam, School);
            CaptureCleanState();
            StatusMessage = $"Αποθηκεύτηκε: {path}";
            return true;
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Σφάλμα αποθήκευσης", MessageBoxButton.OK, MessageBoxImage.Error);
            StatusMessage = "Η αποθήκευση απέτυχε";
            return false;
        }
    }

    private async Task OpenExamAsync()
    {
        if (!ConfirmDiscardChanges()) return;

        var dialog = new OpenFileDialog
        {
            Title = "Άνοιγμα διαγωνίσματος",
            Filter = "Αρχεία ExamBuilder GR (*.exam.json)|*.exam.json|Αρχεία JSON (*.json)|*.json|Όλα τα αρχεία (*.*)|*.*",
            InitialDirectory = _storage.ExamsFolder,
            CheckFileExists = true,
            Multiselect = false
        };

        if (dialog.ShowDialog(Application.Current.MainWindow) != true) return;

        try
        {
            var loadedExam = await _storage.LoadExamAsync(dialog.FileName);
            Exam = loadedExam;
            SelectedSection = Exam.Sections.FirstOrDefault();
            SelectedQuestion = SelectedSection?.Questions.FirstOrDefault();
            RefreshTotals();
            CaptureCleanState();
            StatusMessage = $"Άνοιξε: {dialog.FileName}";
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Σφάλμα ανοίγματος", MessageBoxButton.OK, MessageBoxImage.Error);
            StatusMessage = "Το αρχείο δεν άνοιξε";
        }
    }

    private void OpenExamsFolder()
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = $"\"{_storage.ExamsFolder}\"",
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Άνοιγμα φακέλου", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void OpenDataFolder()
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = $"\"{_storage.RootFolder}\"",
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Άνοιγμα φακέλου δεδομένων", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task CreateBackupAsync()
    {
        if (IsDirty)
        {
            var saveFirst = MessageBox.Show(
                "Υπάρχουν μη αποθηκευμένες αλλαγές. Να αποθηκευτεί πρώτα το διαγώνισμα ώστε να συμπεριληφθεί στο αντίγραφο ασφαλείας;",
                "Δημιουργία αντιγράφου ασφαλείας",
                MessageBoxButton.YesNoCancel,
                MessageBoxImage.Question);

            if (saveFirst == MessageBoxResult.Cancel) return;
            if (saveFirst == MessageBoxResult.Yes && !await SaveAsync()) return;
        }

        try
        {
            await _storage.SaveSchoolAsync(School);
            var path = await _storage.CreateBackupAsync();
            StatusMessage = $"Δημιουργήθηκε αντίγραφο ασφαλείας: {path}";
            MessageBox.Show($"Το αντίγραφο ασφαλείας δημιουργήθηκε επιτυχώς:\n\n{path}",
                "Αντίγραφο ασφαλείας", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Αντίγραφο ασφαλείας", MessageBoxButton.OK, MessageBoxImage.Error);
            StatusMessage = "Η δημιουργία αντιγράφου ασφαλείας απέτυχε";
        }
    }

    private void CreateNewExam()
    {
        if (!ConfirmDiscardChanges()) return;

        var dialog = new NewExamWindow { Owner = Application.Current.MainWindow };
        if (dialog.ShowDialog() != true || dialog.CreatedExam is null) return;

        Exam = dialog.CreatedExam;
        SelectedSection = Exam.Sections.FirstOrDefault();
        SelectedQuestion = SelectedSection?.Questions.FirstOrDefault();
        RefreshTotals();
        IsDirty = true;
        StatusMessage = "Δημιουργήθηκε νέο διαγώνισμα";
    }

    private async Task SaveAsTemplateAsync()
    {
        var dialog = new TemplateNameWindow(Exam.Title)
        {
            Owner = Application.Current.MainWindow
        };

        if (dialog.ShowDialog() != true) return;

        try
        {
            var path = await _templateService.SaveTemplateAsync(dialog.TemplateName, dialog.Description, Exam);
            StatusMessage = $"Το πρότυπο αποθηκεύτηκε: {path}";
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Αποθήκευση προτύπου", MessageBoxButton.OK, MessageBoxImage.Error);
            StatusMessage = "Η αποθήκευση του προτύπου απέτυχε";
        }
    }

    private void OpenTemplates()
    {
        try
        {
            var dialog = new TemplatesWindow(_templateService)
            {
                Owner = Application.Current.MainWindow
            };

            if (dialog.ShowDialog() != true || dialog.CreatedExam is null) return;
            if (!ConfirmDiscardChanges()) return;

            Exam = dialog.CreatedExam;
            SelectedSection = Exam.Sections.FirstOrDefault();
            SelectedQuestion = SelectedSection?.Questions.FirstOrDefault();
            RefreshTotals();
            IsDirty = true;
            StatusMessage = "Δημιουργήθηκε νέο διαγώνισμα από πρότυπο";
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Πρότυπα", MessageBoxButton.OK, MessageBoxImage.Error);
            StatusMessage = "Τα πρότυπα δεν άνοιξαν";
        }
    }

    private async Task SaveQuestionToLibraryAsync()
    {
        if (SelectedQuestion is null)
        {
            MessageBox.Show("Επίλεξε πρώτα μία ερώτηση.", "Βιβλιοθήκη ερωτήσεων",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        try
        {
            var section = Exam.Sections.FirstOrDefault(item => item.Questions.Contains(SelectedQuestion));
            var entry = await _questionBankService.SaveQuestionAsync(
                SelectedQuestion,
                Exam.Subject,
                Exam.Grade,
                section?.Title ?? string.Empty);

            StatusMessage = $"Η ερώτηση αποθηκεύτηκε στη βιβλιοθήκη: {entry.Title}";
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Αποθήκευση ερώτησης",
                MessageBoxButton.OK, MessageBoxImage.Error);
            StatusMessage = "Η αποθήκευση στη βιβλιοθήκη απέτυχε";
        }
    }

    private void OpenQuestionLibrary()
    {
        try
        {
            var dialog = new QuestionLibraryWindow(_questionBankService)
            {
                Owner = Application.Current.MainWindow
            };

            if (dialog.ShowDialog() != true || dialog.SelectedQuestion is null) return;

            var section = SelectedSection ?? Exam.Sections.FirstOrDefault();
            if (section is null)
            {
                AddSection();
                section = SelectedSection;
            }

            if (section is null) return;

            var question = dialog.SelectedQuestion;
            var prefix = GetSectionPrefix(section);
            question.Code = $"{prefix}{section.Questions.Count + 1}";
            section.Questions.Add(question);
            SelectedSection = section;
            SelectedQuestion = question;
            RefreshTotals();
            StatusMessage = $"Η ερώτηση {question.Code} προστέθηκε από τη βιβλιοθήκη";
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Βιβλιοθήκη ερωτήσεων",
                MessageBoxButton.OK, MessageBoxImage.Error);
            StatusMessage = "Η βιβλιοθήκη ερωτήσεων δεν άνοιξε";
        }
    }

    private static void OpenAbout()
    {
        var dialog = new AboutWindow
        {
            Owner = Application.Current.MainWindow
        };
        dialog.ShowDialog();
    }

    private async Task EditSchoolSettingsAsync()
    {
        var dialog = new SchoolSettingsWindow(School) { Owner = Application.Current.MainWindow };
        if (dialog.ShowDialog() != true) return;

        School = dialog.Profile;
        SelectedTheme = School.ThemeName;
        await _storage.SaveSchoolAsync(School);
        StatusMessage = "Οι ρυθμίσεις σχολείου αποθηκεύτηκαν";
        InvalidatePreview();
    }

    private void AddSection()
    {
        var index = Exam.Sections.Count;
        var letter = index < GreekLetters.Length ? GreekLetters[index] : (index + 1).ToString();
        var section = new ExamSection { Title = $"ΘΕΜΑ {letter}" };
        section.Questions.Add(new ExamQuestion
        {
            Code = $"{letter}1",
            Text = "Νέα ερώτηση",
            Points = 0,
            AnswerLines = 6
        });

        Exam.Sections.Add(section);
        SelectedSection = section;
        SelectedQuestion = section.Questions[0];
        RefreshTotals();
        StatusMessage = $"Προστέθηκε το {section.Title}";
    }

    private void DeleteSection(ExamSection? section)
    {
        if (section is null) return;

        var answer = MessageBox.Show($"Να διαγραφεί το {section.Title} μαζί με όλες τις ερωτήσεις του;",
            "Διαγραφή θέματος", MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (answer != MessageBoxResult.Yes) return;

        var wasSelected = ReferenceEquals(section, SelectedSection) ||
                          (SelectedQuestion is not null && section.Questions.Contains(SelectedQuestion));
        Exam.Sections.Remove(section);

        if (wasSelected)
        {
            SelectedSection = Exam.Sections.FirstOrDefault();
            SelectedQuestion = SelectedSection?.Questions.FirstOrDefault();
        }

        RefreshTotals();
        StatusMessage = "Το θέμα διαγράφηκε";
    }

    private void AddQuestion(ExamSection? requestedSection)
    {
        var section = requestedSection ?? SelectedSection ?? Exam.Sections.FirstOrDefault();
        if (section is null)
        {
            AddSection();
            return;
        }

        var prefix = GetSectionPrefix(section);
        var question = new ExamQuestion
        {
            Code = $"{prefix}{section.Questions.Count + 1}",
            Text = "Νέα ερώτηση",
            Points = 0,
            AnswerLines = 6
        };

        section.Questions.Add(question);
        SelectedSection = section;
        SelectedQuestion = question;
        RefreshTotals();
        StatusMessage = $"Προστέθηκε η ερώτηση {question.Code}";
    }

    private void AddTrueFalseItem()
    {
        if (SelectedQuestion is null) return;
        SelectedQuestion.TrueFalseItems.Add(new TrueFalseItem
        {
            Statement = $"Πρόταση {SelectedQuestion.TrueFalseItems.Count + 1}",
            IsTrue = true
        });
        StatusMessage = "Προστέθηκε πρόταση Σωστού / Λάθους";
    }

    private void DeleteTrueFalseItem(TrueFalseItem? item)
    {
        if (SelectedQuestion is null || item is null) return;
        SelectedQuestion.TrueFalseItems.Remove(item);
        StatusMessage = "Η πρόταση αφαιρέθηκε";
    }

    private void AddMatchingLeftItem()
    {
        if (SelectedQuestion is null) return;
        SelectedQuestion.MatchingLeftItems.Add(new MatchingLeftItem
        {
            Text = $"Στοιχείο Α{SelectedQuestion.MatchingLeftItems.Count + 1}"
        });
        StatusMessage = "Προστέθηκε στοιχείο στη Στήλη Α";
    }

    private void DeleteMatchingLeftItem(MatchingLeftItem? item)
    {
        if (SelectedQuestion is null || item is null) return;
        var relations = SelectedQuestion.MatchingRelations
            .Where(relation => relation.LeftItemId == item.Id)
            .ToList();
        foreach (var relation in relations) SelectedQuestion.MatchingRelations.Remove(relation);
        SelectedQuestion.MatchingLeftItems.Remove(item);
        StatusMessage = "Το στοιχείο της Στήλης Α αφαιρέθηκε μαζί με τις σχέσεις του";
    }

    private void AddMatchingRightItem()
    {
        if (SelectedQuestion is null) return;
        SelectedQuestion.MatchingRightItems.Add(new MatchingRightItem
        {
            Text = $"Στοιχείο Β{SelectedQuestion.MatchingRightItems.Count + 1}"
        });
        StatusMessage = "Προστέθηκε στοιχείο στη Στήλη Β";
    }

    private void DeleteMatchingRightItem(MatchingRightItem? item)
    {
        if (SelectedQuestion is null || item is null) return;
        var relations = SelectedQuestion.MatchingRelations
            .Where(relation => relation.RightItemId == item.Id)
            .ToList();
        foreach (var relation in relations) SelectedQuestion.MatchingRelations.Remove(relation);
        SelectedQuestion.MatchingRightItems.Remove(item);
        StatusMessage = "Το στοιχείο της Στήλης Β αφαιρέθηκε μαζί με τις σχέσεις του";
    }

    private void AddMatchingRelation()
    {
        if (SelectedQuestion is null) return;
        if (SelectedQuestion.MatchingLeftItems.Count == 0 || SelectedQuestion.MatchingRightItems.Count == 0)
        {
            StatusMessage = "Πρόσθεσε πρώτα στοιχεία και στις δύο στήλες";
            return;
        }

        var leftId = SelectedQuestion.MatchingLeftItems[0].Id;
        var rightId = SelectedQuestion.MatchingRightItems[0].Id;
        SelectedQuestion.MatchingRelations.Add(new MatchingRelation
        {
            LeftItemId = leftId,
            RightItemId = rightId
        });
        StatusMessage = "Προστέθηκε σωστή σχέση αντιστοίχισης";
    }

    private void DeleteMatchingRelation(MatchingRelation? relation)
    {
        if (SelectedQuestion is null || relation is null) return;
        SelectedQuestion.MatchingRelations.Remove(relation);
        StatusMessage = "Η σχέση αντιστοίχισης αφαιρέθηκε";
    }

    private void ShuffleMatching()
    {
        if (SelectedQuestion is null) return;
        SelectedQuestion.MatchingShuffleSeed = Random.Shared.Next(1, int.MaxValue);
        StatusMessage = "Δημιουργήθηκε νέα τυχαία διάταξη για τις δύο στήλες";
    }

    private void AddFillBlankSentence()
    {
        if (SelectedQuestion is null) return;
        SelectedQuestion.FillBlankSentences.Add(new FillBlankSentence
        {
            MarkedText = $"Νέα πρόταση {SelectedQuestion.FillBlankSentences.Count + 1}"
        });
        StatusMessage = "Προστέθηκε πρόταση συμπλήρωσης κενού";
    }

    private void DeleteFillBlankSentence(FillBlankSentence? sentence)
    {
        if (SelectedQuestion is null || sentence is null) return;
        SelectedQuestion.FillBlankSentences.Remove(sentence);
        StatusMessage = "Η πρόταση συμπλήρωσης κενού αφαιρέθηκε";
    }

    private void AddMultipleChoiceOption()
    {
        if (SelectedQuestion is null) return;

        SelectedQuestion.MultipleChoiceOptions.Add(new MultipleChoiceOption
        {
            Text = $"Επιλογή {SelectedQuestion.MultipleChoiceOptions.Count + 1}",
            IsCorrect = SelectedQuestion.MultipleChoiceOptions.Count == 0
        });

        StatusMessage = "Προστέθηκε επιλογή πολλαπλής επιλογής";
    }

    private void DeleteMultipleChoiceOption(MultipleChoiceOption? option)
    {
        if (SelectedQuestion is null || option is null) return;

        var wasCorrect = option.IsCorrect;
        SelectedQuestion.MultipleChoiceOptions.Remove(option);

        if (wasCorrect && SelectedQuestion.MultipleChoiceOptions.Count > 0)
            SetCorrectMultipleChoiceOption(SelectedQuestion.MultipleChoiceOptions[0]);

        StatusMessage = "Η επιλογή αφαιρέθηκε";
    }

    private void SetCorrectMultipleChoiceOption(MultipleChoiceOption? selectedOption)
    {
        if (SelectedQuestion is null || selectedOption is null) return;

        foreach (var option in SelectedQuestion.MultipleChoiceOptions)
            option.IsCorrect = ReferenceEquals(option, selectedOption);

        StatusMessage = "Ορίστηκε η σωστή απάντηση πολλαπλής επιλογής";
    }

    private void DeleteQuestion()
    {
        if (SelectedQuestion is null) return;

        var section = Exam.Sections.FirstOrDefault(s => s.Questions.Contains(SelectedQuestion));
        if (section is null) return;

        var question = SelectedQuestion;
        section.Questions.Remove(question);
        SelectedSection = section;
        SelectedQuestion = section.Questions.FirstOrDefault();
        RefreshTotals();
        StatusMessage = $"Η ερώτηση {question.Code} διαγράφηκε";
    }

    private void SelectQuestion(ExamQuestion? question)
    {
        if (question is null) return;
        SelectedQuestion = question;
    }

    private static string GetSectionPrefix(ExamSection section)
    {
        var words = section.Title.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return words.Length == 0 ? "Ε" : words[^1];
    }

    private void WireExam(ExamDocument exam)
    {
        exam.PropertyChanged += Exam_PropertyChanged;
        exam.Sections.CollectionChanged += Sections_CollectionChanged;
        foreach (var section in exam.Sections) WireSection(section);
    }

    private void UnwireExam(ExamDocument exam)
    {
        exam.PropertyChanged -= Exam_PropertyChanged;
        exam.Sections.CollectionChanged -= Sections_CollectionChanged;
        foreach (var section in exam.Sections) UnwireSection(section);
    }

    private void WireSection(ExamSection section)
    {
        section.PropertyChanged += Section_PropertyChanged;
        section.Questions.CollectionChanged += Questions_CollectionChanged;
        foreach (var question in section.Questions) question.PropertyChanged += Question_PropertyChanged;
    }

    private void UnwireSection(ExamSection section)
    {
        section.PropertyChanged -= Section_PropertyChanged;
        section.Questions.CollectionChanged -= Questions_CollectionChanged;
        foreach (var question in section.Questions) question.PropertyChanged -= Question_PropertyChanged;
    }

    private void Sections_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        MarkDirty();
        if (e.OldItems is not null)
            foreach (ExamSection section in e.OldItems) UnwireSection(section);
        if (e.NewItems is not null)
            foreach (ExamSection section in e.NewItems) WireSection(section);
        RefreshTotals();
        InvalidatePreview();
    }

    private void Questions_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        MarkDirty();
        if (e.OldItems is not null)
            foreach (ExamQuestion question in e.OldItems) question.PropertyChanged -= Question_PropertyChanged;
        if (e.NewItems is not null)
            foreach (ExamQuestion question in e.NewItems) question.PropertyChanged += Question_PropertyChanged;
        RefreshTotals();
        InvalidatePreview();
    }

    private void Exam_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        MarkDirty();
        InvalidatePreview();
    }

    private void Section_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        MarkDirty();
        InvalidatePreview();
    }

    private void Question_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        MarkDirty();
        if (e.PropertyName == nameof(ExamQuestion.Points)) RefreshTotals();
        if (sender is ExamQuestion question && e.PropertyName == nameof(ExamQuestion.Type))
            EnsureQuestionStructure(question);
        InvalidatePreview();
    }

    private static void EnsureQuestionStructure(ExamQuestion question)
    {
        if (question.Type == QuestionType.TrueFalse && question.TrueFalseItems.Count == 0)
        {
            question.TrueFalseItems.Add(new TrueFalseItem { Statement = "Νέα πρόταση", IsTrue = true });
        }
        else if (question.Type == QuestionType.Matching && question.MatchingLeftItems.Count == 0 && question.MatchingRightItems.Count == 0)
        {
            var left1 = new MatchingLeftItem { Text = "Στοιχείο Α1" };
            var left2 = new MatchingLeftItem { Text = "Στοιχείο Α2" };
            var right1 = new MatchingRightItem { Text = "Στοιχείο Β1" };
            var right2 = new MatchingRightItem { Text = "Στοιχείο Β2" };
            question.MatchingLeftItems.Add(left1);
            question.MatchingLeftItems.Add(left2);
            question.MatchingRightItems.Add(right1);
            question.MatchingRightItems.Add(right2);
            question.MatchingRelations.Add(new MatchingRelation { LeftItemId = left1.Id, RightItemId = right1.Id });
            question.MatchingRelations.Add(new MatchingRelation { LeftItemId = left2.Id, RightItemId = right2.Id });
            question.MatchingShuffleSeed = Random.Shared.Next(1, int.MaxValue);
        }
        else if (question.Type == QuestionType.FillBlank && question.FillBlankSentences.Count == 0)
        {
            question.Text = string.IsNullOrWhiteSpace(question.Text) || question.Text == "Νέα ερώτηση"
                ? "Να συμπληρώσετε τα κενά στις παρακάτω προτάσεις."
                : question.Text;
            question.FillBlankSentences.Add(new FillBlankSentence { MarkedText = "Νέα πρόταση" });
        }
        else if (question.Type == QuestionType.MultipleChoice && question.MultipleChoiceOptions.Count == 0)
        {
            question.MultipleChoiceOptions.Add(new MultipleChoiceOption { Text = "Επιλογή 1", IsCorrect = true });
            question.MultipleChoiceOptions.Add(new MultipleChoiceOption { Text = "Επιλογή 2" });
            question.MultipleChoiceOptions.Add(new MultipleChoiceOption { Text = "Επιλογή 3" });
            question.MultipleChoiceOptions.Add(new MultipleChoiceOption { Text = "Επιλογή 4" });
        }
    }

    private static void NormalizeExam(ExamDocument exam)
    {
        foreach (var section in exam.Sections)
            foreach (var question in section.Questions)
                question.NormalizeLegacyStructures();
    }

    private static string CreateExamSnapshot(ExamDocument exam) =>
        JsonSerializer.Serialize(exam);

    private void CaptureCleanState()
    {
        _cleanExamSnapshot = CreateExamSnapshot(Exam);
        IsDirty = false;
    }

    private void MarkDirty() => IsDirty = true;

    private bool ConfirmDiscardChanges()
    {
        if (!HasUnsavedChanges) return true;

        var result = MessageBox.Show(
            "Υπάρχουν μη αποθηκευμένες αλλαγές στο τρέχον διαγώνισμα. Να συνεχίσει η ενέργεια και να απορριφθούν;",
            "Μη αποθηκευμένες αλλαγές",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        return result == MessageBoxResult.Yes;
    }

    private void RefreshTotals()
    {
        foreach (var section in Exam.Sections) section.RefreshTotal();
        Exam.RefreshTotal();
        Raise(nameof(Exam));
        Raise(nameof(TotalStatus));
        InvalidatePreview();
    }

    private void InvalidatePreview() => PreviewInvalidated?.Invoke(this, EventArgs.Empty);

    private static ExamDocument CreateSampleExam()
    {
        var exam = new ExamDocument();
        exam.Sections.Add(new ExamSection
        {
            Title = "ΘΕΜΑ Α",
            Questions = new ObservableCollection<ExamQuestion>
            {
                new()
                {
                    Code = "Α1",
                    Text = "Να χαρακτηρίσετε τις παρακάτω προτάσεις ως Σωστές ή Λανθασμένες.",
                    Points = 10,
                    Type = QuestionType.TrueFalse,
                    TrueFalseItems = new ObservableCollection<TrueFalseItem>
                    {
                        new() { Statement = "Η δομή επιλογής μπορεί να έχει δύο κλάδους.", IsTrue = true },
                        new() { Statement = "Η εντολή ΓΙΑ χρησιμοποιείται μόνο όταν δεν γνωρίζουμε το πλήθος επαναλήψεων.", IsTrue = false }
                    }
                },
                new() { Code = "Α2", Text = "Να γράψετε τον ορισμό της δομής δεδομένων.", Points = 15, AnswerLines = 8 }
            }
        });
        exam.Sections.Add(new ExamSection
        {
            Title = "ΘΕΜΑ Β",
            Questions = new ObservableCollection<ExamQuestion>
            {
                new() { Code = "Β1", Text = "Να μετατρέψετε τον παρακάτω αλγόριθμο σε πρόγραμμα ΓΛΩΣΣΑ.", Points = 25, AnswerLines = 16 }
            }
        });
        exam.Sections.Add(new ExamSection
        {
            Title = "ΘΕΜΑ Γ",
            Questions = new ObservableCollection<ExamQuestion>
            {
                new() { Code = "Γ1", Text = "Να γράψετε συνάρτηση που δέχεται πίνακα 10 ακεραίων και επιστρέφει το πλήθος των θετικών στοιχείων.", Points = 25, AnswerLines = 14 }
            }
        });
        exam.Sections.Add(new ExamSection
        {
            Title = "ΘΕΜΑ Δ",
            Questions = new ObservableCollection<ExamQuestion>
            {
                new() { Code = "Δ1", Text = "Να αναπτύξετε ολοκληρωμένο αλγόριθμο σύμφωνα με τις εκφωνήσεις του θέματος.", Points = 25, AnswerLines = 20 }
            }
        });
        return exam;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private bool Set<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        Raise(name);
        return true;
    }

    private void Raise([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
