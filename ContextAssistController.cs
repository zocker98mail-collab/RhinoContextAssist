using Eto.Drawing;
using Eto.Forms;
using Rhino;
using Rhino.DocObjects;

namespace RhinoContextAssist;

internal sealed class ContextAssistController : IDisposable
{
    private readonly UITimer _timer;
    private readonly ContextAssistSettings _settings;
    private ContextAssistPopup? _popup;
    private DateTime _refreshDue = DateTime.MaxValue;
    private PointF _selectionMousePosition;
    private bool _started;

    public bool Enabled
    {
        get => _settings.Enabled;
        set { _settings.Enabled = value; _settings.Save(); }
    }

    public ContextAssistController(ContextAssistSettings settings)
    {
        _settings = settings;
        _timer = new UITimer { Interval = 0.10 };
        _timer.Elapsed += (_, _) => Tick();
    }

    public void Start()
    {
        if (_started) return;
        _started = true;
        RhinoDoc.SelectObjects += OnSelectionChanged;
        RhinoDoc.DeselectObjects += OnSelectionChanged;
        RhinoDoc.DeselectAllObjects += OnSelectionCleared;
        RhinoDoc.CloseDocument += OnDocumentClosed;
        RhinoDoc.ActiveDocumentChanged += OnDocumentClosed;
        RhinoApp.EscapeKeyPressed += OnEscape;
        _timer.Start();
    }

    private void OnSelectionChanged(object? sender, RhinoObjectSelectionEventArgs e)
    {
        ClosePopup();
        _selectionMousePosition = Mouse.Position;
        if (Enabled)
            _refreshDue = DateTime.UtcNow.AddMilliseconds(_settings.SelectionDelayMs);
    }

    private void OnSelectionCleared(object? sender, RhinoDeselectAllObjectsEventArgs e) => Hide();
    private void OnDocumentClosed(object? sender, DocumentEventArgs e) => Hide();
    private void OnEscape(object? sender, EventArgs e) => Hide();

    private void Tick()
    {
        var doc = RhinoDoc.ActiveDoc;
        if (!Enabled || doc is null || doc.IsCommandRunning)
        {
            Hide();
            return;
        }

        if (_refreshDue == DateTime.MaxValue || DateTime.UtcNow < _refreshDue)
            return;

        _refreshDue = DateTime.MaxValue;
        if (!MouseStayedNearSelection())
            return;

        RefreshNow();
    }

    private bool MouseStayedNearSelection()
    {
        if (_settings.ProximityRadiusPx <= 0)
            return true;

        var current = Mouse.Position;
        var dx = current.X - _selectionMousePosition.X;
        var dy = current.Y - _selectionMousePosition.Y;
        return dx * dx + dy * dy <=
               _settings.ProximityRadiusPx * _settings.ProximityRadiusPx;
    }

    public void RefreshNow()
    {
        var doc = RhinoDoc.ActiveDoc;
        if (!Enabled || doc is null || doc.IsCommandRunning)
        {
            Hide();
            return;
        }

        var context = SelectionAnalyzer.Analyze(doc);
        var actions = _settings.Resolve(context.Kind);
        if (context.Kind == SelectionKind.None || actions.Count == 0)
        {
            Hide();
            return;
        }

        ClosePopup();
        var popup = new ContextAssistPopup(ExecuteAction, Hide, _settings);
        popup.Closed += (_, _) =>
        {
            if (ReferenceEquals(_popup, popup)) _popup = null;
        };
        popup.UpdateActions(context, actions);
        _popup = popup;
        popup.ShowAt(context, _settings.PositionMode);
    }

    private void ExecuteAction(ContextAction action)
    {
        _settings.RecordUsage(action.Id);
        Hide();
        RhinoApp.InvokeOnUiThread((Action)(() =>
            RhinoApp.RunScript(action.Macro, false)));
    }

    public void Hide()
    {
        _refreshDue = DateTime.MaxValue;
        ClosePopup();
    }

    private void ClosePopup()
    {
        var popup = _popup;
        _popup = null;
        if (popup is null) return;
        try { popup.Close(); } catch { }
    }

    public void Dispose()
    {
        _timer.Stop();
        if (_started)
        {
            RhinoDoc.SelectObjects -= OnSelectionChanged;
            RhinoDoc.DeselectObjects -= OnSelectionChanged;
            RhinoDoc.DeselectAllObjects -= OnSelectionCleared;
            RhinoDoc.CloseDocument -= OnDocumentClosed;
            RhinoDoc.ActiveDocumentChanged -= OnDocumentClosed;
            RhinoApp.EscapeKeyPressed -= OnEscape;
        }
        ClosePopup();
    }
}
