using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using Rhino;
using Rhino.Geometry;
using EtoPointF = Eto.Drawing.PointF;

namespace RhinoContextAssist;

public partial class ContextAssistPopup : Window
{
    private static PopupPositionMode? _lastAutoCorner;

    private readonly Action<ContextAction> _execute;
    private readonly Action _dismiss;
    private readonly ContextAssistSettings _settings;
    private readonly ContextAssistAnchorConduit? _conduit;
    private readonly DispatcherTimer _timer;

    private SelectionContext _context = null!;
    private List<ContextAction> _allActions = new();
    private DateTime _lastInteraction;
    private DateTime? _hoverStart;
    private DateTime? _leaveStart;
    private bool _expanded;
    private PopupPositionMode _activeMode;

    internal ContextAssistPopup(
        Action<ContextAction> execute,
        Action dismiss,
        ContextAssistSettings settings,
        ContextAssistAnchorConduit? conduit = null)
    {
        InitializeComponent();
        _execute = execute;
        _dismiss = dismiss;
        _settings = settings;
        _conduit = conduit;
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
            Style = (Style)FindResource("B"),
            ToolTip = "Weitere kontextabhängige Befehle und Volltextsuche öffnen"
        };
        moreButton.Click += (_, _) =>
        {
            More.IsOpen = !More.IsOpen;
            if (More.IsOpen)
            {
                SearchBox.Text = string.Empty;
                RenderResults(_allActions.Skip(primaryCount));
                AdjustMorePopupPlacementAndClamp();
                SearchBox.Focus();
            }
            else
            {
                RepositionAndClamp();
            }
        };
        Primary.Children.Add(moreButton);
        More.PlacementTarget = moreButton;

