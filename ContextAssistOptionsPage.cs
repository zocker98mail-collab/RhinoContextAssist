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
    private readonly CheckBox _autoPriority = new() { Text = "Automatisch nach Nutzung priorisieren" };
    private readonly DropDown _uxMode = new();
    private readonly DropDown _position = new();
    private readonly NumericStepper _primaryCount = Number(1, 5);
    private readonly NumericStepper _selectionDelay = Number(0, 2000);
    private readonly NumericStepper _hoverDelay = Number(0, 2000);
    private readonly NumericStepper _collapseDelay = Number(0, 5000);
    private readonly NumericStepper _moreCollapseDelay = Number(0, 10000);
    private readonly NumericStepper _autoHide = Number(0, 30000);
    private readonly NumericStepper _proximity = Number(0, 1000);
    private readonly NumericStepper _offset = Number(0, 200);
    private readonly NumericStepper _idleOpacity = Number(15, 100);
    private readonly NumericStepper _activeOpacity = Number(25, 100);

    public ContextAssistOptionsControl(ContextAssistSettings settings)
    {
        _settings = settings;
        _uxMode.Items.Add("Direkt vollständig");
        _uxMode.Items.Add("Kompakte Pille, bei Hover erweitern");
        _uxMode.Items.Add("Manuell: Pille per Klick öffnen");
        _position.Items.Add("Neben Mauszeiger");
        _position.Items.Add("Neben Geometrie");

        var layout = new DynamicLayout
        {
            Padding = 10,
            Spacing = new Eto.Drawing.Size(6, 6)
        };

        layout.AddRow(_enabled);
        layout.AddRow(_autoPriority);
        layout.AddRow(new Label { Text = "Darstellung" }, _uxMode);
        layout.AddRow(new Label { Text = "Position" }, _position);
        layout.AddRow(new Label { Text = "Hauptaktionen" }, _primaryCount);
        layout.AddRow(new Label { Text = "Verzögerung nach Auswahl (ms)" }, _selectionDelay);
        layout.AddRow(new Label { Text = "Hover-Verzögerung (ms)" }, _hoverDelay);
        layout.AddRow(new Label { Text = "Einklappen (ms)" }, _collapseDelay);
        layout.AddRow(new Label { Text = "Einklappen bei offenem Mehr (ms)" }, _moreCollapseDelay);
        layout.AddRow(new Label { Text = "Automatisch ausblenden (ms, 0 = aus)" }, _autoHide);
        layout.AddRow(new Label { Text = "Nähe zur Auswahl (px, 0 = aus)" }, _proximity);
        layout.AddRow(new Label { Text = "Abstand zum Cursor (px)" }, _offset);
        layout.AddRow(new Label { Text = "Ruhe-Deckkraft (%)" }, _idleOpacity);
        layout.AddRow(new Label { Text = "Hover-Deckkraft (%)" }, _activeOpacity);

        Content = layout;
        LoadSettings();
    }

    private static NumericStepper Number(double min, double max) => new()
    {
        MinValue = min,
        MaxValue = max,
        DecimalPlaces = 0
    };

    private void LoadSettings()
    {
        _enabled.Checked = _settings.Enabled;
        _autoPriority.Checked = _settings.AutomaticPrioritization;
        _uxMode.SelectedIndex = (int)_settings.UxMode;
        _position.SelectedIndex = (int)_settings.PositionMode;
        _primaryCount.Value = _settings.PrimaryCount;
        _selectionDelay.Value = _settings.SelectionDelayMs;
        _hoverDelay.Value = _settings.HoverDelayMs;
        _collapseDelay.Value = _settings.CollapseDelayMs;
        _moreCollapseDelay.Value = _settings.MoreCollapseDelayMs;
        _autoHide.Value = _settings.AutoHideMs;
        _proximity.Value = _settings.ProximityRadiusPx;
        _offset.Value = _settings.CursorOffsetPx;
        _idleOpacity.Value = _settings.IdleOpacity * 100;
        _activeOpacity.Value = _settings.ActiveOpacity * 100;
    }

    public void ApplySettings()
    {
        _settings.Enabled = _enabled.Checked == true;
        _settings.AutomaticPrioritization = _autoPriority.Checked == true;
        _settings.UxMode = (PopupUxMode)Math.Max(0, _uxMode.SelectedIndex);
        _settings.PositionMode = (PopupPositionMode)Math.Max(0, _position.SelectedIndex);
        _settings.PrimaryCount = (int)_primaryCount.Value;
        _settings.SelectionDelayMs = (int)_selectionDelay.Value;
        _settings.HoverDelayMs = (int)_hoverDelay.Value;
        _settings.CollapseDelayMs = (int)_collapseDelay.Value;
        _settings.MoreCollapseDelayMs = (int)_moreCollapseDelay.Value;
        _settings.AutoHideMs = (int)_autoHide.Value;
        _settings.ProximityRadiusPx = (int)_proximity.Value;
        _settings.CursorOffsetPx = (int)_offset.Value;
        _settings.IdleOpacity = _idleOpacity.Value / 100.0;
        _settings.ActiveOpacity = _activeOpacity.Value / 100.0;
        _settings.Save();
    }
}
