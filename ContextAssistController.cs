using Eto.Forms;
using Rhino;
using Rhino.DocObjects;
using EtoPointF = Eto.Drawing.PointF;
namespace RhinoContextAssist;
internal sealed class ContextAssistController : IDisposable
{
    private readonly UITimer _timer; private readonly ContextAssistSettings _settings; private ContextAssistPopup? _popup; private DateTime _due = DateTime.MaxValue; private EtoPointF _selectionMouse; private bool _started;
    public bool Enabled { get => _settings.Enabled; set { _settings.Enabled = value; _settings.Save(); } }
    public ContextAssistController(ContextAssistSettings settings) { _settings = settings; _timer = new UITimer { Interval = .1 }; _timer.Elapsed += (_, _) => Tick(); }
    public void Start() { if (_started) return; _started = true; RhinoDoc.SelectObjects += Changed; RhinoDoc.DeselectObjects += Changed; RhinoDoc.DeselectAllObjects += Cleared; RhinoDoc.CloseDocument += Closed; RhinoDoc.ActiveDocumentChanged += Closed; RhinoApp.EscapeKeyPressed += Escape; _timer.Start(); }
    private void Changed(object? s, RhinoObjectSelectionEventArgs e) { Close(); _selectionMouse = Mouse.Position; if (Enabled) _due = DateTime.UtcNow.AddMilliseconds(_settings.SelectionDelayMs); }
    private void Cleared(object? s, RhinoDeselectAllObjectsEventArgs e) => Hide(); private void Closed(object? s, DocumentEventArgs e) => Hide(); private void Escape(object? s, EventArgs e) => Hide();
    private void Tick() { var d = RhinoDoc.ActiveDoc; if (!Enabled || d is null || d.IsCommandRunning) { Hide(); return; } if (_due == DateTime.MaxValue || DateTime.UtcNow < _due) return; _due = DateTime.MaxValue; if (!Near()) return; RefreshNow(); }
    private bool Near() { if (_settings.ProximityRadiusPx <= 0) return true; var p = Mouse.Position; var dx = p.X - _selectionMouse.X; var dy = p.Y - _selectionMouse.Y; return dx * dx + dy * dy <= _settings.ProximityRadiusPx * _settings.ProximityRadiusPx; }
    public void RefreshNow()
    {
        var d = RhinoDoc.ActiveDoc; if (!Enabled || d is null || d.IsCommandRunning) { Hide(); return; }
        var context = SelectionAnalyzer.Analyze(d);
        var sourceKind = context.Kind == SelectionKind.MultipleEnvelopeObjects ? SelectionKind.Multiple : context.Kind;
        var actions = ContextActionPolicy.Apply(context, _settings.Resolve(sourceKind));
        if (context.Kind == SelectionKind.None || actions.Count == 0) { Hide(); return; }
        Close(); var popup = new ContextAssistPopup(Execute, Hide, _settings); popup.Closed += (_, _) => { if (ReferenceEquals(_popup, popup)) _popup = null; }; popup.UpdateActions(context, actions); _popup = popup; popup.ShowAt(context, _settings.PositionMode);
    }
    private void Execute(ContextAction action) { _settings.RecordUsage(action.Id); Hide(); RhinoApp.InvokeOnUiThread((Action)(() => RhinoApp.RunScript(action.Macro, false))); }
    public void Hide() { _due = DateTime.MaxValue; Close(); }
    private void Close() { var p = _popup; _popup = null; if (p is null) return; try { p.Close(); } catch { } }
    public void Dispose() { _timer.Stop(); if (_started) { RhinoDoc.SelectObjects -= Changed; RhinoDoc.DeselectObjects -= Changed; RhinoDoc.DeselectAllObjects -= Cleared; RhinoDoc.CloseDocument -= Closed; RhinoDoc.ActiveDocumentChanged -= Closed; RhinoApp.EscapeKeyPressed -= Escape; } Close(); }
}
