using Rhino;
using Rhino.Commands;
using Rhino.Display;
using Rhino.DocObjects;
using Rhino.Geometry;
using Rhino.UI;
using System;
using System.Linq;

namespace RhinoContextAssist;

internal sealed class ContextAssistController : IDisposable
{
    private readonly ContextAssistSettings _settings;
    private readonly ContextAssistAnchorConduit _conduit;
    private ContextAssistPopup? _popup;
    private bool _manuallyClosedFixed;
    private bool _isUndoing;
    private bool _clearSelectionPending;
    private string? _pendingMacro;
    private DateTime _due = DateTime.MaxValue;
    private DateTime _lastIdleCheck = DateTime.MinValue;
    private DateTime _undoCooldownUntil = DateTime.MinValue;
    private Guid _lastViewportId = Guid.Empty;
    private ShortcutMouseCallback? _mouseCallback;

    public ContextAssistController(ContextAssistSettings settings)
    {
        _settings = settings;
        _conduit = new ContextAssistAnchorConduit();
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
        Rhino.Commands.Command.BeginCommand += OnBeginCommand;
        Rhino.Commands.Command.EndCommand += OnEndCommand;
        Rhino.Commands.Command.UndoRedo += OnUndoRedo;
        RhinoApp.EscapeKeyPressed += OnEscapeKeyPressed;
        RhinoApp.KeyboardEvent += OnKeyboardEvent;
        RhinoApp.Idle += OnRhinoIdle;
        
        RefreshMouseTrigger();
    }

    public void RefreshMouseTrigger()
    {
        if (_settings.ShortcutButton != MouseTriggerButton.None && _mouseCallback == null)
        {
            _mouseCallback = new ShortcutMouseCallback(this, _settings);
            _mouseCallback.Enabled = true;
        }
        else if (_settings.ShortcutButton == MouseTriggerButton.None && _mouseCallback != null)
        {
            _mouseCallback.Enabled = false;
            _mouseCallback = null;
        }
        else if (_mouseCallback != null)
        {
            _mouseCallback.Settings = _settings;
        }
    }

    public void ShowNow()
    {
        _manuallyClosedFixed = false;
        var doc = RhinoDoc.ActiveDoc;
        if (doc == null) return;
        OpenForCurrentSelection(doc);
    }

    public void RefreshConduitSettings()
    {
        _conduit.ShowAnchorDot = _settings.ShowViewportAnchor;
        _conduit.ShowTetherLine = _settings.ShowTetherLine;
        _conduit.TetherOpacity = _settings.TetherLineOpacity;
        _conduit.TetherThickness = (float)_settings.TetherLineThickness;
        RefreshMouseTrigger();

        _manuallyClosedFixed = false;
        if (_popup is { IsVisible: true })
        {
            _popup.SyncModeFromSettings();
            _popup.RepositionAndClamp();
            RhinoDoc.ActiveDoc?.Views.ActiveView?.Redraw();
        }
    }

    private void OnUndoRedo(object? sender, UndoRedoEventArgs e)
    {
        if (e.IsBeginUndo || e.IsBeginRedo)
        {
            _isUndoing = true;
            _undoCooldownUntil = DateTime.UtcNow.AddMilliseconds(350);
            _due = DateTime.MaxValue;
            SelectionAnalyzer.ClearTrackedObjects();
        }
        else if (e.IsEndUndo || e.IsEndRedo)
        {
            _isUndoing = false;
            _undoCooldownUntil = DateTime.UtcNow.AddMilliseconds(200);
            _due = DateTime.UtcNow.AddMilliseconds(Math.Max(220, _settings.SelectionDelayMs));
        }
    }

    private void OnSelectionChanged(object? sender, RhinoObjectSelectionEventArgs e)
    {
        if (e.Selected && e.RhinoObjects != null)
            SelectionAnalyzer.TrackSelectedObjects(e.RhinoObjects);

        if (_isUndoing || DateTime.UtcNow < _undoCooldownUntil)
        {
            _due = DateTime.UtcNow.AddMilliseconds(250);
            return;
        }

        _manuallyClosedFixed = false;
        bool isFixedMode = _settings.PositionMode >= PopupPositionMode.FixedTopLeft;
        if (_popup != null && (_popup.IsPinned || isFixedMode))
        {
            _due = DateTime.UtcNow.AddMilliseconds(_settings.SelectionDelayMs);
            return;
        }

        ClosePopupAndConduit();
        _due = DateTime.UtcNow.AddMilliseconds(_settings.SelectionDelayMs);
    }

