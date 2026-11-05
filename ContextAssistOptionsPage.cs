using Eto.Forms;
using Rhino.UI;

namespace RhinoContextAssist;

internal sealed class ContextAssistOptionsPage : OptionsDialogPage
{
    private readonly ContextAssistSettings _settings;
    private ContextAssistOptionsControl? _control;

    public ContextAssistOptionsPage(ContextAssistSettings settings) : base("Context Assist") => _settings = settings;
    public override object PageControl => _control ??= new ContextAssistOptionsControl(_settings);
    public override bool ShowApplyButton => true;
    public override bool OnApply() { _control?.ApplySettings(); return true; }
}

internal sealed class ContextAssistOptionsControl : Panel
{
    private readonly ContextAssistSettings _settings;

    private readonly DropDown _position = new();
    private readonly DropDown _uxMode = new();
    private readonly DropDown _selectionKind = new();

    private readonly NumericStepper _cornerMargin = CreateNumber(20, 350);
    private readonly NumericStepper _primaryCount = CreateNumber(1, 9);
    private readonly NumericStepper _selectionDelay = CreateNumber(50, 2000, 50);
    private readonly NumericStepper _hoverDelay = CreateNumber(50, 2000, 50);
    private readonly NumericStepper _collapseDelay = CreateNumber(100, 5000, 100);
    private readonly NumericStepper _moreCollapseDelay = CreateNumber(200, 10000, 100);
    private readonly NumericStepper _autoHideDelay = CreateNumber(0, 30000, 500);
    private readonly NumericStepper _idleOpacity = CreateNumber(15, 100, 5);
    private readonly NumericStepper _activeOpacity = CreateNumber(30, 100, 5);

    private readonly CheckBox _showViewportAnchor = new() { Text = "Viewport-Ankerpunkt an Auswahl anzeigen" };
    private readonly CheckBox _showTetherLine = new() { Text = "Dezente Verbindungslinie zur Toolbar zeichnen" };
    private readonly NumericStepper _tetherOpacity = CreateNumber(20, 255, 10);
    private readonly NumericStepper _tetherThickness = new() { MinValue = 0.5, MaxValue = 5.0, Increment = 0.5, DecimalPlaces = 1 };
    private readonly CheckBox _followCameraMovement = new() { Text = "Live-Mitbewegung bei Kamera-Rotation und Zoom" };
    private readonly CheckBox _autoHideOffscreen = new() { Text = "Ausblenden, wenn Auswahl außerhalb des Viewports liegt" };
    private readonly NumericStepper _dynamicOffset = CreateNumber(0, 200, 8);
    private readonly CheckBox _enableEdgeSnapping = new() { Text = "Magnetisches Einrasten an Viewport-Rändern aktivieren" };
    private readonly NumericStepper _edgeSnapDistance = CreateNumber(8, 80, 4);

    private readonly CheckBox _enableNumberShortcuts = new() { Text = "Tastenkürzel (1–9) für Hauptaktionen aktivieren" };
    private readonly DropDown _shortcutButton = new();
    private readonly CheckBox _highlightHovered = new() { Text = "Aktuelle Geometrie bei Button-Hover im Viewport hervorheben" };

    private readonly ListBox _actions = new() { Height = 290 };
    private readonly CheckBox _enabled = new() { Text = "Aktiviert" };
    private readonly CheckBox _automaticPriority = new() { Text = "Automatisch priorisieren inkl. 'Zuletzt genutzt'-Slot" };
    private readonly Label _transferStatusLabel = new() { Text = "Übertrage alle Einstellungen per JSON-Datei auf andere PCs." };

