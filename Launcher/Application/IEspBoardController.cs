namespace Launcher.Application;

/// <summary>
/// RU: Управление ESP без зависимости от транспорта. false означает недоступность
/// или отсутствие подтверждения команды; отмена завершается OperationCanceledException.
/// DE: Transportunabhaengige ESP-Steuerung; false bedeutet keine Bestaetigung.
/// </summary>
public interface IEspBoardController : IDisposable
{
    Task<bool> CheckAvailabilityAsync(CancellationToken cancellationToken);
    Task<bool> SetD1Async(bool enabled, CancellationToken cancellationToken);
}
