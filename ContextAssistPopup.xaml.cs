using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using Rhino;
using EtoPointF = Eto.Drawing.PointF;
using WpfButton = System.Windows.Controls.Button;

namespace RhinoContextAssist;

public partial class ContextAssistPopup : Window
{
    private const int GwlExStyle = -20;
    private const int WsExNoActivate = 0x08000000;
    private const int WsExToolWindow = 0x00000080;
    private const int WmMouseActivate = 0x0021;
    private const int MaNoActivate = 3;
    private const uint MonitorDefaultToNearest = 2;

    private readonly Action<ContextAction> _execute;
    private readonly Action _dismiss;
    private readonly ContextAssistSettings _settings;
    private readonly DispatcherTimer _timer;
    private readonly List<ContextAction> _remaining = new();

    private DateTime _lastInteractionUtc;
    private DateTime? _hoverStartUtc;
    private DateTime? _leaveStartUtc;
    private bool _expanded;
    private WpfButton? _moreButton;

    internal ContextAssistPopup(
        Action<ContextAction> execute,
        Action dismiss,
        ContextAssistSettings settings)
    {
        InitializeComponent();
        _execute = execute;
        _dismiss = dismiss;
        _settings = settings;

        SourceInitialized += OnSourceInitialized;
        MouseEnter += (_, _) => Touch();
        PreviewMouseMove += (_, _) => Touch();

        _timer = new DispatcherTimer(
            TimeSpan.FromMilliseconds(50),
            DispatcherPriority.Background,
            (_, _) => Tick(),
            Dispatcher);
    }

    internal void UpdateActions(
        SelectionContext context,
        IReadOnlyList<ContextAction> actions)
    {
        var icon = ContextIcon(context.Kind);
        PillButton.Content = $"{icon}  {ShortCaption(context.Kind, context.Caption)} · {actions.Count}";
        CaptionText.Text = $"{icon}  {context.Caption}";

        PrimaryPanel.Children.Clear();
        MorePanel.Children.Clear();
        _remaining.Clear();
        MorePopup.IsOpen = false;

        var primaryCount = Math.Min(_settings.PrimaryCount, actions.Count);
        foreach (var action in actions.Take(primaryCount))
            PrimaryPanel.Children.Add(CreateActionButton(action, context.Kind, false));

        _remaining.AddRange(actions.Skip(primaryCount));
        if (_remaining.Count > 0)
        {
            _moreButton = new WpfButton
            {
                Content = $"⋯ Mehr ({_remaining.Count})",
                Style = (Style)FindResource("ActionButton")
            };
            _moreButton.Click += MoreButton_Click;
            PrimaryPanel.Children.Add(_moreButton);
            MorePopup.PlacementTarget = _moreButton;

            foreach (var action in _remaining)
                MorePanel.Children.Add(CreateActionButton(action, context.Kind, true));
        }
        else
        {
            _moreButton = null;
            MorePopup.PlacementTarget = null;
        }
    }

    internal void ShowAt(SelectionContext context, PopupPositionMode mode)
    {
        var position = GetAnchorPosition(context, mode);
        Left = position.X + _settings.CursorOffsetPx;
        Top = position.Y + _settings.CursorOffsetPx;
        _lastInteractionUtc = DateTime.UtcNow;

        SetExpanded(_settings.UxMode == PopupUxMode.Direct);
        Opacity = _settings.IdleOpacity;
        Show();
        ClampToWorkArea();
        _timer.Start();
    }

    private EtoPointF GetAnchorPosition(
        SelectionContext context,
        PopupPositionMode mode)
    {
        var mouse = Eto.Forms.Mouse.Position;
        if (mode != PopupPositionMode.Geometry || !context.Anchor.IsValid)
            return mouse;

        var view = RhinoDoc.ActiveDoc?.Views.ActiveView;
        if (view is null) return mouse;

        var client = view.ActiveViewport.WorldToClient(context.Anchor);
        var screen = view.ScreenRectangle;
        return new EtoPointF(
            (float)(screen.Left + client.X),
            (float)(screen.Top + client.Y));
    }

    private WpfButton CreateActionButton(
        ContextAction action,
        SelectionKind kind,
        bool moreItem)
    {
        var favorite = _settings.IsFavorite(kind, action.Id) ? "★ " : string.Empty;
        var button = new WpfButton
        {
            Content = $"{action.Icon}  {favorite}{action.Label}",
            ToolTip = $"{action.Hint}\nVerwendet: {_settings.UsageCount(action.Id)}×",
            Style = (Style)FindResource(
                moreItem ? "MoreItemButton" : "ActionButton")
        };

        button.Click += (_, _) =>
        {
            Touch();
            MorePopup.IsOpen = false;
            _execute(action);
        };
        return button;
    }

    private void PillButton_Click(object sender, RoutedEventArgs e)
    {
        Touch();
        SetExpanded(true);
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e) => _dismiss();

    private void MoreButton_Click(object sender, RoutedEventArgs e)
    {
        Touch();
        MorePopup.IsOpen = !MorePopup.IsOpen;
    }

