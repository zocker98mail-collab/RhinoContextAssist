using Eto.Forms;
using Rhino.UI;

namespace RhinoContextAssist;

internal sealed class ContextAssistOptionsPage : OptionsDialogPage
{
    private readonly ContextAssistSettings _settings;
    private ContextAssistOptionsControl? _control;

    public ContextAssistOptionsPage(ContextAssistSettings settings)
        : base("Context Assist")
    {
        _settings = settings;
    }

    public override object PageControl =>
        _control ??= new ContextAssistOptionsControl(_settings);

    public override bool ShowApplyButton => true;

    public override bool OnApply()
    {
        _control?.ApplySettings();
        return true;
    }
}

internal sealed class ContextAssistOptionsControl : Panel
{
    private readonly ContextAssistSettings _settings;

    private readonly DropDown _position = new();
    private readonly DropDown _uxMode = new();
    private readonly DropDown _selectionKind = new();

    private readonly NumericStepper _cornerMargin = CreateNumber(20, 350);
    private readonly NumericStepper _primaryCount = CreateNumber(1, 8);

    private readonly NumericStepper _selectionDelay = CreateNumber(50, 2000, 50);
    private readonly NumericStepper _hoverDelay = CreateNumber(50, 2000, 50);
    private readonly NumericStepper _collapseDelay = CreateNumber(100, 5000, 100);
    private readonly NumericStepper _moreCollapseDelay = CreateNumber(200, 10000, 100);
    private readonly NumericStepper _autoHideDelay = CreateNumber(0, 30000, 500);
    private readonly NumericStepper _idleOpacity = CreateNumber(15, 100, 5);
    private readonly NumericStepper _activeOpacity = CreateNumber(30, 100, 5);

    private readonly ListBox _actions = new()
    {
        Height = 310
    };

    private readonly CheckBox _enabled = new()
    {
        Text = "Aktiviert"
    };

    private readonly CheckBox _automaticPriority = new()
    {
        Text = "Automatisch nach Nutzung priorisieren"
    };

    private readonly CheckBox _showViewportAnchor = new()
    {
        Text = "Viewport-Ankerpunkt an Auswahl anzeigen (Hybrid)"
    };

    private readonly CheckBox _showTetherLine = new()
    {
        Text = "Dezente Verbindungslinie zur Toolbar zeichnen"
    };

    public ContextAssistOptionsControl(ContextAssistSettings settings)
    {
        _settings = settings;

        AddPositionItems();
        AddUxModeItems();
        AddSelectionKindItems();

        var favoriteButton = CreateSmallButton("Favorit umschalten", 140);
        var moveUpButton = CreateSmallButton("Nach oben", 105);
        var moveDownButton = CreateSmallButton("Nach unten", 105);

        favoriteButton.Click += (_, _) => ToggleSelectedFavorite();
        moveUpButton.Click += (_, _) => MoveSelectedAction(-1);
        moveDownButton.Click += (_, _) => MoveSelectedAction(1);
        _selectionKind.SelectedIndexChanged += (_, _) => RefreshActions();

        var displayLayout = new DynamicLayout
        {
            Padding = 10,
            Spacing = new Eto.Drawing.Size(6, 6)
        };

        displayLayout.AddRow(_enabled, _automaticPriority);
        displayLayout.AddRow(_showViewportAnchor, _showTetherLine);
        displayLayout.AddRow(new Label { Text = "Darstellung" }, _uxMode);
        displayLayout.AddRow(new Label { Text = "Position" }, _position);
        displayLayout.AddRow(new Label { Text = "Randabstand (px)" }, _cornerMargin);
        displayLayout.AddRow(new Label { Text = "Hauptaktionen (Anzahl)" }, _primaryCount);
        displayLayout.AddRow(new Label { Text = "Auswahlverzögerung (ms)" }, _selectionDelay);
        displayLayout.AddRow(new Label { Text = "Hover-Verzögerung (ms)" }, _hoverDelay);
        displayLayout.AddRow(new Label { Text = "Einklappzeit (ms)" }, _collapseDelay);
        displayLayout.AddRow(new Label { Text = "Mehr-Menü Einklappzeit (ms)" }, _moreCollapseDelay);
        displayLayout.AddRow(new Label { Text = "Auto-Hide (ms, 0 = aus)" }, _autoHideDelay);
        displayLayout.AddRow(new Label { Text = "Ruhe-Deckkraft (%)" }, _idleOpacity);
        displayLayout.AddRow(new Label { Text = "Aktive Deckkraft (%)" }, _activeOpacity);
        displayLayout.AddSpace();

        var actionsLayout = new DynamicLayout
        {
            Padding = 10,
            Spacing = new Eto.Drawing.Size(6, 6)
        };

        actionsLayout.AddRow(_selectionKind);
        actionsLayout.AddRow(_actions);
        actionsLayout.AddRow(CreateButtonRow(
            favoriteButton,
            moveUpButton,
            moveDownButton));

        var tabs = new TabControl();
        tabs.Pages.Add(new TabPage
        {
            Text = "Darstellung & Zeiten",
            Content = displayLayout
        });
        tabs.Pages.Add(new TabPage
        {
            Text = "Aktionen & Favoriten",
            Content = actionsLayout
        });

        Content = tabs;
        LoadSettingsIntoControls();
    }

