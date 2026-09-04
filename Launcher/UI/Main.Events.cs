using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Launcher.Domain;

namespace Launcher.UI;

public partial class Main
{
    #region [RU] Константы | [DE] Konstanten

    private static readonly string[] SurpriseLines =
    {
        "Zufallsmodus aktiviert.",
        "Neue Runde. Neues Glueck.",
        "Mission gestartet.",
        "Bam. Naechstes Projekt!"
    };

    #endregion

    #region [RU] Обработка событий | [DE] Ereignisbehandlung

    /// <summary>
    /// RU: Подписывает обработчики событий формы и контролов.
    /// DE: Registriert Event-Handler fuer Formular und Controls.
    /// </summary>
    private void BindEvents()
    {
        _txtSearch.TextChanged += (_, __) => OnFilterChanged();
        _cbCategory.SelectedIndexChanged += (_, __) => OnFilterChanged();
        _cbSort.SelectedIndexChanged += (_, __) => OnFilterChanged();
        _chkFavoritesOnly.CheckedChanged += (_, __) => OnFilterChanged();
        _chkAvailableOnly.CheckedChanged += (_, __) => OnFilterChanged();

        _btnRefresh.Click += (_, __) =>
        {
            LoadAppCatalog();
            ShowHint("Liste aktualisiert.");
        };

        _btnSurprise.Click += (_, __) => LaunchRandom();
        _btnD1On.Click += (_, __) => BeginEspOperation(true);
        _btnD1Off.Click += (_, __) => BeginEspOperation(false);
        _espPollTimer.Tick += (_, __) => BeginEspOperation();
        _btnRoot.Click += (_, __) => OpenFolder(_solutionRoot ?? _baseDirectory);
        _btnTheme.Click += (_, __) =>
        {
            _isDarkTheme = !_isDarkTheme;
            ApplyTheme();
            PersistState();
        };

        KeyDown += Main_KeyDown;
    }

    private void OnFilterChanged()
    {
        if (_isUiUpdate)
        {
            return;
        }

        Render();
        PersistState();
    }

    private void Main_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Control && e.KeyCode == Keys.F)
        {
            _txtSearch.Focus();
            _txtSearch.SelectAll();
            e.Handled = true;
            return;
        }

        if (e.Control && e.KeyCode == Keys.R)
        {
            LaunchRandom();
            e.Handled = true;
            return;
        }

        if (e.KeyCode == Keys.F5)
        {
            LoadAppCatalog();
            ShowHint("Liste aktualisiert.");
            e.Handled = true;
            return;
        }

        if (e.KeyCode == Keys.Escape && _txtSearch.TextLength > 0)
        {
            _txtSearch.Clear();
            e.Handled = true;
            return;
        }

        if (e.Control && e.KeyCode == Keys.D)
        {
            _isDarkTheme = !_isDarkTheme;
            ApplyTheme();
            PersistState();
            e.Handled = true;
        }
    }

    private void Main_Shown(object sender, EventArgs e)
    {
        LoadAppCatalog();
        _pet.Start();
        _espPollTimer.Start();
        BeginEspOperation();
    }

    #endregion

    #region [RU] Инфраструктура/IO | [DE] Infrastruktur/IO

    private void StartApp(AppEntry app, bool asAdmin = false, bool fromRandom = false)
    {
        try
        {
            if (!File.Exists(app.ExePath))
            {
                MessageBox.Show(this, $"Datei nicht gefunden:\n{app.ExePath}", "Fehler", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var startInfo = new ProcessStartInfo
            {
                FileName = app.ExePath,
                WorkingDirectory = Path.GetDirectoryName(app.ExePath)!,
                UseShellExecute = true
            };

            if (asAdmin)
            {
                startInfo.Verb = "runas";
            }

            Process.Start(startInfo);
            _launcherFacade.RegisterLaunch(app, _state);
            PersistState();
            Render();

            ShowHint(fromRandom
                ? $"{SurpriseLines[_random.Next(SurpriseLines.Length)]} ({app.Name})"
                : $"{app.Name} gestartet.");
        }
        catch (Win32Exception ex) when (ex.NativeErrorCode == 1223)
        {
            ShowHint("Admin-Start wurde abgebrochen.");
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Start-Fehler", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void OpenFolder(string path)
    {
        try
        {
            if (!Directory.Exists(path))
            {
                MessageBox.Show(this, $"Ordner nicht gefunden:\n{path}", "Fehler", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            Process.Start(new ProcessStartInfo
            {
                FileName = path,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Fehler", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void CopyPath(string path)
    {
        try
        {
            Clipboard.SetText(path);
            ShowHint("Pfad kopiert.");
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Clipboard-Fehler", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    #endregion

    #region [RU] Вспомогательные методы | [DE] Hilfsmethoden

    private void LaunchRandom()
    {
        var candidates = BuildVisibleApps()
            .Where(app => File.Exists(app.ExePath))
            .ToList();

        if (candidates.Count == 0)
        {
            ShowHint("Kein startbares Projekt gefunden.");
            return;
        }

        AppEntry selected = candidates[_random.Next(candidates.Count)];
        StartApp(selected, fromRandom: true);
    }

    #endregion
}