    private void Tick()
    {
        if (!IsVisible) return;

        var now = DateTime.UtcNow;
        var inside = IsMouseInsideWindow() ||
                     (MorePopup.IsOpen && MorePopup.IsMouseOver);

        if (inside)
        {
            Touch();
            _leaveStartUtc = null;
            _hoverStartUtc ??= now;
            AnimateOpacity(_settings.ActiveOpacity);

            if (_settings.UxMode == PopupUxMode.Compact &&
                !_expanded &&
                (now - _hoverStartUtc.Value).TotalMilliseconds >=
                _settings.HoverDelayMs)
            {
                SetExpanded(true);
            }
        }
        else
        {
            _hoverStartUtc = null;
            AnimateOpacity(_settings.IdleOpacity);

            if (_expanded && _settings.UxMode == PopupUxMode.Compact)
            {
                _leaveStartUtc ??= now;
                var delay = MorePopup.IsOpen
                    ? _settings.MoreCollapseDelayMs
                    : _settings.CollapseDelayMs;

                if ((now - _leaveStartUtc.Value).TotalMilliseconds >= delay)
                {
                    MorePopup.IsOpen = false;
                    SetExpanded(false);
                }
            }

            if (_settings.AutoHideMs > 0 &&
                (now - _lastInteractionUtc).TotalMilliseconds >=
                _settings.AutoHideMs)
            {
                _dismiss();
            }
        }
    }

    private bool IsMouseInsideWindow()
    {
        var mouse = Eto.Forms.Mouse.Position;
        return mouse.X >= Left && mouse.Y >= Top &&
               mouse.X <= Left + ActualWidth &&
               mouse.Y <= Top + ActualHeight;
    }

    private void SetExpanded(bool expanded)
    {
        _expanded = expanded;
        PillButton.Visibility = expanded
            ? Visibility.Collapsed
            : Visibility.Visible;
        ExpandedPanel.Visibility = expanded
            ? Visibility.Visible
            : Visibility.Collapsed;

        if (!expanded) MorePopup.IsOpen = false;

        Touch();
        Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(() =>
        {
            InvalidateMeasure();
            UpdateLayout();
            ClampToWorkArea();
        }));
    }

    private void AnimateOpacity(double target)
    {
        if (Math.Abs(Opacity - target) < 0.02) return;

        BeginAnimation(
            OpacityProperty,
            new DoubleAnimation
            {
                To = target,
                Duration = TimeSpan.FromMilliseconds(140),
                FillBehavior = FillBehavior.HoldEnd
            },
            HandoffBehavior.SnapshotAndReplace);
    }

    private void Touch() => _lastInteractionUtc = DateTime.UtcNow;

    private void ClampToWorkArea()
    {
        var point = new NativePoint { X = (int)Left, Y = (int)Top };
        var monitor = MonitorFromPoint(point, MonitorDefaultToNearest);
        var info = new MonitorInfo { Size = Marshal.SizeOf<MonitorInfo>() };
        if (!GetMonitorInfo(monitor, ref info)) return;

        var work = info.WorkArea;
        Left = Math.Clamp(Left, work.Left,
            Math.Max(work.Left, work.Right - ActualWidth));
        Top = Math.Clamp(Top, work.Top,
            Math.Max(work.Top, work.Bottom - ActualHeight));
    }

    private void OnSourceInitialized(object? sender, EventArgs e)
    {
        var helper = new WindowInteropHelper(this);
        var handle = helper.Handle;
        var owner = RhinoApp.MainWindowHandle();
        SetWindowLongPtr(handle, -8, owner);

        HwndSource.FromHwnd(handle)?.AddHook(WindowProc);

        var style = GetWindowLongPtr(handle, GwlExStyle).ToInt64();
        style |= WsExNoActivate | WsExToolWindow;
        SetWindowLongPtr(handle, GwlExStyle, new IntPtr(style));
    }

    private static IntPtr WindowProc(
        IntPtr hwnd, int message, IntPtr wParam, IntPtr lParam,
        ref bool handled)
    {
        if (message == WmMouseActivate)
        {
            handled = true;
            return new IntPtr(MaNoActivate);
        }
        return IntPtr.Zero;
    }

    private static string ShortCaption(SelectionKind kind, string fallback) =>
        kind switch
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
        SelectionKind.Mesh or SelectionKind.MeshFace or
            SelectionKind.MeshEdge or SelectionKind.MeshVertex => "△",
        SelectionKind.SubD or SelectionKind.SubDFace or
            SelectionKind.SubDEdge or SelectionKind.SubDVertex => "◇",
        SelectionKind.Block => "◆",
        SelectionKind.Multiple or SelectionKind.Mixed => "▦",
        _ => "•"
    };

    [StructLayout(LayoutKind.Sequential)]
    private struct NativePoint { public int X; public int Y; }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeRect
    {
        public int Left, Top, Right, Bottom;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private struct MonitorInfo
    {
        public int Size;
        public NativeRect Monitor;
        public NativeRect WorkArea;
        public uint Flags;
    }

    [DllImport("user32.dll")]
    private static extern IntPtr MonitorFromPoint(
        NativePoint point, uint flags);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetMonitorInfo(
        IntPtr monitor, ref MonitorInfo monitorInfo);

    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW")]
    private static extern IntPtr GetWindowLongPtr64(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll", EntryPoint = "GetWindowLongW")]
    private static extern IntPtr GetWindowLong32(IntPtr hWnd, int nIndex);

    private static IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex) =>
        IntPtr.Size == 8
            ? GetWindowLongPtr64(hWnd, nIndex)
            : GetWindowLong32(hWnd, nIndex);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW")]
    private static extern IntPtr SetWindowLongPtr64(
        IntPtr hWnd, int nIndex, IntPtr newLong);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongW")]
    private static extern IntPtr SetWindowLong32(
        IntPtr hWnd, int nIndex, IntPtr newLong);

    private static IntPtr SetWindowLongPtr(
        IntPtr hWnd, int nIndex, IntPtr newLong) =>
        IntPtr.Size == 8
            ? SetWindowLongPtr64(hWnd, nIndex, newLong)
            : SetWindowLong32(hWnd, nIndex, newLong);
}
