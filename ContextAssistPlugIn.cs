using Rhino;
using Rhino.PlugIns;
using Rhino.UI;
namespace RhinoContextAssist;
public sealed class ContextAssistPlugIn : PlugIn
{
    internal static ContextAssistPlugIn? Instance { get; private set; }
    internal ContextAssistController? Controller { get; private set; }
    internal ContextAssistSettings SettingsModel { get; private set; } = null!;
    public ContextAssistPlugIn() => Instance = this;
    protected override LoadReturnCode OnLoad(ref string errorMessage)
    {
        SettingsModel = new ContextAssistSettings(Settings);
        Controller = new ContextAssistController(SettingsModel);
        Controller.Start();
        RhinoApp.WriteLine("Rhino Context Assist 1.3.3 geladen.");
        return LoadReturnCode.Success;
    }
    protected override void OptionsDialogPages(List<OptionsDialogPage> pages) => pages.Add(new ContextAssistOptionsPage(SettingsModel));
    protected override void OnShutdown() { Controller?.Dispose(); Controller = null; base.OnShutdown(); }
}
