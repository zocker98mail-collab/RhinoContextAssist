# Rhino Context Assist 2.2 – Stable Hybrid

Kontextabhängige Schnellzugriffsleiste für **Rhino 8** auf Basis der bewährten **WPF-Oberfläche**, ergänzt um eine leichte **DisplayConduit-Ankerhilfe**, Live-Kamera-Mitbewegung, magnetisches Edge-Snapping, vollständige Einstellungspersistenz sowie **Export & Import per JSON**.

## Neue Funktionen in 2.2 Stable Hybrid

1. **Hybrid-Conduit & Live-Kamera-Tracking:**
   - **`FollowCameraMovement`:** Bewegt die WPF-Toolbar bei Kamera-Rotation, Panning und Zoom flüssig mit der 3D-Geometrie mit.
   - **`AutoHideWhenAnchorOffscreen`:** Blendet die Toolbar sanft aus (`Opacity -> 0`), sobald die Auswahl aus dem aktiven Viewport gedreht wird, und blendet sie automatisch wieder ein, sobald die Auswahl zurück im Sichtfeld ist.
   - **Anpassbare Verbindungslinie:** Deckkraft (`TetherLineOpacity`) und Linienstärke (`TetherLineThickness`) lassen sich in den Rhino-Optionen unter *Hybrid & Viewport* feinjustieren.
   - **Dynamischer Geometrie-Abstand (`DynamicOffsetPx`):** Positioniert die Toolbar im Modus *Mit Abstand zur Geometrie* automatisch in Richtung der freieren Viewport-Hälfte.
   - **Magnetisches Edge-Snapping (`EnableEdgeSnapping` & `EdgeSnapDistancePx`):** Beim Verschieben per Drag & Drop am Kopfbereich oder bei der Positionierung rastet die Toolbar sauber an den Viewport-Rändern ein.

2. **Einstellungen Exportieren & Importieren (PC-Transfer):**
   - Über die Rhino-Optionen (*Context Assist -> Aktionen & Transfer*) oder direkt über die Befehle `ContextAssistExportSettings` und `ContextAssistImportSettings` lassen sich sämtliche Einstellungen, Favoriten (`★`), manuelle Aktionsreihenfolgen, eigene Aktionen und Nutzungsstatistiken als `.json`-Datei exportieren und auf andere PCs übertragen.
