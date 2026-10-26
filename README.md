# Rhino Context Assist 1.3.4 Clean Fix

Ersetzt:
- RhinoContextAssist.csproj
- ContextAssistOptionsPage.cs

Zusätzlich in ContextAssistPopup.xaml.cs:
- `Icon` in `ContextIcon` umbenennen.
- Den Aufruf `Icon(context.Kind)` in `ContextIcon(context.Kind)` ändern.

Löschen:
- `/AssemblyInfo.cs`
- `/Properties/AssemblyInfo.cs` oder den kompletten `/Properties`-Ordner, sofern dort nur AssemblyInfo.cs liegt
- `/ActionCatalog-CHANGE.txt`
- `/build-rhino-plugin.yml` in der Repository-Wurzel
- `/manifest.yml` in der Repository-Wurzel
- `/reference-settings.png`
- optional `/README.txt`

Behalten:
- `PluginMetadata.cs`
- `.github/workflows/build-rhino-plugin.yml`
- `package/manifest.yml`
