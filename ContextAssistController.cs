using Eto.Forms; using Rhino; using Rhino.DocObjects;
namespace RhinoContextAssist;
internal sealed class ContextAssistController:IDisposable
{
    readonly UITimer _timer; readonly ContextAssistSettings _settings; ContextToolbarForm? _form; DateTime _due=DateTime.MaxValue; bool _started;
    public bool Enabled{get=>_settings.Enabled;set{_settings.Enabled=value;_settings.Save();}}
    public ContextAssistController(ContextAssistSettings settings){_settings=settings;_timer=new UITimer{Interval=.10};_timer.Elapsed+=(_,_)=>Tick();}
    public void Start(){if(_started)return;_started=true;RhinoDoc.SelectObjects+=Changed;RhinoDoc.DeselectObjects+=Changed;RhinoDoc.DeselectAllObjects+=Cleared;RhinoDoc.CloseDocument+=Closed;RhinoDoc.ActiveDocumentChanged+=Closed;RhinoApp.EscapeKeyPressed+=Escape;_timer.Start();}
    void Changed(object?s,RhinoObjectSelectionEventArgs e){CloseToolbar();if(Enabled)_due=DateTime.UtcNow.AddMilliseconds(160);}void Cleared(object?s,RhinoDeselectAllObjectsEventArgs e)=>Hide();void Closed(object?s,DocumentEventArgs e)=>Hide();void Escape(object?s,EventArgs e)=>Hide();
    void Tick(){var d=RhinoDoc.ActiveDoc;if(!Enabled||d is null||d.IsCommandRunning){Hide();return;}if(_due==DateTime.MaxValue||DateTime.UtcNow<_due)return;_due=DateTime.MaxValue;RefreshNow();}
    public void RefreshNow(){var d=RhinoDoc.ActiveDoc;if(!Enabled||d is null||d.IsCommandRunning){Hide();return;}var c=SelectionAnalyzer.Analyze(d);var a=_settings.Resolve(c.Kind);if(c.Kind==SelectionKind.None||a.Count==0){Hide();return;}CloseToolbar();var f=new ContextToolbarForm(Execute,Hide);f.Closed+=(_,_)=>{if(ReferenceEquals(_form,f))_form=null;};f.UpdateActions(c,a,_settings);_form=f;f.ShowAt(c,_settings.PositionMode);}
    void Execute(ContextAction a){_settings.RecordUsage(a.Id);Hide();RhinoApp.InvokeOnUiThread((Action)(()=>RhinoApp.RunScript(a.Macro,false)));}
    public void Hide(){_due=DateTime.MaxValue;CloseToolbar();}void CloseToolbar(){var f=_form;_form=null;if(f==null)return;try{f.Close();f.Dispose();}catch{}}
    public void Dispose(){_timer.Stop();if(_started){RhinoDoc.SelectObjects-=Changed;RhinoDoc.DeselectObjects-=Changed;RhinoDoc.DeselectAllObjects-=Cleared;RhinoDoc.CloseDocument-=Closed;RhinoDoc.ActiveDocumentChanged-=Closed;RhinoApp.EscapeKeyPressed-=Escape;}CloseToolbar();}
}
