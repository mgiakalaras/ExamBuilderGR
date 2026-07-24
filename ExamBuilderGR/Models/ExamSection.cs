using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ExamBuilderGR.Models;

public sealed class ExamSection : INotifyPropertyChanged
{
    private string _title = "ΘΕΜΑ Α";
    private string _introText = string.Empty;
    private ObservableCollection<ExamQuestion> _questions = new();
    private ObservableCollection<ExamImageAsset> _images = new();

    public ExamSection()
    {
        WireQuestions(_questions);
        WireImages(_images);
    }

    /// <summary>
    /// Ο σύντομος τίτλος του θέματος, π.χ. ΘΕΜΑ Α.
    /// </summary>
    public string Title { get => _title; set => Set(ref _title, value ?? string.Empty); }

    /// <summary>
    /// Προαιρετική κεντρική εκφώνηση/σενάριο που προηγείται των υποερωτημάτων Α1, Α2, Α3 κ.λπ.
    /// </summary>
    public string IntroText { get => _introText; set => Set(ref _introText, value ?? string.Empty); }

    public ObservableCollection<ExamQuestion> Questions
    {
        get => _questions;
        set
        {
            if (ReferenceEquals(_questions, value)) return;
            UnwireQuestions(_questions);
            _questions = value ?? new ObservableCollection<ExamQuestion>();
            WireQuestions(_questions);
            Raise();
            RefreshTotal();
        }
    }


    public ObservableCollection<ExamImageAsset> Images
    {
        get => _images;
        set
        {
            if (ReferenceEquals(_images, value)) return;
            UnwireImages(_images);
            _images = value ?? new ObservableCollection<ExamImageAsset>();
            WireImages(_images);
            Raise();
        }
    }

    /// <summary>
    /// Το σύνολο του θέματος υπολογίζεται αυτόματα από τις μονάδες των υποερωτημάτων.
    /// </summary>
    public int TotalPoints => Questions.Sum(question => question.Points);

    public event PropertyChangedEventHandler? PropertyChanged;

    public void RefreshTotal() => Raise(nameof(TotalPoints));

    private void WireQuestions(ObservableCollection<ExamQuestion> questions)
    {
        questions.CollectionChanged += OnQuestionsChanged;
        foreach (var question in questions)
            question.PropertyChanged += Question_PropertyChanged;
    }

    private void UnwireQuestions(ObservableCollection<ExamQuestion> questions)
    {
        questions.CollectionChanged -= OnQuestionsChanged;
        foreach (var question in questions)
            question.PropertyChanged -= Question_PropertyChanged;
    }

    private void OnQuestionsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems is not null)
            foreach (ExamQuestion question in e.OldItems)
                question.PropertyChanged -= Question_PropertyChanged;

        if (e.NewItems is not null)
            foreach (ExamQuestion question in e.NewItems)
                question.PropertyChanged += Question_PropertyChanged;

        RefreshTotal();
    }

    private void Question_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ExamQuestion.Points))
            RefreshTotal();
    }


    private void WireImages(ObservableCollection<ExamImageAsset> images)
    {
        images.CollectionChanged += OnImagesChanged;
        foreach (var image in images) image.PropertyChanged += Image_PropertyChanged;
    }

    private void UnwireImages(ObservableCollection<ExamImageAsset> images)
    {
        images.CollectionChanged -= OnImagesChanged;
        foreach (var image in images) image.PropertyChanged -= Image_PropertyChanged;
    }

    private void OnImagesChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems is not null)
            foreach (ExamImageAsset image in e.OldItems) image.PropertyChanged -= Image_PropertyChanged;
        if (e.NewItems is not null)
            foreach (ExamImageAsset image in e.NewItems) image.PropertyChanged += Image_PropertyChanged;
        Raise(nameof(Images));
    }

    private void Image_PropertyChanged(object? sender, PropertyChangedEventArgs e) => Raise(nameof(Images));

    private void Set<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return;
        field = value;
        Raise(name);
    }

    private void Raise([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
