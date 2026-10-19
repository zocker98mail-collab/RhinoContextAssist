using Rhino;
using Rhino.Commands;

namespace RhinoContextAssist;

public sealed class ContextAssistOnCommand : Command
{
    public override string EnglishName => "ContextAssistOn";
    protected override Result RunCommand(RhinoDoc doc, RunMode mode)
    {
        var c = ContextAssistPlugIn.Instance?.Controller;
        if (c is null) return Result.Failure;
        c.Enabled = true;
        c.RefreshNow();
        RhinoApp.WriteLine("Context Assist ist eingeschaltet.");
        return Result.Success;
    }
}

public sealed class ContextAssistOffCommand : Command
{
    public override string EnglishName => "ContextAssistOff";
    protected override Result RunCommand(RhinoDoc doc, RunMode mode)
    {
        var c = ContextAssistPlugIn.Instance?.Controller;
        if (c is null) return Result.Failure;
        c.Enabled = false;
        c.Hide();
        RhinoApp.WriteLine("Context Assist ist ausgeschaltet.");
        return Result.Success;
    }
}

public sealed class ContextAssistToggleCommand : Command
{
    public override string EnglishName => "ContextAssistToggle";
    protected override Result RunCommand(RhinoDoc doc, RunMode mode)
    {
        var c = ContextAssistPlugIn.Instance?.Controller;
        if (c is null) return Result.Failure;
        c.Enabled = !c.Enabled;
        if (c.Enabled) c.RefreshNow(); else c.Hide();
        RhinoApp.WriteLine($"Context Assist ist {(c.Enabled ? "eingeschaltet" : "ausgeschaltet")}.");
        return Result.Success;
    }
}
