using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using Rhino;
using EtoPointF = Eto.Drawing.PointF;
using Point2d = Rhino.Geometry.Point2d;
using WpfPoint = System.Windows.Point;
using WpfSize = System.Windows.Size;
using WpfColor = System.Windows.Media.Color;
using WpfBrushes = System.Windows.Media.Brushes;

namespace RhinoContextAssist;

public partial class ContextAssistPopup : Window
{
    private static PopupPositionMode? _lastAutoCorner;

    private readonly Action<ContextAction> _execute;
    private readonly Action<bool> _requestClose;
    private readonly ContextAssistSettings _settings;
    private readonly ContextAssistAnchorConduit? _conduit;
    private readonly DispatcherTimer _timer;

    private SelectionContext _context = null!;
    private List<ContextAction> _allActions = new();
    private List<ContextAction> _primaryActions = new();
    private DateTime _lastInteraction;
    private DateTime? _hoverStart;
    private DateTime? _leaveStart;
    private bool _expanded;
    private PopupPositionMode _activeMode;
    private bool _userManuallyDragged;
    private bool _hiddenDueToOffscreen;
        private Brush _themeTextBrush = WpfBrushes.White;
    private Brush _themeHintBrush = new SolidColorBrush(WpfColor.FromRgb(180, 180, 180));
    private Brush _themeDangerBrush = new SolidColorBrush(WpfColor.FromRgb(255, 180, 180));
    private Brush _themeCategoryBrush = WpfBrushes.Gray;
    private WpfPoint _lastProjectedAnchor = new(double.NaN, double.NaN);

    internal ContextAssistPopup(
        Action<ContextAction> execute,
        Action<bool> requestClose,
        ContextAssistSettings settings,
        ContextAssistAnchorConduit? conduit = null)
    {
        InitializeComponent();
        ApplyTheme();
        _execute = execute;
        _requestClose = requestClose;
        _settings = settings;
        _conduit = conduit;
        _timer = new DispatcherTimer(TimeSpan.FromMilliseconds(50), DispatcherPriority.Background, (_, _) => Tick(), Dispatcher);
        SourceInitialized += InitializeNativeWindow;
    }

    internal void UpdateActions(SelectionContext context, IReadOnlyList<ContextAction> actions)
    {
        _context = context;
        _allActions = actions.ToList();
        string contextIcon = GetContextIcon(context.Kind);
        Pill.Content = $"{contextIcon} {context.Caption} · {actions.Count}";
        TitleText.Text = $"{contextIcon} {context.Level}: {context.Caption}";
        Primary.Children.Clear();
        _primaryActions.Clear();

        int primaryCount = Math.Min(_settings.PrimaryCount, actions.Count);
        int index = 1;
        foreach (ContextAction action in actions.Take(primaryCount))
        {
            _primaryActions.Add(action);
            Primary.Children.Add(CreateActionButton(action, false, _settings.EnableNumberShortcuts ? index++ : 0));
        }

        int remainingCount = Math.Max(0, actions.Count - primaryCount);
        Button moreButton = new Button
        {
            Content = $"⋯ Mehr ({remainingCount})",
            Style = (Style)FindResource("B"),
            ToolTip = "Weitere kontextabhängige Befehle und Volltextsuche öffnen (Strg+F)"
        };
        moreButton.Click += (_, _) => ToggleMoreMenu();
        Primary.Children.Add(moreButton);
        More.PlacementTarget = moreButton;

        if (More.IsOpen)
        {
            SearchBox_TextChanged(SearchBox, null!);
            AdjustMorePopupPlacementAndClamp();
        }

        InitializeLayerButton();
    }

    private void ToggleMoreMenu()
    {
        More.IsOpen = !More.IsOpen;
        if (More.IsOpen)
        {
            SearchBox.Text = string.Empty;
            RenderResults(_allActions.Skip(_primaryActions.Count));
            AdjustMorePopupPlacementAndClamp();
        }
        else
        {
            RepositionAndClamp();
        }
    }

    private void More_Opened(object sender, EventArgs e)
    {
        // Setzt den Fokus bei Öffnen direkt ins Suchfeld
        SearchBox.Focus();
    }

