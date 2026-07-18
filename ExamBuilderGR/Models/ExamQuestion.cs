using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace ExamBuilderGR.Models;

public sealed class ExamQuestion : INotifyPropertyChanged
{
    private string _code = "Α1";
    private string _text = string.Empty;
    private int _points = 5;
    private int _answerLines = 6;
    private QuestionType _type = QuestionType.Development;
    private AnswerAreaType _answerAreaType = AnswerAreaType.Lines;
    private string _modelAnswer = string.Empty;

    // Legacy fields kept for backward compatibility with v0.7.1 and older files.
    private string _fillBlankAnswer = string.Empty;
    private ObservableCollection<MatchingPair> _matchingPairs = new();

    private ObservableCollection<TrueFalseItem> _trueFalseItems = new();
    private ObservableCollection<MultipleChoiceOption> _multipleChoiceOptions = new();
    private ObservableCollection<FillBlankSentence> _fillBlankSentences = new();
    private ObservableCollection<MatchingLeftItem> _matchingLeftItems = new();
    private ObservableCollection<MatchingRightItem> _matchingRightItems = new();
    private ObservableCollection<MatchingRelation> _matchingRelations = new();
    private int _matchingShuffleSeed = Random.Shared.Next(1, int.MaxValue);

    public ExamQuestion()
    {
        WireTrueFalseItems(_trueFalseItems);
        WireMultipleChoiceOptions(_multipleChoiceOptions);
        WireFillBlankSentences(_fillBlankSentences);
        WireMatchingLeftItems(_matchingLeftItems);
        WireMatchingRightItems(_matchingRightItems);
        WireMatchingRelations(_matchingRelations);
        WireMatchingPairs(_matchingPairs);
    }

    public string Code { get => _code; set => Set(ref _code, value ?? string.Empty); }
    public string Text { get => _text; set => Set(ref _text, value ?? string.Empty); }
    public int Points { get => _points; set => Set(ref _points, Math.Max(0, value)); }

    public int AnswerLines
    {
        get => _answerLines;
        set
        {
            if (Set(ref _answerLines, Math.Clamp(value, 0, 40)))
                Raise(nameof(AnswerLineItems));
        }
    }

    public QuestionType Type { get => _type; set => Set(ref _type, value); }
    public AnswerAreaType AnswerAreaType { get => _answerAreaType; set => Set(ref _answerAreaType, value); }
    public string ModelAnswer { get => _modelAnswer; set => Set(ref _modelAnswer, value ?? string.Empty); }

    /// <summary>
    /// Legacy single-answer field. It remains serializable so older .exam.json files can migrate.
    /// New fill-blank questions use FillBlankSentences.
    /// </summary>
    public string FillBlankAnswer { get => _fillBlankAnswer; set => Set(ref _fillBlankAnswer, value ?? string.Empty); }

    /// <summary>
    /// Legacy one-to-one matching pairs. New matching questions use independent columns and relations.
    /// </summary>
    public ObservableCollection<MatchingPair> MatchingPairs
    {
        get => _matchingPairs;
        set
        {
            if (ReferenceEquals(_matchingPairs, value)) return;
            UnwireMatchingPairs(_matchingPairs);
            _matchingPairs = value ?? new ObservableCollection<MatchingPair>();
            WireMatchingPairs(_matchingPairs);
            Raise();
        }
    }

    public ObservableCollection<TrueFalseItem> TrueFalseItems
    {
        get => _trueFalseItems;
        set
        {
            if (ReferenceEquals(_trueFalseItems, value)) return;
            UnwireTrueFalseItems(_trueFalseItems);
            _trueFalseItems = value ?? new ObservableCollection<TrueFalseItem>();
            WireTrueFalseItems(_trueFalseItems);
            Raise();
        }
    }

    public ObservableCollection<MultipleChoiceOption> MultipleChoiceOptions
    {
        get => _multipleChoiceOptions;
        set
        {
            if (ReferenceEquals(_multipleChoiceOptions, value)) return;
            UnwireMultipleChoiceOptions(_multipleChoiceOptions);
            _multipleChoiceOptions = value ?? new ObservableCollection<MultipleChoiceOption>();
            WireMultipleChoiceOptions(_multipleChoiceOptions);
            Raise();
        }
    }

