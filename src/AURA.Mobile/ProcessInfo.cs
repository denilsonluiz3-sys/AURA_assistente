using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace AURA.Mobile;

public sealed class ProcessInfo : INotifyPropertyChanged
{
    private string _status = "Pendente";
    private string _message = string.Empty;
    private double _progress;

    public string Id { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Target { get; init; } = string.Empty;

    public string Status
    {
        get => _status;
        set => Set(ref _status, value);
    }

    public string Message
    {
        get => _message;
        set => Set(ref _message, value);
    }

    public double Progress
    {
        get => _progress;
        set => Set(ref _progress, value);
    }

    public DateTime StartedAt { get; init; } = DateTime.UtcNow;

    public event PropertyChangedEventHandler? PropertyChanged;

    private void Set<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return;
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
