using Eto.Drawing;
using Eto.Forms;
using Rhino;
using Rhino.UI;

namespace RhinoContextAssist;

internal sealed class ContextToolbarForm : Form
{
    private const int WindowPadding = 14;
    private readonly Action<ContextAction> _execute;
    private readonly Action _dismiss;
    private readonly ContextAssistSettings _settings;
    private readonly UITimer _timer;

    private readonly Button _pillButton = new() { Height = 28 };
    private readonly Label _caption = new();
    private readonly StackLayout _primary = new() { Orientation = Orientation.Horizontal, Spacing = 4 };
    private readonly StackLayout _moreList = new() { Spacing = 2, Visible = false };
    private readonly StackLayout _expanded = new() { Spacing = 4, Visible = false };
    private readonly List<Control> _primaryControls = new();
    private readonly List<Control> _moreControls = new();

    private DateTime _lastInteractionUtc;
    private DateTime? _hoverStartUtc;
    private DateTime? _leaveStartUtc;
    private double _targetOpacity;
    private bool _expandedState;

    public ContextToolbarForm(
        Action<ContextAction> execute,
        Action dismiss,
        ContextAssistSettings settings)
    {
        _execute = execute;
        _dismiss = dismiss;
        _settings = settings;
        _targetOpacity = settings.IdleOpacity;

        Title = "Context Assist";
        ShowInTaskbar = false;
        ShowActivated = false;
        Topmost = true;
        Resizable = false;
        WindowStyle = WindowStyle.None;
        Padding = 7;
        Opacity = settings.IdleOpacity;
        this.UseRhinoStyle();

        _pillButton.Click += (_, _) =>
        {
            Touch();
            SetExpanded(true);
        };

        _expanded.Items.Add(_caption);
        _expanded.Items.Add(_primary);
        _expanded.Items.Add(_moreList);

        Content = new StackLayout
        {
            Spacing = 4,
            Items = { _pillButton, _expanded }
        };

        _timer = new UITimer { Interval = 0.05 };
        _timer.Elapsed += (_, _) => Tick();
        Closed += (_, _) => _timer.Stop();
    }

    public void UpdateActions(
        SelectionContext context,
        IReadOnlyList<ContextAction> actions)
    {
        var icon = ContextIcon(context.Kind);
        _pillButton.Text = $"{icon}  {ShortCaption(context.Kind, context.Caption)} · {actions.Count}";
        _caption.Text = $"{icon}  {context.Caption}";

        _primary.Items.Clear();
        _moreList.Items.Clear();
        _primaryControls.Clear();
        _moreControls.Clear();
        _moreList.Visible = false;

        var primaryCount = Math.Min(_settings.PrimaryCount, actions.Count);
        foreach (var action in actions.Take(primaryCount))
            AddPrimary(MakeActionButton(action, context.Kind));

        var remaining = actions.Skip(primaryCount).ToList();
        if (remaining.Count > 0)
        {
            var more = new Button { Text = $"⋯ Mehr ({remaining.Count})", Height = 30 };
            more.Click += (_, _) =>
            {
                Touch();
                _moreList.Visible = !_moreList.Visible;
                ResizeWindow();
                KeepInsideCurrentScreen();
            };
            AddPrimary(more);

            foreach (var action in remaining)
                AddMore(MakeActionButton(action, context.Kind, fullWidth: true));
        }

        var close = new Button { Text = "×", ToolTip = "Ausblenden", Width = 30, Height = 30 };
        close.Click += (_, _) => _dismiss();
        AddPrimary(close);
    }

    private Button MakeActionButton(
        ContextAction action,
        SelectionKind kind,
        bool fullWidth = false)
    {
        var favorite = _settings.IsFavorite(kind, action.Id) ? "★ " : string.Empty;
        var button = new Button
        {
            Text = $"{action.Icon} {favorite}{action.Label}",
            ToolTip = $"{action.Hint}\nVerwendet: {_settings.UsageCount(action.Id)}×",
            Height = 30,
            Width = fullWidth ? 230 : -1
        };
        button.Click += (_, _) =>
        {
            Touch();
            _execute(action);
        };
        return button;
    }

    private void AddPrimary(Control control)
    {
        _primaryControls.Add(control);
        _primary.Items.Add(control);
    }

    private void AddMore(Control control)
    {
        _moreControls.Add(control);
        _moreList.Items.Add(control);
    }

    public void ShowAt(SelectionContext context, PopupPositionMode mode)
    {
        var position = Mouse.Position;
        if (mode == PopupPositionMode.Geometry && context.Anchor.IsValid)
        {
            var view = RhinoDoc.ActiveDoc?.Views.ActiveView;
            if (view is not null)
            {
                var client = view.ActiveViewport.WorldToClient(context.Anchor);
                var screen = view.ScreenRectangle;
                position = new PointF(
                    (float)(screen.Left + client.X),
                    (float)(screen.Top + client.Y));
            }
        }

        Location = new Point(
            (int)position.X + _settings.CursorOffsetPx,
            (int)position.Y + _settings.CursorOffsetPx);

        _lastInteractionUtc = DateTime.UtcNow;
        Show();

        if (_settings.UxMode == PopupUxMode.Direct)
            SetExpanded(true);
        else
            SetExpanded(false);

        ResizeWindow();
        KeepInsideCurrentScreen();
        _timer.Start();
    }

