using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Launcher.Application;
using Launcher.Domain;
using Launcher.Infrastructure;

namespace Launcher.UI;

/// <summary>
/// RU: Главная форма Launcher. Координирует UI и делегирует бизнес-логику фасаду.
/// DE: Hauptformular des Launchers. Koordiniert die UI und delegiert Fachlogik an die Fassade.
/// </summary>
public partial class Main : Form
{
    #region [RU] Поля | [DE] Felder

    private readonly LauncherFacade _launcherFacade;
    private readonly List<AppEntry> _allApps = new();
    private readonly HashSet<string> _favoriteKeys = new(StringComparer.OrdinalIgnoreCase);
    private readonly Random _random = new();

    private readonly string _baseDirectory;
    private readonly string _launcherExeName;
    private readonly string _statePath;

    private LauncherState _state = new();
    private string? _solutionRoot;
    private bool _isDarkTheme;
    private bool _isUiUpdate;

    #endregion

    #region [RU] Конструктор | [DE] Konstruktor

    /// <summary>
    /// RU: Инициализирует форму и все зависимости учебного проекта.
    /// DE: Initialisiert das Formular und alle Abhaengigkeiten des Lernprojekts.
    /// </summary>
    public Main()
    {
        InitializeComponent();

        _launcherFacade = new LauncherFacade(
            new ProjectReferenceDiscoveryService(),
            new JsonStateStorageService(),
            new AppFilterService(),
            new AppSortService());

        _baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
        _launcherExeName = Path.GetFileName(System.Windows.Forms.Application.ExecutablePath);
        _statePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            LauncherConstants.StateFolderName,
            LauncherConstants.StateFileName);

        InitializePet();
        EnableDoubleBuffer(flpApps);

        BindEvents();
        LoadStateAndApplyToUi();
        ApplyTheme();

