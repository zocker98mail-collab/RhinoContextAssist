BUILD-FIX FUER VERSION 0.3

1. ContextAssistOptionsPage.cs im Repository ersetzen.
2. RhinoContextAssist.csproj im Repository ersetzen.
3. Die alte Datei ActionResolver.cs aus dem Repository loeschen.
   Sie stammt aus Version 0.2.x und definiert ContextAction ein zweites Mal.
4. Models.cs NICHT loeschen. Models.cs ist in Version 0.3 die einzige Definition von ContextAction.
5. Committen und GitHub Actions erneut ausfuehren.

Behoben:
- OnApply liefert jetzt bool zurueck.
- Die Hilfsmethode Load wurde in LoadSettingsIntoControls umbenannt.
- UI-Aktualisierungen loesen nicht mehr versehentlich Einstellungs-Aenderungen aus.
- Das Projekt ignoriert vorsichtshalber eine verbliebene ActionResolver.cs beim Kompilieren.
