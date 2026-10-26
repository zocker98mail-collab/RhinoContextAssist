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
    private readonly NumericStepper _cornerMargin = CreateNumber(50, 250);
    private readonly NumericStepper _primaryCount = CreateNumber(1, 5);
    private readonly ListBox _actions = new();
    private readonly ListBox _customActions = new();
    private readonly CheckBox _enabled = new() { Text = "Aktiviert" };
    private readonly CheckBox _automaticPriority = new() { Text = "Automatisch priorisieren" };

    public ContextAssistOptionsControl(ContextAssistSettings settings)
    {
        _settings = settings;

        foreach (var text in new[]
        {
            "Neben Mauszeiger",
            "Direkt an Geometrie",
            "Mit Abstand zur Geometrie",
            "Automatisch freie Ecke",
            "Links oben",
            "Rechts oben",
            "Links unten",
            "Rechts unten"
        })
        {
            _position.Items.Add(text);
        }

        foreach (var text in new[] { "Direkt", "Kompakt", "Manuell" })
            _uxMode.Items.Add(text);

        foreach (var kind in ActionCatalog.Kinds)
            _selectionKind.Items.Add(kind.ToString());

        var addActionButton = new Button { Text = "Eigene Aktion hinzufügen" };
        var deleteActionButton = new Button { Text = "Eigene Aktion löschen" };
        var moveUpButton = new Button { Text = "Nach oben" };
        var moveDownButton = new Button { Text = "Nach unten" };
        var exportButton = new Button { Text = "Konfiguration exportieren" };
        var importButton = new Button { Text = "Konfiguration importieren" };

        addActionButton.Click += (_, _) => AddCustomAction();
        deleteActionButton.Click += (_, _) => DeleteSelectedCustomAction();
        moveUpButton.Click += (_, _) => MoveSelectedAction(-1);
        moveDownButton.Click += (_, _) => MoveSelectedAction(1);
        exportButton.Click += (_, _) => Rhino.RhinoApp.RunScript("_ContextAssistExport", false);
        importButton.Click += (_, _) => Rhino.RhinoApp.RunScript("_ContextAssistImport", false);
        _selectionKind.SelectedIndexChanged += (_, _) => RefreshActions();

        var displayLayout = new DynamicLayout { Padding = 10 };
        displayLayout.AddRow(_enabled, _automaticPriority);
        displayLayout.AddRow(new Label { Text = "Darstellung" }, _uxMode);
        displayLayout.AddRow(new Label { Text = "Position" }, _position);
        displayLayout.AddRow(new Label { Text = "Randabstand Ecke (px)" }, _cornerMargin);
        displayLayout.AddRow(new Label { Text = "Hauptaktionen" }, _primaryCount);

        var actionsLayout = new DynamicLayout { Padding = 10 };
        actionsLayout.AddRow(_selectionKind);
        actionsLayout.AddRow(_actions);
        actionsLayout.AddRow(moveUpButton, moveDownButton);

        var customLayout = new DynamicLayout { Padding = 10 };
        customLayout.AddRow(_customActions);
        customLayout.AddRow(addActionButton, deleteActionButton);
        customLayout.AddRow(exportButton, importButton);

        var tabs = new TabControl();
        tabs.Pages.Add(new TabPage { Text = "Darstellung", Content = displayLayout });
        tabs.Pages.Add(new TabPage { Text = "Aktionen", Content = actionsLayout });
        tabs.Pages.Add(new TabPage { Text = "Eigene Aktionen", Content = customLayout });
        Content = tabs;

        LoadSettingsIntoControls();
    }

    private static NumericStepper CreateNumber(double min, double max) => new()
    {
        MinValue = min,
        MaxValue = max,
        DecimalPlaces = 0
    };

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
        if (_selectionKind.SelectedIndex < 0)
            return;

        var kind = ActionCatalog.Kinds[_selectionKind.SelectedIndex];
        foreach (var action in ActionCatalog.Defaults(kind))
            _actions.Items.Add($"{action.Icon} {action.Label} [{action.Category}]");
    }

    private void RefreshCustomActions()
    {
        _customActions.Items.Clear();
        foreach (var action in _settings.CustomActions)
            _customActions.Items.Add($"{action.Label} → {action.Requirement}");
    }

    private void DeleteSelectedCustomAction()
    {
        if (_customActions.SelectedIndex < 0)
            return;

        _settings.CustomActions.RemoveAt(_customActions.SelectedIndex);
        _settings.Save();
        RefreshCustomActions();
    }

    private void MoveSelectedAction(int direction)
    {
        if (_selectionKind.SelectedIndex < 0 || _actions.SelectedIndex < 0)
            return;

        var kind = ActionCatalog.Kinds[_selectionKind.SelectedIndex];
        var preferences = _settings.Preferences[kind];
        var currentIndex = _actions.SelectedIndex;
        var targetIndex = currentIndex + direction;

        if (targetIndex < 0 || targetIndex >= preferences.Count)
            return;

        (preferences[currentIndex], preferences[targetIndex]) =
            (preferences[targetIndex], preferences[currentIndex]);

        _settings.Save();
        RefreshActions();
        _actions.SelectedIndex = targetIndex;
    }

    private void AddCustomAction()
    {
        var dialog = new Dialog<bool>
        {
            Title = "Eigene Aktion",
            Size = new Eto.Drawing.Size(440, 330)
        };

        var nameTextBox = new TextBox();
        var macroTextBox = new TextBox();
        var iconTextBox = new TextBox { Text = "•" };
        var categoryTextBox = new TextBox { Text = "Eigene" };
        var objectTypeDropDown = new DropDown();
        var destructiveCheckBox = new CheckBox { Text = "Destruktive Aktion" };
        var addButton = new Button { Text = "Hinzufügen" };

        foreach (var kind in ActionCatalog.Kinds)
            objectTypeDropDown.Items.Add(kind.ToString());
        objectTypeDropDown.SelectedIndex = 0;

        addButton.Click += (_, _) =>
        {
            if (string.IsNullOrWhiteSpace(nameTextBox.Text) ||
                string.IsNullOrWhiteSpace(macroTextBox.Text))
            {
                return;
            }

            var requirement = objectTypeDropDown.SelectedValue?.ToString() ?? "Any";
            var action = new ContextAction(
                "custom_" + Guid.NewGuid().ToString("N"),
                iconTextBox.Text,
                nameTextBox.Text,
                macroTextBox.Text,
                nameTextBox.Text,
                categoryTextBox.Text,
                destructiveCheckBox.Checked == true,
                requirement,
                true);

            _settings.CustomActions.Add(action);
            _settings.Save();
            dialog.Close(true);
        };

        var layout = new DynamicLayout { Padding = 10 };
        layout.AddRow(new Label { Text = "Name" }, nameTextBox);
        layout.AddRow(new Label { Text = "Makro" }, macroTextBox);
        layout.AddRow(new Label { Text = "Icon" }, iconTextBox);
        layout.AddRow(new Label { Text = "Kategorie" }, categoryTextBox);
        layout.AddRow(new Label { Text = "Objekttyp" }, objectTypeDropDown);
        layout.AddRow(destructiveCheckBox);
        layout.AddRow(addButton);

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
