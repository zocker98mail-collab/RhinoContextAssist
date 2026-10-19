using Rhino; using Rhino.Commands;
namespace RhinoContextAssist;
public sealed class ContextAssistAnCommand:Command{public override string EnglishName=>"ContextAssistAn";protected override Result RunCommand(RhinoDoc d,RunMode m){var c=ContextAssistPlugIn.Instance?.Controller;if(c==null)return Result.Failure;c.Enabled=true;c.RefreshNow();return Result.Success;}}
public sealed class ContextAssistAusCommand:Command{public override string EnglishName=>"ContextAssistAus";protected override Result RunCommand(RhinoDoc d,RunMode m){var c=ContextAssistPlugIn.Instance?.Controller;if(c==null)return Result.Failure;c.Enabled=false;c.Hide();return Result.Success;}}
public sealed class ContextAssistOptionenCommand:Command{public override string EnglishName=>"ContextAssistOptionen";protected override Result RunCommand(RhinoDoc d,RunMode m){RhinoApp.RunScript("_OptionsPage _Context Assist",false);return Result.Success;}}
