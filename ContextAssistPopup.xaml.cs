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
    private const int GwlExStyle=-20, WsExNoActivate=0x08000000, WsExToolWindow=0x80, WmMouseActivate=0x21, MaNoActivate=3;
    private readonly Action<ContextAction> _execute; private readonly Action _dismiss; private readonly ContextAssistSettings _settings; private readonly DispatcherTimer _timer;
    private DateTime _lastInteraction; private DateTime? _hoverStart, _leaveStart; private bool _expanded;
    internal ContextAssistPopup(Action<ContextAction> execute, Action dismiss, ContextAssistSettings settings) { InitializeComponent(); _execute=execute; _dismiss=dismiss; _settings=settings; SourceInitialized+=OnSourceInitialized; PreviewMouseMove+=(_,_)=>Touch(); _timer=new DispatcherTimer(TimeSpan.FromMilliseconds(50),DispatcherPriority.Background,(_,_)=>Tick(),Dispatcher); }
    internal void UpdateActions(SelectionContext context, IReadOnlyList<ContextAction> actions)
    {
        var icon=Icon(context.Kind); PillButton.Content=$"{icon}  {Short(context.Kind,context.Caption)} · {actions.Count}"; CaptionText.Text=$"{icon}  {context.Caption}"; PrimaryPanel.Children.Clear(); MorePanel.Children.Clear(); MorePopup.IsOpen=false;
        var count=Math.Min(_settings.PrimaryCount,actions.Count); foreach(var a in actions.Take(count))PrimaryPanel.Children.Add(Button(a,context.Kind,false)); var remaining=actions.Skip(count).ToList();
        if(remaining.Count>0){var more=new WpfButton{Content=$"⋯ Mehr ({remaining.Count})",Style=(Style)FindResource("Action")};more.Click+=(_,_)=>{Touch();MorePopup.IsOpen=!MorePopup.IsOpen;};PrimaryPanel.Children.Add(more);MorePopup.PlacementTarget=more;foreach(var a in remaining)MorePanel.Children.Add(Button(a,context.Kind,true));}
    }
    internal void ShowAt(SelectionContext context, PopupPositionMode mode) { var p=Anchor(context,mode); Left=p.X+_settings.CursorOffsetPx; Top=p.Y+_settings.CursorOffsetPx; _lastInteraction=DateTime.UtcNow; SetExpanded(_settings.UxMode==PopupUxMode.Direct); Opacity=_settings.IdleOpacity; Show(); Clamp(); _timer.Start(); }
    private EtoPointF Anchor(SelectionContext context,PopupPositionMode mode)
    {
        var mouse=Eto.Forms.Mouse.Position;var view=RhinoDoc.ActiveDoc?.Views.ActiveView;if(view is null)return mouse;var screen=view.ScreenRectangle;var projected=mouse;
        if(context.Anchor.IsValid){var c=view.ActiveViewport.WorldToClient(context.Anchor);projected=new EtoPointF((float)(screen.Left+c.X),(float)(screen.Top+c.Y));}
        if(mode==PopupPositionMode.Mouse)return mouse;if(mode==PopupPositionMode.Geometry)return projected;
        if(mode==PopupPositionMode.GeometryOffset){var cx=screen.Left+screen.Width/2f;var cy=screen.Top+screen.Height/2f;var dx=projected.X-cx;var dy=projected.Y-cy;var len=Math.Sqrt(dx*dx+dy*dy);if(len<1){dx=1;dy=-1;len=Math.Sqrt(2);}const float distance=110;return new EtoPointF(projected.X+(float)(dx/len*distance),projected.Y+(float)(dy/len*distance));}
        const float m=28;var corners=new[]{new EtoPointF(screen.Left+m,screen.Top+m),new EtoPointF(screen.Right-m,screen.Top+m),new EtoPointF(screen.Left+m,screen.Bottom-m),new EtoPointF(screen.Right-m,screen.Bottom-m)};return corners.OrderByDescending(x=>{var dx=x.X-projected.X;var dy=x.Y-projected.Y;return dx*dx+dy*dy;}).First();
    }
    private WpfButton Button(ContextAction a,SelectionKind k,bool more){var fav=_settings.IsFavorite(k,a.Id)?"★ ":"";var b=new WpfButton{Content=$"{a.Icon}  {fav}{a.Label}",ToolTip=$"{a.Hint}\nVerwendet: {_settings.UsageCount(a.Id)}×",Style=(Style)FindResource(more?"MoreItem":"Action")};b.Click+=(_,_)=>{Touch();MorePopup.IsOpen=false;_execute(a);};return b;}
    private void PillButton_Click(object s,RoutedEventArgs e){Touch();SetExpanded(true);} private void CloseButton_Click(object s,RoutedEventArgs e)=>_dismiss();
    private void Tick(){if(!IsVisible)return;var now=DateTime.UtcNow;var mouse=Eto.Forms.Mouse.Position;var inside=(mouse.X>=Left&&mouse.Y>=Top&&mouse.X<=Left+ActualWidth&&mouse.Y<=Top+ActualHeight)||(MorePopup.IsOpen&&MorePopup.IsMouseOver);if(inside){Touch();_leaveStart=null;_hoverStart??=now;Animate(_settings.ActiveOpacity);if(_settings.UxMode==PopupUxMode.Compact&&!_expanded&&(now-_hoverStart.Value).TotalMilliseconds>=_settings.HoverDelayMs)SetExpanded(true);}else{_hoverStart=null;Animate(_settings.IdleOpacity);if(_expanded&&_settings.UxMode==PopupUxMode.Compact){_leaveStart??=now;var delay=MorePopup.IsOpen?_settings.MoreCollapseDelayMs:_settings.CollapseDelayMs;if((now-_leaveStart.Value).TotalMilliseconds>=delay){MorePopup.IsOpen=false;SetExpanded(false);}}if(_settings.AutoHideMs>0&&(now-_lastInteraction).TotalMilliseconds>=_settings.AutoHideMs)_dismiss();}}
    private void SetExpanded(bool x){_expanded=x;PillButton.Visibility=x?Visibility.Collapsed:Visibility.Visible;ExpandedPanel.Visibility=x?Visibility.Visible:Visibility.Collapsed;if(!x)MorePopup.IsOpen=false;Touch();Dispatcher.BeginInvoke(DispatcherPriority.Loaded,new Action(()=>{UpdateLayout();Clamp();}));}
    private void Animate(double target){if(Math.Abs(Opacity-target)<.02)return;BeginAnimation(OpacityProperty,new DoubleAnimation{To=target,Duration=TimeSpan.FromMilliseconds(140),FillBehavior=FillBehavior.HoldEnd},HandoffBehavior.SnapshotAndReplace);} private void Touch()=>_lastInteraction=DateTime.UtcNow;
    private void Clamp(){var width=SystemParameters.WorkArea.Width;var height=SystemParameters.WorkArea.Height;Left=Math.Clamp(Left,SystemParameters.WorkArea.Left,Math.Max(SystemParameters.WorkArea.Left,width-ActualWidth));Top=Math.Clamp(Top,SystemParameters.WorkArea.Top,Math.Max(SystemParameters.WorkArea.Top,height-ActualHeight));}
    private void OnSourceInitialized(object? s,EventArgs e){var h=new WindowInteropHelper(this).Handle;SetWindowLongPtr(h,-8,RhinoApp.MainWindowHandle());HwndSource.FromHwnd(h)?.AddHook(WndProc);var style=GetWindowLongPtr(h,GwlExStyle).ToInt64()|WsExNoActivate|WsExToolWindow;SetWindowLongPtr(h,GwlExStyle,new IntPtr(style));}
    private static IntPtr WndProc(IntPtr h,int msg,IntPtr w,IntPtr l,ref bool handled){if(msg==WmMouseActivate){handled=true;return new IntPtr(MaNoActivate);}return IntPtr.Zero;}
    private static string Short(SelectionKind k,string f)=>k switch{SelectionKind.ClosedPlanarCurve=>"Planare Kurve",SelectionKind.PlanarBrepFace=>"Planare Teilfläche",SelectionKind.SolidBrep=>"Körper",SelectionKind.Block=>"Block",_=>f};
    private static string Icon(SelectionKind k)=>k switch{SelectionKind.Curve or SelectionKind.ClosedPlanarCurve=>"⌁",SelectionKind.PlanarSurface or SelectionKind.Surface=>"▰",SelectionKind.PlanarBrepFace or SelectionKind.BrepFace=>"▣",SelectionKind.BrepEdge=>"⌒",SelectionKind.SolidBrep or SelectionKind.Brep or SelectionKind.MultipleEnvelopeObjects=>"⬒",SelectionKind.Mesh or SelectionKind.MeshFace or SelectionKind.MeshEdge or SelectionKind.MeshVertex=>"△",SelectionKind.SubD or SelectionKind.SubDFace or SelectionKind.SubDEdge or SelectionKind.SubDVertex=>"◇",SelectionKind.Block=>"◆",SelectionKind.Multiple or SelectionKind.Mixed=>"▦",_=>"•"};
    [DllImport("user32.dll",EntryPoint="GetWindowLongPtrW")]private static extern IntPtr GetWindowLongPtr64(IntPtr h,int i);[DllImport("user32.dll",EntryPoint="GetWindowLongW")]private static extern IntPtr GetWindowLong32(IntPtr h,int i);private static IntPtr GetWindowLongPtr(IntPtr h,int i)=>IntPtr.Size==8?GetWindowLongPtr64(h,i):GetWindowLong32(h,i);
    [DllImport("user32.dll",EntryPoint="SetWindowLongPtrW")]private static extern IntPtr SetWindowLongPtr64(IntPtr h,int i,IntPtr n);[DllImport("user32.dll",EntryPoint="SetWindowLongW")]private static extern IntPtr SetWindowLong32(IntPtr h,int i,IntPtr n);private static IntPtr SetWindowLongPtr(IntPtr h,int i,IntPtr n)=>IntPtr.Size==8?SetWindowLongPtr64(h,i,n):SetWindowLong32(h,i,n);
}
