using Rhino;
using Rhino.PlugIns;

namespace RhinoContextAssist;

public sealed class ContextAssistPlugIn : PlugIn
{
    internal static ContextAssistPlugIn? Instance { get; private set; }
    private ContextAssistController? _controller;

    public ContextAssistPlugIn() => Instance = this;

    protected override LoadReturnCode OnLoad(ref string errorMessage)
    {
        _controller = new ContextAssistController();
        _controller.Start();
        RhinoApp.WriteLine("Rhino Context Assist v0.1 geladen. Befehle: ContextAssistOn, ContextAssistOff, ContextAssistToggle.");
        return LoadReturnCode.Success;
    }

    protected override void OnShutdown()
    {
        _controller?.Dispose();
        _controller = null;
        base.OnShutdown();
    }

    internal ContextAssistController? Controller => _controller;
}
