using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ExamBuilderGR.Models;

public sealed class MatchingRightItem : INotifyPropertyChanged
{
    private string _text = "Στοιχείο Στήλης Β";

    public Guid Id { get; set; } = Guid.NewGuid();
    public string Text { get => _text; set => Set(ref _text, value ?? string.Empty); }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void Set<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return;
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
