# Firmware upload from ESP Controller

The firmware panel can flash Launcher IO v1–v5 **only when an ESP is already
connected and identified by HELLO**. The selected port is captured before
compilation. Neither compiling nor uploading probes other serial ports.

## One-time Windows setup

Install Arduino CLI and the `esp8266:esp8266` core. Put `arduino-cli.exe`
beside `EspController.exe` or add it to PATH. If the core is not installed:

```powershell
arduino-cli core update-index --additional-urls https://arduino.esp8266.com/stable/package_esp8266com_index.json
arduino-cli core install esp8266:esp8266 --additional-urls https://arduino.esp8266.com/stable/package_esp8266com_index.json
```

The sketch sources are copied to a `Firmware` subfolder when building or
publishing EspController; keep that directory beside the executable.

## Operation

1. Connect in ESP Controller, wait for the green connected state and a COM
   port confirmed by a valid Launcher IO v1–v5 HELLO response.
2. Select desired target firmware and click **Kompilieren & Flashen**.
3. The controller compiles while the original firmware keeps running; a build
   failure does not close the serial connection.
4. On successful compilation it closes the current connection, waits until
   the COM port is released, then uploads the binary **only to the captured
   COM port**.
5. After the upload (successful or failed), it reconnects exclusively to
   that same COM port and automatically identifies the new firmware. To switch
   to another device, manually disconnect and connect again.
6. The log shows command output, errors and the flash result. A closing window
   skips uploading if it is still compiling, but waits for an ongoing upload
   to finish.

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

Test a valid compile, an intentional compile failure, correct target port,
successful upload, reconnection, and a disconnected board. Native Windows
build and on-board flashing have **not** been exercised in the authoring
environment.
