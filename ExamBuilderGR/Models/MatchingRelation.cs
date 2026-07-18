using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ExamBuilderGR.Models;

public sealed class MatchingRelation : INotifyPropertyChanged
{
    private Guid _leftItemId;
    private Guid _rightItemId;

    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid LeftItemId { get => _leftItemId; set => Set(ref _leftItemId, value); }
    public Guid RightItemId { get => _rightItemId; set => Set(ref _rightItemId, value); }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void Set<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return;
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
