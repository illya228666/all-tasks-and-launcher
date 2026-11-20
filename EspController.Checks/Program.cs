using EspController.Firmware;

internal static class Program
{
    private const string Scenario = "ESP_CONTROLLER_FLASH_CHECK";

    private static async Task<int> Main(string[] args)
    {
        if (args.Length > 0 && args[0] is "upload" or "compile")
        {
            if (args[0] == "upload" && Array.IndexOf(args, "COM_TEST") < 0) return 9;
            string mode = Environment.GetEnvironmentVariable(Scenario) ?? "";
            if (mode == "hang") await Task.Delay(Timeout.Infinite);
            if (mode is "verified" or "late-fatal" or "exit-error" or "long-output")
                Console.WriteLine("Hash of data verified.");
            if (mode == "stderr-verified") Console.Error.WriteLine("Hash of data verified.");
            if (mode is "stderr-fatal" or "late-fatal")
                Console.Error.WriteLine("A fatal esptool.py error occurred: Timed out waiting for packet header");
            if (mode == "stdout-fatal") Console.WriteLine("A fatal esptool.py error occurred: COM_TEST missing");
            if (mode == "unverified") Console.WriteLine("Writing at 0x00030000... (100 %)");
            if (mode == "long-output") for (int i = 0; i < 300; i++) Console.WriteLine("Loader finished: extra diagnostic output.");
            return mode == "exit-error" ? 3 : 0;
        }

        await DigitalOutputChecks.RunAsync();

        string fakeCli = Path.Combine(AppContext.BaseDirectory, "arduino-cli.exe");
        string? previousScenario = Environment.GetEnvironmentVariable(Scenario);
        if (File.Exists(fakeCli)) throw new IOException("Refusing to replace an existing arduino-cli.exe.");
        File.Copy(Environment.ProcessPath!, fakeCli);
        try
        {
            var log = new Log();
            foreach (string mode in new[] { "verified", "stderr-verified", "long-output" })
            {
                Environment.SetEnvironmentVariable(Scenario, mode);
                await FirmwareFlasher.UploadAsync(3, "COM_TEST", AppContext.BaseDirectory, log);
            }
            foreach (string mode in new[] { "unverified", "stderr-fatal", "stdout-fatal", "late-fatal", "exit-error" })
            {
                Environment.SetEnvironmentVariable(Scenario, mode);
                string expected = mode switch
                {
                    "exit-error" => "Exit 3",
                    "unverified" => "100 %",
                    "stdout-fatal" => "COM_TEST missing",
                    _ => "Timed out waiting for packet header"
                };
                await ExpectAsync<IOException>(
                    () => FirmwareFlasher.UploadAsync(3, "COM_TEST", AppContext.BaseDirectory, log), expected);
            }
            Environment.SetEnvironmentVariable(Scenario, "unverified");
            await FirmwareFlasher.CompileAsync(6, AppContext.BaseDirectory, log, CancellationToken.None);
            Environment.SetEnvironmentVariable(Scenario, "exit-error");
            await ExpectAsync<IOException>(
                () => FirmwareFlasher.CompileAsync(3, AppContext.BaseDirectory, log, CancellationToken.None), "Exit 3");
            Environment.SetEnvironmentVariable(Scenario, "hang");
            using var cancel = new CancellationTokenSource(TimeSpan.FromSeconds(1));
            await ExpectAsync<OperationCanceledException>(
                () => FirmwareFlasher.CompileAsync(3, AppContext.BaseDirectory, log, cancel.Token), "");
            await ExpectAsync<ArgumentException>(
                () => FirmwareFlasher.UploadAsync(3, " ", AppContext.BaseDirectory, log), "COM-Port");
            Console.WriteLine("PASS: upload verification, fatal errors on both streams, exit errors, log tail, compile and cancellation.");
            return 0;
        }
        finally
        {
            Environment.SetEnvironmentVariable(Scenario, previousScenario);
            File.Delete(fakeCli);
        }
    }

    private static async Task ExpectAsync<T>(Func<Task> action, string message) where T : Exception
    {
        try { await action(); }
        catch (T error) when (error.Message.Contains(message, StringComparison.Ordinal)) { return; }
        throw new InvalidOperationException("Expected " + typeof(T).Name + " containing: " + message);
    }

    private sealed class Log : IProgress<string>
    {
        public void Report(string value) { }
    }
}