    public ObservableCollection<FillBlankSentence> FillBlankSentences
    {
        get => _fillBlankSentences;
        set
        {
            if (ReferenceEquals(_fillBlankSentences, value)) return;
            UnwireFillBlankSentences(_fillBlankSentences);
            _fillBlankSentences = value ?? new ObservableCollection<FillBlankSentence>();
            WireFillBlankSentences(_fillBlankSentences);
            Raise();
        }
    }

    public ObservableCollection<MatchingLeftItem> MatchingLeftItems
    {
        get => _matchingLeftItems;
        set
        {
            if (ReferenceEquals(_matchingLeftItems, value)) return;
            UnwireMatchingLeftItems(_matchingLeftItems);
            _matchingLeftItems = value ?? new ObservableCollection<MatchingLeftItem>();
            WireMatchingLeftItems(_matchingLeftItems);
            Raise();
        }
    }

    public ObservableCollection<MatchingRightItem> MatchingRightItems
    {
        get => _matchingRightItems;
        set
        {
            if (ReferenceEquals(_matchingRightItems, value)) return;
            UnwireMatchingRightItems(_matchingRightItems);
            _matchingRightItems = value ?? new ObservableCollection<MatchingRightItem>();
            WireMatchingRightItems(_matchingRightItems);
            Raise();
        }
    }

    public ObservableCollection<MatchingRelation> MatchingRelations
    {
        get => _matchingRelations;
        set
        {
            if (ReferenceEquals(_matchingRelations, value)) return;
            UnwireMatchingRelations(_matchingRelations);
            _matchingRelations = value ?? new ObservableCollection<MatchingRelation>();
            WireMatchingRelations(_matchingRelations);
            Raise();
        }
    }

    public int MatchingShuffleSeed
    {
        get => _matchingShuffleSeed;
        set => Set(ref _matchingShuffleSeed, value == 0 ? 1 : value);
    }

    [JsonIgnore]
    public IEnumerable<int> AnswerLineItems => Enumerable.Range(1, Math.Max(0, AnswerLines));

    [JsonIgnore]
    public string FillBlankStudentText => FillBlankSentences.Count > 0
        ? string.Join(Environment.NewLine, FillBlankSentences.Select(sentence => sentence.StudentText))
        : Text;

