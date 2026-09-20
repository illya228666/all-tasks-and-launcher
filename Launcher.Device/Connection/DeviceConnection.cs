using Launcher.Device.Data;
using Launcher.Device.Protocol;
namespace Launcher.Device.Connection;

// One worker owns the transport. The lock protects only lifecycle and the latest pending output.
public sealed class DeviceConnection : IAsyncDisposable
{
    private readonly object _gate = new();
    private readonly IDeviceTransportFactory _factory;
    private readonly DeviceConnectionTiming _timing;
    private readonly CancellationTokenSource _lifetime = new();
    private Task _loop = Task.CompletedTask;
    private DeviceState _state = new(DeviceConnectionPhase.Waiting);
    private PendingDeviceOutput? _pending;
    private bool _started, _stopped, _disposed;
    public event Action<DeviceState>? StateChanged;
    public event Action<DeviceInput>? InputReceived;
    public DeviceState State { get { lock (_gate) return _state; } }
    public DeviceConnection() : this(new SerialDeviceTransportFactory(), new()) { }
    internal DeviceConnection(IDeviceTransportFactory factory, DeviceConnectionTiming timing)
    {
        _factory = factory;
        _timing = timing;
    }
    public void Start()
    {
        lock (_gate)
        {
            if (_started || _stopped) return;
            _started = true;
            _loop = Task.Run(RunAsync);
        }
    }
    public Task<DeviceCommandResult> SetIndicatorAsync(bool enabled) => Queue(DeviceCapabilities.Indicator, enabled, DeviceLights.None);
    public Task<DeviceCommandResult> SetLightsAsync(DeviceLights lights)
    {
        if ((lights & ~DeviceLights.All) != 0) throw new ArgumentOutOfRangeException(nameof(lights));
        return Queue(DeviceCapabilities.Lights, false, lights);
    }
    private Task<DeviceCommandResult> Queue(DeviceCapabilities capability, bool indicator, DeviceLights lights)
    {
        lock (_gate)
        {
            if (_stopped) return Task.FromResult(DeviceCommandResult.Cancelled);
            if (!_state.IsConnected) return Task.FromResult(DeviceCommandResult.Disconnected);
            if ((_state.Capabilities & capability) == 0) return Task.FromResult(DeviceCommandResult.Unsupported);
            _pending?.Completion.TrySetResult(DeviceCommandResult.Superseded);
            var completion = new TaskCompletionSource<DeviceCommandResult>(TaskCreationOptions.RunContinuationsAsynchronously);
            _pending = new(capability, indicator, lights, completion);
            return completion.Task;
        }
    }
    private void Publish(DeviceState state)
    {
        lock (_gate)
        {
            _state = state;
            if (!state.IsConnected)
            {
                _pending?.Completion.TrySetResult(_stopped ? DeviceCommandResult.Cancelled : DeviceCommandResult.Disconnected);
                _pending = null;
            }
        }
        StateChanged?.Invoke(state);
    }
    private async Task RunAsync()
    {
        var token = _lifetime.Token;
        bool faulted = false;
        try
        {
            while (true)
            {
                token.ThrowIfCancellationRequested();
                Publish(new(DeviceConnectionPhase.Searching));
                IReadOnlyList<string> ports;
                try { ports = _factory.GetPortNames(); }
                catch (Exception error) when (IsExpected(error))
                {
                    Publish(new(DeviceConnectionPhase.Retry, Failure: Classify(error), Detail: error.Message));
                    await Task.Delay(_timing.RetryDelayMs, token).ConfigureAwait(false);
                    continue;
                }
                if (ports.Count == 0) Publish(new(DeviceConnectionPhase.Retry, Failure: DeviceFailure.NoPorts));
                foreach (string name in ports)
                {
                    token.ThrowIfCancellationRequested();
                    Publish(new(DeviceConnectionPhase.Connecting, name));
                    try
                    {
                        using var transport = _factory.Open(name);
                        await Task.Delay(_timing.StartupDelayMs, token).ConfigureAwait(false);
                        string identity;
                        try { identity = transport.Exchange(DeviceProtocol.Hello, token); }
                        catch (TimeoutException)
                        {
                            await Task.Delay(_timing.HelloRetryMs, token).ConfigureAwait(false);
                            identity = transport.Exchange(DeviceProtocol.Hello, token);
                        }
                        var protocol = DeviceProtocol.FromIdentity(identity);
                        Publish(new(DeviceConnectionPhase.Connected, name, protocol.Version, protocol.Capabilities));
                        await ServeAsync(transport, protocol, token).ConfigureAwait(false);
                    }
                    catch (Exception error) when (IsExpected(error))
                    {
                        Publish(new(DeviceConnectionPhase.Retry, name, Failure: Classify(error), Detail: error.Message));
                    }
                }
                await Task.Delay(_timing.RetryDelayMs, token).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested) { }
        catch (Exception error)
        {
            faulted = true;
            Publish(new(DeviceConnectionPhase.Faulted, Failure: DeviceFailure.Unexpected, Detail: error.Message));
        }
        finally
        {
            lock (_gate) _stopped = true;
            if (!faulted) Publish(new(DeviceConnectionPhase.Stopped));
        }
    }
    private async Task ServeAsync(IDeviceTransport transport, IDeviceProtocolVersion protocol, CancellationToken token)
    {
        while (true)
        {
            token.ThrowIfCancellationRequested();
            PendingDeviceOutput? pending;
            lock (_gate) { pending = _pending; _pending = null; }
            if (pending is not null)
            {
                try
                {
                    var wire = pending.Capability == DeviceCapabilities.Indicator ? protocol.Indicator(pending.Indicator) : protocol.Lights(pending.Lights);
                    if (wire is null) pending.Completion.TrySetResult(DeviceCommandResult.Unsupported);
                    else
                    {
                        string reply = transport.Exchange(wire.Value.Command, token);
                        if (reply != wire.Value.ExpectedReply) throw new DeviceProtocolException(DeviceFailure.InvalidReply, reply);
                        pending.Completion.TrySetResult(DeviceCommandResult.Completed);
                    }
                }
                catch (OperationCanceledException) when (token.IsCancellationRequested)
                {
                    pending.Completion.TrySetResult(DeviceCommandResult.Cancelled);
                    throw;
                }
                catch
                {
                    pending.Completion.TrySetResult(DeviceCommandResult.Failed);
                    throw;
                }
            }
            string inputReply = transport.Exchange(protocol.PollCommand, token);
            if (protocol.ReadInput(inputReply) is DeviceInput input) InputReceived?.Invoke(input);
            await Task.Delay(_timing.PollIntervalMs, token).ConfigureAwait(false);
        }
    }
    private static bool IsExpected(Exception error) => error is IOException or UnauthorizedAccessException or TimeoutException or InvalidOperationException;
    private static DeviceFailure Classify(Exception error) => error switch
    {
        DeviceProtocolException protocol => protocol.Failure,
        UnauthorizedAccessException => DeviceFailure.AccessDenied,
        TimeoutException => DeviceFailure.Timeout,
        _ => DeviceFailure.Transport
    };
    public async Task StopAsync()
    {
        Task loop;
        lock (_gate)
        {
            if (!_stopped)
            {
                _stopped = true;
                _lifetime.Cancel();
            }
            _pending?.Completion.TrySetResult(DeviceCommandResult.Cancelled);
            _pending = null;
            loop = _loop;
        }
        await loop.ConfigureAwait(false);
        if (!_started) Publish(new(DeviceConnectionPhase.Stopped));
    }
    public async ValueTask DisposeAsync()
    {
        await StopAsync().ConfigureAwait(false);
        lock (_gate)
        {
            if (_disposed) return;
            _disposed = true;
            _lifetime.Dispose();
        }
    }
}
