# Firmware upload from ESP Controller

The firmware panel can flash Launcher IO v1–v5 **only when an ESP is already
connected and identified by HELLO**. The selected port is captured before
compilation. Neither compiling nor uploading probes other serial ports.

## One-time Windows setup

Install Arduino CLI and the `esp8266:esp8266` core. ESP Controller looks for
`arduino-cli.exe` beside `EspController.exe`, then in
`%LOCALAPPDATA%\Programs\Arduino CLI`, then on PATH. The per-user folder also
works when ESP Controller is started from the Launcher output directory.
If the core is not installed:

```powershell
arduino-cli core update-index --additional-urls https://arduino.esp8266.com/stable/package_esp8266com_index.json
arduino-cli core install esp8266:esp8266 --additional-urls https://arduino.esp8266.com/stable/package_esp8266com_index.json
```

The sketch sources are copied to a `Firmware` subfolder when building or
publishing EspController; keep that directory beside the executable.

## Operation

1. Connect in ESP Controller, wait for the green connected state and a COM
   port confirmed by a valid Launcher IO v1–v5 HELLO response.
2. Select the target firmware and click **Kompilieren & Flashen**. When changing
   versions, confirm that the wiring matches the target.
3. The controller compiles while the original firmware keeps running; a build
   failure does not close the serial connection.
4. After compilation, the original HELLO session must answer one more poll.
   If it was lost, even if the same COM port has reconnected, the upload is
   cancelled. Otherwise the app releases the port and uploads **only to the
   captured COM port**. A physical swap between releasing the port and Arduino
   CLI opening it can go undetected if Windows assigns the same COM name.
5. After the upload (successful or failed), it reconnects exclusively to
   that same COM port and automatically identifies the new firmware. To switch
   to another device, manually disconnect and connect again.
6. Closing the window cancels compilation or skips an upload that has yet to
   start. An upload already in progress can finish. Compilation has a five
   minute limit and upload has a two minute limit. After an upload timeout the
   firmware state is unknown.

The controller intentionally uses the standard Arduino CLI bootloader tool
instead of implementing its own protocol or scanning disconnected ports.

**Before flashing:** verify that the destination firmware's board pinout,
GPIO boot-mode constraints, LED current and active polarity match the physical
breadboard. Versions v1–v5 have *different* wiring. The provided v5
`BoardConfig.h` has example pins that are not verified from the screenshot.

The firmware tool does not attempt to recover non-responsive boards; a failed
upload may need manual recovery via Arduino IDE/CLI.

## Check locally

```powershell
dotnet build EspController/EspController.csproj -c Release
```

Test a successful upload and reconnection on the same COM port, a compilation
failure, and a lost connection during compilation. Also check that closing
the window before upload cancels the operation.
