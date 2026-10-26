using Eto.Forms;
using Rhino.UI;

namespace RhinoContextAssist;

internal sealed class ContextAssistOptionsPage : OptionsDialogPage
{
    private readonly ContextAssistSettings _settings;
    private ContextAssistOptionsControl? _control;

    public ContextAssistOptionsPage(ContextAssistSettings settings)
        : base("Context Assist") => _settings = settings;

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
    private readonly CheckBox _enabled = new() { Text = "Context Assist aktivieren" };
    private readonly CheckBox _auto = new() { Text = "Automatisch nach Nutzung priorisieren" };
    private readonly DropDown _ux = new();
    private readonly DropDown _position = new();
    private readonly NumericStepper _primary = Number(1, 5);
    private readonly NumericStepper _selection = Number(0, 2000);
    private readonly NumericStepper _hover = Number(0, 2000);
    private readonly NumericStepper _collapse = Number(0, 5000);
    private readonly NumericStepper _moreCollapse = Number(0, 10000);
    private readonly NumericStepper _hide = Number(0, 30000);
    private readonly NumericStepper _near = Number(0, 1000);
    private readonly NumericStepper _offset = Number(0, 200);
    private readonly NumericStepper _idle = Number(15, 100);
    private readonly NumericStepper _active = Number(25, 100);

    public ContextAssistOptionsControl(ContextAssistSettings settings)
    {
        _settings = settings;
        _ux.Items.Add("Direkt vollständig");
        _ux.Items.Add("Kompakte Pille, bei Hover erweitern");
        _ux.Items.Add("Manuell: Pille per Klick öffnen");

        _position.Items.Add("Neben Mauszeiger");
        _position.Items.Add("Direkt neben Geometrie");
        _position.Items.Add("Mit Abstand zur Geometrie (Gumball-freundlich)");
        _position.Items.Add("Freie Ecke im aktiven Viewport");

        var reset = new Button { Text = "Nutzungsstatistik zurücksetzen" };
        reset.Click += (_, _) => _settings.ResetUsage();

        var layout = new DynamicLayout
        {
            Padding = 10,
            Spacing = new Eto.Drawing.Size(6, 6)
        };
        layout.AddRow(_enabled);
        layout.AddRow(_auto);
        layout.AddRow(new Label { Text = "Darstellung" }, _ux);
        layout.AddRow(new Label { Text = "Position" }, _position);
        layout.AddRow(new Label { Text = "Hauptaktionen" }, _primary);
        layout.AddRow(new Label { Text = "Verzögerung nach Auswahl (ms)" }, _selection);
        layout.AddRow(new Label { Text = "Hover-Verzögerung (ms)" }, _hover);
        layout.AddRow(new Label { Text = "Einklappen (ms)" }, _collapse);
        layout.AddRow(new Label { Text = "Einklappen bei offenem Mehr (ms)" }, _moreCollapse);
        layout.AddRow(new Label { Text = "Automatisch ausblenden (ms, 0 = aus)" }, _hide);
        layout.AddRow(new Label { Text = "Nähe zur Auswahl (px, 0 = aus)" }, _near);
        layout.AddRow(new Label { Text = "Abstand (px)" }, _offset);
        layout.AddRow(new Label { Text = "Ruhe-Deckkraft (%)" }, _idle);
        layout.AddRow(new Label { Text = "Hover-Deckkraft (%)" }, _active);
        layout.AddRow(reset);
        Content = layout;

        LoadSettingsIntoControls();
    }

    private static NumericStepper Number(double min, double max) => new()
    {
        MinValue = min,
        MaxValue = max,
        DecimalPlaces = 0
    };

    private void LoadSettingsIntoControls()
    {
        _enabled.Checked = _settings.Enabled;
        _auto.Checked = _settings.AutomaticPrioritization;
        _ux.SelectedIndex = (int)_settings.UxMode;
        _position.SelectedIndex = (int)_settings.PositionMode;
        _primary.Value = _settings.PrimaryCount;
        _selection.Value = _settings.SelectionDelayMs;
        _hover.Value = _settings.HoverDelayMs;
        _collapse.Value = _settings.CollapseDelayMs;
        _moreCollapse.Value = _settings.MoreCollapseDelayMs;
        _hide.Value = _settings.AutoHideMs;
        _near.Value = _settings.ProximityRadiusPx;
        _offset.Value = _settings.CursorOffsetPx;
        _idle.Value = _settings.IdleOpacity * 100;
        _active.Value = _settings.ActiveOpacity * 100;
    }

    public void ApplySettings()
    {
        _settings.Enabled = _enabled.Checked == true;
        _settings.AutomaticPrioritization = _auto.Checked == true;
        _settings.UxMode = (PopupUxMode)Math.Max(0, _ux.SelectedIndex);
        _settings.PositionMode = (PopupPositionMode)Math.Max(0, _position.SelectedIndex);
        _settings.PrimaryCount = (int)_primary.Value;
        _settings.SelectionDelayMs = (int)_selection.Value;
        _settings.HoverDelayMs = (int)_hover.Value;
        _settings.CollapseDelayMs = (int)_collapse.Value;
        _settings.MoreCollapseDelayMs = (int)_moreCollapse.Value;
        _settings.AutoHideMs = (int)_hide.Value;
        _settings.ProximityRadiusPx = (int)_near.Value;
        _settings.CursorOffsetPx = (int)_offset.Value;
        _settings.IdleOpacity = _idle.Value / 100.0;
        _settings.ActiveOpacity = _active.Value / 100.0;
        _settings.Save();
    }
}
