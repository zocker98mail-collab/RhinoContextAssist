using Rhino;using Rhino.Commands;
namespace RhinoContextAssist;
public sealed class ContextAssistWiederholenCommand:Command{public override string EnglishName=>"ContextAssistWiederholen";protected override Result RunCommand(RhinoDoc d,RunMode m){var a=ContextAssistPlugIn.Instance?.LastAction;if(a==null)return Result.Nothing;RhinoApp.RunScript(a.Macro,false);return Result.Success;}}
public sealed class ContextAssistOptionenCommand:Command{public override string EnglishName=>"ContextAssistOptionen";protected override Result RunCommand(RhinoDoc d,RunMode m){RhinoApp.RunScript("_OptionsPage _Context Assist",false);return Result.Success;}}