    private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            if (More.IsOpen)
            {
                More.IsOpen = false;
                RepositionAndClamp();
                e.Handled = true;
            }
            else if (LayerPopup != null && LayerPopup.IsOpen)
            {
                LayerPopup.IsOpen = false;
                RepositionAndClamp();
                e.Handled = true;
            }
            else
            {
                bool isFixedMode = _settings.PositionMode >= PopupPositionMode.FixedTopLeft;
                if (!isFixedMode && !IsPinned)
                {
                    _requestClose(false);
                }
                else
                {
                    ReturnFocusToRhino();
                    if (ContextAssistPlugIn.Instance?.Controller != null)
                        ContextAssistPlugIn.Instance.Controller.QueueClearSelection();
                }
                e.Handled = true;
            }
        }
        else if (e.Key == Key.F && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
        {
            if (!More.IsOpen) ToggleMoreMenu();
            e.Handled = true;
        }
        else if (_settings.EnableNumberShortcuts && !SearchBox.IsFocused && (LayerPopup == null || !LayerSearchBox.IsFocused))
        {
            if (Rhino.Commands.Command.InCommand())
            {
                ReturnFocusToRhino();
                return;
            }

            // Zahlen 1-9 direkt in Aktionen mappen (nur wenn kein Rhino-Befehl aktiv ist!)
            if (e.Key >= Key.D1 && e.Key <= Key.D9)
            {
                if (TryExecuteShortcutIndex(e.Key - Key.D1))
                    e.Handled = true;
            }
            else if (e.Key >= Key.NumPad1 && e.Key <= Key.NumPad9)
            {
                if (TryExecuteShortcutIndex(e.Key - Key.NumPad1))
                    e.Handled = true;
            }
        }
        
        if (e.Key == Key.Down || e.Key == Key.Up)
        {
            if ((More.IsOpen && !SearchBox.IsFocused) || (LayerPopup != null && LayerPopup.IsOpen && !LayerSearchBox.IsFocused))
            {
                var focused = Keyboard.FocusedElement as UIElement;
                focused?.MoveFocus(new System.Windows.Input.TraversalRequest(e.Key == Key.Down ? System.Windows.Input.FocusNavigationDirection.Next : System.Windows.Input.FocusNavigationDirection.Previous));
                e.Handled = true;
            }
        }
    }

    private void SearchBox_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            if (SearchResults.Items.Count > 0)
            {
                var firstBtn = SearchResults.Items.OfType<System.Windows.Controls.Button>().FirstOrDefault() ?? (SearchResults.Items[0] as System.Windows.Controls.Button);
                if (firstBtn != null && firstBtn.Tag is ContextAction action)
                {
                    _execute(action);
                    e.Handled = true;
                    return;
                }
            }
        }
        else if (e.Key == Key.Escape)
        {
            More.IsOpen = false;
            RepositionAndClamp();
            e.Handled = true;
        }
        else if (e.Key == Key.Down)
        {
            if (SearchResults.Items.Count > 0)
            {
                var firstBtn = SearchResults.Items.OfType<System.Windows.Controls.Button>().FirstOrDefault() ?? (SearchResults.Items[0] as System.Windows.Controls.Button);
                if (firstBtn != null)
                {
                    firstBtn.Focus();
                    e.Handled = true;
                }
            }
        }
    }

    private Button CreateActionButton(ContextAction action, bool detailed, int shortcutNumber)
    {
        bool favorite = _settings.IsFavorite(_context.Kind, action.Id);
        string englishCommand = GetEnglishCommand(action.Macro);

        StackPanel content = new StackPanel { Orientation = Orientation.Vertical };

        string shortcutPrefix = shortcutNumber > 0 ? $"[{shortcutNumber}] " : string.Empty;
        TextBlock commandText = new TextBlock
        {
            Text = $"{shortcutPrefix}{(favorite ? "★ " : string.Empty)}{action.Icon} {action.Label}",
            Foreground = action.Destructive ? _themeDangerBrush : _themeTextBrush,
            FontWeight = FontWeights.SemiBold
        };
        content.Children.Add(commandText);

        if (detailed)
        {
            TextBlock descriptionText = new TextBlock
            {
                Text = action.Hint,
                Foreground = _themeHintBrush,
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
            Tag = action, // Tag für das Enter-Key-Ausführen im Suchfeld
            Style = (Style)FindResource("B"),
            HorizontalContentAlignment = detailed ? HorizontalAlignment.Left : HorizontalAlignment.Center,
            MinWidth = detailed ? 285 : 0,
            ToolTip = BuildLearningTooltip(action, englishCommand)
        };

        button.Click += (_, _) =>
        {
            ReturnFocusToRhino();
            _execute(action);
        };
        button.MouseRightButtonUp += (_, mouseEvent) =>
        {
            _settings.ToggleFavorite(_context.Kind, action.Id);
            mouseEvent.Handled = true;
            UpdateActions(_context, ActionService.Get(_context, _settings));
            Dispatcher.BeginInvoke(new Action(RepositionAndClamp), DispatcherPriority.Loaded);
        };
        
        // Hover-Highlight
        if (_settings.HighlightHoveredGeometry && _conduit != null)
        {
            button.MouseEnter += (_, _) => { _conduit.HighlightMode = action.Macro; RhinoDoc.ActiveDoc?.Views.ActiveView?.Redraw(); };
            button.MouseLeave += (_, _) => { _conduit.HighlightMode = null; RhinoDoc.ActiveDoc?.Views.ActiveView?.Redraw(); };
        }

        return button;
    }

    private static string BuildLearningTooltip(ContextAction action, string englishCommand)
    {
        return $"{action.Label}\n\n{action.Hint}\n\nEnglischer Befehl / Makro: {englishCommand}\nRechtsklick: Favorit umschalten";
    }

    private static string GetEnglishCommand(string macro)
    {
        if (string.IsNullOrWhiteSpace(macro)) return string.Empty;
        return macro.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries)[0].TrimStart('_', '-', '!');
    }

    private void RenderResults(IEnumerable<ContextAction> actions)
    {
        SearchResults.Items.Clear();
        foreach (IGrouping<string, ContextAction> category in actions.GroupBy(a => a.Category))
        {
            SearchResults.Items.Add(new TextBlock { Text = category.Key, Foreground = _themeCategoryBrush, Margin = new Thickness(6, 8, 2, 2) });
            foreach (ContextAction action in category) SearchResults.Items.Add(CreateActionButton(action, true, 0));
        }
    }

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs? e)
    {
        string query = SearchBox.Text.Trim();
        IEnumerable<ContextAction> results;
        if (string.IsNullOrEmpty(query))
        {
            results = _allActions.Skip(_primaryActions.Count);
        }
        else
        {
            var parts = query.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            results = _allActions.Where(action =>
            {
                string searchable = $"{action.Label} {action.Hint} {action.Category} {GetEnglishCommand(action.Macro)} {action.Macro}";
                return parts.All(p => searchable.Contains(p, StringComparison.OrdinalIgnoreCase));
            });
        }
        RenderResults(results);
    }

    internal void ShowAt(SelectionContext context, PopupPositionMode mode)
    {
        _context = context;
        _activeMode = mode;
        _userManuallyDragged = false;
        _hiddenDueToOffscreen = false;
        _lastInteraction = DateTime.UtcNow;

        bool isFixedMode = _settings.PositionMode >= PopupPositionMode.FixedTopLeft;
        if (isFixedMode)
        {
            PinButton.IsChecked = true;
            PinButton.Visibility = Visibility.Collapsed;
        }
        else
        {
            PinButton.Visibility = Visibility.Visible;
        }
        bool initialExpanded = _settings.UxMode == PopupUxMode.Direct || isFixedMode;
        _expanded = initialExpanded;
        Pill.Visibility = initialExpanded ? Visibility.Collapsed : Visibility.Visible;
        Expanded.Visibility = initialExpanded ? Visibility.Visible : Visibility.Collapsed;

        Opacity = 0;
        Show();
        MeasureAndUpdateLayout();
        RepositionAndClamp();
        
        // Sanftes Einblenden (Fade + Scale)
        DoubleAnimation fade = new DoubleAnimation(_settings.IdleOpacity / 100d, TimeSpan.FromMilliseconds(180)) { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } };
        DoubleAnimation scale = new DoubleAnimation(1.0, TimeSpan.FromMilliseconds(220)) { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } };
        BeginAnimation(OpacityProperty, fade);
        RootScale.BeginAnimation(ScaleTransform.ScaleXProperty, scale);
        RootScale.BeginAnimation(ScaleTransform.ScaleYProperty, scale);
        RootBorder.LayoutTransform = new ScaleTransform(_settings.UiScale, _settings.UiScale);

        _timer.Start();
    }

    private (double Width, double Height) MeasureAndUpdateLayout()
    {
        if (ActualWidth <= 1 || ActualHeight <= 1)
        {
            try { UpdateLayout(); } catch { }
        }
        double w = ActualWidth > 1 ? ActualWidth : (DesiredSize.Width > 1 ? DesiredSize.Width : 260);
        double h = ActualHeight > 1 ? ActualHeight : (DesiredSize.Height > 1 ? DesiredSize.Height : 56);
        return (w, h);
    }

    internal void RepositionAndClamp()
    {
        if (!IsVisible) return;
        var (popupWidth, popupHeight) = MeasureAndUpdateLayout();
        Rect bounds = GetActiveViewportBounds();
        WpfPoint target = _userManuallyDragged ? new WpfPoint(Left, Top) : CalculatePosition(_context, _activeMode, bounds, popupWidth, popupHeight);
        if (_settings.EnableEdgeSnapping) target = ApplyMagneticEdgeSnapping(target, bounds, popupWidth, popupHeight);
        WpfPoint clamped = ClampToBounds(target, bounds, popupWidth, popupHeight);
        Left = clamped.X; Top = clamped.Y;
        UpdateConduitScreenAnchor(bounds, popupWidth, popupHeight);
    }

    private void AdjustMorePopupPlacementAndClamp()
    {
        var (popupWidth, popupHeight) = MeasureAndUpdateLayout();
        Rect bounds = GetActiveViewportBounds();
        double spaceBelow = bounds.Bottom - (Top + popupHeight) - 16;
        double spaceAbove = Top - bounds.Top - 16;

        if (spaceBelow < 260 && spaceAbove > spaceBelow)
        {
            More.Placement = PlacementMode.Top;
            SearchResults.MaxHeight = Math.Clamp(spaceAbove - 65, 140, 420);
        }
        else
        {
            More.Placement = PlacementMode.Bottom;
            SearchResults.MaxHeight = Math.Clamp(Math.Max(spaceBelow, SystemParameters.WorkArea.Bottom - (Top + popupHeight) - 24) - 65, 140, 420);
        }

        WpfPoint clamped = ClampToBounds(new WpfPoint(Left, Top), bounds, popupWidth, popupHeight);
        Left = clamped.X; Top = clamped.Y;
        UpdateConduitScreenAnchor(bounds, popupWidth, popupHeight);
    }

    private Rect GetActiveViewportBounds()
    {
        var view = RhinoDoc.ActiveDoc?.Views.ActiveView;
        if (view != null)
        {
            var r = view.ScreenRectangle;
            if (r.Width > 80 && r.Height > 80) return new Rect(r.Left, r.Top, r.Width, r.Height);
        }
        return SystemParameters.WorkArea;
    }

    private WpfPoint CalculatePosition(SelectionContext context, PopupPositionMode mode, Rect viewportRect, double popupWidth, double popupHeight)
    {
        var view = RhinoDoc.ActiveDoc?.Views.ActiveView;
        EtoPointF mouseEto = Eto.Forms.Mouse.Position;
        WpfPoint mouse = new WpfPoint(mouseEto.X, mouseEto.Y);
        if (view is null) return new WpfPoint(mouse.X + 16, mouse.Y + 16);

        WpfPoint projected = mouse;
        if (context.Anchor.IsValid)
        {
            var client = view.ActiveViewport.WorldToClient(context.Anchor);
            projected = new WpfPoint(viewportRect.Left + client.X, viewportRect.Top + client.Y);
            _lastProjectedAnchor = projected;
        }

        if (mode == PopupPositionMode.Mouse) return new WpfPoint(mouse.X + 18, mouse.Y - popupHeight / 2);
        if (mode == PopupPositionMode.Geometry) return new WpfPoint(projected.X - popupWidth / 2, projected.Y - popupHeight - 18);

        if (mode == PopupPositionMode.GeometryOffset)
        {
            double baseOffset = Math.Max(16, _settings.DynamicOffsetPx);
            double centerX = viewportRect.Left + viewportRect.Width / 2.0;
            double centerY = viewportRect.Top + viewportRect.Height / 2.0;
            double dirX = projected.X <= centerX ? 1.0 : -1.0;
            double dirY = projected.Y >= centerY ? -1.0 : 1.0;
            return new WpfPoint(dirX > 0 ? projected.X + baseOffset : projected.X - popupWidth - baseOffset, dirY < 0 ? projected.Y - popupHeight - baseOffset * 0.75 : projected.Y + baseOffset * 0.75);
        }

        double margin = Math.Max(12, _settings.CornerMarginPx);
        double safeRightX = Math.Max(viewportRect.Left + 12, viewportRect.Right - margin - popupWidth);
        double safeBottomY = Math.Max(viewportRect.Top + 12, viewportRect.Bottom - margin - popupHeight);
        double safeLeftX = Math.Min(safeRightX, viewportRect.Left + margin);
        double safeTopY = Math.Min(safeBottomY, viewportRect.Top + margin);

        var corners = new Dictionary<PopupPositionMode, WpfPoint>
        {
            { PopupPositionMode.TopLeft, new WpfPoint(safeLeftX, safeTopY) },
            { PopupPositionMode.TopRight, new WpfPoint(safeRightX, safeTopY) },
            { PopupPositionMode.BottomLeft, new WpfPoint(safeLeftX, safeBottomY) },
            { PopupPositionMode.BottomRight, new WpfPoint(safeRightX, safeBottomY) },
            { PopupPositionMode.FixedTopLeft, new WpfPoint(safeLeftX, safeTopY) },
            { PopupPositionMode.FixedTopRight, new WpfPoint(safeRightX, safeTopY) },
            { PopupPositionMode.FixedBottomLeft, new WpfPoint(safeLeftX, safeBottomY) },
            { PopupPositionMode.FixedBottomRight, new WpfPoint(safeRightX, safeBottomY) }
        };

        if (mode == PopupPositionMode.FixedTopLeft) return corners[PopupPositionMode.FixedTopLeft];
        if (mode == PopupPositionMode.FixedTopRight) return corners[PopupPositionMode.FixedTopRight];
        if (mode == PopupPositionMode.FixedBottomLeft) return corners[PopupPositionMode.FixedBottomLeft];
        if (mode == PopupPositionMode.FixedBottomRight) return corners[PopupPositionMode.FixedBottomRight];
        
        if (mode != PopupPositionMode.AutoViewportCorner) return corners.TryGetValue(mode, out var fixedCorner) ? fixedCorner : corners[PopupPositionMode.TopRight];

        double ScoreCorner(PopupPositionMode cornerMode) => Distance(new WpfPoint(corners[cornerMode].X + popupWidth / 2, corners[cornerMode].Y + popupHeight / 2), projected);
        var bestCorner = corners.Keys.OrderByDescending(ScoreCorner).First();
        if (_lastAutoCorner.HasValue && corners.ContainsKey(_lastAutoCorner.Value))
        {
            if (ScoreCorner(bestCorner) - ScoreCorner(_lastAutoCorner.Value) < 65.0) bestCorner = _lastAutoCorner.Value;
        }

        _lastAutoCorner = bestCorner;
        return corners[bestCorner];
    }

    private WpfPoint ApplyMagneticEdgeSnapping(WpfPoint desired, Rect viewportRect, double popupWidth, double popupHeight)
    {
        double margin = Math.Max(12, _settings.CornerMarginPx);
        double snapDist = Math.Max(8, _settings.EdgeSnapDistancePx);
        double leftEdge = viewportRect.Left + margin;
        double rightEdge = viewportRect.Right - margin - popupWidth;
        double topEdge = viewportRect.Top + margin;
        double bottomEdge = viewportRect.Bottom - margin - popupHeight;

        double x = desired.X; double y = desired.Y;
        if (Math.Abs(x - leftEdge) <= snapDist) x = leftEdge; else if (Math.Abs(x - rightEdge) <= snapDist) x = rightEdge;
        if (Math.Abs(y - topEdge) <= snapDist) y = topEdge; else if (Math.Abs(y - bottomEdge) <= snapDist) y = bottomEdge;
        return new WpfPoint(x, y);
    }

    private WpfPoint ClampToBounds(WpfPoint desired, Rect viewportRect, double popupWidth, double popupHeight)
    {
        Rect workArea = SystemParameters.WorkArea;
        Rect safeRect = Rect.Intersect(viewportRect, workArea);
        if (safeRect.IsEmpty || safeRect.Width < popupWidth + 24 || safeRect.Height < popupHeight + 24) safeRect = workArea;

        double pad = Math.Min(16, Math.Max(8, _settings.CornerMarginPx / 4.0));
        double extraBottom = (More.IsOpen && More.Placement == PlacementMode.Bottom) ? Math.Min(240, Math.Max(0, safeRect.Height - popupHeight - pad * 2)) : 0;
        double minX = safeRect.Left + pad;
        double maxX = Math.Max(minX, safeRect.Right - popupWidth - pad);
        double minY = safeRect.Top + pad;
        double maxY = Math.Max(minY, safeRect.Bottom - popupHeight - pad - extraBottom);

        return new WpfPoint(Math.Clamp(desired.X, minX, maxX), Math.Clamp(desired.Y, minY, maxY));
    }

    private void UpdateConduitScreenAnchor(Rect viewportRect, double popupWidth, double popupHeight)
    {
        if (_conduit == null) return;
        _conduit.ToolbarScreenAnchor = new Point2d(Left + popupWidth / 2.0 - viewportRect.Left, Top + popupHeight / 2.0 - viewportRect.Top);
    }

    private static double Distance(WpfPoint first, WpfPoint second) => Math.Sqrt((first.X - second.X) * (first.X - second.X) + (first.Y - second.Y) * (first.Y - second.Y));

    private bool _cameraRepositionPending;

    internal void OnViewportCameraChanged()
    {
        if (!IsVisible || _userManuallyDragged || !_settings.FollowCameraMovement || _cameraRepositionPending) return;
        if (_activeMode is PopupPositionMode.Geometry or PopupPositionMode.GeometryOffset or PopupPositionMode.AutoViewportCorner)
        {
            _cameraRepositionPending = true;
            Dispatcher.BeginInvoke(new Action(() =>
            {
                _cameraRepositionPending = false;
                RepositionAndClamp();
            }), DispatcherPriority.Background);
        }
    }

    private bool _hiddenDueToBackgroundApp;

    private void Tick()
    {
        UpdateAppForegroundState();
        if (!IsVisible || _hiddenDueToBackgroundApp) return;
        UpdateCameraTrackingAndOcclusion();
        if (_hiddenDueToOffscreen) return;

        DateTime now = DateTime.UtcNow;
        bool inside = IsMouseOver || (More.IsOpen && More.IsMouseOver) || LayerPopup.IsOpen;

        if (inside)
        {
            _lastInteraction = now;
            _leaveStart = null;
            _hoverStart ??= now;
            FadeTo(_settings.ActiveOpacity / 100d);

            if (_settings.UxMode == PopupUxMode.Compact && !_expanded && (now - _hoverStart.Value).TotalMilliseconds >= _settings.HoverDelayMs)
                SetExpanded(true);
        }
        else
        {
            _hoverStart = null;
            FadeTo(_settings.IdleOpacity / 100d);

            bool isFixedMode = _settings.PositionMode >= PopupPositionMode.FixedTopLeft;

            if (_expanded && _settings.UxMode == PopupUxMode.Compact && !IsPinned && !isFixedMode)
            {
                _leaveStart ??= now;
                int delay = More.IsOpen ? _settings.MoreCollapseDelayMs : _settings.CollapseDelayMs;
                if ((now - _leaveStart.Value).TotalMilliseconds >= delay) SetExpanded(false);
            }

            // Ignoriere Auto-Hide, wenn Pin gesetzt oder fester Modus aktiv ist!
            if (!IsPinned && !isFixedMode && _settings.AutoHideMs > 0 && (now - _lastInteraction).TotalMilliseconds >= _settings.AutoHideMs)
                _requestClose(false);
        }
    }

    
        private void ApplyTheme()
    {
        bool isDark = true;
        try
        {
            var c = Rhino.ApplicationSettings.AppearanceSettings.GetPaintColor(Rhino.ApplicationSettings.PaintColor.NormalStart);
            isDark = (c.R + c.G + c.B) / 3 < 128;
        }
        catch { }

        if (isDark)
        {
            RootBorder.Background = new SolidColorBrush(WpfColor.FromArgb(239, 40, 40, 40));
            More.PopupAnimation = PopupAnimation.Fade;
            if (More.Child is Border b) b.Background = new SolidColorBrush(WpfColor.FromArgb(252, 40, 40, 40));
            LayerPopupBorder.Background = new SolidColorBrush(WpfColor.FromArgb(252, 40, 40, 40));
            LayerSearchBox.Foreground = WpfBrushes.White;
            LayerSearchBox.Background = new SolidColorBrush(WpfColor.FromArgb(50, 255, 255, 255));
            LayerSearchBox.BorderBrush = new SolidColorBrush(WpfColor.FromArgb(30, 255, 255, 255));
            
            _themeTextBrush = WpfBrushes.White;
            _themeHintBrush = new SolidColorBrush(WpfColor.FromRgb(180, 180, 180));
            _themeDangerBrush = new SolidColorBrush(WpfColor.FromRgb(255, 180, 180));
            _themeCategoryBrush = WpfBrushes.Gray;
            
            TitleText.Foreground = WpfBrushes.LightGray;
            SearchBox.Foreground = WpfBrushes.White;
            SearchBox.Background = new SolidColorBrush(WpfColor.FromArgb(50, 255, 255, 255));
            SearchBox.BorderBrush = new SolidColorBrush(WpfColor.FromArgb(30, 255, 255, 255));
        }
        else
        {
            RootBorder.Background = new SolidColorBrush(WpfColor.FromArgb(245, 245, 245, 245));
            if (More.Child is Border b) b.Background = new SolidColorBrush(WpfColor.FromArgb(252, 245, 245, 245));
            LayerPopupBorder.Background = new SolidColorBrush(WpfColor.FromArgb(252, 245, 245, 245));
            LayerSearchBox.Foreground = WpfBrushes.Black;
            LayerSearchBox.Background = new SolidColorBrush(WpfColor.FromArgb(50, 0, 0, 0));
            LayerSearchBox.BorderBrush = new SolidColorBrush(WpfColor.FromArgb(30, 0, 0, 0));
            
            _themeTextBrush = new SolidColorBrush(WpfColor.FromRgb(30, 30, 30));
            _themeHintBrush = new SolidColorBrush(WpfColor.FromRgb(100, 100, 100));
            _themeDangerBrush = new SolidColorBrush(WpfColor.FromRgb(220, 40, 40));
            _themeCategoryBrush = new SolidColorBrush(WpfColor.FromRgb(90, 90, 90));
            
            TitleText.Foreground = WpfBrushes.DarkGray;
            SearchBox.Foreground = WpfBrushes.Black;
            SearchBox.Background = new SolidColorBrush(WpfColor.FromArgb(50, 0, 0, 0));
            SearchBox.BorderBrush = new SolidColorBrush(WpfColor.FromArgb(30, 0, 0, 0));
        }
        
        this.Foreground = _themeTextBrush;
        LayerButton.Background = new SolidColorBrush(isDark ? WpfColor.FromArgb(15, 255, 255, 255) : WpfColor.FromArgb(15, 0, 0, 0));
    }

    private void UpdateCameraTrackingAndOcclusion()
    {
        bool isFixedMode = _settings.PositionMode >= PopupPositionMode.FixedTopLeft;
        if (IsPinned || isFixedMode || !_context.Anchor.IsValid)
        {
            if (_hiddenDueToOffscreen)
            {
                _hiddenDueToOffscreen = false;
                IsHitTestVisible = true;
                FadeTo((IsMouseOver ? _settings.ActiveOpacity : _settings.IdleOpacity) / 100d);
            }
            return;
        }
        var view = RhinoDoc.ActiveDoc?.Views.ActiveView;
        if (view == null) return;

        bool visibleInViewport = ContextAssistAnchorConduit.CheckAnchorVisible(view.ActiveViewport, _context.Anchor, out var clientPt);

        if (_settings.AutoHideWhenAnchorOffscreen)
        {
            if (!visibleInViewport)
            {
                if (!_hiddenDueToOffscreen)
                {
                    _hiddenDueToOffscreen = true; More.IsOpen = false; IsHitTestVisible = false; FadeTo(0.0);
                }
                return;
            }
            if (_hiddenDueToOffscreen)
            {
                _hiddenDueToOffscreen = false; IsHitTestVisible = true; _lastInteraction = DateTime.UtcNow;
                FadeTo((IsMouseOver ? _settings.ActiveOpacity : _settings.IdleOpacity) / 100d);
            }
        }
    }

    private void HeaderBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState == MouseButtonState.Pressed)
        {
            _userManuallyDragged = true;
            try { DragMove(); } catch { }
            RepositionAndClamp();
        }
    }

    private void SetExpanded(bool expanded)
    {
        _expanded = expanded;
        Pill.Visibility = expanded ? Visibility.Collapsed : Visibility.Visible;
        Expanded.Visibility = expanded ? Visibility.Visible : Visibility.Collapsed;
        if (!expanded) More.IsOpen = false;
        Dispatcher.BeginInvoke(new Action(RepositionAndClamp), DispatcherPriority.Loaded);
    }

    private void Pill_Click(object sender, RoutedEventArgs e) => SetExpanded(true);
    
    public bool ClosedByUserButton { get; private set; }

    internal bool CloseSubMenusIfOpen()
    {
        bool closedAny = false;
        if (More.IsOpen)
        {
            More.IsOpen = false;
            closedAny = true;
        }
        if (LayerPopup != null && LayerPopup.IsOpen)
        {
            LayerPopup.IsOpen = false;
            closedAny = true;
        }
        if (closedAny) RepositionAndClamp();
        return closedAny;
    }

    internal void SyncModeFromSettings()
    {
        _activeMode = _settings.PositionMode;
        bool isFixedMode = _settings.PositionMode >= PopupPositionMode.FixedTopLeft;
        if (isFixedMode)
        {
            PinButton.IsChecked = true;
            PinButton.Visibility = Visibility.Collapsed;
            if (!_expanded) SetExpanded(true);
        }
        else
        {
            PinButton.Visibility = Visibility.Visible;
        }
    }

    internal void SafeClose()
    {
        try { _timer.Stop(); } catch { }
        try { More.IsOpen = false; } catch { }
        try { if (LayerPopup != null) LayerPopup.IsOpen = false; } catch { }
        try { SourceInitialized -= InitializeNativeWindow; } catch { }
        try { Close(); } catch { }
    }

    // Close button (always forces close, overriding Pin and Fixed mode)
    private void Close_Click(object sender, RoutedEventArgs e)
    {
        ClosedByUserButton = true;
        _requestClose(true);
    }

    private double _targetOpacity = -1.0;

    private void FadeTo(double opacity)
    {
        if (Math.Abs(_targetOpacity - opacity) < 0.005) return;
        _targetOpacity = opacity;
        BeginAnimation(OpacityProperty, new DoubleAnimation(opacity, TimeSpan.FromMilliseconds(120)));
    }

    private void InitializeNativeWindow(object? sender, EventArgs e) => SetWindowLongPtr(new WindowInteropHelper(this).Handle, -8, RhinoApp.MainWindowHandle());

    private static string GetContextIcon(SelectionKind kind) => kind switch
    {
        SelectionKind.Block => "◆",
        SelectionKind.Mesh or SelectionKind.MeshFace or SelectionKind.MeshEdge or SelectionKind.MeshVertex => "△",
        SelectionKind.Curve or SelectionKind.ClosedPlanarCurve => "⌁",
        SelectionKind.SubD or SelectionKind.SubDFace or SelectionKind.SubDEdge or SelectionKind.SubDVertex => "◇",
        _ => "⬒"
    };

    public bool IsPinned => PinButton.IsChecked == true;
    internal SelectionKind CurrentSelectionKind => _context?.Kind ?? SelectionKind.None;
    internal int CurrentSelectionCount => _context?.Count ?? 0;
    public bool IsSearchFocused => SearchBox.IsFocused || LayerSearchBox.IsFocused;

    
    private void Settings_Click(object sender, RoutedEventArgs e)
    {
        _requestClose(true);
        if (ContextAssistPlugIn.Instance?.Controller != null)
            ContextAssistPlugIn.Instance.Controller.QueueMacroExecution("! _-OptionsPage ContextAssist _Enter");
        else
            RhinoApp.RunScript("! _-OptionsPage ContextAssist _Enter", false);
    }

    private void InitializeLayerButton()
    {
        var doc = RhinoDoc.ActiveDoc;
        if (doc == null) return;
        
        var selectedObjects = SelectionAnalyzer.GetEffectiveSelectedObjects(doc);
        if (selectedObjects.Count == 0)
        {
            LayerButton.Visibility = Visibility.Collapsed;
            return;
        }
        LayerButton.Visibility = Visibility.Visible;
        
        var firstLayerIndex = selectedObjects.First().Attributes.LayerIndex;
        bool sameLayer = selectedObjects.All(o => o.Attributes.LayerIndex == firstLayerIndex);
        
        if (sameLayer)
        {
            var layer = doc.Layers.FindIndex(firstLayerIndex);
            string name = layer?.Name ?? "Unbekannt";
            System.Drawing.Color c = layer?.Color ?? System.Drawing.Color.Black;
            
            StackPanel sp = new StackPanel { Orientation = Orientation.Horizontal };
            var ell = new System.Windows.Shapes.Ellipse { 
                Width = 10, Height = 10, 
                Fill = new SolidColorBrush(WpfColor.FromRgb(c.R, c.G, c.B)), 
                Margin = new Thickness(0,0,6,0), 
                VerticalAlignment = VerticalAlignment.Center 
            };
            sp.Children.Add(ell);
            sp.Children.Add(new TextBlock { 
                Text = $"Layer: {name}", 
                Foreground = _themeTextBrush 
            });
            LayerButton.Content = sp;
        }
        else
        {
            LayerButton.Content = "Layer: Mehrere ausgewählt";
        }
    }
    
    private void LayerButton_Click(object sender, RoutedEventArgs e)
    {
        LayerPopup.PlacementTarget = LayerButton;
        LayerPopup.IsOpen = true;
    }

    private void LayerPopup_Opened(object sender, EventArgs e)
    {
        LayerSearchBox.Text = string.Empty;
        RenderLayers(string.Empty);
        LayerSearchBox.Focus();
    }

    private void LayerPopup_Closed(object sender, EventArgs e)
    {
        RepositionAndClamp();
    }

    private void LayerSearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        RenderLayers(LayerSearchBox.Text);
    }
    
    private void LayerSearchBox_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            LayerPopup.IsOpen = false;
            RepositionAndClamp();
            e.Handled = true;
        }
        else if (e.Key == Key.Enter)
        {
            if (LayerResults.Items.Count > 0)
            {
                var firstBtn = LayerResults.Items.OfType<System.Windows.Controls.Button>().FirstOrDefault() ?? (LayerResults.Items[0] as System.Windows.Controls.Button);
                if (firstBtn != null && System.Windows.Input.Mouse.PrimaryDevice != null)
                {
                    var args = new MouseButtonEventArgs(System.Windows.Input.Mouse.PrimaryDevice, 0, MouseButton.Left) { RoutedEvent = Button.ClickEvent };
                    firstBtn.RaiseEvent(args);
                    e.Handled = true;
                }
            }
        }
        else if (e.Key == Key.Down)
        {
            if (LayerResults.Items.Count > 0)
            {
                var firstBtn = LayerResults.Items.OfType<System.Windows.Controls.Button>().FirstOrDefault() ?? (LayerResults.Items[0] as System.Windows.Controls.Button);
                if (firstBtn != null)
                {
                    firstBtn.Focus();
                    e.Handled = true;
                }
            }
        }
    }

    private void RenderLayers(string query)
    {
        var doc = RhinoDoc.ActiveDoc;
        if (doc == null) return;

        LayerResults.Items.Clear();
        var layers = doc.Layers.Where(l => !l.IsDeleted).OrderBy(l => l.FullPath).ToList();

        if (!string.IsNullOrWhiteSpace(query))
        {
            var parts = query.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            layers = layers.Where(l => parts.All(p => l.FullPath.Contains(p, StringComparison.OrdinalIgnoreCase))).ToList();
        }

        foreach (var layer in layers.Take(150))
        {
            var c = layer.Color;
            var sp = new StackPanel { Orientation = Orientation.Horizontal };
            sp.Children.Add(new System.Windows.Shapes.Ellipse {
                Width = 12, Height = 12,
                Fill = new SolidColorBrush(WpfColor.FromRgb(c.R, c.G, c.B)),
                Margin = new Thickness(0, 0, 8, 0),
                VerticalAlignment = VerticalAlignment.Center
            });
            sp.Children.Add(new TextBlock {
                Text = layer.FullPath,
                Foreground = _themeTextBrush,
                VerticalAlignment = VerticalAlignment.Center
            });

            var btn = new Button
            {
                Content = sp,
                Style = (Style)FindResource("B"),
                HorizontalContentAlignment = HorizontalAlignment.Left
            };
            
            btn.Click += (s, args) =>
            {
                uint undoRecord = doc.BeginUndoRecord("Context Assist: Layer ändern");
                try
                {
                    var selectedObjects = SelectionAnalyzer.GetEffectiveSelectedObjects(doc);
                    foreach (var obj in selectedObjects)
                    {
                        obj.Attributes.LayerIndex = layer.Index;
                        obj.CommitChanges();
                    }
                }
                finally
                {
                    if (undoRecord > 0) doc.EndUndoRecord(undoRecord);
                }
                doc.Views.Redraw();
                InitializeLayerButton();
                LayerPopup.IsOpen = false;
                ReturnFocusToRhino();
            };

            LayerResults.Items.Add(btn);
        }
    }


    internal bool TryExecuteShortcutIndex(int idx)
    {
        if (idx >= 0 && idx < _primaryActions.Count)
        {
            ReturnFocusToRhino();
            _execute(_primaryActions[idx]);
            return true;
        }
        return false;
    }

    private void UpdateAppForegroundState()
    {
        try
        {
            IntPtr fgHwnd = GetForegroundWindow();
            if (fgHwnd == IntPtr.Zero) return;

            GetWindowThreadProcessId(fgHwnd, out uint fgPid);
            uint currentPid = (uint)Environment.ProcessId;
            bool isRhinoActive = fgPid == currentPid && !IsIconic(RhinoApp.MainWindowHandle());

            if (!isRhinoActive)
            {
                if (!_hiddenDueToBackgroundApp)
                {
                    _hiddenDueToBackgroundApp = true;
                    CloseSubMenusIfOpen();
                    IsHitTestVisible = false;
                    _targetOpacity = 0.0;
                    BeginAnimation(OpacityProperty, null);
                    Opacity = 0.0;
                }
            }
            else if (_hiddenDueToBackgroundApp)
            {
                _hiddenDueToBackgroundApp = false;
                IsHitTestVisible = true;
                _lastInteraction = DateTime.UtcNow;
                FadeTo((IsMouseOver ? _settings.ActiveOpacity : _settings.IdleOpacity) / 100d);
            }
        }
        catch { }
    }

    internal static void ReturnFocusToRhino()
    {
        try
        {
            IntPtr mainHwnd = RhinoApp.MainWindowHandle();
            if (mainHwnd != IntPtr.Zero)
                SetForegroundWindow(mainHwnd);
        }
        catch { }
    }

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    [DllImport("user32.dll")]
    private static extern bool IsIconic(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW")]
    private static extern IntPtr SetWindowLongPtr(IntPtr windowHandle, int index, IntPtr value);
}
