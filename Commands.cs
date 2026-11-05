using Eto.Forms;
using Rhino;
using Rhino.Commands;
using RhinoCommand = Rhino.Commands.Command;

namespace RhinoContextAssist;

public sealed class ContextAssistToggleCommand : RhinoCommand
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
            plugin.Controller?.HideRequest(true);

        RhinoApp.WriteLine(plugin.SettingsModel.Enabled
            ? "Rhino Context Assist: Aktiviert."
            : "Rhino Context Assist: Deaktiviert.");
        return Result.Success;
    }
}

public sealed class ContextAssistShowCommand : RhinoCommand
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

public sealed class ContextAssistRepeatLastCommand : RhinoCommand
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

public sealed class ContextAssistExportSettingsCommand : RhinoCommand
{
    public override string EnglishName => "ContextAssistExportSettings";

    protected override Result RunCommand(RhinoDoc doc, RunMode mode)
    {
        var plugin = ContextAssistPlugIn.Instance;
        if (plugin == null)
            return Result.Failure;

        using var dialog = new SaveFileDialog
        {
            Title = "Rhino Context Assist Einstellungen exportieren",
            FileName = "RhinoContextAssist-Settings.json"
        };
        dialog.Filters.Add(new FileFilter("JSON-Einstellungsdatei (*.json)", ".json"));

        if (dialog.ShowDialog(Rhino.UI.RhinoEtoApp.MainWindow) != DialogResult.Ok ||
            string.IsNullOrWhiteSpace(dialog.FileName))
        {
            return Result.Cancel;
        }

        plugin.SettingsModel.ExportToFile(dialog.FileName);
        RhinoApp.WriteLine($"Rhino Context Assist: Einstellungen exportiert nach '{dialog.FileName}'.");
        return Result.Success;
    }
}

public sealed class ContextAssistImportSettingsCommand : RhinoCommand
{
    public override string EnglishName => "ContextAssistImportSettings";

    protected override Result RunCommand(RhinoDoc doc, RunMode mode)
    {
        var plugin = ContextAssistPlugIn.Instance;
        if (plugin == null)
            return Result.Failure;

        using var dialog = new OpenFileDialog
        {
            Title = "Rhino Context Assist Einstellungen importieren",
            MultiSelect = false
        };
        dialog.Filters.Add(new FileFilter("JSON-Einstellungsdatei (*.json)", ".json"));

        if (dialog.ShowDialog(Rhino.UI.RhinoEtoApp.MainWindow) != DialogResult.Ok ||
            string.IsNullOrWhiteSpace(dialog.FileName))
        {
            return Result.Cancel;
        }

        if (!plugin.SettingsModel.ImportFromFile(dialog.FileName, out string msg))
        {
            RhinoApp.WriteLine($"Rhino Context Assist: {msg}");
            return Result.Failure;
        }

        plugin.Controller?.RefreshConduitSettings();
        RhinoApp.WriteLine($"Rhino Context Assist: {msg}");
        return Result.Success;
    }
}
