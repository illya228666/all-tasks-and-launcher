using System.Windows.Input;

namespace EspController.Presentation;

internal sealed class AsyncCommand : ICommand
{
    private readonly Func<object?, Task> _execute;
    private readonly Func<bool> _canExecute;
    private bool _running;
    internal AsyncCommand(Func<object?, Task> execute, Func<bool> canExecute)
    {
        _execute = execute;
        _canExecute = canExecute;
    }
    public event EventHandler? CanExecuteChanged;
    public bool CanExecute(object? parameter) => !_running && _canExecute();
    public async void Execute(object? parameter)
    {
        if (!CanExecute(parameter)) return;
        _running = true;
        Refresh();
        try { await _execute(parameter); }
        finally { _running = false; Refresh(); }
    }
    internal void Refresh() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}
