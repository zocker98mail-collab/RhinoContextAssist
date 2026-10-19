using Eto.Forms; using Rhino; using Rhino.DocObjects;
namespace RhinoContextAssist;
internal sealed class ContextAssistController:IDisposable{readonly UITimer timer;ContextToolbarForm? form;DateTime due=DateTime.MaxValue;string sig="";bool started;public bool Enabled{get;set;}=true;public ContextAssistController(){timer=new UITimer{Interval=.10};timer.Elapsed+=(_,_)=>Tick();}
 public void Start(){if(started)return;started=true;RhinoDoc.SelectObjects+=Changed;RhinoDoc.DeselectObjects+=Changed;RhinoDoc.DeselectAllObjects+=Cleared;RhinoDoc.CloseDocument+=Closed;RhinoDoc.ActiveDocumentChanged+=Closed;RhinoApp.EscapeKeyPressed+=Escape;timer.Start();}
 void Changed(object? s,RhinoObjectSelectionEventArgs e){if(Enabled)due=DateTime.UtcNow.AddMilliseconds(160);}void Cleared(object?s,RhinoDeselectAllObjectsEventArgs e)=>Hide();void Closed(object?s,DocumentEventArgs e)=>Hide();void Escape(object?s,EventArgs e)=>Hide();
 void Tick(){if(!Enabled){Hide();return;}var d=RhinoDoc.ActiveDoc;if(d is null||d.IsCommandRunning){Hide();return;}if(due==DateTime.MaxValue||DateTime.UtcNow<due)return;due=DateTime.MaxValue;RefreshNow();}
 public void RefreshNow(){var d=RhinoDoc.ActiveDoc;if(!Enabled||d is null||d.IsCommandRunning){Hide();return;}var c=SelectionAnalyzer.Analyze(d);if(c.Kind==SelectionKind.None){Hide();return;}var a=ActionResolver.Resolve(c).Take(5).ToList();if(a.Count==0){Hide();return;}form=new ContextToolbarForm(Execute,Hide);form.UpdateActions(c,a);sig=c.Signature;form.ShowSafelyNearMouse();}
 void Execute(ContextAction a){Hide();RhinoApp.InvokeOnUiThread((Action)(()=>RhinoApp.RunScript(a.Macro,false)));}
 public void Hide(){due=DateTime.MaxValue;sig="";if(form is not null){try{form.Close();}catch{}form=null;}}
 public void Dispose(){timer.Stop();if(started){RhinoDoc.SelectObjects-=Changed;RhinoDoc.DeselectObjects-=Changed;RhinoDoc.DeselectAllObjects-=Cleared;RhinoDoc.CloseDocument-=Closed;RhinoDoc.ActiveDocumentChanged-=Closed;RhinoApp.EscapeKeyPressed-=Escape;}Hide();}}
