using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ExamBuilderGR.Models;

public sealed class ExamDocument : INotifyPropertyChanged
{
    private string _title = "1ο Διαγώνισμα ΑΕΠΠ";
    private string _subject = "ΑΕΠΠ";
    private string _grade = "Γ' Λυκείου";
    private string _classSection = string.Empty;
    private string _orientation = "Οικονομίας και Πληροφορικής";
    private DateTime _examDate = DateTime.Today;
    private int _durationMinutes = 180;
    private string _instructions = "Να απαντήσετε σε όλα τα θέματα. Οι απαντήσεις να είναι σαφείς και τεκμηριωμένες.";
    private bool _generateAnswerKey;
    private ObservableCollection<ExamSection> _sections = new();

    public ExamDocument()
    {
        _sections.CollectionChanged += OnSectionsChanged;
    }

    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get => _title; set => Set(ref _title, value); }
    public string Subject { get => _subject; set => Set(ref _subject, value); }
    public string Grade { get => _grade; set => Set(ref _grade, value); }
    public string ClassSection { get => _classSection; set => Set(ref _classSection, value); }
    public string Orientation { get => _orientation; set => Set(ref _orientation, value); }
    public DateTime ExamDate { get => _examDate; set => Set(ref _examDate, value); }
    public int DurationMinutes { get => _durationMinutes; set => Set(ref _durationMinutes, Math.Max(0, value)); }
    public string Instructions { get => _instructions; set => Set(ref _instructions, value); }
    public bool GenerateAnswerKey { get => _generateAnswerKey; set => Set(ref _generateAnswerKey, value); }

    public ObservableCollection<ExamSection> Sections
    {
        get => _sections;
        set
        {
            if (ReferenceEquals(_sections, value)) return;
            _sections.CollectionChanged -= OnSectionsChanged;
            _sections = value ?? new ObservableCollection<ExamSection>();
            _sections.CollectionChanged += OnSectionsChanged;
            UpdatedAt = DateTime.Now;
            Raise();
            RefreshTotal();
        }
    }

    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
    public int TotalPoints => Sections.Sum(s => s.TotalPoints);

    public event PropertyChangedEventHandler? PropertyChanged;

    public void RefreshTotal() => Raise(nameof(TotalPoints));

    private void OnSectionsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        UpdatedAt = DateTime.Now;
        RefreshTotal();
    }

    private void Set<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return;
        field = value;
        UpdatedAt = DateTime.Now;
        Raise(name);
    }

    private void Raise([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
