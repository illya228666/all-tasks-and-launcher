namespace Launcher.UI;

public partial class Main
{
    private const int EspPollIntervalMs = 2000; // Период проверки подключения ESP.
    private readonly CancellationTokenSource _espLifetime = new();
    private Task _espOperation = Task.CompletedTask;
    private bool _closing;
    private bool _readyToClose;

    private void BeginEspOperation(bool? enabled = null)
    {
        // Tick и Click приходят в UI-поток: новый scan/command не запускается поверх текущего.
        if (_closing || IsDisposed || !_espOperation.IsCompleted)
            return;

        _espOperation = UpdateEspAsync(enabled);
    }

    private async Task UpdateEspAsync(bool? enabled)
    {
        SetEspButtonsEnabled(false);
        try
        {
            bool available = enabled.HasValue
                ? await _espBoard.SetD1Async(enabled.Value, _espLifetime.Token)
                : await _espBoard.CheckAvailabilityAsync(_espLifetime.Token);

            if (_closing || IsDisposed)
                return;

            SetEspButtonsEnabled(available);
            if (enabled.HasValue)
                ShowHint(available ? (enabled.Value ? "D1 eingeschaltet." : "D1 ausgeschaltet.") : "ESP nicht erreichbar.");
        }
        catch (OperationCanceledException) when (_espLifetime.IsCancellationRequested)
        {
            // Закрытие формы отменяет текущую проверку/команду без сообщения пользователю.
        }
    }

    private void SetEspButtonsEnabled(bool enabled)
    {
        _btnD1On.Enabled = enabled;
        _btnD1Off.Enabled = enabled;
    }

    private async void Main_FormClosing(object? sender, FormClosingEventArgs e)
    {
        if (_readyToClose || e.Cancel)
            return;

        e.Cancel = true;
        if (_closing)
            return;

        _closing = true;
        _espPollTimer.Stop();
        _espLifetime.Cancel();
        SetEspButtonsEnabled(false);
        _pet.Stop();
        PersistState();

        // Форма остаётся живой, пока фоновый IO и его UI-продолжение не завершатся.
        await _espOperation;
        _readyToClose = true;
        // Отложенный Close также исключает повторный вход в первый FormClosing.
        if (!IsDisposed)
            BeginInvoke(new Action(Close));
    }

    private void DisposeEsp()
    {
        _closing = true;
        _espLifetime.Cancel();
        _espBoard?.Dispose();
        _espLifetime.Dispose();
    }
}
