using Eto.Forms;
using Rhino;
using Rhino.DocObjects;

namespace RhinoContextAssist;

internal sealed class ContextAssistController : IDisposable
{
    private readonly UITimer _timer;
    private ContextToolbarForm? _form;
    private DateTime _refreshDue = DateTime.MaxValue;
    private string _lastSignature = string.Empty;
    private bool _started;
    private bool _executingOwnAction;

    public bool Enabled { get; set; } = true;

    public ContextAssistController()
    {
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
        RhinoDoc.ActiveDocumentChanged += OnActiveDocumentChanged;
        RhinoApp.EscapeKeyPressed += OnEscape;
        _timer.Start();
    }

    private void OnSelectionChanged(object? sender, RhinoObjectSelectionEventArgs e) => ScheduleRefresh();
    private void OnSelectionCleared(object? sender, RhinoDeselectAllObjectsEventArgs e) => Hide();
    private void OnDocumentClosed(object? sender, DocumentEventArgs e) => Hide();
    private void OnActiveDocumentChanged(object? sender, DocumentEventArgs e) => Hide();
    private void OnEscape(object? sender, EventArgs e) => Hide();

    private void ScheduleRefresh()
    {
        if (Enabled) _refreshDue = DateTime.UtcNow.AddMilliseconds(160);
    }

    private void Tick()
    {
        if (!Enabled) { Hide(); return; }
        var doc = RhinoDoc.ActiveDoc;
        if (doc is null) { Hide(); return; }

        // Toolbar während laufender Rhino-Befehle, Punktabfragen und Transformationen ausblenden.
        if (!_executingOwnAction && (doc.IsCommandRunning || doc.InGetPoint))
        {
            Hide();
            return;
        }

        if (DateTime.UtcNow < _refreshDue) return;
        if (_refreshDue == DateTime.MaxValue) return;
        _refreshDue = DateTime.MaxValue;
        RefreshNow();
    }

    public void RefreshNow()
    {
        if (!Enabled) { Hide(); return; }
        var doc = RhinoDoc.ActiveDoc;
        if (doc is null || doc.IsCommandRunning || doc.InGetPoint) { Hide(); return; }

        var context = SelectionAnalyzer.Analyze(doc);
        if (context.Kind == SelectionKind.None) { Hide(); return; }
        var actions = ActionResolver.Resolve(context).Take(5).ToList();
        if (actions.Count == 0) { Hide(); return; }

        _form ??= new ContextToolbarForm(ExecuteAction, Hide);
        if (_lastSignature != context.Signature || !_form.Visible)
        {
            _form.UpdateActions(context, actions);
            _lastSignature = context.Signature;
        }
        _form.ShowSafelyNearMouse();
    }

    private void ExecuteAction(ContextAction action)
    {
        Hide();
        _executingOwnAction = true;
        RhinoApp.InvokeOnUiThread((Action)(() =>
        {
            try { RhinoApp.RunScript(action.Macro, false); }
            finally { _executingOwnAction = false; }
        }));
    }

    public void Hide()
    {
        _refreshDue = DateTime.MaxValue;
        _lastSignature = string.Empty;
        if (_form?.Visible == true) _form.Hide();
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
            RhinoDoc.ActiveDocumentChanged -= OnActiveDocumentChanged;
            RhinoApp.EscapeKeyPressed -= OnEscape;
        }
        if (_form is not null)
        {
            try { _form.Close(); } catch { }
            _form = null;
        }
    }
}
