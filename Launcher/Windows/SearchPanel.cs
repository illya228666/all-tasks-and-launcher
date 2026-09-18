using Launcher.Apps.Data;

namespace Launcher.Windows;
internal sealed class SearchPanel : FlowLayoutPanel
{
    private readonly TextBox _search = new()
    {
        Width = 210,
        PlaceholderText = "Suche nach Name/Kategorie",
        AccessibleName = "Suche"
    };
    private readonly ComboBox _category = new()
    {
        Width = 215,
        DropDownStyle = ComboBoxStyle.DropDownList,
        AccessibleName = "Kategorie"
    };
    private readonly ComboBox _sort = new()
    {
        Width = 160,
        DropDownStyle = ComboBoxStyle.DropDownList,
        AccessibleName = "Sortierung"
    };
    private readonly CheckBox _favorites = new()
    {
        Text = "Nur Favoriten",
        AutoSize = true
    };
    private readonly CheckBox _available = new()
    {
        Text = "Nur lauffaehige",
        AutoSize = true
    };
    private bool _updating;
    internal event Action<AppFilter>? FilterChanged;
    internal event Action? RefreshRequested;
    internal event Action? RandomRequested;
    internal event Action? RootRequested;
    internal SearchPanel()
    {
        Dock = DockStyle.Fill;
        AutoSize = true;
        WrapContents = true;
        Padding = new Padding(8);
        _sort.Items.AddRange(new object[] { "Nach Kategorie", "Nach Name", "Zuletzt gestartet", "Am haeufigsten", "Favoriten zuerst" });
        Controls.AddRange(new Control[] { _search, _category, _sort, _favorites, _available });
        AddButton("Refresh (F5)", () => RefreshRequested?.Invoke());
        AddButton("Surprise (Ctrl+R)", () => RandomRequested?.Invoke());
        AddButton("Root", () => RootRequested?.Invoke());
        for (int i = 0; i < Controls.Count; i++)
            Controls[i].TabIndex = i;
        _search.TextChanged += Changed;
        _category.SelectedIndexChanged += Changed;
        _sort.SelectedIndexChanged += Changed;
        _favorites.CheckedChanged += Changed;
        _available.CheckedChanged += Changed;
    }

    private void AddButton(string text, Action action)
    {
        var button = new Button
        {
            Text = text,
            AutoSize = true
        };
        button.Click += (_, _) => action();
        Controls.Add(button);
    }

    private void Changed(object? sender, EventArgs args)
    {
        if (_updating)
            return;
        FilterChanged?.Invoke(new(_search.Text, (_category.SelectedItem as CategoryChoice)?.Category, _favorites.Checked, _available.Checked, (AppSortOrder)Math.Max(0, _sort.SelectedIndex)));
    }

    internal void Display(AppFilter filter, IEnumerable<string> categories)
    {
        _updating = true;
        try
        {
            _search.Text = filter.Search;
            _category.Items.Clear();
            _category.Items.Add(new CategoryChoice(null));
            foreach (string category in categories)
                _category.Items.Add(new CategoryChoice(category));
            _category.SelectedIndex = 0;
            for (int i = 1; i < _category.Items.Count; i++)
                if (_category.Items[i] is CategoryChoice choice
                    && string.Equals(choice.Category, filter.Category, StringComparison.OrdinalIgnoreCase))
                    _category.SelectedIndex = i;
            _sort.SelectedIndex = (int)filter.Sort;
            _favorites.Checked = filter.FavoritesOnly;
            _available.Checked = filter.AvailableOnly;
        }
        finally
        {
            _updating = false;
        }
    }

    internal void FocusSearch()
    {
        _search.Focus();
        _search.SelectAll();
    }

    internal void ClearSearch() => _search.Clear();
}
