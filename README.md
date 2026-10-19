# Rhino Context Assist - Testversion 0.1

Automatisch erscheinende, dynamische Mini-Toolbar fuer Rhino 8. Nach einer Auswahl wartet das Plug-in 160 ms, analysiert Objekt oder Brep-Unterobjekt und zeigt passende Rhino-Befehle nahe dem Mauszeiger.

## Voraussetzungen
- Rhino 8
- Windows 10/11 oder macOS (Eto UI; getestet werden muss auf dem Zielsystem)
- .NET 7 SDK zum Bauen
- Internetzugang beim ersten Build fuer das RhinoCommon-NuGet-Paket

## Bauen
1. Terminal im Projektordner oeffnen.
2. `dotnet restore`
3. `dotnet build -c Release`
4. Ergebnis: `bin/Release/net7.0/RhinoContextAssist.rhp`

Alternativ in Visual Studio 2022 die `.csproj` oeffnen und Release bauen.

## Installieren / Testladen
1. Rhino 8 starten.
2. Befehl `PluginManager` ausfuehren.
3. `Installieren...` waehlen.
4. `RhinoContextAssist.rhp` aus `bin/Release/net7.0` waehlen.
5. Rhino neu starten, falls verlangt.

Schneller Entwicklertest: Die `.rhp`-Datei per Drag-and-drop in Rhino ziehen.

## Befehle
- `ContextAssistOn`
- `ContextAssistOff`
- `ContextAssistToggle`

## Test
- Geschlossene planare Kurve waehlen: ExtrudeCrv, PlanarSrf, Offset, Hatch.
- Einzelflaeche waehlen: PushPull/ExtrudeSrf, OffsetSrf, DupBorder.
- Mit Strg+Umschalt eine Brep-Flaeche waehlen: PushPull, MoveFace, Inset, ExtractSrf, DupBorder.
- Mit Strg+Umschalt eine Brep-Kante waehlen: FilletEdge, ChamferEdge, DupEdge.
- Geschlossenen Koerper waehlen: Shell, Boolesche Befehle, Cap.

## Bekannte Grenzen der Testversion
- Die Aktionen starten native Rhino-Befehle; einige verlangen weitere Eingaben.
- `Inset` ist je nach Geometrietyp/Rhino-Befehlskontext eventuell nicht anwendbar.
- Das Popup sitzt beim Mauszeiger, nicht geometrisch am 3D-Selektionspunkt.
- Bei Mehrfachauswahl werden allgemeine Aktionen angeboten.
- Persistente Einstellungen, Favoriten, Icons, Nutzungsstatistik und Yak-Paket folgen erst in einer spaeteren Version.