    public ContextAssistOptionsControl(ContextAssistSettings settings)
    {
        _settings = settings;

        foreach (var label in new[] { "Neben Mauszeiger", "Direkt an Geometrie", "Mit Abstand zur Geometrie", "Automatisch freie Ecke", "Links oben", "Rechts oben", "Links unten", "Rechts unten" }) _position.Items.Add(label);
        foreach (var label in new[] { "Direkt", "Kompakt", "Manuell" }) _uxMode.Items.Add(label);
        foreach (var kind in ActionCatalog.Kinds) _selectionKind.Items.Add(kind.ToString());
        
        _shortcutButton.Items.Add("Deaktiviert (Nur via Rhino-Befehl)");
        _shortcutButton.Items.Add("Mittlere Maustaste (MMB)");
        _shortcutButton.Items.Add("Daumentaste 1 (Zurück)");
        _shortcutButton.Items.Add("Daumentaste 2 (Vorwärts)");

        var favoriteButton = CreateSmallButton("Favorit umschalten", 140);
        var moveUpButton = CreateSmallButton("Nach oben", 105);
        var moveDownButton = CreateSmallButton("Nach unten", 105);
        var exportButton = CreateSmallButton("Einstellungen exportieren …", 190);
        var importButton = CreateSmallButton("Einstellungen importieren …", 190);

        favoriteButton.Click += (_, _) => ToggleSelectedFavorite();
        moveUpButton.Click += (_, _) => MoveSelectedAction(-1);
        moveDownButton.Click += (_, _) => MoveSelectedAction(1);
        _selectionKind.SelectedIndexChanged += (_, _) => RefreshActions();
        exportButton.Click += (_, _) => ExportSettingsDialog();
        importButton.Click += (_, _) => ImportSettingsDialog();

        var displayLayout = new DynamicLayout { Padding = 10, Spacing = new Eto.Drawing.Size(6, 6) };
        displayLayout.AddRow(_enabled, _automaticPriority);
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

        var hybridLayout = new DynamicLayout { Padding = 10, Spacing = new Eto.Drawing.Size(6, 6) };
        hybridLayout.AddRow(_showViewportAnchor);
        hybridLayout.AddRow(_showTetherLine);
        hybridLayout.AddRow(new Label { Text = "Linien-Deckkraft (20–255)" }, _tetherOpacity);
        hybridLayout.AddRow(new Label { Text = "Linienstärke (px)" }, _tetherThickness);
        hybridLayout.AddRow(_followCameraMovement);
        hybridLayout.AddRow(_autoHideOffscreen);
        hybridLayout.AddRow(new Label { Text = "Dynamischer Geometrie-Abstand (px)" }, _dynamicOffset);
        hybridLayout.AddRow(_enableEdgeSnapping);
        hybridLayout.AddRow(new Label { Text = "Magnetischer Einrastabstand (px)" }, _edgeSnapDistance);
        hybridLayout.AddRow(new Label { Text = "", Height = 10 });
        hybridLayout.AddRow(new Label { Text = "Bedienung & Ergonomie", Font = new Eto.Drawing.Font(Eto.Drawing.SystemFont.Bold) });
        hybridLayout.AddRow(_enableNumberShortcuts);
        hybridLayout.AddRow(new Label { Text = "Manueller Aufruf (Maustaste):" }, _shortcutButton);
        hybridLayout.AddRow(_highlightHovered);
        hybridLayout.AddSpace();

        var actionsLayout = new DynamicLayout { Padding = 10, Spacing = new Eto.Drawing.Size(6, 6) };
        actionsLayout.AddRow(_selectionKind);
        actionsLayout.AddRow(_actions);
        actionsLayout.AddRow(CreateButtonRow(favoriteButton, moveUpButton, moveDownButton));
        actionsLayout.AddRow(new Label { Text = "Einstellungen sichern / auf andere PCs übertragen:" });
        actionsLayout.AddRow(CreateButtonRow(exportButton, importButton));
        actionsLayout.AddRow(_transferStatusLabel);
        actionsLayout.AddSpace();

        var tabs = new TabControl();
        tabs.Pages.Add(new TabPage { Text = "Darstellung & Zeiten", Content = displayLayout });
        tabs.Pages.Add(new TabPage { Text = "Hybrid & Ergonomie", Content = hybridLayout });
        tabs.Pages.Add(new TabPage { Text = "Aktionen & Transfer", Content = actionsLayout });

        Content = tabs;
        LoadSettingsIntoControls();
    }

