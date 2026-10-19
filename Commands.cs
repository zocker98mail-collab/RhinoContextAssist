using Rhino;
using Rhino.Commands;

namespace RhinoContextAssist;

public sealed class ContextAssistAnCommand : Command
{
    public override string EnglishName => "ContextAssistAn";
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

public sealed class ContextAssistAusCommand : Command
{
    public override string EnglishName => "ContextAssistAus";
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

public sealed class ContextAssistUmschaltenCommand : Command
{
    public override string EnglishName => "ContextAssistUmschalten";
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
