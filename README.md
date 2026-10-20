# Rhino Context Assist 1.0

## Funktionen
- Icon-Glyphen für alle Aktionen, ohne externe Bildabhängigkeiten.
- Persistente Nutzungsstatistik je Aktion.
- Optionale automatische Priorisierung: Favoriten zuerst, danach Nutzungshäufigkeit, danach manuelle Reihenfolge.
- Mesh-Unterobjekte: Fläche, Kante und Vertex.
- SubD-Unterobjekte: Fläche, Kante und Vertex.
- Optionsseite mit Aktivierung, Position, Hauptanzahl, Reihenfolge, Favoriten und Statistik-Reset.
- Yak-Manifest, lokales Yak-Buildskript und GitHub-Actions-Artefakt.

## GitHub Build
Den Inhalt in die Repository-Wurzel hochladen und den Workflow `Rhino Context Assist 1.0 bauen` ausführen. Das Artefakt enthält die RHP-Dateien und `RhinoContextAssist-1.0.0-rh8-win.yak`.

## Lokales offizielles Yak-Build
Auf einem Windows-PC mit Rhino 8:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\build-yak.ps1
```

Das Skript verwendet `C:\Program Files\Rhino 8\System\yak.exe`.

## Testinstallation
Die `.yak`-Datei auf Rhino ziehen oder über die Rhino-8-Yak-CLI installieren:

```powershell
& "C:\Program Files\Rhino 8\System\yak.exe" install .\RhinoContextAssist-1.0.0-rh8-win.yak
```

## Veröffentlichung im Paketmanager
1. In `package/manifest.yml` die GitHub-URL ersetzen.
2. `yak login` ausführen.
3. Das mit der offiziellen Yak-CLI erzeugte Paket mit `yak push <datei.yak>` hochladen.
4. Danach ist das Plug-in über Rhinos `PackageManager` anhand des Namens `RhinoContextAssist` installierbar.

## Datenschutz
Die Statistik bleibt ausschließlich in Rhinos lokalen Plug-in-Einstellungen. Es werden keine Daten übertragen.