    private static NumericStepper CreateNumber(double min, double max, double increment = 1) => new() { MinValue = min, MaxValue = max, Increment = increment, DecimalPlaces = 0 };
    private static Button CreateSmallButton(string text, int width) => new() { Text = text, Width = width, Height = 30 };
    private static StackLayout CreateButtonRow(params Button[] buttons) { var row = new StackLayout { Orientation = Orientation.Horizontal, Spacing = 6 }; foreach (var button in buttons) row.Items.Add(button); return row; }

    private void LoadSettingsIntoControls()
    {
        _enabled.Checked = _settings.Enabled;
        _automaticPriority.Checked = _settings.AutomaticPrioritization;
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

        _showViewportAnchor.Checked = _settings.ShowViewportAnchor;
        _showTetherLine.Checked = _settings.ShowTetherLine;
        _tetherOpacity.Value = _settings.TetherLineOpacity;
        _tetherThickness.Value = _settings.TetherLineThickness;
        _followCameraMovement.Checked = _settings.FollowCameraMovement;
        _autoHideOffscreen.Checked = _settings.AutoHideWhenAnchorOffscreen;
        _dynamicOffset.Value = _settings.DynamicOffsetPx;
        _enableEdgeSnapping.Checked = _settings.EnableEdgeSnapping;
        _edgeSnapDistance.Value = _settings.EdgeSnapDistancePx;
        
        _enableNumberShortcuts.Checked = _settings.EnableNumberShortcuts;
        _shortcutButton.SelectedIndex = (int)_settings.ShortcutButton;
        _highlightHovered.Checked = _settings.HighlightHoveredGeometry;

        if (_selectionKind.SelectedIndex < 0) _selectionKind.SelectedIndex = 0;
        RefreshActions();
    }

    private void RefreshActions()
    {
        _actions.Items.Clear();
        if (_selectionKind.SelectedIndex < 0) return;
        var kind = ActionCatalog.Kinds[_selectionKind.SelectedIndex];
        var ordered = _settings.Order(kind, ActionCatalog.Defaults(kind).ToList());

        foreach (var action in ordered)
        {
            var favoriteMarker = _settings.IsFavorite(kind, action.Id) ? "★" : "☆";
            var count = _settings.UsageCount(action.Id);
            var usageSuffix = count > 0 ? $" · {count}×" : string.Empty;
            _actions.Items.Add($"{favoriteMarker} {action.Icon} {action.Label} [{action.Category}]{usageSuffix}");
        }
    }

    private void ToggleSelectedFavorite() { /* Logic unchanged from before... */
        if (_selectionKind.SelectedIndex < 0 || _actions.SelectedIndex < 0) return;
        var kind = ActionCatalog.Kinds[_selectionKind.SelectedIndex];
        var ordered = _settings.Order(kind, ActionCatalog.Defaults(kind).ToList());
        if (_actions.SelectedIndex >= ordered.Count) return;

        int selected = _actions.SelectedIndex;
        _settings.ToggleFavorite(kind, ordered[selected].Id);
        RefreshActions();
        _actions.SelectedIndex = selected;
    }

    private void MoveSelectedAction(int direction) { /* Logic unchanged... */ 
        if (_selectionKind.SelectedIndex < 0 || _actions.SelectedIndex < 0) return;
        var kind = ActionCatalog.Kinds[_selectionKind.SelectedIndex];
        var ordered = _settings.Order(kind, ActionCatalog.Defaults(kind).ToList());

        var currentIndex = _actions.SelectedIndex;
        var targetIndex = currentIndex + direction;

        if (targetIndex < 0 || targetIndex >= ordered.Count) return;
        if (!_settings.Preferences.TryGetValue(kind, out var preferences)) return;

        var idA = ordered[currentIndex].Id;
        var idB = ordered[targetIndex].Id;
        int prefIdxA = preferences.FindIndex(p => p.Id == idA);
        int prefIdxB = preferences.FindIndex(p => p.Id == idB);

        if (prefIdxA >= 0 && prefIdxB >= 0)
        {
            (preferences[prefIdxA], preferences[prefIdxB]) = (preferences[prefIdxB], preferences[prefIdxA]);
            _settings.Save();
            RefreshActions();
            _actions.SelectedIndex = targetIndex;
        }
    }