        if (More.IsOpen)
        {
            SearchBox_TextChanged(SearchBox, null!);
            AdjustMorePopupPlacementAndClamp();
        }
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
            Dispatcher.BeginInvoke(new Action(RepositionAndClamp), DispatcherPriority.Loaded);
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

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs? e)
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
                    $"{action.Label} {action.Hint} {action.Category} {englishCommand} {action.Macro}";

                return searchable.Contains(
                    query,
                    StringComparison.OrdinalIgnoreCase);
            });
        }

        RenderResults(results);
    }

    internal void ShowAt(SelectionContext context, PopupPositionMode mode)
    {
        _context = context;
        _activeMode = mode;
        _lastInteraction = DateTime.UtcNow;

        bool initialExpanded = _settings.UxMode == PopupUxMode.Direct;
        _expanded = initialExpanded;
        Pill.Visibility = initialExpanded ? Visibility.Collapsed : Visibility.Visible;
        Expanded.Visibility = initialExpanded ? Visibility.Visible : Visibility.Collapsed;

        Opacity = _settings.IdleOpacity / 100d;

        // Erst anzeigen und Layout aktualisieren, damit ActualWidth & ActualHeight echt gemessen werden
        Show();
        MeasureAndUpdateLayout();
        RepositionAndClamp();

        _timer.Start();
    }

    private (double Width, double Height) MeasureAndUpdateLayout()
    {
        UpdateLayout();
        Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        UpdateLayout();

        double w = ActualWidth > 1 ? ActualWidth : (DesiredSize.Width > 1 ? DesiredSize.Width : 260);
        double h = ActualHeight > 1 ? ActualHeight : (DesiredSize.Height > 1 ? DesiredSize.Height : 56);
        return (w, h);
    }

    internal void RepositionAndClamp()
    {
        if (!IsVisible)
            return;

        var (popupWidth, popupHeight) = MeasureAndUpdateLayout();
        Rect bounds = GetActiveViewportBounds();

        Point target = CalculatePosition(_context, _activeMode, bounds, popupWidth, popupHeight);
        Point clamped = ClampToBounds(target, bounds, popupWidth, popupHeight);

        Left = clamped.X;
        Top = clamped.Y;

        UpdateConduitScreenAnchor(bounds, popupWidth, popupHeight);
    }

    private void AdjustMorePopupPlacementAndClamp()
    {
        var (popupWidth, popupHeight) = MeasureAndUpdateLayout();
        Rect bounds = GetActiveViewportBounds();

        double spaceBelow = bounds.Bottom - (Top + popupHeight) - 16;
        double spaceAbove = Top - bounds.Top - 16;

        // Wenn unten weniger als 260 px Platz ist und oben mehr Platz zur Verfügung steht, öffne das Mehr-Menü nach oben
        if (spaceBelow < 260 && spaceAbove > spaceBelow)
        {
            More.Placement = PlacementMode.Top;
            MoreScrollViewer.MaxHeight = Math.Clamp(spaceAbove - 65, 140, 420);
        }
        else
        {
            More.Placement = PlacementMode.Bottom;
            double available = Math.Max(spaceBelow, SystemParameters.WorkArea.Bottom - (Top + popupHeight) - 24);
            MoreScrollViewer.MaxHeight = Math.Clamp(available - 65, 140, 420);
        }

        Point clamped = ClampToBounds(new Point(Left, Top), bounds, popupWidth, popupHeight);
        Left = clamped.X;
        Top = clamped.Y;

        UpdateConduitScreenAnchor(bounds, popupWidth, popupHeight);
    }

    private Rect GetActiveViewportBounds()
    {
        var view = RhinoDoc.ActiveDoc?.Views.ActiveView;
        if (view != null)
        {
            var r = view.ScreenRectangle;
            if (r.Width > 80 && r.Height > 80)
                return new Rect(r.Left, r.Top, r.Width, r.Height);
        }

        return SystemParameters.WorkArea;
    }

    private Point CalculatePosition(
        SelectionContext context,
        PopupPositionMode mode,
        Rect viewportRect,
        double popupWidth,
        double popupHeight)
    {
        var view = RhinoDoc.ActiveDoc?.Views.ActiveView;
        EtoPointF mouseEto = Eto.Forms.Mouse.Position;
        Point mouse = new Point(mouseEto.X, mouseEto.Y);

        if (view is null)
            return new Point(mouse.X + 16, mouse.Y + 16);

        Point projected = mouse;
        if (context.Anchor.IsValid)
        {
            var client = view.ActiveViewport.WorldToClient(context.Anchor);
            projected = new Point(
                viewportRect.Left + client.X,
                viewportRect.Top + client.Y);
        }

        if (mode == PopupPositionMode.Mouse)
            return new Point(mouse.X + 18, mouse.Y - popupHeight / 2);

        if (mode == PopupPositionMode.Geometry)
            return new Point(projected.X - popupWidth / 2, projected.Y - popupHeight - 18);

        if (mode == PopupPositionMode.GeometryOffset)
            return new Point(projected.X + 48, projected.Y - popupHeight - 36);

        double margin = Math.Max(12, _settings.CornerMarginPx);
        double safeRightX = Math.Max(viewportRect.Left + 12, viewportRect.Right - margin - popupWidth);
        double safeBottomY = Math.Max(viewportRect.Top + 12, viewportRect.Bottom - margin - popupHeight);
        double safeLeftX = Math.Min(safeRightX, viewportRect.Left + margin);
        double safeTopY = Math.Min(safeBottomY, viewportRect.Top + margin);

        var corners = new Dictionary<PopupPositionMode, Point>
        {
            { PopupPositionMode.TopLeft, new Point(safeLeftX, safeTopY) },
            { PopupPositionMode.TopRight, new Point(safeRightX, safeTopY) },
            { PopupPositionMode.BottomLeft, new Point(safeLeftX, safeBottomY) },
            { PopupPositionMode.BottomRight, new Point(safeRightX, safeBottomY) }
        };

        if (mode != PopupPositionMode.AutoViewportCorner)
            return corners.TryGetValue(mode, out var fixedCorner) ? fixedCorner : corners[PopupPositionMode.TopRight];

        // Berechne Abstand zwischen dem Zentrum der Toolbar in jeder Ecke und der projizierten Geometrie
        double ScoreCorner(PopupPositionMode cornerMode)
        {
            Point topLeft = corners[cornerMode];
            Point center = new Point(topLeft.X + popupWidth / 2, topLeft.Y + popupHeight / 2);
            return Distance(center, projected);
        }

        var bestCorner = corners.Keys
            .OrderByDescending(ScoreCorner)
            .First();

        // Hysterese: Bleibe in der bisherigen Ecke, solange die neue Ecke nicht mindestens 65 Pixel weiter von der Geometrie entfernt ist
        if (_lastAutoCorner.HasValue && corners.ContainsKey(_lastAutoCorner.Value))
        {
            double currentScore = ScoreCorner(_lastAutoCorner.Value);
            double bestScore = ScoreCorner(bestCorner);
            if (bestScore - currentScore < 65.0)
                bestCorner = _lastAutoCorner.Value;
        }

        _lastAutoCorner = bestCorner;
        return corners[bestCorner];
    }

    private Point ClampToBounds(Point desired, Rect viewportRect, double popupWidth, double popupHeight)
    {
        Rect workArea = SystemParameters.WorkArea;
        Rect safeRect = Rect.Intersect(viewportRect, workArea);
        if (safeRect.IsEmpty || safeRect.Width < popupWidth + 24 || safeRect.Height < popupHeight + 24)
            safeRect = workArea;

        double pad = Math.Min(16, Math.Max(8, _settings.CornerMarginPx / 4.0));

        // Wenn das Mehr-Menü nach unten geöffnet ist, lasse nach Möglichkeit Platz für das Dropdown
        double extraBottom = (More.IsOpen && More.Placement == PlacementMode.Bottom)
            ? Math.Min(240, Math.Max(0, safeRect.Height - popupHeight - pad * 2))
            : 0;

        double minX = safeRect.Left + pad;
        double maxX = Math.Max(minX, safeRect.Right - popupWidth - pad);

        double minY = safeRect.Top + pad;
        double maxY = Math.Max(minY, safeRect.Bottom - popupHeight - pad - extraBottom);

        return new Point(
            Math.Clamp(desired.X, minX, maxX),
            Math.Clamp(desired.Y, minY, maxY));
    }

    private void UpdateConduitScreenAnchor(Rect viewportRect, double popupWidth, double popupHeight)
    {
        if (_conduit == null)
            return;

        double centerX = Left + popupWidth / 2.0 - viewportRect.Left;
        double centerY = Top + popupHeight / 2.0 - viewportRect.Top;
        _conduit.ToolbarScreenAnchor = new Point2d(centerX, centerY);
        RhinoDoc.ActiveDoc?.Views.ActiveView?.Redraw();
    }

    private static double Distance(Point first, Point second)
    {
        double x = first.X - second.X;
        double y = first.Y - second.Y;
        return Math.Sqrt(x * x + y * y);
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

        Dispatcher.BeginInvoke(new Action(RepositionAndClamp), DispatcherPriority.Loaded);
    }

    private void Pill_Click(object sender, RoutedEventArgs e) => SetExpanded(true);
    private void Close_Click(object sender, RoutedEventArgs e) => _dismiss();

    private void FadeTo(double opacity)
    {
        BeginAnimation(
            OpacityProperty,
            new DoubleAnimation(opacity, TimeSpan.FromMilliseconds(130)));
    }

    private void InitializeNativeWindow(object? sender, EventArgs e)
    {
        IntPtr handle = new WindowInteropHelper(this).Handle;
        SetWindowLongPtr(handle, -8, RhinoApp.MainWindowHandle());
    }

    private static string GetContextIcon(SelectionKind kind) => kind switch
    {
        SelectionKind.Block => "◆",
        SelectionKind.Mesh or SelectionKind.MeshFace or SelectionKind.MeshEdge or SelectionKind.MeshVertex => "△",
        SelectionKind.Curve or SelectionKind.ClosedPlanarCurve => "⌁",
        SelectionKind.SubD or SelectionKind.SubDFace or SelectionKind.SubDEdge or SelectionKind.SubDVertex => "◇",
        _ => "⬒"
    };

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW")]
    private static extern IntPtr SetWindowLongPtr(
        IntPtr windowHandle,
        int index,
        IntPtr value);
}
