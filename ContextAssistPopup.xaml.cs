using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using Rhino;
using EtoPointF = Eto.Drawing.PointF;

namespace RhinoContextAssist;

public partial class ContextAssistPopup : Window
{
    private readonly Action<ContextAction> _execute;
    private readonly Action _dismiss;
    private readonly ContextAssistSettings _settings;
    private readonly DispatcherTimer _timer;
    private SelectionContext _context = null!;
    private List<ContextAction> _allActions = new List<ContextAction>();
    private DateTime _lastInteraction;
    private DateTime? _hoverStart;
    private DateTime? _leaveStart;
    private bool _expanded;

    internal ContextAssistPopup(
        Action<ContextAction> execute,
        Action dismiss,
        ContextAssistSettings settings)
    {
        InitializeComponent();
        _execute = execute;
        _dismiss = dismiss;
        _settings = settings;
        _timer = new DispatcherTimer(
            TimeSpan.FromMilliseconds(50),
            DispatcherPriority.Background,
            (_, _) => Tick(),
            Dispatcher);
        SourceInitialized += InitializeNativeWindow;
    }

    internal void UpdateActions(
        SelectionContext context,
        IReadOnlyList<ContextAction> actions)
    {
        _context = context;
        _allActions = actions.ToList();
        string contextIcon = GetContextIcon(context.Kind);
        Pill.Content = $"{contextIcon} {context.Caption} · {actions.Count}";
        TitleText.Text = $"{contextIcon} {context.Level}: {context.Caption}";
        Primary.Children.Clear();

        int primaryCount = Math.Min(_settings.PrimaryCount, actions.Count);
        foreach (ContextAction action in actions.Take(primaryCount))
            Primary.Children.Add(CreateActionButton(action, false));

        int remainingCount = Math.Max(0, actions.Count - primaryCount);
        Button moreButton = new Button
        {
            Content = $"⋯ Mehr ({remainingCount})",
            Style = (Style)FindResource("B")
        };
        moreButton.Click += (_, _) =>
        {
            More.IsOpen = !More.IsOpen;
            SearchBox.Text = string.Empty;
            RenderResults(_allActions.Skip(primaryCount));
        };
        Primary.Children.Add(moreButton);
        More.PlacementTarget = moreButton;
    }

    private Button CreateActionButton(ContextAction action, bool detailed)
    {
        bool favorite = _settings.IsFavorite(_context.Kind, action.Id);
        string englishCommand = GetEnglishCommand(action.Macro);

        StackPanel content = new StackPanel
        {
            Orientation = Orientation.Vertical
        };

        TextBlock commandText = new TextBlock
        {
            Text = $"{(favorite ? "★ " : string.Empty)}{action.Icon} {action.Label}",
            Foreground = action.Destructive
                ? new SolidColorBrush(Color.FromRgb(255, 180, 180))
                : Brushes.White,
            FontWeight = FontWeights.SemiBold
        };
        content.Children.Add(commandText);

        if (detailed)
        {
            TextBlock descriptionText = new TextBlock
            {
                Text = action.Hint,
                Foreground = new SolidColorBrush(Color.FromRgb(180, 180, 180)),
                FontSize = 10,
                Margin = new Thickness(22, 2, 4, 0),
                TextWrapping = TextWrapping.Wrap,
                MaxWidth = 260
            };
            content.Children.Add(descriptionText);
        }

        Button button = new Button
        {
            Content = content,
            Style = (Style)FindResource(action.Destructive ? "Danger" : "B"),
            HorizontalContentAlignment = detailed
                ? HorizontalAlignment.Left
                : HorizontalAlignment.Center,
            MinWidth = detailed ? 285 : 0,
            ToolTip = BuildLearningTooltip(action, englishCommand)
        };

        button.Click += (_, _) => _execute(action);
        button.MouseRightButtonUp += (_, mouseEvent) =>
        {
            _settings.ToggleFavorite(_context.Kind, action.Id);
            mouseEvent.Handled = true;
            UpdateActions(_context, ActionService.Get(_context, _settings));
        };

        return button;
    }