    private void OnSelectionCleared(object? sender, RhinoDeselectAllObjectsEventArgs e)
    {
        SelectionAnalyzer.ClearTrackedObjects();
        if (_isUndoing || DateTime.UtcNow < _undoCooldownUntil)
        {
            _due = DateTime.UtcNow.AddMilliseconds(250);
            return;
        }

        bool isFixedMode = _settings.PositionMode >= PopupPositionMode.FixedTopLeft;
        if (_popup != null && (_popup.IsPinned || isFixedMode))
        {
            _due = DateTime.UtcNow.AddMilliseconds(_settings.SelectionDelayMs);
            return;
        }
        HideRequest(false);
    }

    private void OnDocumentEventHide(object? sender, DocumentEventArgs e)
    {
        SelectionAnalyzer.ClearTrackedObjects();
        HideRequest(false);
    }

    private void OnActiveViewChanged(object? sender, ViewEventArgs e)
    {
        if (e.View == null || _isUndoing) return;
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
        if (!_isUndoing && _popup is { IsVisible: true } && _settings.FollowCameraMovement)
            _popup.OnViewportCameraChanged();
    }

    private void OnBeginCommand(object? sender, Rhino.Commands.CommandEventArgs e)
    {
        if (e.CommandEnglishName != null &&
            (e.CommandEnglishName.Equals("Undo", StringComparison.OrdinalIgnoreCase) ||
             e.CommandEnglishName.Equals("Redo", StringComparison.OrdinalIgnoreCase)))
        {
            _isUndoing = true;
            _undoCooldownUntil = DateTime.UtcNow.AddMilliseconds(350);
            _due = DateTime.MaxValue;
            SelectionAnalyzer.ClearTrackedObjects();
            return;
        }

        if (e.CommandEnglishName != null && e.CommandEnglishName.StartsWith("ContextAssist", StringComparison.OrdinalIgnoreCase))
            return;
            
        if (_popup != null && !_popup.IsPinned && _settings.PositionMode < PopupPositionMode.FixedTopLeft)
            _popup.Dispatcher.BeginInvoke(new Action(() => HideRequest(false)));
    }

    private void OnEndCommand(object? sender, Rhino.Commands.CommandEventArgs e)
    {
        if (e.CommandEnglishName != null &&
            (e.CommandEnglishName.Equals("Undo", StringComparison.OrdinalIgnoreCase) ||
             e.CommandEnglishName.Equals("Redo", StringComparison.OrdinalIgnoreCase)))
        {
            _isUndoing = false;
            _undoCooldownUntil = DateTime.UtcNow.AddMilliseconds(200);
        }

        bool isFixedMode = _settings.PositionMode >= PopupPositionMode.FixedTopLeft;
        if (_popup != null && (_popup.IsPinned || isFixedMode))
        {
            _due = DateTime.UtcNow.AddMilliseconds(Math.Max(120, _settings.SelectionDelayMs));
        }
    }

    private void OnEscapeKeyPressed(object? sender, EventArgs e)
    {
        if (_popup != null)
        {
            _popup.Dispatcher.BeginInvoke(new Action(() =>
            {
                if (_popup != null && _popup.CloseSubMenusIfOpen())
                    return;
                HideRequest(false);
            }));
        }
    }