        Resize += (_, __) => Render();
        Shown += (_, __) =>
        {
            _petTimer.Start();
            ScheduleNextPetMovement();
            ScheduleNextPetJump();
            _petCursorTimer.Start();
            UpdatePetCursorTracking();
        };
        FormClosing += (_, __) => PersistState();
        FormClosed += (_, __) => DisposePet();
    }

    #endregion

    #region [RU] Бизнес-логика | [DE] Fachlogik

    /// <summary>
    /// RU: Загружает список приложений и обновляет фильтр категорий.
    /// DE: Laedt die Anwendungsliste und aktualisiert den Kategorie-Filter.
    /// </summary>
    private void LoadAppCatalog()
    {
        _allApps.Clear();
        _allApps.AddRange(_launcherFacade.LoadApps(_baseDirectory, _launcherExeName, out _solutionRoot));

        UpdateCategoryItems();
        Render();
    }

    /// <summary>
    /// RU: Возвращает приложения после фильтрации и сортировки.
    /// DE: Liefert Anwendungen nach Filterung und Sortierung.
    /// </summary>
    private List<AppEntry> BuildVisibleApps()
    {
        SortMode selectedSort = GetSelectedSortMode();

        return _launcherFacade.FilterAndSortApps(
            _allApps,
            _txtSearch.Text.Trim(),
            _cbCategory.SelectedItem as string ?? LauncherConstants.AllCategories,
            _chkFavoritesOnly.Checked,
            _chkAvailableOnly.Checked,
            selectedSort,
            _favoriteKeys,
            _state);
    }

    /// <summary>
    /// RU: Обновляет подпись статуса внизу формы.
    /// DE: Aktualisiert den Status-Text im unteren Formularbereich.
    /// </summary>
    private void UpdateStats(IReadOnlyCollection<AppEntry> visibleApps)
    {
        int runnable = visibleApps.Count(app => File.Exists(app.ExePath));
        int launches = _state.Usage.Values.Sum(usage => usage.Count);

        _lblStats.Text = $"Sichtbar: {visibleApps.Count}/{_allApps.Count} | Lauffaehig: {runnable} | Favoriten: {_favoriteKeys.Count} | Starts gesamt: {launches}";
    }

    /// <summary>
    /// RU: Загружает состояние и применяет его в UI.
    /// DE: Laedt den Zustand und uebertraegt ihn in die UI.
    /// </summary>
    private void LoadStateAndApplyToUi()
    {
        _state = _launcherFacade.LoadState(_statePath);
        _isUiUpdate = true;

        _isDarkTheme = _state.DarkMode;
        _txtSearch.Text = _state.LastSearch;
        _chkFavoritesOnly.Checked = _state.FavoritesOnly;
        _chkAvailableOnly.Checked = _state.AvailableOnly;
        SetSelectedSortMode(_state.LastSort);

        _favoriteKeys.Clear();
        foreach (string favorite in _state.Favorites.Where(value => !string.IsNullOrWhiteSpace(value)))
        {
            _favoriteKeys.Add(favorite);
        }

        _isUiUpdate = false;
    }

    /// <summary>
    /// RU: Считывает текущее состояние UI и сохраняет его в JSON.
    /// DE: Liest den aktuellen UI-Zustand und speichert ihn als JSON.
    /// </summary>
    private void PersistState()
    {
        _state.DarkMode = _isDarkTheme;
        _state.FavoritesOnly = _chkFavoritesOnly.Checked;
        _state.AvailableOnly = _chkAvailableOnly.Checked;
        _state.LastSearch = _txtSearch.Text;
        _state.LastCategory = _cbCategory.SelectedItem as string ?? LauncherConstants.AllCategories;
        _state.LastSort = GetSelectedSortMode();
        _state.Favorites = _favoriteKeys.OrderBy(value => value).ToList();

        _launcherFacade.SaveState(_statePath, _state);
    }

    /// <summary>
    /// RU: Обновляет список категорий в выпадающем списке.
    /// DE: Aktualisiert die Kategorien im Dropdown.
    /// </summary>
    private void UpdateCategoryItems()
    {
        string? currentSelection = _cbCategory.SelectedItem as string;

        _isUiUpdate = true;
        _cbCategory.Items.Clear();
        _cbCategory.Items.Add(LauncherConstants.AllCategories);

        foreach (string category in _launcherFacade.BuildCategories(_allApps))
        {
            _cbCategory.Items.Add(category);
        }

        if (!string.IsNullOrWhiteSpace(currentSelection) && _cbCategory.Items.Contains(currentSelection))
        {
            _cbCategory.SelectedItem = currentSelection;
        }
        else if (_cbCategory.Items.Contains(_state.LastCategory))
        {
            _cbCategory.SelectedItem = _state.LastCategory;
        }
        else
        {
            _cbCategory.SelectedIndex = 0;
        }

        _isUiUpdate = false;
    }

    /// <summary>
    /// RU: Преобразует выбранный текст в enum сортировки.
    /// DE: Wandelt den gewaelten Text in den Sortier-Enum um.
    /// </summary>
    private SortMode GetSelectedSortMode()
    {
        SortModeText.TryParse(_cbSort.SelectedItem as string, out SortMode mode);
        return mode;
    }

    /// <summary>
    /// RU: Устанавливает режим сортировки в UI.
    /// DE: Setzt den Sortiermodus in der UI.
    /// </summary>
    private void SetSelectedSortMode(SortMode mode)
    {
        string text = SortModeText.ToDisplayText(mode);
        _cbSort.SelectedItem = _cbSort.Items.Contains(text) ? text : SortModeText.ByCategory;
    }

    /// <summary>
    /// RU: Показывает краткую подсказку пользователю.
    /// DE: Zeigt dem Benutzer einen kurzen Hinweis.
    /// </summary>
    private void ShowHint(string text)
    {
        _lblHint.Text = text;
    }

    #endregion

    #region [RU] Вспомогательные методы | [DE] Hilfsmethoden

    private static void EnableDoubleBuffer(Control control)
    {
        typeof(Control)
            .GetProperty("DoubleBuffered", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
            ?.SetValue(control, true, null);
    }

    #endregion
}
