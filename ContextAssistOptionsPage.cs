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
    private readonly NumericStepper _cornerMargin = Number(50, 250);
    private readonly NumericStepper _primaryCount = Number(1, 5);
    private readonly ListBox _actions = new() { Height = 300 };
    private readonly ListBox _customActions = new() { Height = 260 };
    private readonly CheckBox _enabled = new() { Text = "Aktiviert" };
    private readonly CheckBox _automaticPriority = new() { Text = "Automatisch priorisieren" };

    public ContextAssistOptionsControl(ContextAssistSettings settings)
    {
        _settings = settings;
        foreach (var text in new[] { "Neben Mauszeiger", "Direkt an Geometrie", "Mit Abstand zur Geometrie", "Automatisch freie Ecke", "Links oben", "Rechts oben", "Links unten", "Rechts unten" }) _position.Items.Add(text);
        foreach (var text in new[] { "Direkt", "Kompakt", "Manuell" }) _uxMode.Items.Add(text);
        foreach (var kind in ActionCatalog.Kinds) _selectionKind.Items.Add(kind.ToString());

        var add = SmallButton("Eigene Aktion hinzufügen", 168);
        var delete = SmallButton("Eigene Aktion löschen", 158);
        var up = SmallButton("Nach oben", 105);
        var down = SmallButton("Nach unten", 105);
        var export = SmallButton("Konfiguration exportieren", 165);
        var import = SmallButton("Konfiguration importieren", 165);

        add.Click += (_, _) => AddCustomAction();
        delete.Click += (_, _) => DeleteCustomAction();
        up.Click += (_, _) => MoveAction(-1);
        down.Click += (_, _) => MoveAction(1);
        export.Click += (_, _) => Rhino.RhinoApp.RunScript("_ContextAssistExport", false);
        import.Click += (_, _) => Rhino.RhinoApp.RunScript("_ContextAssistImport", false);
        _selectionKind.SelectedIndexChanged += (_, _) => RefreshActions();

        var display = new DynamicLayout { Padding = 10, Spacing = new Eto.Drawing.Size(6, 6) };
        display.AddRow(_enabled, _automaticPriority);
        display.AddRow(new Label { Text = "Darstellung" }, _uxMode);
        display.AddRow(new Label { Text = "Position" }, _position);
        display.AddRow(new Label { Text = "Randabstand Ecke (px)" }, _cornerMargin);
        display.AddRow(new Label { Text = "Hauptaktionen" }, _primaryCount);

        var actions = new DynamicLayout { Padding = 10, Spacing = new Eto.Drawing.Size(6, 6) };
        actions.AddRow(_selectionKind);
        actions.AddRow(_actions);
        actions.AddRow(ButtonRow(up, down));

        var custom = new DynamicLayout { Padding = 10, Spacing = new Eto.Drawing.Size(6, 6) };
        custom.AddRow(_customActions);
        custom.AddRow(ButtonRow(add, delete));
        custom.AddRow(ButtonRow(export, import));

        var tabs = new TabControl();
        tabs.Pages.Add(new TabPage { Text = "Darstellung", Content = display });
        tabs.Pages.Add(new TabPage { Text = "Aktionen", Content = actions });
        tabs.Pages.Add(new TabPage { Text = "Eigene Aktionen", Content = custom });
        Content = tabs;
        LoadSettingsIntoControls();
    }

    private static Button SmallButton(string text, int width) => new() { Text = text, Width = width, Height = 30 };
    private static StackLayout ButtonRow(params Button[] buttons)
    {
        var row = new StackLayout { Orientation = Orientation.Horizontal, Spacing = 6 };
        foreach (var button in buttons) row.Items.Add(button);
        return row;
    }
    private static NumericStepper Number(double min, double max) => new() { MinValue = min, MaxValue = max, DecimalPlaces = 0 };

    private void LoadSettingsIntoControls()
    {
        _enabled.Checked = _settings.Enabled;
        _automaticPriority.Checked = _settings.AutomaticPrioritization;
        _uxMode.SelectedIndex = (int)_settings.UxMode;
        _position.SelectedIndex = (int)_settings.PositionMode;
        _cornerMargin.Value = _settings.CornerMarginPx;
        _primaryCount.Value = _settings.PrimaryCount;
        _selectionKind.SelectedIndex = 0;
        RefreshActions();
        RefreshCustomActions();
    }

    private void RefreshActions()
    {
        _actions.Items.Clear();
        if (_selectionKind.SelectedIndex < 0) return;
        var kind = ActionCatalog.Kinds[_selectionKind.SelectedIndex];
        foreach (var action in ActionCatalog.Defaults(kind)) _actions.Items.Add($"{action.Icon} {action.Label} [{action.Category}]");
    }

    private void RefreshCustomActions()
    {
        _customActions.Items.Clear();
        foreach (var action in _settings.CustomActions) _customActions.Items.Add($"{action.Label} → {action.Requirement}");
    }

    private void MoveAction(int direction)
    {
        if (_selectionKind.SelectedIndex < 0 || _actions.SelectedIndex < 0) return;
        var kind = ActionCatalog.Kinds[_selectionKind.SelectedIndex];
        var preferences = _settings.Preferences[kind];
        var current = _actions.SelectedIndex;
        var target = current + direction;
        if (target < 0 || target >= preferences.Count) return;
        (preferences[current], preferences[target]) = (preferences[target], preferences[current]);
        _settings.Save();
        RefreshActions();
        _actions.SelectedIndex = target;
    }

    private void DeleteCustomAction()
    {
        if (_customActions.SelectedIndex < 0) return;
        _settings.CustomActions.RemoveAt(_customActions.SelectedIndex);
        _settings.Save();
        RefreshCustomActions();
    }

    private void AddCustomAction()
    {
        var dialog = new Dialog<bool> { Title = "Eigene Aktion", Size = new Eto.Drawing.Size(440, 330) };
        var name = new TextBox();
        var macro = new TextBox();
        var icon = new TextBox { Text = "•" };
        var category = new TextBox { Text = "Eigene" };
        var objectType = new DropDown();
        var destructive = new CheckBox { Text = "Destruktive Aktion" };
        var add = SmallButton("Hinzufügen", 110);
        foreach (var kind in ActionCatalog.Kinds) objectType.Items.Add(kind.ToString());
        objectType.SelectedIndex = 0;
        add.Click += (_, _) =>
        {
            if (string.IsNullOrWhiteSpace(name.Text) || string.IsNullOrWhiteSpace(macro.Text)) return;
            _settings.CustomActions.Add(new ContextAction("custom_" + Guid.NewGuid().ToString("N"), icon.Text, name.Text, macro.Text, name.Text, category.Text, destructive.Checked == true, objectType.SelectedValue?.ToString() ?? "Any", true));
            _settings.Save();
            dialog.Close(true);
        };
        var layout = new DynamicLayout { Padding = 10, Spacing = new Eto.Drawing.Size(6, 6) };
        layout.AddRow(new Label { Text = "Name" }, name);
        layout.AddRow(new Label { Text = "Makro" }, macro);
        layout.AddRow(new Label { Text = "Icon" }, icon);
        layout.AddRow(new Label { Text = "Kategorie" }, category);
        layout.AddRow(new Label { Text = "Objekttyp" }, objectType);
        layout.AddRow(destructive);
        layout.AddRow(ButtonRow(add));
        dialog.Content = layout;
        dialog.ShowModal(this);
        RefreshCustomActions();
    }

    public void ApplySettings()
    {
        _settings.Enabled = _enabled.Checked == true;
        _settings.AutomaticPrioritization = _automaticPriority.Checked == true;
        _settings.UxMode = (PopupUxMode)Math.Max(0, _uxMode.SelectedIndex);
        _settings.PositionMode = (PopupPositionMode)Math.Max(0, _position.SelectedIndex);
        _settings.CornerMarginPx = (int)_cornerMargin.Value;
        _settings.PrimaryCount = (int)_primaryCount.Value;
        _settings.Save();
    }
}
