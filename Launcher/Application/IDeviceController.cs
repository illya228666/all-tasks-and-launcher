namespace Launcher.Application;

public enum DeviceInputEvent
{
    PrimaryButtonPressed
}

// Событие ввода отдельно от доступности: подключение само по себе не является нажатием.
public readonly record struct DevicePollResult(bool IsConnected, DeviceInputEvent? Input = null);

/// <summary>
/// Семантический контракт устройства: без GPIO, serial-строк и поведения питомца.
/// Недоступность возвращается результатом; отмена — OperationCanceledException.
/// </summary>
public interface IDeviceController : IDisposable
{
    Task<DevicePollResult> PollAsync(CancellationToken cancellationToken);
    Task<bool> SetIndicatorAsync(bool enabled, CancellationToken cancellationToken);
}
