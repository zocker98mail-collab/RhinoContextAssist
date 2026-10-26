# Rhino Context Assist 1.3.4 Build-Fix

## Ursache
Im Repository werden mindestens zwei Assembly-Metadatendateien kompiliert. Deshalb entstehen doppelte Attribute wie `AssemblyTitle`, `AssemblyProduct`, `Guid` und `ComVisible`.

## Installation
1. `RhinoContextAssist.csproj` ersetzen.
2. `PluginMetadata.cs` in die Repository-Wurzel hochladen.
3. Den kompletten Ordner `Properties` aus dem Repository löschen, sofern er nur `AssemblyInfo.cs` enthält.
4. Zusätzlich jede andere Datei namens `AssemblyInfo.cs` löschen.
5. GitHub Actions erneut starten.

Die Projektdatei schließt vorsichtshalber alle verbliebenen `AssemblyInfo.cs` vom Build aus.

## Warnungen
Die Warnungen zu `Load()` und `Icon()` verhindern den Build nicht. Optional:
- `ContextAssistOptionsControl.Load()` in `LoadSettingsIntoControls()` umbenennen.
- `ContextAssistPopup.Icon(...)` in `ContextIcon(...)` umbenennen.
