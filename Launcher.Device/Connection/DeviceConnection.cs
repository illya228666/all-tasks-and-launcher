using Launcher.Device.Data;
using Launcher.Device.Protocol;
namespace Launcher.Device.Connection;

// One worker owns the transport. The lock protects only lifecycle and the latest pending output.
public sealed class DeviceConnection : IAsyncDisposable
{
    private readonly object _gate = new();
    private readonly IDeviceTransportFactory _factory;
    private readonly string? _onlyPort;
    private readonly DeviceConnectionTiming _timing;
    private readonly CancellationTokenSource _lifetime = new();
    private Task _loop = Task.CompletedTask;
    private DeviceState _state = new(DeviceConnectionPhase.Waiting);
    private PendingDeviceOutput? _pending;
    private bool _started, _stopped, _disposed;
    public event Action<DeviceState>? StateChanged;
    public event Action<DeviceInput>? InputReceived;
    public event Action<byte>? FadeReceived;
    public event Action<DeviceDiceRoll>? DiceReceived;
    public event Action<DeviceDisplay>? DisplayReceived;
    public DeviceState State { get { lock (_gate) return _state; } }
    public DeviceConnection() : this(new SerialDeviceTransportFactory(), new(), null) { }
    // A firmware update reconnects exclusively to its previously confirmed port.
    public DeviceConnection(string port) : this(new SerialDeviceTransportFactory(), new(), port) { }
    internal DeviceConnection(IDeviceTransportFactory factory, DeviceConnectionTiming timing)
        : this(factory, timing, null) { }
    private DeviceConnection(IDeviceTransportFactory factory, DeviceConnectionTiming timing, string? onlyPort)
    {
        _factory = factory;
        _timing = timing;
        _onlyPort = onlyPort;
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
    public Task<DeviceCommandResult> SetDisplayValueAsync(int value)
    {
        if (value is < 0 or > 99) throw new ArgumentOutOfRangeException(nameof(value));
        return QueueDisplay("SET " + value, "OK SET");
    }
    public Task<DeviceCommandResult> SetDisplayBrightnessAsync(int value)
    {
        if (value is < 1 or > 100) throw new ArgumentOutOfRangeException(nameof(value));
        return QueueDisplay("BRIGHT " + value, "OK BRIGHT");
    }
    public Task<DeviceCommandResult> SetDisplayIntervalAsync(int milliseconds)
    {
        if (milliseconds is < 100 or > 2000) throw new ArgumentOutOfRangeException(nameof(milliseconds));
        return QueueDisplay("SPEED " + milliseconds, "OK SPEED");
    }
    public Task<DeviceCommandResult> SetDisplayModeAsync(DeviceDisplayMode mode)
    {
        if (mode is not (DeviceDisplayMode.Hold or DeviceDisplayMode.Up or DeviceDisplayMode.Down or DeviceDisplayMode.Bounce))
            throw new ArgumentOutOfRangeException(nameof(mode));
        return QueueDisplay("MODE " + mode.ToString().ToUpperInvariant(), "OK MODE");
    }
    public Task<DeviceCommandResult> StartDisplayTestAsync() => QueueDisplay("TEST", "OK TEST");
    private Task<DeviceCommandResult> QueueDisplay(string command, string expected) =>
        Queue(DeviceCapabilities.Display, false, DeviceLights.None, new DeviceWireCommand(command, expected));

    private Task<DeviceCommandResult> Queue(DeviceCapabilities capability, bool indicator, DeviceLights lights, DeviceWireCommand? wire = null)
    {
        lock (_gate)
        {
            if (_stopped) return Task.FromResult(DeviceCommandResult.Cancelled);
            if (!_state.IsConnected) return Task.FromResult(DeviceCommandResult.Disconnected);
            if ((_state.Capabilities & capability) == 0) return Task.FromResult(DeviceCommandResult.Unsupported);
            _pending?.Completion.TrySetResult(DeviceCommandResult.Superseded);
            var completion = new TaskCompletionSource<DeviceCommandResult>(TaskCreationOptions.RunContinuationsAsynchronously);
            _pending = new(capability, indicator, lights, completion, wire);
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
                try { ports = _onlyPort is null ? _factory.GetPortNames() : new[] { _onlyPort }; }
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
        uint? lastDiceSequence = null;
        DeviceDisplay? lastDisplay = null;
        while (true)
        {
            token.ThrowIfCancellationRequested();
            PendingDeviceOutput? pending;
            lock (_gate) { pending = _pending; _pending = null; }
            if (pending is not null)
            {
                try
                {
                    var wire = pending.Wire ?? (pending.Capability == DeviceCapabilities.Indicator
                        ? protocol.Indicator(pending.Indicator) : protocol.Lights(pending.Lights));
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
            DevicePoll poll = protocol.ReadPoll(inputReply);
            if (poll.Input is DeviceInput input) InputReceived?.Invoke(input);
            if (poll.Fade is byte fade) FadeReceived?.Invoke(fade);
            if (poll.Dice is { } dice && dice.Sequence != lastDiceSequence)
            {
                lastDiceSequence = dice.Sequence;
                DiceReceived?.Invoke(dice);
            }
            if (poll.Display is { } display && display != lastDisplay)
            {
                lastDisplay = display;
                DisplayReceived?.Invoke(display);
            }
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
