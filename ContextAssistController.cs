using Eto.Forms; using Rhino; using Rhino.DocObjects; using EtoPointF=Eto.Drawing.PointF;
namespace RhinoContextAssist;
internal sealed class ContextAssistController:IDisposable
{
 readonly UITimer timer;readonly ContextAssistSettings settings;ContextAssistPopup? popup;DateTime due=DateTime.MaxValue;EtoPointF selectionMouse;bool started;
 public bool Enabled{get=>settings.Enabled;set{settings.Enabled=value;settings.Save();}}
 public ContextAssistController(ContextAssistSettings s){settings=s;timer=new UITimer{Interval=.1};timer.Elapsed+=(_,_)=>Tick();}
 public void Start(){if(started)return;started=true;RhinoDoc.SelectObjects+=Changed;RhinoDoc.DeselectObjects+=Changed;RhinoDoc.DeselectAllObjects+=Cleared;RhinoDoc.CloseDocument+=Closed;RhinoDoc.ActiveDocumentChanged+=Closed;RhinoApp.EscapeKeyPressed+=Escape;timer.Start();}
 void Changed(object?s,RhinoObjectSelectionEventArgs e){Close();selectionMouse=Mouse.Position;if(Enabled)due=DateTime.UtcNow.AddMilliseconds(settings.SelectionDelayMs);}void Cleared(object?s,RhinoDeselectAllObjectsEventArgs e)=>Hide();void Closed(object?s,DocumentEventArgs e)=>Hide();void Escape(object?s,EventArgs e)=>Hide();
 void Tick(){var d=RhinoDoc.ActiveDoc;if(!Enabled||d is null||d.IsCommandRunning){Hide();return;}if(due==DateTime.MaxValue||DateTime.UtcNow<due)return;due=DateTime.MaxValue;if(!Near())return;RefreshNow();}
 bool Near(){if(settings.ProximityRadiusPx<=0)return true;var p=Mouse.Position;var dx=p.X-selectionMouse.X;var dy=p.Y-selectionMouse.Y;return dx*dx+dy*dy<=settings.ProximityRadiusPx*settings.ProximityRadiusPx;}
 public void RefreshNow(){var d=RhinoDoc.ActiveDoc;if(!Enabled||d is null||d.IsCommandRunning){Hide();return;}var c=SelectionAnalyzer.Analyze(d);IReadOnlyList<ContextAction> baseActions=c.Kind==SelectionKind.MultipleEnvelopeObjects?settings.Resolve(SelectionKind.Multiple):settings.Resolve(c.Kind);var actions=ContextActionPolicy.Apply(c,baseActions,settings);if(c.Kind==SelectionKind.None||actions.Count==0){Hide();return;}Close();var p=new ContextAssistPopup(Execute,Hide,settings);p.Closed+=(_,_)=>{if(ReferenceEquals(popup,p))popup=null;};p.UpdateActions(c,actions);popup=p;p.ShowAt(c,settings.PositionMode);}
 void Execute(ContextAction a){settings.RecordUsage(a.Id);Hide();RhinoApp.InvokeOnUiThread((Action)(()=>RhinoApp.RunScript(a.Macro,false)));}public void Hide(){due=DateTime.MaxValue;Close();}void Close(){var p=popup;popup=null;if(p==null)return;try{p.Close();}catch{}}
 public void Dispose(){timer.Stop();if(started){RhinoDoc.SelectObjects-=Changed;RhinoDoc.DeselectObjects-=Changed;RhinoDoc.DeselectAllObjects-=Cleared;RhinoDoc.CloseDocument-=Closed;RhinoDoc.ActiveDocumentChanged-=Closed;RhinoApp.EscapeKeyPressed-=Escape;}Close();}
}
