using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace EspController.Firmware;

// Deliberately no serial-port discovery: a confirmed DeviceConnection provides
// the exact COM port. Compilation is completed before that connection is closed.
internal static class FirmwareFlasher
{
    internal const string Board = "esp8266:esp8266:nodemcuv2";

    internal static string SketchFolder(int version)
    {
        if (version is < 1 or > 5) throw new ArgumentOutOfRangeException(nameof(version));
        string name = "LauncherIoEsp8266" + (version == 1 ? "" : "V" + version);
        string folder = Path.Combine(AppContext.BaseDirectory, "Firmware", name);
        if (!File.Exists(Path.Combine(folder, name + ".ino")))
            throw new FileNotFoundException("Firmware-Quellen fehlen im Ausgabeverzeichnis: " + folder);
        return folder;
    }

    private static string ToolPath()
    {
        string local = Path.Combine(AppContext.BaseDirectory, "arduino-cli.exe");
        if (File.Exists(local)) return local;
        string user = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Programs", "Arduino CLI", "arduino-cli.exe");
        return File.Exists(user) ? user : "arduino-cli.exe";
    }

    internal static Task CompileAsync(int version, string outputFolder, IProgress<string> log, CancellationToken cancellation)
    {
        string sketch = SketchFolder(version);
        return ExecuteAsync(sketch, log, TimeSpan.FromMinutes(5), cancellation,
            "compile", "--fqbn", Board, "--output-dir", outputFolder, sketch);
    }

    internal static Task UploadAsync(int version, string port, string outputFolder, IProgress<string> log)
    {
        // The port is taken from an active, recognized ESP, never from a scan.
        if (string.IsNullOrWhiteSpace(port)) throw new ArgumentException("Kein bestätigter COM-Port.", nameof(port));
        string sketch = SketchFolder(version);
        return ExecuteAsync(sketch, log, TimeSpan.FromMinutes(2), CancellationToken.None,
            "upload", "--fqbn", Board, "--port", port, "--input-dir", outputFolder, sketch);
    }

    private static async Task ExecuteAsync(string directory, IProgress<string> log,
        TimeSpan timeout, CancellationToken cancellation, params string[] args)
    {
        cancellation.ThrowIfCancellationRequested();
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellation);
        deadline.CancelAfter(timeout);
        var info = new ProcessStartInfo(ToolPath())
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            WorkingDirectory = directory
        };
        foreach (string arg in args) info.ArgumentList.Add(arg);

        using var process = new Process { StartInfo = info };
        var errorTail = new StringBuilder();
        void Output(object sender, DataReceivedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(e.Data)) log.Report(e.Data.Trim());
        }
        void Error(object sender, DataReceivedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(e.Data)) return;
            string line = e.Data.Trim();
            lock (errorTail)
            {
                errorTail.AppendLine(line);
                if (errorTail.Length > 4000) errorTail.Remove(0, errorTail.Length - 4000);
            }
            log.Report(line);
        }
        process.OutputDataReceived += Output;
        process.ErrorDataReceived += Error;
        try
        {
            try
            {
                if (!process.Start()) throw new IOException("Arduino CLI konnte nicht gestartet werden.");
            }
            catch (Win32Exception e) when (e.NativeErrorCode is 2 or 3)
            {
                throw new IOException("arduino-cli.exe fehlt. Neben EspController.exe, unter %LOCALAPPDATA%\\Programs\\Arduino CLI oder im PATH ablegen.", e);
            }
            catch (Win32Exception e)
            {
                throw new IOException($"Arduino CLI konnte nicht gestartet werden ({info.FileName}): {e.Message}", e);
            }
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            try
            {
                await process.WaitForExitAsync(deadline.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (deadline.IsCancellationRequested)
            {
                string message = args[0] == "upload"
                    ? "Zeitlimit beim Flashen überschritten. Firmwarezustand unbekannt."
                    : cancellation.IsCancellationRequested ? "Kompilierung abgebrochen." : "Zeitlimit beim Kompilieren überschritten.";
                try
                {
                    try { process.Kill(entireProcessTree: true); }
                    catch (InvalidOperationException) when (process.HasExited) { }
                    using var stopDeadline = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                    await process.WaitForExitAsync(stopDeadline.Token).ConfigureAwait(false);
                }
                catch (Exception error) when (error is Win32Exception or AggregateException or InvalidOperationException or OperationCanceledException)
                {
                    throw new IOException(message + " Das Ende von Arduino CLI konnte nicht bestätigt werden.", error);
                }
                cancellation.ThrowIfCancellationRequested();
                throw new IOException(message);
            }
            if (process.ExitCode != 0)
                throw new IOException("Arduino CLI " + args[0] + " fehlgeschlagen (Exit " + process.ExitCode + "). " + errorTail);
        }
        finally
        {
            process.OutputDataReceived -= Output;
            process.ErrorDataReceived -= Error;
        }
    }
}
