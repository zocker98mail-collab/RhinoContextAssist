# Rhino Context Assist 2.0 Hybrid HUD Preview

## Architektur
- `ContextAssistHudConduit`: zeichnet Kontext, Hauptaktionen und Lern-Tooltip direkt in den aktiven Rhino-Viewport.
- `ContextAssistMouseCallback`: Hover, Linksklick und Rechtsklick/Favorit ohne WPF-Fokus.
- `CommandSearchWindow`: WPF wird nur noch für Suche, lange Listen und Scrollen verwendet.

## Bedienung
- Linksklick auf HUD-Aktion: Befehl ausführen.
- Rechtsklick: Favorit umschalten.
- Hover: Lern-Tooltip nach 180 ms.
- `Mehr`: WPF-Befehlssuche öffnen.
- Doppelklick auf Suchergebnis: ausführen.

## Installation
Repository-Inhalt vollständig ersetzen. Alte Dateien `ContextAssistPopup.xaml`, `ContextAssistPopup.xaml.cs`, `ContextToolbarForm.cs`, `ContextActionPolicy.cs`, `AssemblyInfo.cs` und `Properties/` löschen.
