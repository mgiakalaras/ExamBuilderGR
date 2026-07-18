using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ExamBuilderGR.Models;

public sealed class MultipleChoiceOption : INotifyPropertyChanged
{
    private string _text = string.Empty;
    private bool _isCorrect;

    public Guid Id { get; set; } = Guid.NewGuid();

    public string Text
    {
        get => _text;
        set => Set(ref _text, value);
    }

    public bool IsCorrect
    {
        get => _isCorrect;
        set => Set(ref _isCorrect, value);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private bool Set<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        return true;
    }
}
