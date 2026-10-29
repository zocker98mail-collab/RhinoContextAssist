using Rhino;using Rhino.Commands;
namespace RhinoContextAssist;
public sealed class ContextAssistOptionenCommand:Command{public override string EnglishName=>"ContextAssistOptionen";protected override Result RunCommand(RhinoDoc d,RunMode m){RhinoApp.RunScript("_OptionsPage _Context Assist HUD",false);return Result.Success;}}