    private void AddPositionItems()
    {
        var labels = new[]
        {
            "Neben Mauszeiger",
            "Direkt an Geometrie",
            "Mit Abstand zur Geometrie",
            "Automatisch freie Ecke",
            "Links oben",
            "Rechts oben",
            "Links unten",
            "Rechts unten"
        };

        foreach (var label in labels)
            _position.Items.Add(label);
    }

    private void AddUxModeItems()
    {
        _uxMode.Items.Add("Direkt");
        _uxMode.Items.Add("Kompakt");
        _uxMode.Items.Add("Manuell");
    }

    private void AddSelectionKindItems()
    {
        foreach (var kind in ActionCatalog.Kinds)
            _selectionKind.Items.Add(kind.ToString());
    }

    private static NumericStepper CreateNumber(double min, double max, double increment = 1) => new()
    {
        MinValue = min,
        MaxValue = max,
        Increment = increment,
        DecimalPlaces = 0
    };

    private static Button CreateSmallButton(string text, int width) => new()
    {
        Text = text,
        Width = width,
        Height = 30
    };

    private static StackLayout CreateButtonRow(params Button[] buttons)
    {
        var row = new StackLayout
        {
            Orientation = Orientation.Horizontal,
            Spacing = 6
        };

        foreach (var button in buttons)
            row.Items.Add(button);

        return row;
    }

    private void LoadSettingsIntoControls()
    {
        _enabled.Checked = _settings.Enabled;
        _automaticPriority.Checked = _settings.AutomaticPrioritization;
        _showViewportAnchor.Checked = _settings.ShowViewportAnchor;
        _showTetherLine.Checked = _settings.ShowTetherLine;

        _position.SelectedIndex = (int)_settings.PositionMode;
        _uxMode.SelectedIndex = (int)_settings.UxMode;
        _cornerMargin.Value = _settings.CornerMarginPx;
        _primaryCount.Value = _settings.PrimaryCount;

        _selectionDelay.Value = _settings.SelectionDelayMs;
        _hoverDelay.Value = _settings.HoverDelayMs;
        _collapseDelay.Value = _settings.CollapseDelayMs;
        _moreCollapseDelay.Value = _settings.MoreCollapseDelayMs;
        _autoHideDelay.Value = _settings.AutoHideMs;
        _idleOpacity.Value = _settings.IdleOpacity;
        _activeOpacity.Value = _settings.ActiveOpacity;

        _selectionKind.SelectedIndex = 0;
        RefreshActions();
    }

