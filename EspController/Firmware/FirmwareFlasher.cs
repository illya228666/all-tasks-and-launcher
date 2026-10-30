using System.ComponentModel;
using System.Diagnostics;
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
        return File.Exists(local) ? local : "arduino-cli";
    }

    internal static async Task CompileAsync(int version, string outputFolder, IProgress<string> log)
    {
        string sketch = SketchFolder(version);
        await ExecuteAsync(sketch, log,
            "compile", "--fqbn", Board, "--output-dir", outputFolder, sketch);
    }

    internal static Task UploadAsync(int version, string port, string outputFolder, IProgress<string> log)
    {
        // The port is taken from an active, recognized ESP, never from a scan.
        if (string.IsNullOrWhiteSpace(port)) throw new ArgumentException("Kein bestätigter COM-Port.", nameof(port));
        string sketch = SketchFolder(version);
        return ExecuteAsync(sketch, log,
            "upload", "--fqbn", Board, "--port", port, "--input-dir", outputFolder, sketch);
    }

    private static async Task ExecuteAsync(string directory, IProgress<string> log, params string[] args)
    {
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
            catch (Win32Exception e)
            {
                throw new IOException("arduino-cli.exe nicht gefunden. Arduino CLI installieren oder neben EspController.exe ablegen.", e);
            }
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            await process.WaitForExitAsync();
            process.WaitForExit(); // let asynchronous stdout/stderr handlers finish
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
