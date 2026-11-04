using Rhino.PlugIns;
using Rhino.UI;

namespace RhinoContextAssist;

public sealed class ContextAssistPlugIn : PlugIn
{
    internal static ContextAssistPlugIn? Instance { get; private set; }
    internal ContextAssistSettings SettingsModel { get; private set; } = null!;
    internal ContextAction? LastAction { get; set; }
    internal ContextAssistController? Controller { get; private set; }

    public ContextAssistPlugIn() => Instance = this;

    protected override LoadReturnCode OnLoad(ref string errorMessage)
    {
        SettingsModel = new ContextAssistSettings(Settings);
        Controller = new ContextAssistController(SettingsModel);
        Controller.Start();
        return LoadReturnCode.Success;
    }

    protected override void OptionsDialogPages(List<OptionsDialogPage> pages) =>
        pages.Add(new ContextAssistOptionsPage(SettingsModel));

    protected override void OnShutdown()
    {
        SettingsModel?.Save();
        Controller?.Dispose();
    }
}