    private void Tick()
    {
        if (!Visible) return;

        var now = DateTime.UtcNow;
        var inside = IsMouseInside();

        if (inside)
        {
            Touch();
            _leaveStartUtc = null;
            _hoverStartUtc ??= now;
            _targetOpacity = _settings.ActiveOpacity;

            if (_settings.UxMode == PopupUxMode.Compact &&
                !_expandedState &&
                (now - _hoverStartUtc.Value).TotalMilliseconds >= _settings.HoverDelayMs)
            {
                SetExpanded(true);
            }
        }
        else
        {
            _hoverStartUtc = null;
            _targetOpacity = _settings.IdleOpacity;

            if (_expandedState && _settings.UxMode == PopupUxMode.Compact)
            {
                _leaveStartUtc ??= now;
                var delay = _moreList.Visible
                    ? _settings.MoreCollapseDelayMs
                    : _settings.CollapseDelayMs;

                if ((now - _leaveStartUtc.Value).TotalMilliseconds >= delay)
                    SetExpanded(false);
            }

            if (_settings.AutoHideMs > 0 &&
                (now - _lastInteractionUtc).TotalMilliseconds >= _settings.AutoHideMs)
            {
                _dismiss();
                return;
            }
        }

        FadeOpacity();
    }

    private void Touch() => _lastInteractionUtc = DateTime.UtcNow;

    private bool IsMouseInside()
    {
        var mouse = Mouse.Position;
        return mouse.X >= Location.X && mouse.Y >= Location.Y &&
               mouse.X <= Location.X + Width && mouse.Y <= Location.Y + Height;
    }

    private void FadeOpacity()
    {
        const double step = 0.10;
        if (Math.Abs(Opacity - _targetOpacity) < 0.01)
        {
            Opacity = _targetOpacity;
            return;
        }

        Opacity += Opacity < _targetOpacity ? step : -step;
        Opacity = Math.Clamp(
            Opacity,
            Math.Min(_settings.IdleOpacity, _settings.ActiveOpacity),
            Math.Max(_settings.IdleOpacity, _settings.ActiveOpacity));
    }

    private void SetExpanded(bool expanded)
    {
        _expandedState = expanded;
        _pillButton.Visible = !expanded;
        _expanded.Visible = expanded;
        if (!expanded) _moreList.Visible = false;
        Touch();
        ResizeWindow();
        KeepInsideCurrentScreen();
    }

    private void ResizeWindow()
    {
        if (!Visible) return;

        if (!_expandedState)
        {
            var pill = _pillButton.GetPreferredSize(SizeF.PositiveInfinity);
            ClientSize = new Size(
                Math.Max(150, (int)Math.Ceiling(pill.Width) + WindowPadding),
                44);
            return;
        }

        var caption = _caption.GetPreferredSize(SizeF.PositiveInfinity);
        var primaryWidth = MeasureHorizontal(_primaryControls);
        var moreWidth = _moreList.Visible ? MeasureVerticalWidth(_moreControls) : 0;
        var width = Math.Max((int)Math.Ceiling(caption.Width), Math.Max(primaryWidth, moreWidth));
        var height = (int)Math.Ceiling(caption.Height) + 30 + WindowPadding + 4;

        if (_moreList.Visible && _moreControls.Count > 0)
            height += _moreControls.Count * 32 + 4;

        ClientSize = new Size(
            Math.Max(220, width + WindowPadding),
            Math.Max(58, height));
    }

    private static int MeasureHorizontal(IReadOnlyList<Control> controls)
    {
        var width = 0;
        foreach (var control in controls)
        {
            var preferred = control.GetPreferredSize(SizeF.PositiveInfinity);
            width += Math.Max(control.Width, (int)Math.Ceiling(preferred.Width));
        }
        return width + Math.Max(0, controls.Count - 1) * 4;
    }

    private static int MeasureVerticalWidth(IReadOnlyList<Control> controls)
    {
        var width = 0;
        foreach (var control in controls)
        {
            var preferred = control.GetPreferredSize(SizeF.PositiveInfinity);
            width = Math.Max(width, Math.Max(control.Width, (int)Math.Ceiling(preferred.Width)));
        }
        return width;
    }

    private void KeepInsideCurrentScreen()
    {
        var center = new PointF(
            Location.X + ClientSize.Width / 2f,
            Location.Y + ClientSize.Height / 2f);
        var work = Screen.FromPoint(center).WorkingArea;
        var left = (int)work.Left;
        var top = (int)work.Top;
        var right = (int)work.Right;
        var bottom = (int)work.Bottom;
        Location = new Point(
            Math.Clamp(Location.X, left, Math.Max(left, right - Width)),
            Math.Clamp(Location.Y, top, Math.Max(top, bottom - Height)));
    }

    private static string ShortCaption(SelectionKind kind, string fallback) => kind switch
    {
        SelectionKind.ClosedPlanarCurve => "Planare Kurve",
        SelectionKind.PlanarBrepFace => "Planare Teilfläche",
        SelectionKind.SolidBrep => "Körper",
        SelectionKind.Block => "Block",
        _ => fallback
    };

    private static string ContextIcon(SelectionKind kind) => kind switch
    {
        SelectionKind.Curve or SelectionKind.ClosedPlanarCurve => "⌁",
        SelectionKind.PlanarSurface or SelectionKind.Surface => "▰",
        SelectionKind.PlanarBrepFace or SelectionKind.BrepFace => "▣",
        SelectionKind.BrepEdge => "⌒",
        SelectionKind.SolidBrep or SelectionKind.Brep => "⬒",
        SelectionKind.Mesh or SelectionKind.MeshFace or SelectionKind.MeshEdge or SelectionKind.MeshVertex => "△",
        SelectionKind.SubD or SelectionKind.SubDFace or SelectionKind.SubDEdge or SelectionKind.SubDVertex => "◇",
        SelectionKind.Block => "◆",
        SelectionKind.Multiple or SelectionKind.Mixed => "▦",
        _ => "•"
    };
}