    /// <summary>
    /// Converts older single-blank and one-to-one matching data to the current structures.
    /// Calling it repeatedly is safe.
    /// </summary>
    public void NormalizeLegacyStructures()
    {
        if (Type == QuestionType.FillBlank && FillBlankSentences.Count == 0 && !string.IsNullOrWhiteSpace(FillBlankAnswer))
        {
            var marked = Text ?? string.Empty;
            var index = marked.IndexOf(FillBlankAnswer, StringComparison.CurrentCultureIgnoreCase);
            if (index >= 0)
            {
                marked = marked[..index] + "[[" + marked.Substring(index, FillBlankAnswer.Length) + "]]" +
                         marked[(index + FillBlankAnswer.Length)..];
            }

            FillBlankSentences.Add(new FillBlankSentence { MarkedText = marked });
            Text = "Να συμπληρώσετε τα κενά στις παρακάτω προτάσεις.";
        }

        if (Type == QuestionType.Matching && MatchingLeftItems.Count == 0 && MatchingRightItems.Count == 0 && MatchingPairs.Count > 0)
        {
            foreach (var pair in MatchingPairs)
            {
                var left = new MatchingLeftItem { Id = pair.Id, Text = pair.LeftText };
                var right = new MatchingRightItem { Text = pair.RightText };
                MatchingLeftItems.Add(left);
                MatchingRightItems.Add(right);
                MatchingRelations.Add(new MatchingRelation
                {
                    LeftItemId = left.Id,
                    RightItemId = right.Id
                });
            }
        }

        if (MatchingShuffleSeed == 0)
            MatchingShuffleSeed = Random.Shared.Next(1, int.MaxValue);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void WireTrueFalseItems(ObservableCollection<TrueFalseItem> items) => WireCollection(items, TrueFalseItems_CollectionChanged);
    private void UnwireTrueFalseItems(ObservableCollection<TrueFalseItem> items) => UnwireCollection(items, TrueFalseItems_CollectionChanged);
    private void WireMultipleChoiceOptions(ObservableCollection<MultipleChoiceOption> items) => WireCollection(items, MultipleChoiceOptions_CollectionChanged);
    private void UnwireMultipleChoiceOptions(ObservableCollection<MultipleChoiceOption> items) => UnwireCollection(items, MultipleChoiceOptions_CollectionChanged);
    private void WireFillBlankSentences(ObservableCollection<FillBlankSentence> items) => WireCollection(items, FillBlankSentences_CollectionChanged);
    private void UnwireFillBlankSentences(ObservableCollection<FillBlankSentence> items) => UnwireCollection(items, FillBlankSentences_CollectionChanged);
    private void WireMatchingLeftItems(ObservableCollection<MatchingLeftItem> items) => WireCollection(items, MatchingLeftItems_CollectionChanged);
    private void UnwireMatchingLeftItems(ObservableCollection<MatchingLeftItem> items) => UnwireCollection(items, MatchingLeftItems_CollectionChanged);
    private void WireMatchingRightItems(ObservableCollection<MatchingRightItem> items) => WireCollection(items, MatchingRightItems_CollectionChanged);
    private void UnwireMatchingRightItems(ObservableCollection<MatchingRightItem> items) => UnwireCollection(items, MatchingRightItems_CollectionChanged);
    private void WireMatchingRelations(ObservableCollection<MatchingRelation> items) => WireCollection(items, MatchingRelations_CollectionChanged);
    private void UnwireMatchingRelations(ObservableCollection<MatchingRelation> items) => UnwireCollection(items, MatchingRelations_CollectionChanged);
    private void WireMatchingPairs(ObservableCollection<MatchingPair> items) => WireCollection(items, MatchingPairs_CollectionChanged);
    private void UnwireMatchingPairs(ObservableCollection<MatchingPair> items) => UnwireCollection(items, MatchingPairs_CollectionChanged);

    private void WireCollection<T>(ObservableCollection<T> items, NotifyCollectionChangedEventHandler handler)
        where T : INotifyPropertyChanged
    {
        items.CollectionChanged += handler;
        foreach (var item in items) item.PropertyChanged += NestedItem_PropertyChanged;
    }

    private void UnwireCollection<T>(ObservableCollection<T> items, NotifyCollectionChangedEventHandler handler)
        where T : INotifyPropertyChanged
    {
        items.CollectionChanged -= handler;
        foreach (var item in items) item.PropertyChanged -= NestedItem_PropertyChanged;
    }

    private void TrueFalseItems_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) => HandleNestedCollectionChanged(e, nameof(TrueFalseItems));
    private void MultipleChoiceOptions_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) => HandleNestedCollectionChanged(e, nameof(MultipleChoiceOptions));
    private void FillBlankSentences_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) => HandleNestedCollectionChanged(e, nameof(FillBlankSentences));
    private void MatchingLeftItems_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) => HandleNestedCollectionChanged(e, nameof(MatchingLeftItems));
    private void MatchingRightItems_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) => HandleNestedCollectionChanged(e, nameof(MatchingRightItems));
    private void MatchingRelations_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) => HandleNestedCollectionChanged(e, nameof(MatchingRelations));
    private void MatchingPairs_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) => HandleNestedCollectionChanged(e, nameof(MatchingPairs));

    private void HandleNestedCollectionChanged(NotifyCollectionChangedEventArgs e, string propertyName)
    {
        if (e.OldItems is not null)
            foreach (INotifyPropertyChanged item in e.OldItems) item.PropertyChanged -= NestedItem_PropertyChanged;
        if (e.NewItems is not null)
            foreach (INotifyPropertyChanged item in e.NewItems) item.PropertyChanged += NestedItem_PropertyChanged;
        Raise(propertyName);
        if (propertyName == nameof(FillBlankSentences)) Raise(nameof(FillBlankStudentText));
    }

    private void NestedItem_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        var propertyName = sender switch
        {
            TrueFalseItem => nameof(TrueFalseItems),
            MultipleChoiceOption => nameof(MultipleChoiceOptions),
            FillBlankSentence => nameof(FillBlankSentences),
            MatchingLeftItem => nameof(MatchingLeftItems),
            MatchingRightItem => nameof(MatchingRightItems),
            MatchingRelation => nameof(MatchingRelations),
            MatchingPair => nameof(MatchingPairs),
            _ => null
        };

        if (propertyName is not null)
        {
            Raise(propertyName);
            if (propertyName == nameof(FillBlankSentences)) Raise(nameof(FillBlankStudentText));
        }
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
}
