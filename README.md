# Rhino Context Assist 0.2

## Änderungen
- Vollständig deutsche Beschriftungen und Tooltips, interne Rhino-Makros bleiben sprachunabhängig englisch mit `_`.
- Popup bleibt innerhalb des Arbeitsbereichs des aktuellen Monitors.
- Ein einziges Popup wird wiederverwendet; weniger Flackern und keine ständig neuen Fenster.
- Automatisches Ausblenden bei Abwahl, Escape, Dokumentwechsel, Dokumentende und laufenden Rhino-Befehlen/Punktabfragen.
- Befehlsliste auf sinnvolle, üblicherweise mit Vorauswahl verwendbare Befehle reduziert. `Inset` und `Shell` wurden vorerst entfernt, weil das Verhalten je nach Auswahlkontext uneinheitlich ist.
- Deutsche Steuerbefehle: `ContextAssistAn`, `ContextAssistAus`, `ContextAssistUmschalten`.
- GitHub-Actions-Workflow ist enthalten.

## Online kompilieren
1. Den Inhalt dieses Ordners in die oberste Ebene eines GitHub-Repositorys hochladen.
2. Unter Actions den Workflow `Rhino Context Assist bauen` öffnen.
3. `Run workflow` klicken oder durch einen Commit automatisch starten lassen.
4. Nach erfolgreichem Build das Artefakt `RhinoContextAssist-0.2` herunterladen.
5. ZIP entpacken und `RhinoContextAssist.rhp` per Drag-and-drop oder über `PluginManager` in Rhino 8 installieren.

## Testfälle
- Geschlossene planare Kurve: Kurve extrudieren, Fläche erzeugen, Kurve versetzen, Schraffieren.
- Planare Einzelfläche: Drücken/Ziehen, Fläche extrudieren, Fläche versetzen, Rand duplizieren.
- Strg+Umschalt auf Brep-Fläche: Drücken/Ziehen, Fläche verschieben, Fläche lösen, Rand duplizieren.
- Strg+Umschalt auf Brep-Kante: Kante abrunden, Kante fasen, Kante duplizieren.
- Escape, Abwahl und Start eines Rhino-Befehls blenden das Popup aus.
- Auswahl nahe allen Bildschirmrändern und auf einem zweiten Monitor testen.

## Hinweis zur Befehlsausführung
Die sichtbaren Namen sind deutsch. Die Makros bleiben absichtlich `_PushPull`, `_Offset` usw. Der führende Unterstrich weist Rhino an, den internationalen englischen Befehlsnamen zu verwenden. Dadurch funktioniert das Plugin auch mit einer deutschen Rhino-Oberfläche.