    private static string BuildLearningTooltip(
        ContextAction action,
        string englishCommand)
    {
        return $"{action.Label}\n\n" +
               $"{action.Hint}\n\n" +
               $"Englischer Befehl / Makro: {englishCommand}\n" +
               "Rechtsklick: Favorit umschalten";
    }

    private static string GetEnglishCommand(string macro)
    {
        if (string.IsNullOrWhiteSpace(macro))
            return string.Empty;

        string firstToken = macro.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries)[0];
        return firstToken.TrimStart('_', '-', '!');
    }

    private void RenderResults(IEnumerable<ContextAction> actions)
    {
        SearchResults.Children.Clear();

        foreach (IGrouping<string, ContextAction> category in actions.GroupBy(a => a.Category))
        {
            SearchResults.Children.Add(new TextBlock
            {
                Text = category.Key,
                Foreground = Brushes.Gray,
                Margin = new Thickness(6, 8, 2, 2)
            });

            foreach (ContextAction action in category)
                SearchResults.Children.Add(CreateActionButton(action, true));
        }
    }

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        string query = SearchBox.Text.Trim();
        int primaryCount = Math.Min(_settings.PrimaryCount, _allActions.Count);

        IEnumerable<ContextAction> results;
        if (string.IsNullOrEmpty(query))
        {
            results = _allActions.Skip(primaryCount);
        }
        else
        {
            results = _allActions.Where(action =>
            {
                string englishCommand = GetEnglishCommand(action.Macro);
                string searchable =
                    $"{action.Label} {action.Hint} {action.Category} {englishCommand}";

                return searchable.Contains(
                    query,
                    StringComparison.OrdinalIgnoreCase);
            });
        }

        RenderResults(results);
    }

    internal void ShowAt(SelectionContext context, PopupPositionMode mode)
    {
        EtoPointF position = CalculatePosition(context, mode);
        Left = position.X;
        Top = position.Y;
        _lastInteraction = DateTime.UtcNow;
        SetExpanded(_settings.UxMode == PopupUxMode.Direct);
        Opacity = _settings.IdleOpacity / 100d;
        Show();
        ClampToWorkingArea();
        _timer.Start();
    }

    private EtoPointF CalculatePosition(
        SelectionContext context,
        PopupPositionMode mode)
    {
        var view = RhinoDoc.ActiveDoc?.Views.ActiveView;
        EtoPointF mouse = Eto.Forms.Mouse.Position;
        if (view is null)
            return mouse;

        var rectangle = view.ScreenRectangle;
        EtoPointF projected = mouse;
        if (context.Anchor.IsValid)
        {
            var client = view.ActiveViewport.WorldToClient(context.Anchor);
            projected = new EtoPointF(
                (float)(rectangle.Left + client.X),
                (float)(rectangle.Top + client.Y));
        }

        if (mode == PopupPositionMode.Mouse)
            return mouse;
        if (mode == PopupPositionMode.Geometry)
            return projected;
        if (mode == PopupPositionMode.GeometryOffset)
            return new EtoPointF(projected.X + 120, projected.Y - 80);

        int margin = _settings.CornerMarginPx;
        Dictionary<PopupPositionMode, EtoPointF> corners = new Dictionary<PopupPositionMode, EtoPointF>
        {
            { PopupPositionMode.TopLeft, new EtoPointF(rectangle.Left + margin, rectangle.Top + margin) },
            { PopupPositionMode.TopRight, new EtoPointF(rectangle.Right - margin - 430, rectangle.Top + margin) },
            { PopupPositionMode.BottomLeft, new EtoPointF(rectangle.Left + margin, rectangle.Bottom - margin - 110) },
            { PopupPositionMode.BottomRight, new EtoPointF(rectangle.Right - margin - 430, rectangle.Bottom - margin - 110) }
        };

        if (mode != PopupPositionMode.AutoViewportCorner)
            return corners[mode];

        return corners
            .OrderByDescending(pair => DistanceSquared(pair.Value, projected))
            .First()
            .Value;
    }

    private static double DistanceSquared(EtoPointF first, EtoPointF second)
    {
        double x = first.X - second.X;
        double y = first.Y - second.Y;
        return x * x + y * y;
    }

    private void Tick()
    {
        if (!IsVisible)
            return;

        DateTime now = DateTime.UtcNow;
        bool inside = IsMouseOver || (More.IsOpen && More.IsMouseOver);

        if (inside)
        {
            _lastInteraction = now;
            _leaveStart = null;
            _hoverStart ??= now;
            FadeTo(_settings.ActiveOpacity / 100d);

            if (_settings.UxMode == PopupUxMode.Compact &&
                !_expanded &&
                (now - _hoverStart.Value).TotalMilliseconds >= _settings.HoverDelayMs)
            {
                SetExpanded(true);
            }
        }
        else
        {
            _hoverStart = null;
            FadeTo(_settings.IdleOpacity / 100d);

            if (_expanded && _settings.UxMode == PopupUxMode.Compact)
            {
                _leaveStart ??= now;
                int delay = More.IsOpen
                    ? _settings.MoreCollapseDelayMs
                    : _settings.CollapseDelayMs;

                if ((now - _leaveStart.Value).TotalMilliseconds >= delay)
                    SetExpanded(false);
            }

            if (_settings.AutoHideMs > 0 &&
                (now - _lastInteraction).TotalMilliseconds >= _settings.AutoHideMs)
            {
                _dismiss();
            }
        }
    }

    private void SetExpanded(bool expanded)
    {
        _expanded = expanded;
        Pill.Visibility = expanded ? Visibility.Collapsed : Visibility.Visible;
        Expanded.Visibility = expanded ? Visibility.Visible : Visibility.Collapsed;
        if (!expanded)
            More.IsOpen = false;

        Dispatcher.BeginInvoke(new Action(() =>
        {
            UpdateLayout();
            ClampToWorkingArea();
        }));
    }

    private void Pill_Click(object sender, RoutedEventArgs e) => SetExpanded(true);
    private void Close_Click(object sender, RoutedEventArgs e) => _dismiss();

    private void FadeTo(double opacity)
    {
        BeginAnimation(
            OpacityProperty,
            new DoubleAnimation(opacity, TimeSpan.FromMilliseconds(130)));
    }

    private void ClampToWorkingArea()
    {
        Rect workingArea = SystemParameters.WorkArea;
        Left = Math.Clamp(
            Left,
            workingArea.Left + 30,
            Math.Max(workingArea.Left + 30, workingArea.Right - ActualWidth - 30));
        Top = Math.Clamp(
            Top,
            workingArea.Top + 30,
            Math.Max(workingArea.Top + 30, workingArea.Bottom - ActualHeight - 30));
    }

    private void InitializeNativeWindow(object? sender, EventArgs e)
    {
        IntPtr handle = new WindowInteropHelper(this).Handle;
        SetWindowLongPtr(handle, -8, RhinoApp.MainWindowHandle());
    }

    private static string GetContextIcon(SelectionKind kind) => kind switch
    {
        SelectionKind.Block => "◆",
        SelectionKind.Mesh or SelectionKind.MeshFace or SelectionKind.MeshEdge => "△",
        SelectionKind.Curve or SelectionKind.ClosedPlanarCurve => "⌁",
        SelectionKind.SubD or SelectionKind.SubDFace or SelectionKind.SubDEdge => "◇",
        _ => "⬒"
    };

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW")]
    private static extern IntPtr SetWindowLongPtr(
        IntPtr windowHandle,
        int index,
        IntPtr value);
}
