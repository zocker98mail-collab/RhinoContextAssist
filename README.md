# Rhino Context Assist WPF 1.3.2

## Standardwerte für Neuinstallationen
- Aktiviert: Ja
- Automatische Priorisierung: Ja
- Darstellung: Kompakte Pille, bei Hover erweitern
- Position: Neben Mauszeiger
- Hauptaktionen: 5
- Auswahlverzögerung: 300 ms
- Hover: 300 ms
- Einklappen: 600 ms
- Einklappen bei offenem Mehr: 1500 ms
- Auto-Hide: 5000 ms
- Nähe: 0 px (deaktiviert)
- Cursorabstand: 5 px
- Ruhedeckkraft: 42 %
- Hoverdeckkraft: 100 %

Bereits gespeicherte Einstellungen werden absichtlich nicht überschrieben.

## X-Position
`ContextAssistPopup.xaml` verwendet im Kopfbereich ein Grid mit `*` und `Auto`. Der Schließen-Button bleibt dadurch rechts oben.

## Linien-Hüllkörper
`Models.cs` und `SelectionAnalyzer.cs` erkennen mehrere ausschließlich ausgewählte Körper/Flächenverbände als `MultipleBodies`.
Die Datei `ActionCatalog-CHANGE.txt` beschreibt die erforderliche kleine Änderung an deinem bestehenden ActionCatalog, damit die Aktion nur bei `SolidBrep`, `Brep` und `MultipleBodies` erscheint.
