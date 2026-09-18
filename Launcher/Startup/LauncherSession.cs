using Launcher.Apps.Launch;
using Launcher.Apps.List;
using Launcher.Connections;
using Launcher.Device.Connection;
using Launcher.Pet.Windows;
using Launcher.Pet.Windows.Drawing;
using Launcher.Settings;
using Launcher.Windows;

namespace Launcher.Startup;
internal sealed class LauncherSession : IDisposable
{
    private readonly AppList _apps;
    private readonly SettingsFile _settingsFile;
    private readonly LauncherSettings _settings;
    private readonly DeviceConnection _device;
    private readonly AppListConnection _appConnection;
    private readonly DevicePanelConnection _devicePanelConnection;
    private readonly DeviceInputConnection _deviceInputConnection;
    private readonly PetWindowsSession? _pet;
    private readonly string? _startupWarning;
    private bool _closing, _readyToClose, _disposed;
    internal MainWindow Window { get; }

    internal LauncherSession(MainWindow window, DeviceConnection device, PetWindowsSession? pet,
        AppList apps, AppLaunch launch, SettingsFile settingsFile, LauncherSettings settings, string? warning)
    {
        _apps = apps;
        _settingsFile = settingsFile;
        _settings = settings;
        Window = window;
        _device = device;
        _pet = pet;
        _startupWarning = warning;
        if (_pet is not null)
            _pet.Problem += ShowProblem;
        _appConnection = new(Window, apps, launch, Save);
        _devicePanelConnection = new(_device, Window, Post);
        _deviceInputConnection = new(_device, _pet, _appConnection, Window, Post);
        Window.Shown += Shown;
        Window.FormClosing += Closing;
        Window.ThemeRequested += ChangeTheme;
        Window.CollisionsChanged += ChangeCollisions;
        ApplyTheme();
        _pet?.ShowCollisions(settings.ShowCollisions);
    }

    private void Shown(object? sender, EventArgs args)
    {
        _appConnection.Refresh();
        if (!string.IsNullOrWhiteSpace(_startupWarning))
            ShowProblem(_startupWarning);
        _pet?.Start();
        _device.Start();
    }

    private void ChangeTheme()
    {
        _settings.DarkTheme = !_settings.DarkTheme;
        ApplyTheme();
        Save();
    }

    private void ChangeCollisions(bool enabled)
    {
        _settings.ShowCollisions = enabled;
        _pet?.ShowCollisions(enabled);
        Save();
    }

    private void ApplyTheme()
    {
        var theme = new WindowTheme(_settings.DarkTheme);
        Window.SetTheme(theme);
        _pet?.SetColors(new PetColors(theme.SurfaceAlt, theme.Surface, theme.Text, theme.Border));
    }

    private void ShowProblem(string message) => Window.Status.ShowHint(message);
    private void Save()
    {
        _settings.Apps = _apps.SaveSettings();
        if (_settingsFile.Write(_settings) is string error)
            ShowProblem(error);
    }

    private void Post(Action action)
    {
        if (_closing || _disposed || !Window.IsHandleCreated || Window.IsDisposed)
            return;
        try
        {
            Window.BeginInvoke(new Action(() =>
            {
                if (!_closing && !_disposed && !Window.IsDisposed)
                    action();
            }));
        }
        catch (InvalidOperationException) when (_closing || Window.IsDisposed || !Window.IsHandleCreated)
        {
        }
    }

    private async void Closing(object? sender, FormClosingEventArgs args)
    {
        if (_readyToClose || args.Cancel)
            return;
        args.Cancel = true;
        if (_closing)
            return;
        _closing = true;
        Window.DisableActions();
        try
        {
            try
            {
                await _device.StopAsync();
            }
            catch (Exception error)
            {
                MessageBox.Show(Window, error.Message, "Controller", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }

            _pet?.Stop();
            _settings.Apps = _apps.SaveSettings();
            if (_settingsFile.Write(_settings) is string settingsError)
                MessageBox.Show(Window, settingsError, "Einstellungen", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        finally
        {
            _readyToClose = true;
            if (!Window.IsDisposed)
                Window.BeginInvoke(new Action(Window.Close));
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;
        _closing = true;

        _deviceInputConnection.Dispose();
        _devicePanelConnection.Dispose();

        try
        {
            _device.DisposeAsync().AsTask().GetAwaiter().GetResult();
        }
        catch (Exception error)
        {
            System.Diagnostics.Debug.WriteLine(error);
        }

        _appConnection.Dispose();
        if (_pet is not null)
        {
            _pet.Problem -= ShowProblem;
            _pet.Dispose();
        }

        Window.Shown -= Shown;
        Window.FormClosing -= Closing;
        Window.ThemeRequested -= ChangeTheme;
        Window.CollisionsChanged -= ChangeCollisions;
        Window.Dispose();
    }
}
