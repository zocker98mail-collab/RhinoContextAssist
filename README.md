# Rhino Context Assist 2.0.2 Legacy Cleanup Fix

## Ursache

Im Repository liegt noch `ActionService.cs` aus Version 1.5. Die Hybrid-HUD-Version 2.0 verwendet diese Klasse nicht mehr. Die alte Datei erwartet inzwischen entfernte Typen und Eigenschaften:

- `SelectionKind.MultipleEnvelopeObjects`
- `ContextAssistSettings.CustomActions`
- `ActionCatalog.Defaults`
- `SelectionContext.HasOtherBlocks`
- `ContextAction.Requirement`

## Installation

1. `ActionService.cs` aus dem GitHub-Repository löschen.
2. Die vorhandene `RhinoContextAssist.csproj` durch die Datei aus diesem Paket ersetzen.
3. Weitere Dateien laut `DELETE_THESE_LEGACY_FILES.txt` löschen.
4. Committen und GitHub Actions erneut starten.

Die neue Projektdatei schließt alte Dateien zusätzlich vom Build aus, falls eine davon versehentlich im Repository verbleibt.