    private void ExportSettingsDialog()
    {
        ApplySettings();
        using var dialog = new Eto.Forms.SaveFileDialog { Title = "Rhino Context Assist Einstellungen exportieren", FileName = "RhinoContextAssist-Settings.json" };
        dialog.Filters.Add(new FileFilter("JSON-Einstellungsdatei (*.json)", ".json"));
        if (dialog.ShowDialog(this) == DialogResult.Ok && !string.IsNullOrWhiteSpace(dialog.FileName))
        {
            try { _settings.ExportToFile(dialog.FileName); _transferStatusLabel.Text = $"Exportiert nach: {dialog.FileName}"; }
            catch (Exception ex) { _transferStatusLabel.Text = $"Fehler beim Export: {ex.Message}"; }
        }
    }

    private void ImportSettingsDialog()
    {
        using var dialog = new Eto.Forms.OpenFileDialog { Title = "Rhino Context Assist Einstellungen importieren", MultiSelect = false };
        dialog.Filters.Add(new FileFilter("JSON-Einstellungsdatei (*.json)", ".json"));
        if (dialog.ShowDialog(this) == DialogResult.Ok && !string.IsNullOrWhiteSpace(dialog.FileName))
        {
            if (_settings.ImportFromFile(dialog.FileName, out string msg))
            {
                LoadSettingsIntoControls();
                ContextAssistPlugIn.Instance?.Controller?.RefreshConduitSettings();
            }
            _transferStatusLabel.Text = msg;
        }
    }

    public void ApplySettings()
    {
        _settings.Enabled = _enabled.Checked == true;
        _settings.AutomaticPrioritization = _automaticPriority.Checked == true;
        _settings.PositionMode = (PopupPositionMode)Math.Max(0, _position.SelectedIndex);
        _settings.UxMode = (PopupUxMode)Math.Max(0, _uxMode.SelectedIndex);
        _settings.CornerMarginPx = (int)_cornerMargin.Value;
        _settings.PrimaryCount = (int)_primaryCount.Value;
        _settings.SelectionDelayMs = (int)_selectionDelay.Value;
        _settings.HoverDelayMs = (int)_hoverDelay.Value;
        _settings.CollapseDelayMs = (int)_collapseDelay.Value;
        _settings.MoreCollapseDelayMs = (int)_moreCollapseDelay.Value;
        _settings.AutoHideMs = (int)_autoHideDelay.Value;
        _settings.IdleOpacity = (int)_idleOpacity.Value;
        _settings.ActiveOpacity = (int)_activeOpacity.Value;

        _settings.ShowViewportAnchor = _showViewportAnchor.Checked == true;
        _settings.ShowTetherLine = _showTetherLine.Checked == true;
        _settings.TetherOpacity = (int)_tetherOpacity.Value;
        _settings.TetherLineThickness = _tetherThickness.Value;
        _settings.FollowCameraMovement = _followCameraMovement.Checked == true;
        _settings.AutoHideWhenAnchorOffscreen = _autoHideOffscreen.Checked == true;
        _settings.DynamicOffsetPx = (int)_dynamicOffset.Value;
        _settings.EnableEdgeSnapping = _enableEdgeSnapping.Checked == true;
        _settings.EdgeSnapDistancePx = (int)_edgeSnapDistance.Value;
        
        _settings.EnableNumberShortcuts = _enableNumberShortcuts.Checked == true;
        _settings.ShortcutButton = (MouseTriggerButton)Math.Max(0, _shortcutButton.SelectedIndex);
        _settings.HighlightHoveredGeometry = _highlightHovered.Checked == true;

        _settings.Save();
        ContextAssistPlugIn.Instance?.Controller?.RefreshConduitSettings();
    }
}
