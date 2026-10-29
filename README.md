# Rhino Context Assist 1.5

Vollständiges Projekt mit Kontextstufen, Smart Actions, Favoriten, erweiterten Unterobjektaktionen und Befehlssuche.

## Neu
- Kontextstufen: planar, offen, geschlossen, Unterobjekt, Mehrfachauswahl, Blockinstanz.
- Smart Actions werden nur angezeigt, wenn die aktuelle Auswahl die Voraussetzung erfüllt.
- Favorit per Rechtsklick auf eine Aktion oder in den Optionen umschalten.
- Erweiterte Brep-, Mesh- und SubD-Unterobjektaktionen.
- Suchfeld im Mehr-Menü durchsucht Beschriftung, Hinweis und Kategorie.

## Repository
Vor Upload alte Dateien vollständig ersetzen. Insbesondere löschen: ContextActionPolicy.cs, ContextToolbarForm.cs, AssemblyInfo.cs, Properties/, build-rhino-plugin.yml und manifest.yml in der Repository-Wurzel. Behalten: .github/workflows/build-rhino-plugin.yml und package/manifest.yml.
