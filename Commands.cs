using Rhino;
using Rhino.Commands;

namespace RhinoContextAssist;

public sealed class ContextAssistToggleCommand : Command
{
    public override string EnglishName => "ContextAssistToggle";

    protected override Result RunCommand(RhinoDoc doc, RunMode mode)
    {
        var plugin = ContextAssistPlugIn.Instance;
        if (plugin == null)
            return Result.Failure;

        plugin.SettingsModel.Enabled = !plugin.SettingsModel.Enabled;
        plugin.SettingsModel.Save();

        if (!plugin.SettingsModel.Enabled)
            plugin.Controller?.Hide();

        RhinoApp.WriteLine(plugin.SettingsModel.Enabled
            ? "Rhino Context Assist: Aktiviert."
            : "Rhino Context Assist: Deaktiviert.");
        return Result.Success;
    }
}

public sealed class ContextAssistShowCommand : Command
{
    public override string EnglishName => "ContextAssistShow";

    protected override Result RunCommand(RhinoDoc doc, RunMode mode)
    {
        var plugin = ContextAssistPlugIn.Instance;
        if (plugin?.Controller == null)
            return Result.Failure;

        plugin.Controller.ShowNow();
        return Result.Success;
    }
}

public sealed class ContextAssistRepeatLastCommand : Command
{
    public override string EnglishName => "ContextAssistRepeatLast";

    protected override Result RunCommand(RhinoDoc doc, RunMode mode)
    {
        var last = ContextAssistPlugIn.Instance?.LastAction;
        if (last == null)
        {
            RhinoApp.WriteLine("Rhino Context Assist: Noch keine Aktion in dieser Sitzung ausgeführt.");
            return Result.Nothing;
        }

        ContextAssistPlugIn.Instance!.SettingsModel.RecordUsage(last.Id);
        RhinoApp.RunScript(last.Macro, false);
        return Result.Success;
    }
}
