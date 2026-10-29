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

    private readonly ListBox _actions = new()
    {
        Height = 330
    };

    private readonly CheckBox _enabled = new()
    {
        Text = "Aktiviert"
    };

    private readonly CheckBox _automaticPriority = new()
    {
        Text = "Automatisch priorisieren"
    };

    public ContextAssistOptionsControl(ContextAssistSettings settings)
    {
        _settings = settings;

        AddPositionItems();
        AddUxModeItems();
        AddSelectionKindItems();

        var favoriteButton = CreateSmallButton("Favorit umschalten", 135);
        var moveUpButton = CreateSmallButton("Nach oben", 100);
        var moveDownButton = CreateSmallButton("Nach unten", 100);

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
        displayLayout.AddRow(new Label { Text = "Darstellung" }, _uxMode);
        displayLayout.AddRow(new Label { Text = "Position" }, _position);
        displayLayout.AddRow(new Label { Text = "Randabstand" }, _cornerMargin);
        displayLayout.AddRow(new Label { Text = "Hauptaktionen" }, _primaryCount);

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
            Text = "Darstellung",
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

    private static NumericStepper CreateNumber(double min, double max) => new()
    {
        MinValue = min,
        MaxValue = max,
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
        _position.SelectedIndex = (int)_settings.PositionMode;
        _uxMode.SelectedIndex = (int)_settings.UxMode;
        _cornerMargin.Value = _settings.CornerMarginPx;
        _primaryCount.Value = _settings.PrimaryCount;
        _selectionKind.SelectedIndex = 0;
        RefreshActions();
    }

    private void RefreshActions()
    {
        _actions.Items.Clear();

        if (_selectionKind.SelectedIndex < 0)
            return;

        var kind = ActionCatalog.Kinds[_selectionKind.SelectedIndex];

        foreach (var action in ActionCatalog.Defaults(kind))
        {
            var favoriteMarker = _settings.IsFavorite(kind, action.Id)
                ? "★"
                : "☆";

            _actions.Items.Add(
                $"{favoriteMarker} {action.Icon} {action.Label} [{action.Category}]");
        }
    }

    private void ToggleSelectedFavorite()
    {
        if (_selectionKind.SelectedIndex < 0 || _actions.SelectedIndex < 0)
            return;

        var kind = ActionCatalog.Kinds[_selectionKind.SelectedIndex];
        var catalog = ActionCatalog.Defaults(kind);

        if (_actions.SelectedIndex >= catalog.Count)
            return;

        _settings.ToggleFavorite(kind, catalog[_actions.SelectedIndex].Id);
        RefreshActions();
    }

    private void MoveSelectedAction(int direction)
    {
        if (_selectionKind.SelectedIndex < 0 || _actions.SelectedIndex < 0)
            return;

        var kind = ActionCatalog.Kinds[_selectionKind.SelectedIndex];

        if (!_settings.Preferences.TryGetValue(kind, out var preferences))
            return;

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

    public void ApplySettings()
    {
        _settings.Enabled = _enabled.Checked == true;
        _settings.AutomaticPrioritization = _automaticPriority.Checked == true;
        _settings.PositionMode =
            (PopupPositionMode)Math.Max(0, _position.SelectedIndex);
        _settings.UxMode =
            (PopupUxMode)Math.Max(0, _uxMode.SelectedIndex);
        _settings.CornerMarginPx = (int)_cornerMargin.Value;
        _settings.PrimaryCount = (int)_primaryCount.Value;
        _settings.Save();
    }
}
