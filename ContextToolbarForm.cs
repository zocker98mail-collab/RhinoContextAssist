using Eto.Drawing;
using Eto.Forms;
using Rhino;
using Rhino.UI;

namespace RhinoContextAssist;

internal sealed class ContextToolbarForm : Form
{
    private const double IdleOpacity = 0.42;
    private const double ActiveOpacity = 1.00;
    private const double FadeStep = 0.10;
    private const int HoverDelayMs = 300;
    private const int CollapseDelayMs = 600;
    private const int AutoHideMs = 5000;

    private readonly Action<ContextAction> _execute;
    private readonly Action _dismiss;
    private readonly UITimer _interactionTimer;
    private readonly Panel _root = new();
    private readonly StackLayout _pill = new() { Orientation = Orientation.Horizontal, Spacing = 6 };
    private readonly StackLayout _primary = new() { Orientation = Orientation.Horizontal, Spacing = 4 };
    private readonly StackLayout _more = new() { Orientation = Orientation.Horizontal, Spacing = 4, Visible = false };
    private readonly StackLayout _expanded = new() { Spacing = 4, Visible = false };
    private readonly Label _pillIcon = new();
    private readonly Label _pillCaption = new();
    private readonly Label _pillCount = new();
    private readonly Label _expandedCaption = new();

    private DateTime _shownUtc;
    private DateTime? _hoverStartedUtc;
    private DateTime? _leaveStartedUtc;
    private double _targetOpacity = IdleOpacity;
    private bool _isExpanded;

    public ContextToolbarForm(Action<ContextAction> execute, Action dismiss)
    {
        _execute = execute;
        _dismiss = dismiss;

        Title = "Context Assist";
        ShowInTaskbar = false;
        ShowActivated = false;
        Topmost = true;
        Resizable = false;
        WindowStyle = WindowStyle.None;
        Padding = 7;
        Opacity = IdleOpacity;
        this.UseRhinoStyle();

        _pill.Items.Add(_pillIcon);
        _pill.Items.Add(_pillCaption);
        _pill.Items.Add(_pillCount);

        _expanded.Items.Add(_expandedCaption);
        _expanded.Items.Add(_primary);
        _expanded.Items.Add(_more);

        _root.Content = new StackLayout
        {
            Spacing = 4,
            Items = { _pill, _expanded }
        };
        Content = _root;

        _interactionTimer = new UITimer { Interval = 0.05 };
        _interactionTimer.Elapsed += (_, _) => InteractionTick();
        Closed += (_, _) => _interactionTimer.Stop();
    }

    public void UpdateActions(
        SelectionContext context,
        IReadOnlyList<ContextAction> actions,
        ContextAssistSettings settings)
    {
        _pillIcon.Text = ContextIcon(context.Kind);
        _pillCaption.Text = context.Caption;
        _pillCount.Text = actions.Count.ToString();
        _expandedCaption.Text = $"{ContextIcon(context.Kind)}  {context.Caption}";

        _primary.Items.Clear();
        _more.Items.Clear();
        _more.Visible = false;

        var primaryCount = Math.Min(settings.PrimaryCount, actions.Count);
        foreach (var action in actions.Take(primaryCount))
            _primary.Items.Add(MakeActionButton(action, context.Kind, settings));

        var remaining = actions.Skip(primaryCount).ToList();
        if (remaining.Count > 0)
        {
            var moreButton = new Button
            {
                Text = $"⋯ Mehr ({remaining.Count})",
                Height = 30
            };
            moreButton.Click += (_, _) =>
            {
                _more.Visible = !_more.Visible;
                Application.Instance.AsyncInvoke(ResizeToContent);
            };
            _primary.Items.Add(moreButton);

            foreach (var action in remaining)
                _more.Items.Add(MakeActionButton(action, context.Kind, settings));
        }

        var close = new Button { Text = "×", ToolTip = "Ausblenden", Width = 30, Height = 30 };
        close.Click += (_, _) => _dismiss();
        _primary.Items.Add(close);
    }

    private Button MakeActionButton(
        ContextAction action,
        SelectionKind kind,
        ContextAssistSettings settings)
    {
        var favorite = settings.IsFavorite(kind, action.Id) ? "★ " : string.Empty;
        var button = new Button
        {
            Text = $"{action.Icon} {favorite}{action.Label}",
            ToolTip = $"{action.Hint}\nVerwendet: {settings.UsageCount(action.Id)}×",
            Height = 30
        };
        button.Click += (_, _) => _execute(action);
        return button;
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

        Location = new Point((int)position.X + 24, (int)position.Y + 24);
        _shownUtc = DateTime.UtcNow;
        SetExpanded(false);
        Show();
        ResizeToContent();
        KeepInsideCurrentScreen();
        _interactionTimer.Start();
    }

    private void InteractionTick()
    {
        if (!Visible)
            return;

        var now = DateTime.UtcNow;
        var inside = IsMouseInsideWindow();

        if (inside)
        {
            _leaveStartedUtc = null;
            _hoverStartedUtc ??= now;
            _targetOpacity = ActiveOpacity;

            if (!_isExpanded && (now - _hoverStartedUtc.Value).TotalMilliseconds >= HoverDelayMs)
                SetExpanded(true);
        }
        else
        {
            _hoverStartedUtc = null;
            _targetOpacity = IdleOpacity;

            if (_isExpanded)
            {
                _leaveStartedUtc ??= now;
                if ((now - _leaveStartedUtc.Value).TotalMilliseconds >= CollapseDelayMs)
                    SetExpanded(false);
            }

            if ((now - _shownUtc).TotalMilliseconds >= AutoHideMs)
            {
                _dismiss();
                return;
            }
        }

        FadeOpacity();
    }

    private bool IsMouseInsideWindow()
    {
        var mouse = Mouse.Position;
        return mouse.X >= Location.X &&
               mouse.Y >= Location.Y &&
               mouse.X <= Location.X + Width &&
               mouse.Y <= Location.Y + Height;
    }

    private void FadeOpacity()
    {
        if (Math.Abs(Opacity - _targetOpacity) < 0.01)
        {
            Opacity = _targetOpacity;
            return;
        }

        Opacity += Opacity < _targetOpacity ? FadeStep : -FadeStep;
        Opacity = Math.Clamp(Opacity, IdleOpacity, ActiveOpacity);
    }

    private void SetExpanded(bool expanded)
    {
        _isExpanded = expanded;
        _pill.Visible = !expanded;
        _expanded.Visible = expanded;
        if (!expanded)
            _more.Visible = false;

        Application.Instance.AsyncInvoke(() =>
        {
            ResizeToContent();
            KeepInsideCurrentScreen();
        });
    }

    private void ResizeToContent()
    {
        if (!Visible)
            return;

        var width = _isExpanded
            ? Math.Max(_primary.Width, _more.Visible ? _more.Width : 0) + 14
            : _pill.Width + 14;

        var height = _isExpanded
            ? _expandedCaption.Height + _primary.Height + (_more.Visible ? _more.Height + 4 : 0) + 14
            : _pill.Height + 14;

        ClientSize = new Size(Math.Max(150, width), Math.Max(34, height));
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
