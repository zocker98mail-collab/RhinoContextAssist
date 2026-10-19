using Eto.Drawing; using Eto.Forms; using Rhino.UI;
namespace RhinoContextAssist;
internal sealed class ContextToolbarForm:Form{
 readonly Label caption=new(); readonly StackLayout buttons=new(){Orientation=Orientation.Horizontal,Spacing=4}; readonly Action<ContextAction> execute; readonly Action dismiss;
 public ContextToolbarForm(Action<ContextAction> e,Action d){execute=e;dismiss=d;Title="Context Assist";ShowInTaskbar=false;ShowActivated=false;Topmost=true;Resizable=false;WindowStyle=WindowStyle.None;Padding=7;this.UseRhinoStyle();Content=new StackLayout{Spacing=4,Items={caption,buttons}};}
 public void UpdateActions(SelectionContext c,IReadOnlyList<ContextAction>a){caption.Text=c.Caption;buttons.Items.Clear();foreach(var x in a){var b=new Button{Text=x.Label,ToolTip=x.Hint,Height=28};b.Click+=(_,_)=>execute(x);buttons.Items.Add(b);}var close=new Button{Text="×",ToolTip="Ausblenden",Width=30,Height=28};close.Click+=(_,_)=>dismiss();buttons.Items.Add(close);}
 public void ShowSafelyNearMouse(){var m=Mouse.Position;Location=new Point((int)m.X+14,(int)m.Y+18);if(!Visible)Show();var w=Screen.FromPoint(m).WorkingArea;var l=(int)w.Left;var t=(int)w.Top;var r=(int)w.Right;var bot=(int)w.Bottom;var x=Location.X;var y=Location.Y;if(x+Width>r)x=Math.Max(l,r-Width);if(y+Height>bot)y=Math.Max(t,bot-Height);if(x<l)x=l;if(y<t)y=t;Location=new Point(x,y);}}
