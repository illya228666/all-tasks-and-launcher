using Launcher.Application;

namespace Launcher.UI;

public partial class Main
{
    // Кнопочное событие хранится на контроллере до POLL, поэтому частый опрос даёт
    // хорошую реакцию UI без зависимости приложения от электрического состояния входа.
    private const int EspPollIntervalMs = 100;
    private readonly CancellationTokenSource _espLifetime = new();
    private Task _espOperation = Task.CompletedTask;
    private bool _closing;
    private bool _readyToClose;

    private void BeginEspOperation(bool? indicatorEnabled = null)
    {
        // Tick и Click приходят в UI-поток: новый poll/command не запускается поверх текущего.
        if (_closing || IsDisposed || !_espOperation.IsCompleted)
            return;

        _espOperation = UpdateEspAsync(indicatorEnabled);
    }

    private async Task UpdateEspAsync(bool? indicatorEnabled)
    {
        try
        {
            if (indicatorEnabled.HasValue)
            {
                SetEspButtonsEnabled(false);
                bool available = await _espBoard.SetIndicatorAsync(indicatorEnabled.Value, _espLifetime.Token);

                if (_closing || IsDisposed)
                    return;

                SetEspButtonsEnabled(available);
                ShowHint(available
                    ? (indicatorEnabled.Value ? "LED eingeschaltet." : "LED ausgeschaltet.")
                    : "Controller nicht erreichbar.");
                return;
            }

            DevicePollResult pollResult = await _espBoard.PollAsync(_espLifetime.Token);
            if (_closing || IsDisposed)
                return;

            SetEspButtonsEnabled(pollResult.IsConnected);
            if (pollResult.Input is DeviceInputEvent input)
                HandleDeviceInput(input);
        }
        catch (OperationCanceledException) when (_espLifetime.IsCancellationRequested)
        {
            // Закрытие формы отменяет текущую проверку/команду без сообщения пользователю.
        }
    }

    private void HandleDeviceInput(DeviceInputEvent input)
    {
        switch (input)
        {
            case DeviceInputEvent.PrimaryButtonPressed:
                _pet.TryStartEarthquake();
                break;
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
