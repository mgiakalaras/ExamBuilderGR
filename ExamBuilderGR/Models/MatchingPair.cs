using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ExamBuilderGR.Models;

public sealed class MatchingPair : INotifyPropertyChanged
{
    private string _leftText = "Στοιχείο αριστερής στήλης";
    private string _rightText = "Σωστή αντιστοίχιση";

    public Guid Id { get; set; } = Guid.NewGuid();
    public string LeftText { get => _leftText; set => Set(ref _leftText, value); }
    public string RightText { get => _rightText; set => Set(ref _rightText, value); }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void Set<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return;
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