    private List<ContextAction> GetOrderedCatalogForKind(SelectionKind kind)
    {
        var catalog = ActionCatalog.Defaults(kind).ToList();
        if (!_settings.Preferences.TryGetValue(kind, out var prefs))
            return catalog;

        var orderMap = prefs
            .Select((p, idx) => new { p.Id, idx })
            .GroupBy(x => x.Id)
            .ToDictionary(g => g.Key, g => g.First().idx);

        return catalog
            .OrderBy(a => orderMap.TryGetValue(a.Id, out var i) ? i : int.MaxValue)
            .ToList();
    }

    private void RefreshActions()
    {
        _actions.Items.Clear();

        if (_selectionKind.SelectedIndex < 0)
            return;

        var kind = ActionCatalog.Kinds[_selectionKind.SelectedIndex];
        var ordered = GetOrderedCatalogForKind(kind);

        foreach (var action in ordered)
        {
            var favoriteMarker = _settings.IsFavorite(kind, action.Id)
                ? "★"
                : "☆";
            var count = _settings.UsageCount(action.Id);
            var usageSuffix = count > 0 ? $" · {count}×" : string.Empty;

            _actions.Items.Add(
                $"{favoriteMarker} {action.Icon} {action.Label} [{action.Category}]{usageSuffix}");
        }
    }

    private void ToggleSelectedFavorite()
    {
        if (_selectionKind.SelectedIndex < 0 || _actions.SelectedIndex < 0)
            return;

        var kind = ActionCatalog.Kinds[_selectionKind.SelectedIndex];
        var ordered = GetOrderedCatalogForKind(kind);

        if (_actions.SelectedIndex >= ordered.Count)
            return;

        int selected = _actions.SelectedIndex;
        _settings.ToggleFavorite(kind, ordered[selected].Id);
        RefreshActions();
        _actions.SelectedIndex = selected;
    }

    private void MoveSelectedAction(int direction)
    {
        if (_selectionKind.SelectedIndex < 0 || _actions.SelectedIndex < 0)
            return;

        var kind = ActionCatalog.Kinds[_selectionKind.SelectedIndex];
        var ordered = GetOrderedCatalogForKind(kind);

        var currentIndex = _actions.SelectedIndex;
        var targetIndex = currentIndex + direction;

        if (targetIndex < 0 || targetIndex >= ordered.Count)
            return;

        if (!_settings.Preferences.TryGetValue(kind, out var preferences))
            return;

        var idA = ordered[currentIndex].Id;
        var idB = ordered[targetIndex].Id;

        int prefIdxA = preferences.FindIndex(p => p.Id == idA);
        int prefIdxB = preferences.FindIndex(p => p.Id == idB);

        if (prefIdxA >= 0 && prefIdxB >= 0)
        {
            (preferences[prefIdxA], preferences[prefIdxB]) =
                (preferences[prefIdxB], preferences[prefIdxA]);

            _settings.Save();
            RefreshActions();
            _actions.SelectedIndex = targetIndex;
        }
    }

    public void ApplySettings()
    {
        _settings.Enabled = _enabled.Checked == true;
        _settings.AutomaticPrioritization = _automaticPriority.Checked == true;
        _settings.ShowViewportAnchor = _showViewportAnchor.Checked == true;
        _settings.ShowTetherLine = _showTetherLine.Checked == true;

        _settings.PositionMode =
            (PopupPositionMode)Math.Max(0, _position.SelectedIndex);
        _settings.UxMode =
            (PopupUxMode)Math.Max(0, _uxMode.SelectedIndex);
        _settings.CornerMarginPx = (int)_cornerMargin.Value;
        _settings.PrimaryCount = (int)_primaryCount.Value;

        _settings.SelectionDelayMs = (int)_selectionDelay.Value;
        _settings.HoverDelayMs = (int)_hoverDelay.Value;
        _settings.CollapseDelayMs = (int)_collapseDelay.Value;
        _settings.MoreCollapseDelayMs = (int)_moreCollapseDelay.Value;
        _settings.AutoHideMs = (int)_autoHideDelay.Value;
        _settings.IdleOpacity = (int)_idleOpacity.Value;
        _settings.ActiveOpacity = (int)_activeOpacity.Value;

        _settings.Save();
    }
}
