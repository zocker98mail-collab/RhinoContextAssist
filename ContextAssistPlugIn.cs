using Rhino; using Rhino.PlugIns;
namespace RhinoContextAssist;
public sealed class ContextAssistPlugIn:PlugIn { internal static ContextAssistPlugIn? Instance{get;private set;} internal ContextAssistController? Controller{get;private set;} public ContextAssistPlugIn()=>Instance=this; protected override LoadReturnCode OnLoad(ref string e){Controller=new();Controller.Start();RhinoApp.WriteLine("Rhino Context Assist 0.2.1 geladen.");return LoadReturnCode.Success;} protected override void OnShutdown(){Controller?.Dispose();Controller=null;base.OnShutdown();}}
