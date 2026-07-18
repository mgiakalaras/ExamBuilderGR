using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ExamBuilderGR.Models;

public sealed class TrueFalseItem : INotifyPropertyChanged
{
    private string _statement = "Νέα πρόταση";
    private bool _isTrue = true;

    public Guid Id { get; set; } = Guid.NewGuid();
    public string Statement { get => _statement; set => Set(ref _statement, value); }
    public bool IsTrue { get => _isTrue; set => Set(ref _isTrue, value); }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void Set<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return;
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
