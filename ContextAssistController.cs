using Eto.Forms;
using Rhino;
using Rhino.Display;
using Rhino.DocObjects;
namespace RhinoContextAssist;
internal sealed class ContextAssistController:IDisposable{
 readonly UITimer timer;readonly ContextAssistSettings settings;ContextAssistPopup? popup;DateTime due=DateTime.MaxValue;
 internal ContextAssistAnchorConduit? Conduit{get;private set;};internal SelectionContext? LastContext{get;private set;};
 public ContextAssistController(ContextAssistSettings s){
  settings=s;
  Conduit=new ContextAssistAnchorConduit{
   ShowAnchorDot=s.ShowViewportAnchor,
   ShowTetherLine=s.ShowTetherLine,
   TetherOpacity=s.TetherLineOpacity,
   TetherThickness=s.TetherLineThickness
  };
  Conduit.Enabled=true;
  timer=new UITimer{Interval=.04};
  timer.Elapsed+=(_,_)=>Tick();
 }
 public void Start(){
  RhinoDoc.SelectObjects+=Changed;
  RhinoDoc.DeselectObjects+=Changed;
  RhinoDoc.DeselectAllObjects+=Cleared;
  DisplayPipeline.CalculateBoundingBox+=OnCalculateBoundingBox;
  timer.Start();
 }
 public void RefreshConduitSettings(){
  if(Conduit==null)return;
  Conduit.ShowAnchorDot=settings.ShowViewportAnchor;
  Conduit.ShowTetherLine=settings.ShowTetherLine;
  Conduit.TetherOpacity=settings.TetherLineOpacity;
  Conduit.TetherThickness=settings.TetherLineThickness;
  RhinoDoc.ActiveDoc?.Views.ActiveView?.Redraw();
 }
 void Changed(object?s,RhinoObjectSelectionEventArgs e){Close();due=DateTime.UtcNow.AddMilliseconds(settings.SelectionDelayMs);}
 void Cleared(object?s,RhinoDeselectAllObjectsEventArgs e)=>Hide();
 void OnCalculateBoundingBox(object? sender, CalculateBoundingBoxEventArgs e){
  if(popup==null||!popup.IsVisible||!settings.FollowCameraMovement)return;
  var view=RhinoDoc.ActiveDoc?.Views.ActiveView;
  if(view==null||view.ActiveViewport.Id!=e.Viewport.Id)return;
  popup.Dispatcher.BeginInvoke(new Action(()=>{
   if(popup!=null&&popup.IsVisible)popup.OnViewportCameraChanged();
  }));
 }
 void Tick(){
  var d=RhinoDoc.ActiveDoc;
  if(!settings.Enabled||d==null||d.IsCommandRunning)return;
  if(due==DateTime.MaxValue||DateTime.UtcNow<due)return;
  due=DateTime.MaxValue;
  var c=SelectionAnalyzer.Analyze(d);
  LastContext=c;
  if(c.Kind==SelectionKind.None){Hide();return;}
  var a=ActionService.Get(c,settings);
  if(a.Count==0){Hide();return;}
  if(Conduit!=null)Conduit.AnchorWorld=c.Anchor;
  if(popup==null){
   popup=new(Execute,Hide,settings,Conduit);
   popup.UpdateActions(c,a);
   popup.ShowAt(c,settings.PositionMode);
  }else{
   popup.UpdateActions(c,a);
  }
 }
 public void ShowNow(){
  var d=RhinoDoc.ActiveDoc;
  if(d==null)return;
  due=DateTime.MaxValue;
  var c=SelectionAnalyzer.Analyze(d);
  LastContext=c;
  if(c.Kind==SelectionKind.None)return;
  var a=ActionService.Get(c,settings);
  if(a.Count==0)return;
  if(Conduit!=null)Conduit.AnchorWorld=c.Anchor;
  Close();
  popup=new(Execute,Hide,settings,Conduit);
  popup.UpdateActions(c,a);
  popup.ShowAt(c,settings.PositionMode);
 }
 void Execute(ContextAction a){ContextAssistPlugIn.Instance!.LastAction=a;settings.RecordUsage(a.Id);Hide();RhinoApp.RunScript(a.Macro,false);}
 public void Hide(){due=DateTime.MaxValue;if(Conduit!=null)Conduit.AnchorWorld=Point3d.Unset;Close();RhinoDoc.ActiveDoc?.Views.ActiveView?.Redraw();}
 void Close(){try{popup?.Close();}catch{}popup=null;}
 public void Dispose(){
  timer.Stop();
  RhinoDoc.SelectObjects-=Changed;
  RhinoDoc.DeselectObjects-=Changed;
  RhinoDoc.DeselectAllObjects-=Cleared;
  DisplayPipeline.CalculateBoundingBox-=OnCalculateBoundingBox;
  if(Conduit!=null){Conduit.Enabled=false;Conduit=null;}
  Close();
 }
}