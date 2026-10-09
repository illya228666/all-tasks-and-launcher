using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Threading;
using Launcher.Device.Connection;
using Launcher.Device.Data;

namespace EspController.Presentation;

internal sealed class ControllerViewModel : INotifyPropertyChanged, IAsyncDisposable
{
    private readonly Dispatcher _dispatcher;
    private readonly Func<DeviceConnection> _createConnection;
    private DeviceConnection? _connection;
    private DeviceState _state = new(DeviceConnectionPhase.Waiting);
    private bool _busy, _closing, _indicator;
    private DeviceLights _lights;
    private byte? _fade;
    private DeviceDiceRoll? _diceRoll;
    private int _inputCount;
    private string _lastInput = "Noch kein Tastendruck", _lastInputTime = "Warte auf Eingaben vom Controller";
    private string _commandStatus = "Ausgänge werden nach der Verbindung freigeschaltet.";

    internal ControllerViewModel(Dispatcher dispatcher, Func<DeviceConnection>? createConnection = null)
    {
        _dispatcher = dispatcher;
        _createConnection = createConnection ?? (() => new DeviceConnection());
        ConnectionCommand = new(_ => ToggleConnectionAsync(), () => !_closing && !_busy);
        IndicatorCommand = new(_ => SetIndicatorAsync(), () => CanUseIndicator);
        LightsCommand = new(ToggleLightAsync, () => CanUseLights);
        AllOffCommand = new(_ => SendLightsAsync(DeviceLights.None), () => CanUseLights);
        AllOnCommand = new(_ => SendLightsAsync(DeviceLights.All), () => CanUseLights);
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    public ObservableCollection<string> Events { get; } = new();
    public AsyncCommand ConnectionCommand { get; }
    public AsyncCommand IndicatorCommand { get; }
    public AsyncCommand LightsCommand { get; }
    public AsyncCommand AllOffCommand { get; }
    public AsyncCommand AllOnCommand { get; }
    public bool IsConnected => _state.IsConnected;
    public bool IsSearching => _connection is not null && !IsConnected && _state.Phase != DeviceConnectionPhase.Faulted;
    public string ConnectionAction => _connection is null ? "Verbinden" : "Trennen";
    public string StatusTitle => _state.Phase switch
    {
        DeviceConnectionPhase.Connected => "Controller verbunden",
        DeviceConnectionPhase.Searching => "Suche nach Controller",
        DeviceConnectionPhase.Connecting => $"Prüfe {_state.PortName}",
        DeviceConnectionPhase.Retry => "Warte auf Verbindung",
        DeviceConnectionPhase.Faulted => "Verbindungsfehler",
        DeviceConnectionPhase.Stopped => "Verbindung getrennt",
        _ => "Bereit zum Verbinden"
    };
    public string StatusDetail => _state.Failure != DeviceFailure.None
        ? FailureText(_state.Failure) + (string.IsNullOrWhiteSpace(_state.Detail) ? "" : " · " + _state.Detail)
        : _state.Phase switch
        {
            DeviceConnectionPhase.Connected => "USB-Verbindung aktiv. Alle verfügbaren Funktionen sind bereit.",
            DeviceConnectionPhase.Searching or DeviceConnectionPhase.Connecting => "COM-Ports werden automatisch erkannt. Einen Moment bitte …",
            DeviceConnectionPhase.Stopped => "Der COM-Port ist freigegeben. Du kannst jederzeit neu verbinden.",
            _ => "ESP8266 per USB anschließen und die Verbindung starten."
        };
    public string Port => _state.PortName ?? "—";
    public string Protocol => _state.ProtocolVersion is { } version ? $"Launcher IO v{(int)version}" : "Automatisch";
    public bool IsDiceFirmware => IsConnected && _state.ProtocolVersion == DeviceProtocolVersion.V4;
    public DeviceDiceRoll? DiceRoll => _diceRoll;
    public string ConnectionBadge => IsConnected ? "ONLINE" : IsSearching ? "SUCHE" : "OFFLINE";
    public bool CanUseIndicator => !_closing && !_busy && IsConnected && _state.Capabilities.HasFlag(DeviceCapabilities.Indicator);
    public bool CanUseLights => !_closing && !_busy && IsConnected && _state.Capabilities.HasFlag(DeviceCapabilities.Lights);
    public bool IndicatorOn => _indicator;
    public bool RedOn => _lights.HasFlag(DeviceLights.Red);
    public bool YellowOn => _lights.HasFlag(DeviceLights.Yellow);
    public bool GreenOn => _lights.HasFlag(DeviceLights.Green);
    public string IndicatorLabel => _indicator ? "Indikator ausschalten" : "Indikator einschalten";
    public string IndicatorHint => IsConnected && _state.Capabilities.HasFlag(DeviceCapabilities.Indicator) ? "LED am Controller · IO v1" : "Verfügbar mit Launcher IO v1";
    public string LightsHint => IsConnected && _state.Capabilities.HasFlag(DeviceCapabilities.Lights) ? "Klicken zum Umschalten · IO v2" : "Verfügbar mit Launcher IO v2";
    public string CommandStatus => _commandStatus;
    public bool HasFade => IsConnected && _state.ProtocolVersion == DeviceProtocolVersion.V3 && _fade.HasValue;
    public int FadeValue => _fade ?? 0;
    public string FadeText => HasFade ? _fade!.Value.ToString() : "—";
    public string FadePercent => HasFade ? $"{FadeValue / 255.0:P0}" : "Kein Signal";
    public string FadeHint => IsConnected && _state.ProtocolVersion == DeviceProtocolVersion.V3
        ? "Live von A0 · Die Platine regelt damit die LED an D4."
        : "Potentiometer auf A0 · Verfügbar mit Launcher IO v3";
    public string LastInput => _lastInput;
    public string LastInputTime => _lastInputTime;
    public string InputCount => _inputCount.ToString("D2");
    public string InputHint => IsConnected && _state.ProtocolVersion == DeviceProtocolVersion.V3 ? "IO v3 meldet ausschließlich den Potentiometerwert." : "BUTTON · LEFT · RIGHT · BOTH";

    internal Task StartAsync()
    {
        if (_connection is not null || _closing) return Task.CompletedTask;
        var connection = _createConnection();
        _connection = connection;
        connection.StateChanged += state => Post(connection, () => DisplayState(state));
        connection.InputReceived += input => Post(connection, () => DisplayInput(input));
        connection.FadeReceived += fade => Post(connection, () => { _fade = fade; NotifyFade(); });
        connection.DiceReceived += dice => Post(connection, () =>
        {
            _diceRoll = dice;
            Log($"Wurf #{dice.Sequence} · ESP-Ergebnis: {dice.Value}");
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DiceRoll)));
        });
        Log("Verbindung gestartet · automatische COM-Port-Erkennung");
        connection.Start();
        Notify();
        return Task.CompletedTask;
    }

    internal async Task ToggleConnectionAsync()
    {
        if (_closing || _busy) return;
        if (_connection is null) { await StartAsync(); return; }
        _busy = true;
        Notify();
        try { await StopConnectionAsync(); }
        finally { _busy = false; Notify(); }
    }

    private async Task StopConnectionAsync()
    {
        DeviceConnection? connection = _connection;
        _connection = null;
        if (connection is not null) await connection.DisposeAsync();
        DisplayState(new(DeviceConnectionPhase.Stopped));
    }

    private void Post(DeviceConnection connection, Action action)
    {
        if (_dispatcher.HasShutdownStarted) return;
        _dispatcher.BeginInvoke(new Action(() =>
        {
            if (!_closing && ReferenceEquals(connection, _connection)) action();
        }));
    }

    private void DisplayState(DeviceState state)
    {
        bool changed = _state != state;
        _state = state;
        if (!state.IsConnected)
        {
            _indicator = false;
            _lights = DeviceLights.None;
            _fade = null;
            _diceRoll = null;
            _commandStatus = "Ausgänge werden nach der Verbindung freigeschaltet.";
        }
        else if (changed)
        {
            _commandStatus = "Bereit. Der angezeigte Ausgangszustand folgt bestätigten Befehlen.";
        }
        if (changed) Log(StatusTitle + (state.IsConnected ? $" · {Port} · {Protocol}" : state.Failure == DeviceFailure.None ? "" : " · " + StatusDetail));
        Notify();
    }

    private void DisplayInput(DeviceInput input)
    {
        _inputCount++;
        _lastInput = input switch
        {
            DeviceInput.PrimaryButtonPressed => "BUTTON gedrückt",
            DeviceInput.LeftButtonPressed => "LEFT gedrückt",
            DeviceInput.RightButtonPressed => "RIGHT gedrückt",
            DeviceInput.BothButtonsPressed => "BOTH gedrückt",
            _ => input.ToString()
        };
        _lastInputTime = "Letztes Ereignis um " + DateTime.Now.ToString("HH:mm:ss");
        Log(_lastInput);
        Notify();
    }

    internal async Task SetIndicatorAsync()
    {
        if (!CanUseIndicator || _connection is null) return;
        var connection = _connection;
        bool desired = !_indicator;
        await SendAsync(connection, () => connection.SetIndicatorAsync(desired), () => _indicator = desired, desired ? "Indikator EIN" : "Indikator AUS");
    }

    private Task ToggleLightAsync(object? parameter)
    {
        if (!Enum.TryParse(parameter?.ToString(), out DeviceLights light) || light is not (DeviceLights.Red or DeviceLights.Yellow or DeviceLights.Green))
            return Task.CompletedTask;
        return SendLightsAsync(_lights ^ light);
    }

    internal Task SendLightsAsync(DeviceLights lights)
    {
        if (!CanUseLights || _connection is null) return Task.CompletedTask;
        var connection = _connection;
        return SendAsync(connection, () => connection.SetLightsAsync(lights), () => _lights = lights, "LEDs: " + lights);
    }

    private async Task SendAsync(DeviceConnection connection, Func<Task<DeviceCommandResult>> send, Action confirmed, string description)
    {
        _busy = true;
        _commandStatus = "Befehl wird gesendet …";
        Notify();
        try
        {
            DeviceCommandResult result = await send();
            if (_closing || !ReferenceEquals(connection, _connection)) return;
            if (result == DeviceCommandResult.Completed) confirmed();
            _commandStatus = result switch
            {
                DeviceCommandResult.Completed => description + " · bestätigt",
                DeviceCommandResult.Unsupported => "Diese Firmware unterstützt den Ausgang nicht.",
                DeviceCommandResult.Disconnected => "Keine Verbindung zum Controller.",
                DeviceCommandResult.Cancelled => "Befehl abgebrochen.",
                DeviceCommandResult.Superseded => "Befehl durch eine neuere Ausgabe ersetzt.",
                _ => "Keine Bestätigung erhalten. Bitte Verbindung prüfen."
            };
            Log(_commandStatus);
        }
        finally { _busy = false; Notify(); }
    }

    private void Log(string message)
    {
        Events.Insert(0, DateTime.Now.ToString("HH:mm:ss") + "   " + message);
        while (Events.Count > 200) Events.RemoveAt(Events.Count - 1);
    }

    private void Notify()
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(null));
        ConnectionCommand.Refresh(); IndicatorCommand.Refresh(); LightsCommand.Refresh(); AllOffCommand.Refresh(); AllOnCommand.Refresh();
    }
    private void NotifyFade()
    {
        foreach (string property in new[] { nameof(FadeValue), nameof(FadeText), nameof(FadePercent), nameof(HasFade) })
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
    }
    private static string FailureText(DeviceFailure failure) => failure switch
    {
        DeviceFailure.NoPorts => "Kein COM-Port gefunden. Bitte USB-Kabel prüfen.",
        DeviceFailure.AccessDenied => "COM-Port belegt. Bitte andere Serial-Monitore schließen.",
        DeviceFailure.Timeout => "Keine Antwort. Passende Launcher-IO-Firmware prüfen.",
        DeviceFailure.UnsupportedProtocol => "Die Protokollversion wird nicht unterstützt.",
        DeviceFailure.InvalidReply => "Ungültige Antwort vom Controller.",
        DeviceFailure.Transport => "USB-Verbindung unterbrochen. Neuer Versuch folgt automatisch.",
        _ => "Unerwarteter Fehler. Trennen und erneut verbinden."
    };

    public async ValueTask DisposeAsync()
    {
        if (_closing) return;
        _closing = true;
        Notify();
        await StopConnectionAsync();
    }
}
