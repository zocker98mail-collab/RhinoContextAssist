# Rhino Context Assist WPF UI 1.3

## Dateien ersetzen/ergänzen

1. `RhinoContextAssist.csproj` ersetzen.
2. `ContextAssistController.cs` ersetzen.
3. `ContextAssistPopup.xaml` ergänzen.
4. `ContextAssistPopup.xaml.cs` ergänzen.
5. Die alte `ContextToolbarForm.cs` kann gelöscht werden. Die Projektdatei schließt sie vorsichtshalber vom Build aus.
6. Die Eto-Datei `ContextAssistOptionsPage.cs` bleibt unverändert.

## Voraussetzungen

- Rhino 8 für Windows.
- GitHub Actions mit `windows-latest`.
- .NET 7 Windows Desktop Targeting wird durch `UseWPF=true` aktiviert.

## Verhalten

- WPF `SizeToContent=WidthAndHeight` übernimmt die dynamische Fenstergröße.
- `Mehr` ist ein echtes WPF-Popup und vergrößert das Hauptfenster nicht.
- Abgerundete Ecken, Schatten, moderne Hoverzustände und Opazitätsanimationen.
- `ShowActivated=false`, `WS_EX_NOACTIVATE` und `WM_MOUSEACTIVATE/MA_NOACTIVATE` verhindern Fokusübernahme.
- Optionen und persistente Einstellungen bleiben in Eto/Rhino `PersistentSettings`.
- Die bestehende Auswahl-, Statistik-, Favoriten- und Priorisierungslogik bleibt erhalten.
