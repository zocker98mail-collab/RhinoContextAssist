using Eto.Forms;
using Rhino;
using Rhino.Display;
using Rhino.DocObjects;
using Rhino.Geometry;

namespace RhinoContextAssist;

internal sealed class ContextAssistController : IDisposable
{
    private readonly UITimer _timer;
    private readonly ContextAssistSettings _settings;
    private readonly ContextAssistAnchorConduit _conduit;
    private ContextAssistPopup? _popup;
    private DateTime _due = DateTime.MaxValue;
    private Guid _lastViewportId = Guid.Empty;

    public ContextAssistController(ContextAssistSettings settings)
    {
        _settings = settings;
        _conduit = new ContextAssistAnchorConduit();
        _timer = new UITimer { Interval = 0.03 };
        _timer.Elapsed += (_, _) => Tick();
    }

    public void Start()
    {
        RhinoDoc.SelectObjects += OnSelectionChanged;
        RhinoDoc.DeselectObjects += OnSelectionChanged;
        RhinoDoc.DeselectAllObjects += OnSelectionCleared;
        RhinoDoc.BeginSaveDocument += OnDocumentEventHide;
        RhinoDoc.CloseDocument += OnDocumentEventHide;
        RhinoView.SetActive += OnActiveViewChanged;
        DisplayPipeline.CalculateBoundingBox += OnCalculateBoundingBox;
        _timer.Start();
    }

    public void ShowNow()
    {
        var doc = RhinoDoc.ActiveDoc;
        if (doc == null)
            return;

        ClosePopupAndConduit();
        OpenForCurrentSelection(doc);
    }

    public void RefreshConduitSettings()
    {
        _conduit.ShowAnchorDot = _settings.ShowViewportAnchor;
        _conduit.ShowTetherLine = _settings.ShowTetherLine;
        _conduit.TetherOpacity = _settings.TetherLineOpacity;
        _conduit.TetherThickness = (float)_settings.TetherLineThickness;

        if (_popup is { IsVisible: true })
        {
            _popup.RepositionAndClamp();
            RhinoDoc.ActiveDoc?.Views.ActiveView?.Redraw();
        }
    }

    private void OnSelectionChanged(object? sender, RhinoObjectSelectionEventArgs e)
    {
        ClosePopupAndConduit();
        _due = DateTime.UtcNow.AddMilliseconds(_settings.SelectionDelayMs);
    }

    private void OnSelectionCleared(object? sender, RhinoDeselectAllObjectsEventArgs e) => Hide();

    private void OnDocumentEventHide(object? sender, DocumentEventArgs e) => Hide();

    private void OnActiveViewChanged(object? sender, ViewEventArgs e)
    {
        if (e.View == null)
            return;

        var newId = e.View.ActiveViewportID;
        if (newId != _lastViewportId && _popup is { IsVisible: true })
        {
            _lastViewportId = newId;
            _conduit.ActiveViewportId = newId;
            _popup.RepositionAndClamp();
            e.View.Redraw();
        }
    }

    private void OnCalculateBoundingBox(object? sender, CalculateBoundingBoxEventArgs e)
    {
        if (_popup is { IsVisible: true } && _settings.FollowCameraMovement)
        {
            _popup.OnViewportCameraChanged();
        }
    }

    private void Tick()
    {
        var doc = RhinoDoc.ActiveDoc;
        if (!_settings.Enabled || doc == null || Rhino.Commands.Command.InScriptRunnerCommand())
            return;

        if (_due == DateTime.MaxValue || DateTime.UtcNow < _due)
            return;

        _due = DateTime.MaxValue;
        OpenForCurrentSelection(doc);
    }

    private void OpenForCurrentSelection(RhinoDoc doc)
    {
        var context = SelectionAnalyzer.Analyze(doc);
        if (context.Kind == SelectionKind.None)
            return;

        var actions = ActionService.Get(context, _settings);
        if (actions.Count == 0)
            return;

        var activeView = doc.Views.ActiveView;
        _lastViewportId = activeView?.ActiveViewportID ?? Guid.Empty;

        if ((_settings.ShowViewportAnchor || _settings.ShowTetherLine) && context.Anchor.IsValid)
        {
            _conduit.AnchorWorld = context.Anchor;
            _conduit.ShowAnchorDot = _settings.ShowViewportAnchor;
            _conduit.ShowTetherLine = _settings.ShowTetherLine;
            _conduit.TetherOpacity = _settings.TetherLineOpacity;
            _conduit.TetherThickness = (float)_settings.TetherLineThickness;
            _conduit.ActiveViewportId = _lastViewportId;
            _conduit.Enabled = true;
            activeView?.Redraw();
        }

        _popup = new ContextAssistPopup(Execute, Hide, _settings, _conduit);
        _popup.UpdateActions(context, actions);
        _popup.ShowAt(context, _settings.PositionMode);
    }

    private void Execute(ContextAction action)
    {
        if (ContextAssistPlugIn.Instance != null)
            ContextAssistPlugIn.Instance.LastAction = action;

        _settings.RecordUsage(action.Id);
        Hide();
        RhinoApp.RunScript(action.Macro, false);
    }

    public void Hide()
    {
        _due = DateTime.MaxValue;
        ClosePopupAndConduit();
    }

    private void ClosePopupAndConduit()
    {
        if (_conduit.Enabled)
        {
            _conduit.Enabled = false;
            _conduit.AnchorWorld = Point3d.Unset;
            _conduit.ToolbarScreenAnchor = null;
            RhinoDoc.ActiveDoc?.Views.ActiveView?.Redraw();
        }

        try
        {
            _popup?.Close();
        }
        catch
        {
            // Ignoriere Fehler
        }

        _popup = null;
    }

    public void Dispose()
    {
        _timer.Stop();
        RhinoDoc.SelectObjects -= OnSelectionChanged;
        RhinoDoc.DeselectObjects -= OnSelectionChanged;
        RhinoDoc.DeselectAllObjects -= OnSelectionCleared;
        RhinoDoc.BeginSaveDocument -= OnDocumentEventHide;
        RhinoDoc.CloseDocument -= OnDocumentEventHide;
        RhinoView.SetActive -= OnActiveViewChanged;
        DisplayPipeline.CalculateBoundingBox -= OnCalculateBoundingBox;
        ClosePopupAndConduit();
    }
}