    private void OnKeyboardEvent(int key)
    {
        if (_popup is not { IsVisible: true } || _popup.IsSearchFocused || _isUndoing || DateTime.UtcNow < _undoCooldownUntil)
            return;

        if (Rhino.Commands.Command.InCommand() || Rhino.Commands.Command.InScriptRunnerCommand())
            return;

        string currentPrompt = RhinoApp.CommandPrompt ?? string.Empty;

        if (_settings.EnableNumberShortcuts && currentPrompt.Length <= 1)
        {
            int shortcutIdx = -1;
            if (key >= 49 && key <= 57) // Tasten 1-9 (VK_1 bis VK_9)
                shortcutIdx = key - 49;
            else if (key >= 97 && key <= 105) // Numpad 1-9 (VK_NUMPAD1 bis VK_NUMPAD9)
                shortcutIdx = key - 97;

            if (shortcutIdx >= 0)
            {
                _popup.Dispatcher.BeginInvoke(new Action(() =>
                {
                    if (_popup != null && !Rhino.Commands.Command.InCommand())
                    {
                        RhinoApp.CommandPrompt = string.Empty;
                        _popup.TryExecuteShortcutIndex(shortcutIdx);
                    }
                }));
                return;
            }
        }

        if ((key >= 65 && key <= 90) || (key >= 48 && key <= 57))
        {
            if (!_popup.IsPinned && _settings.PositionMode < PopupPositionMode.FixedTopLeft)
                _popup.Dispatcher.BeginInvoke(new Action(() => HideRequest(false)));
        }
    }

    private void OnRhinoIdle(object? sender, EventArgs e)
    {
        if (_isUndoing) return;

        var now = DateTime.UtcNow;
        if (now < _undoCooldownUntil) return;

        // 1. Ausstehende Auswahl-Aufhebung (durch ESC im Popup) sicher im Rhino-Idle ausführen
        if (_clearSelectionPending)
        {
            _clearSelectionPending = false;
            var activeDoc = RhinoDoc.ActiveDoc;
            if (activeDoc != null && !Rhino.Commands.Command.InCommand())
            {
                activeDoc.Objects.UnselectAll();
                activeDoc.Views.Redraw();
            }
            return;
        }

        // 2. Ausstehendes Makro sicher im Rhino-Idle ausführen
        if (!string.IsNullOrWhiteSpace(_pendingMacro))
        {
            var macro = _pendingMacro;
            _pendingMacro = null;
            ContextAssistPopup.ReturnFocusToRhino();
            RhinoApp.RunScript(macro, false);

            bool fixedModeAfterMacro = _settings.PositionMode >= PopupPositionMode.FixedTopLeft;
            if (_popup != null && (_popup.IsPinned || fixedModeAfterMacro))
            {
                _due = DateTime.UtcNow.AddMilliseconds(120);
            }
            return;
        }

        // Throttle Idle-Checks auf max. alle 60ms
        if ((now - _lastIdleCheck).TotalMilliseconds < 60)
            return;
        _lastIdleCheck = now;

        var doc = RhinoDoc.ActiveDoc;
        if (!_settings.Enabled || doc == null || Rhino.Commands.Command.InCommand() || Rhino.Commands.Command.InScriptRunnerCommand())
            return;

        bool isFixedMode = _settings.PositionMode >= PopupPositionMode.FixedTopLeft;
        
        if (isFixedMode && _popup == null && !_manuallyClosedFixed)
        {
            OpenForCurrentSelection(doc);
            return;
        }

        if (_popup is { IsVisible: true } && (_popup.IsPinned || isFixedMode) && _due == DateTime.MaxValue)
        {
            bool anySelected = SelectionAnalyzer.HasAnySelectionFast(doc);
            if (!anySelected && _popup.CurrentSelectionKind != SelectionKind.None)
            {
                OpenForCurrentSelection(doc);
                return;
            }
            if (anySelected && _popup.CurrentSelectionKind == SelectionKind.None)
            {
                OpenForCurrentSelection(doc);
                return;
            }
        }

        if (_due == DateTime.MaxValue || now < _due)
            return;

        _due = DateTime.MaxValue;
        OpenForCurrentSelection(doc);
    }

    private void OpenForCurrentSelection(RhinoDoc doc)
    {
        if (_isUndoing || DateTime.UtcNow < _undoCooldownUntil) return;

        var context = SelectionAnalyzer.Analyze(doc);
        bool isFixedMode = _settings.PositionMode >= PopupPositionMode.FixedTopLeft;
        
        if (context.Kind == SelectionKind.None)
        {
            if (_popup == null || (!_popup.IsPinned && !isFixedMode)) 
            {
                ClosePopupAndConduit();
                return;
            }
        }

        var actions = ActionService.Get(context, _settings);
        if (actions.Count == 0) 
        {
            if (_popup == null || (!_popup.IsPinned && !isFixedMode)) ClosePopupAndConduit();
            return;
        }

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
        else if (_conduit.Enabled)
        {
            _conduit.Enabled = false;
            _conduit.HighlightMode = null;
            activeView?.Redraw();
        }

        if (_popup == null)
        {
            _popup = new ContextAssistPopup(Execute, HideRequest, _settings, _conduit);
            _popup.UpdateActions(context, actions);
            _popup.ShowAt(context, _settings.PositionMode);
        }
        else
        {
            _popup.UpdateActions(context, actions);
            _popup.RepositionAndClamp();
        }
    }

