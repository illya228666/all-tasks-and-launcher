namespace Launcher.Windows;
internal sealed record CategoryChoice(string? Category)
{
    public override string ToString() => Category ?? "Alle Kategorien";
}
