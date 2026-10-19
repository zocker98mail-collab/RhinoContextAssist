using Eto.Forms;
using Rhino;
using Rhino.DocObjects;

namespace RhinoContextAssist;

internal sealed class ContextAssistController : IDisposable
{
    private readonly UITimer _timer;
    private ContextToolbarForm? _form;
    private bool _started;

    public bool Enabled { get; set; } = true;

    public ContextAssistController()
    {
        _timer = new UITimer { Interval = 0.16 };
        _timer.Elapsed += (_, _) => RefreshNow();
    }

    public void Start()
    {
        if (_started) return;
        _started = true;
        RhinoDoc.SelectObjects += OnSelectionChanged;
        RhinoDoc.DeselectObjects += OnSelectionChanged;
        RhinoDoc.DeselectAllObjects += OnSelectionCleared;
        RhinoDoc.CloseDocument += OnDocumentClosed;
    }

    private void OnSelectionChanged(object? sender, RhinoObjectSelectionEventArgs e) => ScheduleRefresh();
    private void OnSelectionCleared(object? sender, RhinoDeselectAllObjectsEventArgs e) => Hide();
    private void OnDocumentClosed(object? sender, DocumentEventArgs e) => Hide();

    private void ScheduleRefresh()
    {
        if (!Enabled || RhinoDoc.ActiveDoc is null) return;
        _timer.Stop();
        _timer.Start();
    }

    public void RefreshNow()
    {
        _timer.Stop();
        if (!Enabled) { Hide(); return; }

        var doc = RhinoDoc.ActiveDoc;
        if (doc is null || doc.IsCommandRunning) { Hide(); return; }

        var context = SelectionAnalyzer.Analyze(doc);
        if (context.Kind == SelectionKind.None) { Hide(); return; }

        var actions = ActionResolver.Resolve(context).Take(5).ToList();
        if (actions.Count == 0) { Hide(); return; }

        Hide();
        _form = new ContextToolbarForm(context, actions, ExecuteAction);
        _form.ShowNearMouse(doc);
    }

    private void ExecuteAction(ContextAction action)
    {
        Hide();
        RhinoApp.InvokeOnUiThread((Action)(() => RhinoApp.RunScript(action.Macro, false)));
    }

    public void Hide()
    {
        if (_form is null) return;
        try { _form.Close(); } catch { /* ignore shutdown race */ }
        _form = null;
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
        }
        Hide();
    }
}