    private void Execute(ContextAction action)
    {
        if (ContextAssistPlugIn.Instance != null)
            ContextAssistPlugIn.Instance.LastAction = action;

        _settings.RecordUsage(action.Id);
        
        bool isFixedMode = _settings.PositionMode >= PopupPositionMode.FixedTopLeft;
        if (_popup == null || (!_popup.IsPinned && !isFixedMode && action.Id != "selprev"))
            HideRequest(false);

        QueueMacroExecution(action.Macro);
    }

    internal void QueueMacroExecution(string macro)
    {
        _pendingMacro = macro;
        ContextAssistPopup.ReturnFocusToRhino();
    }

    internal void QueueClearSelection()
    {
        _clearSelectionPending = true;
    }

    public void HideRequest(bool force)
    {
        bool isFixedMode = _settings.PositionMode >= PopupPositionMode.FixedTopLeft;
        if (!force && _popup != null && (_popup.IsPinned || isFixedMode)) return;
        if (_popup?.ClosedByUserButton == true)
            _manuallyClosedFixed = true;
        _due = DateTime.MaxValue;
        ClosePopupAndConduit();
    }

    private void ClosePopupAndConduit()
    {
        if (_conduit.Enabled)
        {
            _conduit.Enabled = false;
            _conduit.HighlightMode = null;
            _conduit.AnchorWorld = Point3d.Unset;
            _conduit.ToolbarScreenAnchor = null;
            RhinoDoc.ActiveDoc?.Views.ActiveView?.Redraw();
        }

        var p = _popup;
        _popup = null;
        if (p != null)
        {
            try { p.SafeClose(); } catch { }
        }
    }

    public void Dispose()
    {
        RhinoApp.Idle -= OnRhinoIdle;
        RhinoDoc.SelectObjects -= OnSelectionChanged;
        RhinoDoc.DeselectObjects -= OnSelectionChanged;
        RhinoDoc.DeselectAllObjects -= OnSelectionCleared;
        RhinoDoc.BeginSaveDocument -= OnDocumentEventHide;
        RhinoDoc.CloseDocument -= OnDocumentEventHide;
        RhinoView.SetActive -= OnActiveViewChanged;
        DisplayPipeline.CalculateBoundingBox -= OnCalculateBoundingBox;
        Rhino.Commands.Command.BeginCommand -= OnBeginCommand;
        Rhino.Commands.Command.EndCommand -= OnEndCommand;
        Rhino.Commands.Command.UndoRedo -= OnUndoRedo;
        RhinoApp.EscapeKeyPressed -= OnEscapeKeyPressed;
        RhinoApp.KeyboardEvent -= OnKeyboardEvent;
        if (_mouseCallback != null)
        {
            _mouseCallback.Enabled = false;
            _mouseCallback = null;
        }
        ClosePopupAndConduit();
    }

    private class ShortcutMouseCallback : MouseCallback
    {
        private readonly ContextAssistController _ctrl;
        public ContextAssistSettings Settings { get; set; }
        
        public ShortcutMouseCallback(ContextAssistController ctrl, ContextAssistSettings settings)
        {
            _ctrl = ctrl;
            Settings = settings;
        }
        
        protected override void OnMouseDown(MouseCallbackEventArgs e)
        {
            bool trigger = false;
            if (Settings.ShortcutButton == MouseTriggerButton.Middle && (int)e.Button == 4194304) trigger = true;
            if (Settings.ShortcutButton == MouseTriggerButton.XButton1 && (int)e.Button == 8388608) trigger = true;
            if (Settings.ShortcutButton == MouseTriggerButton.XButton2 && (int)e.Button == 16777216) trigger = true;

            if (trigger && e.View != null)
            {
                _ctrl.ShowNow();
                if ((int)e.Button != 4194304)
                    e.Cancel = true;
            }
            base.OnMouseDown(e);
        }
    }
}
