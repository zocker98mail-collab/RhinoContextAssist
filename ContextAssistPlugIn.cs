using Rhino; using Rhino.PlugIns; using Rhino.UI;
namespace RhinoContextAssist;
public sealed class ContextAssistPlugIn:PlugIn{
 internal static ContextAssistPlugIn? Instance{get;private set;} internal ContextAssistController? Controller{get;private set;} internal ContextAssistSettings SettingsModel{get;private set;}=null!;
 public ContextAssistPlugIn()=>Instance=this;
 protected override LoadReturnCode OnLoad(ref string e){SettingsModel=new ContextAssistSettings(Settings);Controller=new(SettingsModel);Controller.Start();RhinoApp.WriteLine("Rhino Context Assist 0.3 geladen. Optionen: ContextAssistOptionen.");return LoadReturnCode.Success;}
 protected override void OptionsDialogPages(List<OptionsDialogPage> pages)=>pages.Add(new ContextAssistOptionsPage(SettingsModel));
 protected override void OnShutdown(){Controller?.Dispose();Controller=null;base.OnShutdown();}}
