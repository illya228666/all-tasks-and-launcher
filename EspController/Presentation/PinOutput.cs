using System.ComponentModel;

namespace EspController.Presentation;

internal sealed class PinOutput : INotifyPropertyChanged
{
    internal PinOutput(int number)
    {
        Number = number;
    }

    public int Number { get; }
    public string Name => $"D{Number}";
    public string Level { get; private set; } = "—";

    public event PropertyChangedEventHandler? PropertyChanged;

    internal void Update(int? levels)
    {
        string level = levels is int mask ? ((mask & (1 << Number)) != 0 ? "HIGH" : "LOW") : "—";
        if (level == Level) return;
        Level = level;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Level)));
    }
}
