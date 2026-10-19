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
    public override bool ShowDefaultsButton => false;

    public override bool OnApply()
    {
        _control?.ApplySettings();
        return true;
    }
}

internal sealed class ContextAssistOptionsControl : Panel
{
    private readonly ContextAssistSettings _settings;

    private readonly CheckBox _enabled = new()
    {
        Text = "Context Assist aktivieren"
    };

    private readonly DropDown _position = new();

    private readonly NumericStepper _primaryCount = new()
    {
        MinValue = 1,
        MaxValue = 5,
        DecimalPlaces = 0
    };

    private readonly DropDown _selectionKind = new();
    private readonly ListBox _actions = new();

    private readonly CheckBox _actionEnabled = new()
    {
        Text = "Aktion aktiviert"
    };

    private readonly CheckBox _favorite = new()
    {
        Text = "Favorit (★)"
    };

    private List<ActionPreference> _current = new();
    private bool _updatingControls;

    public ContextAssistOptionsControl(ContextAssistSettings settings)
    {
        _settings = settings;

        _position.Items.Add("Neben Mauszeiger");
        _position.Items.Add("Neben Geometrie");

        foreach (var kind in ActionCatalog.ConfigurableKinds)
            _selectionKind.Items.Add(GetSelectionKindName(kind));

        var moveUp = new Button { Text = "Nach oben" };
        var moveDown = new Button { Text = "Nach unten" };

        moveUp.Click += (_, _) => MoveAction(-1);
        moveDown.Click += (_, _) => MoveAction(1);
        _selectionKind.SelectedIndexChanged += (_, _) => LoadSelectedKind();
        _actions.SelectedIndexChanged += (_, _) => LoadSelectedAction();
        _actionEnabled.CheckedChanged += (_, _) => EditSelectedAction();
        _favorite.CheckedChanged += (_, _) => EditSelectedAction();

        var layout = new DynamicLayout
        {
            Padding = 10,
            Spacing = new Eto.Drawing.Size(6, 6)
        };

        layout.AddRow(_enabled);
        layout.AddRow(new Label { Text = "Position" }, _position);
        layout.AddRow(new Label { Text = "Anzahl Hauptaktionen" }, _primaryCount);
        layout.AddRow(new Label { Text = "Objekttyp" }, _selectionKind);
        layout.AddRow(_actions);
        layout.AddRow(_actionEnabled, _favorite);
        layout.AddRow(moveUp, moveDown);

        Content = layout;
        LoadSettingsIntoControls();
    }

    private void LoadSettingsIntoControls()
    {
        _updatingControls = true;
        _enabled.Checked = _settings.Enabled;
        _position.SelectedIndex = (int)_settings.PositionMode;
        _primaryCount.Value = _settings.PrimaryCount;
        _selectionKind.SelectedIndex = 0;
        _updatingControls = false;

        LoadSelectedKind();
    }

    private void LoadSelectedKind()
    {
        if (_selectionKind.SelectedIndex < 0)
            return;

        var kind = ActionCatalog.ConfigurableKinds[_selectionKind.SelectedIndex];
        _current = _settings.Preferences[kind]
            .Select(x => new ActionPreference(x.Id, x.Enabled, x.Favorite))
            .ToList();

        RefreshActionList();
    }

    private void RefreshActionList()
    {
        if (_selectionKind.SelectedIndex < 0)
            return;

        var kind = ActionCatalog.ConfigurableKinds[_selectionKind.SelectedIndex];
        var catalog = ActionCatalog.Defaults(kind).ToDictionary(x => x.Id);
        var previousIndex = _actions.SelectedIndex;

        _updatingControls = true;
        _actions.Items.Clear();

        foreach (var preference in _current)
        {
            if (!catalog.TryGetValue(preference.Id, out var action))
                continue;

            var star = preference.Favorite ? "★ " : string.Empty;
            var state = preference.Enabled ? "☑" : "☐";
            _actions.Items.Add($"{star}{state} {action.Label}");
        }

        if (_current.Count > 0)
            _actions.SelectedIndex = Math.Clamp(previousIndex, 0, _current.Count - 1);

        _updatingControls = false;
        LoadSelectedAction();
    }

    private void LoadSelectedAction()
    {
        var index = _actions.SelectedIndex;
        if (index < 0 || index >= _current.Count)
            return;

        _updatingControls = true;
        _actionEnabled.Checked = _current[index].Enabled;
        _favorite.Checked = _current[index].Favorite;
        _updatingControls = false;
    }

    private void EditSelectedAction()
    {
        if (_updatingControls)
            return;

        var index = _actions.SelectedIndex;
        if (index < 0 || index >= _current.Count)
            return;

        _current[index] = _current[index] with
        {
            Enabled = _actionEnabled.Checked == true,
            Favorite = _favorite.Checked == true
        };

        SaveCurrentKind();
        RefreshActionList();
    }

    private void MoveAction(int delta)
    {
        var index = _actions.SelectedIndex;
        var target = index + delta;

        if (index < 0 || target < 0 || target >= _current.Count)
            return;

        (_current[index], _current[target]) =
            (_current[target], _current[index]);

        SaveCurrentKind();
        RefreshActionList();
        _actions.SelectedIndex = target;
    }

    private void SaveCurrentKind()
    {
        if (_selectionKind.SelectedIndex < 0)
            return;

        var kind = ActionCatalog.ConfigurableKinds[_selectionKind.SelectedIndex];
        _settings.Preferences[kind] = _current
            .Select(x => new ActionPreference(x.Id, x.Enabled, x.Favorite))
            .ToList();
    }

    public void ApplySettings()
    {
        SaveCurrentKind();

        _settings.Enabled = _enabled.Checked == true;
        _settings.PositionMode = (PopupPositionMode)Math.Max(0, _position.SelectedIndex);
        _settings.PrimaryCount = (int)_primaryCount.Value;
        _settings.Save();
    }

    private static string GetSelectionKindName(SelectionKind kind) => kind switch
    {
        SelectionKind.PlanarBrepFace => "Planare Teilfläche",
        SelectionKind.BrepFace => "Teilfläche",
        SelectionKind.BrepEdge => "Brep-Kante",
        SelectionKind.ClosedPlanarCurve => "Geschlossene planare Kurve",
        SelectionKind.Curve => "Kurve",
        SelectionKind.PlanarSurface => "Planare Fläche",
        SelectionKind.Surface => "Fläche",
        SelectionKind.SolidBrep => "Geschlossener Körper",
        SelectionKind.Brep => "Flächenverband",
        SelectionKind.Mesh => "Mesh",
        SelectionKind.SubD => "SubD",
        SelectionKind.Multiple => "Mehrfachauswahl",
        SelectionKind.Mixed => "Gemischte Auswahl",
        _ => kind.ToString()
    };
}
