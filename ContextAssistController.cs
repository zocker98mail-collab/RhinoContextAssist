using Eto.Forms;
using Rhino;
using Rhino.DocObjects;

namespace RhinoContextAssist;

internal sealed class ContextAssistController : IDisposable
{
    private readonly UITimer _timer;
    private ContextToolbarForm? _form;
    private DateTime _refreshDue = DateTime.MaxValue;
    private bool _started;

    public bool Enabled { get; set; } = true;

    public ContextAssistController()
    {
        _timer = new UITimer { Interval = 0.10 };
        _timer.Elapsed += (_, _) => Tick();
    }

    public void Start()
    {
        if (_started)
            return;

        _started = true;

        RhinoDoc.SelectObjects += OnSelectionChanged;
        RhinoDoc.DeselectObjects += OnSelectionChanged;
        RhinoDoc.DeselectAllObjects += OnSelectionCleared;
        RhinoDoc.CloseDocument += OnDocumentClosed;
        RhinoDoc.ActiveDocumentChanged += OnActiveDocumentChanged;
        RhinoApp.EscapeKeyPressed += OnEscape;

        _timer.Start();
    }

    private void OnSelectionChanged(
        object? sender,
        RhinoObjectSelectionEventArgs e)
    {
        // Die bisherige Leiste sofort entfernen. So bleibt während der
        // 160-ms-Entprellung keine veraltete Leiste sichtbar.
        CloseToolbar();

        if (Enabled)
            _refreshDue = DateTime.UtcNow.AddMilliseconds(160);
    }

    private void OnSelectionCleared(
        object? sender,
        RhinoDeselectAllObjectsEventArgs e) => Hide();

    private void OnDocumentClosed(
        object? sender,
        DocumentEventArgs e) => Hide();

    private void OnActiveDocumentChanged(
        object? sender,
        DocumentEventArgs e) => Hide();

    private void OnEscape(
        object? sender,
        EventArgs e) => Hide();

    private void Tick()
    {
        if (!Enabled)
        {
            Hide();
            return;
        }

        var doc = RhinoDoc.ActiveDoc;

        if (doc is null || doc.IsCommandRunning)
        {
            Hide();
            return;
        }

        if (_refreshDue == DateTime.MaxValue ||
            DateTime.UtcNow < _refreshDue)
        {
            return;
        }

        _refreshDue = DateTime.MaxValue;
        RefreshNow();
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

        if (context.Kind == SelectionKind.None)
        {
            Hide();
            return;
        }

        var actions = ActionResolver
            .Resolve(context)
            .Take(5)
            .ToList();

        if (actions.Count == 0)
        {
            Hide();
            return;
        }

        // Sicherheitsnetz: Vor jeder neuen Leiste wird die vorherige
        // Instanz garantiert geschlossen.
        CloseToolbar();

        var newForm = new ContextToolbarForm(ExecuteAction, Hide);

        // Nur die aktuell registrierte Instanz darf _form zurücksetzen.
        // Das verhindert Race Conditions durch verzögerte Closed-Events.
        newForm.Closed += (_, _) =>
        {
            if (ReferenceEquals(_form, newForm))
                _form = null;
        };

        newForm.UpdateActions(context, actions);
        _form = newForm;
        newForm.ShowSafelyNearMouse();
    }

    private void ExecuteAction(ContextAction action)
    {
        Hide();

        RhinoApp.InvokeOnUiThread((Action)(() =>
            RhinoApp.RunScript(action.Macro, false)));
    }

    public void Hide()
    {
        _refreshDue = DateTime.MaxValue;
        CloseToolbar();
    }

    private void CloseToolbar()
    {
        var oldForm = _form;
        _form = null;

        if (oldForm is null)
            return;

        try
        {
            oldForm.Close();
            oldForm.Dispose();
        }
        catch
        {
            // Rhino kann während des Beendens bereits UI-Ressourcen abbauen.
        }
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

        CloseToolbar();
    }
}
