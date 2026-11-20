using System.Threading.Channels;
using Launcher.Device.Connection;
using Launcher.Device.Data;
using Launcher.Device.Protocol;

internal static class DigitalOutputChecks
{
    internal static async Task RunAsync()
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var reports = Channel.CreateUnbounded<int>();
        await using var connection = new DeviceConnection(new PinTransport(), new(0, 1, 1, 1));
        connection.PinLevelsReceived += levels => reports.Writer.TryWrite(levels);
        connection.Start();

        Require(await reports.Reader.ReadAsync(timeout.Token) == 24, "v6 reports D3 and D4 HIGH at startup");
        DeviceState session = connection.State;
        Require(session.ProtocolVersion == DeviceProtocolVersion.V6, "HELLO selects v6");
        Require(await connection.SetIndicatorAsync(true) == DeviceCommandResult.Unsupported, "v6 exposes GPIO only");
        Require(await connection.SetPinAsync(8, true, session with { }).WaitAsync(timeout.Token) == DeviceCommandResult.Disconnected,
            "a GPIO command is bound to the current session");
        Require(await connection.SetPinAsync(8, true, session).WaitAsync(timeout.Token) == DeviceCommandResult.Completed, "D8 HIGH is acknowledged");
        Require(await reports.Reader.ReadAsync(timeout.Token) == 280, "D8 HIGH preserves D3 and D4");
        Require(await connection.SetPinAsync(3, false, session).WaitAsync(timeout.Token) == DeviceCommandResult.Completed, "D3 LOW is acknowledged");
        Require(await reports.Reader.ReadAsync(timeout.Token) == 272, "D3 LOW preserves other pins");

        var protocol = DeviceProtocol.FromIdentity("LAUNCHER_IO 6");
        try { protocol.ReadPoll("PINS 512"); }
        catch (DeviceProtocolException error) when (error.Failure == DeviceFailure.InvalidReply)
        {
            Console.WriteLine("PASS: v6 handshake, GPIO commands, poll updates and bounds validation.");
            return;
        }
        throw new InvalidOperationException("v6 accepted a mask outside D0–D8.");
    }

    private static void Require(bool result, string expected)
    {
        if (!result) throw new InvalidOperationException(expected);
    }

    private sealed class PinTransport : IDeviceTransportFactory, IDeviceTransport
    {
        private int _levels = 24;
        public IReadOnlyList<string> GetPortNames() => new[] { "COM_TEST" };
        public IDeviceTransport Open(string portName) => this;
        public void Dispose() { }

        public string Exchange(string command, CancellationToken cancellation)
        {
            cancellation.ThrowIfCancellationRequested();
            switch (command)
            {
                case "HELLO": return "LAUNCHER_IO 6";
                case "POLL": return $"PINS {_levels}";
                case "PIN D8 HIGH": _levels |= 256; break;
                case "PIN D3 LOW": _levels &= ~8; break;
                default: throw new InvalidOperationException($"Unexpected command {command}.");
            }
            return "OK " + command;
        }
    }
}
