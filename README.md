# Rhino Context Assist 2.1 – Stable Hybrid

Kontextabhängige Schnellzugriffsleiste für **Rhino 8** auf Basis der bewährten **WPF-Oberfläche**, ergänzt um eine leichte **DisplayConduit-Ankerhilfe**, vollständige Einstellungspersistenz und exakte Viewport-Positionierung.

## Architektur (Stable Hybrid)

1. **WPF (`ContextAssistPopup`) bleibt das alleinige interaktive UI:**
   - Kompakte Pille & erweiterbare Hauptleiste
   - Integriertes `⋯ Mehr`-Popup direkt am Button mit Live-Volltextsuche (deutscher Befehlsname, Beschreibung, Kategorie, englisches Makro)
   - Beschleunigte Lern-Tooltips (`180 ms` InitialDelay, `0 ms` BetweenShowDelay, `12000 ms` ShowDuration)
   - Favoriten-Umschaltung per Rechtsklick (`★`) und automatische Priorisierung nach Nutzungshäufigkeit

2. **`SelectionAnalyzer` (unverändert vollständig):**
   - Erkennt Einzelobjekte, Kurven, geschlossene planare Kurven, Flächen, planare Flächen, offene Flächenverbände, geschlossene Körper, Extrusionen (inkl. `ToBrep()`-Konvertierung für Unterobjekte), Teilflächen (`PlanarBrepFace`, `BrepFace`), `BrepEdge`, `Mesh` (+ Face/Edge/Vertex), `SubD` (+ Face/Edge/Vertex), Blockinstanzen sowie Mehrfach- und gemischte Auswahlen.

3. **Leichter `ContextAssistAnchorConduit`:**
   - Zeichnet **keine** Buttons und führt **kein** Mouse-Hit-Testing durch.
   - Visualisiert optional einen dezenten Ankerpunkt an der 3D-Geometrie sowie optional eine feine Verbindungslinie zur WPF-Toolbar.

4. **Exakte Positionierung & vollständige Persistenz (`2.1`):**
   - Misst nach `UpdateLayout()` die tatsächliche WPF-Größe (`ActualWidth` / `ActualHeight`) statt fester Schätzwerte (`430 × 110 px`).
   - Hält die Toolbar unter Berücksichtigung des eingestellten Randabstands sicher innerhalb des aktiven Viewports.
   - Öffnet das `Mehr`-Menü automatisch nach oben, wenn am unteren Viewportrand zu wenig Platz ist.
   - Verhindert nervöses Springen im Modus *Automatisch freie Ecke* durch eine 65-Pixel-Hysterese.
   - Speichert und lädt **alle** Einstellungen zuverlässig (`Preferences`, `Usage`, `CustomActions`, Zeiten, Deckkraft, Randabstand, Hybrid-Anker).

## Empfohlener Git-Workflow

```bash
# 1. Aktuellen stabilen Stand sichern
git checkout main
git branch stable-wpf
git tag v1.5.3-stable
git push origin stable-wpf --tags

# 2. Neuen Feature-Branch für 2.1 Stable Hybrid anlegen
git checkout -b feature/stable-hybrid-anchor
```
