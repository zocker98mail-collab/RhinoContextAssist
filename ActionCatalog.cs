namespace RhinoContextAssist;

internal static class ActionCatalog
{
    internal static readonly SelectionKind[] Kinds = Enum.GetValues<SelectionKind>()
        .Where(kind => kind != SelectionKind.None && kind != SelectionKind.MultipleEnvelopeObjects)
        .ToArray();

    private static ContextAction A(
        string id, string icon, string germanCommand, string macro,
        string description, string category = "Bearbeiten",
        bool destructive = false, string requirement = "Any",
        bool secondary = false) =>
        new(id, icon, germanCommand, macro, description, category, destructive, requirement, false, secondary);

    internal static IReadOnlyList<ContextAction> Defaults(SelectionKind kind) => kind switch
    {
        SelectionKind.PlanarBrepFace => FaceActions(true),
        SelectionKind.BrepFace => FaceActions(false),

        SelectionKind.BrepEdge => new[]
        {
            A("fillet", "⌒", "KantenVerrunden", "_FilletEdge", "Ausgewählte Kante abrunden."),
            A("chamfer", "◩", "KantenFasen", "_ChamferEdge", "Ausgewählte Kante fasen."),
            A("dupedge", "⧉", "KanteDuplizieren", "_DupEdge", "Ausgewählte Kante als Kurve duplizieren.", "Erzeugen"),
            A("blendedge", "∿", "KantenÜberblenden", "_BlendEdge", "Glatte G2-Flächenüberblendung entlang der Kante erzeugen.", "Bearbeiten", secondary: true),
            A("pipe_edge", "◉", "RohrAnKante", "_Pipe", "Rohr entlang der ausgewählten Kante erzeugen.", "Erzeugen", secondary: true),
            A("unjoin", "✂", "KanteTrennen", "_UnjoinEdge", "Verbundene Kante auftrennen.", "Bearbeiten", secondary: true)
        },

        SelectionKind.SolidBrep => new[]
        {
            A("fillet", "⌒", "KantenVerrunden", "_FilletEdge", "Kanten des Körpers auswählen und abrunden."),
            A("chamfer", "◩", "KantenFasen", "_ChamferEdge", "Kanten des Körpers auswählen und fasen."),
            A("shell", "◫", "Schale", "_Shell", "Aus dem Volumenkörper einen Hohlkörper erzeugen."),
            A("offsetsrf", "⇆", "Parallelfläche", "_OffsetSrf", "Flächen des Körpers parallel versetzen."),
            A("extractsrf", "⇱", "FlächeLösen", "_ExtractSrf", "Ausgewählte Teilflächen aus dem Körper lösen."),
            A("volume", "V", "Volumen", "_Volume", "Volumen des Körpers ermitteln.", "Analyse"),
            A("area", "A", "Flächeninhalt", "_Area", "Oberflächeninhalt ermitteln.", "Analyse"),
            A("check", "✓", "Prüfen", "_Check", "Objekt auf geometrische Fehler prüfen.", "Analyse"),
            // Erweiterte Funktionen (unter "⋯ Mehr")
            A("wirecut", "✀", "Drahtschneiden", "_WireCut", "Volumenkörper mit einer Leitkurve durchschneiden.", "Bearbeiten", secondary: true),
            A("booleansplit", "◪", "BooleschesTeilen", "_BooleanSplit", "Körper mit Flächen oder anderen Körpern teilen.", "Bearbeiten", secondary: true),
            A("mergeallfaces", "▤", "AlleKoplanarenSeitenVereinigen", "_MergeAllCoplanarFaces", "Alle koplanaren Teilflächen des Körpers verschmelzen.", "Smart", secondary: true),
            A("dupfaceborder", "⧉", "FlächenrandDuplizieren", "_DupFaceBorder", "Randkurven von Teilflächen extrahieren.", "Erzeugen", secondary: true),
            A(" contour", "▤", "Kontur", "_Contour", "Konturschnitte in gleichmäßigen Abständen durch den Körper legen.", "Erzeugen", secondary: true),
            A("section", "⊘", "Schnitt", "_Section", "Ebene Schnittkurve durch das Objekt legen.", "Erzeugen", secondary: true),
            A("make2d", "◰", "2DZeichnung", "_Make2D", "2D-Linienzeichnung aus der aktuellen Ansicht ableiten.", "Dokumentation", secondary: true),
            A("boxedit", "⊞", "BoxEdit", "_BoxEdit", "Numerische Größe, Position und Skalierung im BoxEdit-Panel bearbeiten.", "Transformieren", secondary: true)
        },

        SelectionKind.Brep => new[]
        {
            A("cap", "▰", "PlanareÖffnungenSchließen", "_Cap", "Planare Öffnungen des Flächenverbands schließen.", "Smart", false, "Open"),
            A("merge", "▤", "AlleKoplanarenSeitenVereinigen", "_MergeAllCoplanarFaces", "Koplanare Seiten des Flächenverbands vereinigen.", "Smart", false, "MultipleFaces"),
            A("fillet", "⌒", "KantenVerrunden", "_FilletEdge", "Kanten des Flächenverbands abrunden."),
            A("chamfer", "◩", "KantenFasen", "_ChamferEdge", "Kanten des Flächenverbands fasen."),
            A("offsetsrf", "⇆", "Parallelfläche", "_OffsetSrf", "Flächen parallel versetzen."),
            A("shownaked", "⊠", "KantenAnzeigen", "_ShowEdges _NakedEdges", "Offene Kanten des Flächenverbands anzeigen.", "Analyse"),
            A("explode", "⤢", "Explodieren", "_Explode", "Flächenverband in einzelne Flächen zerlegen.", "Kritisch", true),
            // Erweiterte Funktionen (unter "⋯ Mehr")
            A("unrollsrf", "◱", "FlächeAbwickeln", "_UnrollSrf", "Abwickelbare Fläche flach auf die Konstruktionsebene legen.", "Erzeugen", secondary: true),
            A("dupborder", "⧉", "RandDuplizieren", "_DupBorder", "Außenränder des Flächenverbands als Kurven duplizieren.", "Erzeugen", secondary: true),
            A(" flip", "⇄", "RichtungUmkehren", "_Flip", "Normalenrichtung des Flächenverbands umkehren.", "Bearbeiten", secondary: true),
            A("contour", "▤", "Kontur", "_Contour", "Parallele Schnittkurven erzeugen.", "Erzeugen", secondary: true),
            A("make2d", "◰", "2DZeichnung", "_Make2D", "2D-Projektionszeichnung ableiten.", "Dokumentation", secondary: true)
        },

        SelectionKind.ClosedPlanarCurve => new[]
        {
            A("solidextrude", "⬒", "KurveExtrudieren", "_ExtrudeCrv _Solid=_Yes", "Geschlossene Kurve als Volumenkörper extrudieren.", "Erzeugen"),
            A("planarsrf", "▰", "PlanareFläche", "_PlanarSrf", "Aus der geschlossenen Kurve eine planare Fläche erzeugen.", "Erzeugen"),
            A("pipe", "◉", "Rohr", "_Pipe", "Rohr entlang der Kurve erzeugen.", "Erzeugen"),
            A("offset", "⇆", "Versetzen", "_Offset", "Parallele Kurve in einem Abstand erzeugen."),
            A("hatch", "▧", "Schraffierung", "_Hatch", "Schraffierung innerhalb der Kurve erzeugen.", "Dokumentation"),
            // Erweiterte Kurven-/Flächenfunktionen (unter "⋯ Mehr")
            A("loft", "∿", "Loft", "_Loft", "Fläche durch ausgewählte Profilkurven spannen.", "Erzeugen", secondary: true),
            A("sweep1", "↝", "Aufziehen1Leitkurve", "_Sweep1", "Fläche entlang einer Leitkurve (Sweep 1) aufziehen.", "Erzeugen", secondary: true),
            A("sweep2", "⇉", "Aufziehen2Leitkurven", "_Sweep2", "Fläche entlang zweier Leitkurven (Sweep 2) aufziehen.", "Erzeugen", secondary: true),
            A("revolve", "↻", "Rotationsfläche", "_Revolve", "Kurve um eine Achse zu einer Fläche rotieren.", "Erzeugen", secondary: true),
            A("ribbon", "═", "Band", "_Ribbon", "Flaches Band aus der Kurve versetzen.", "Erzeugen", secondary: true),
            A("filletcorners", "⌒", "EckenVerrunden", "_FilletCorners", "Alle Knickpunkte der Polykurve gleichzeitig abrunden.", "Bearbeiten", secondary: true),
            A("curveboolean", "∪", "KurvenBoolesch", "_CurveBoolean", "Überlappende Kurvenbereiche interaktiv kombinieren.", "Bearbeiten", secondary: true),
            A("simplifycrv", "⚡", "KurveVereinfachen", "_SimplifyCrv", "Kollineare Segmente und Kreisbögen vereinfachen.", "Bearbeiten", secondary: true),
            A("divide", "⋯", "Teilen (Punkte)", "_Divide", "Kurve in gleichmäßige Segmente/Punkte unterteilen.", "Erzeugen", secondary: true),
            A("area", "A", "Flächeninhalt", "_Area", "Flächeninhalt der geschlossenen Kurve berechnen.", "Analyse", secondary: true)
        },

        SelectionKind.Curve => new[]
        {
            A("offset", "⇆", "Versetzen", "_Offset", "Parallele Kurve in einem Abstand erzeugen."),
            A("pipe", "◉", "Rohr", "_Pipe", "Rohr entlang der Kurve erzeugen.", "Erzeugen"),
            A("extrude", "⇧", "KurveExtrudieren", "_ExtrudeCrv", "Kurve zu einer Fläche extrudieren.", "Erzeugen"),
            A("rebuild", "⟳", "NeuAufbauen", "_Rebuild", "Kurve mit neuer Punkt- und Gradverteilung neu aufbauen."),
            A("length", "↔", "Länge", "_Length", "Länge der Kurve ermitteln.", "Analyse"),
            A("dir", "➜", "Richtung", "_Dir", "Richtung der Kurve anzeigen oder umkehren.", "Analyse"),
            // Erweiterte Kurven-/Flächenfunktionen (unter "⋯ Mehr")
            A("loft", "∿", "Loft", "_Loft", "Fläche durch ausgewählte Kurven spannen.", "Erzeugen", secondary: true),
            A("sweep1", "↝", "Aufziehen1Leitkurve", "_Sweep1", "Profilkurven entlang einer Leitkurve (Sweep 1) aufziehen.", "Erzeugen", secondary: true),
            A("sweep2", "⇉", "Aufziehen2Leitkurven", "_Sweep2", "Profilkurven entlang zweier Leitkurven (Sweep 2) aufziehen.", "Erzeugen", secondary: true),
            A("revolve", "↻", "Rotationsfläche", "_Revolve", "Kurve um eine Rotationsachse drehen.", "Erzeugen", secondary: true),
            A("networksrf", "🕸", "FlächeAusKurvennetzwerk", "_NetworkSrf", "Fläche aus einem sich kreuzenden Kurvennetzwerk erzeugen.", "Erzeugen", secondary: true),
            A("extrudecrvtapered", "⏢", "KurveVerjüngtExtrudieren", "_ExtrudeCrvTapered", "Kurve mit Entformungsschräge/Winkel extrudieren.", "Erzeugen", secondary: true),
            A("blendcrv", "∿", "KurvenÜberblenden", "_BlendCrv", "Glatte Übergangskurve (G1/G2) zwischen Kurven erzeugen.", "Bearbeiten", secondary: true),
            A("filletcrv", "⌒", "KurvenVerrunden", "_Fillet", "Zwei Kurven mit einem Radius verbinden.", "Bearbeiten", secondary: true),
            A("chamfercrv", "◩", "KurvenFasen", "_Chamfer", "Zwei Kurven mit einer Fase verbinden.", "Bearbeiten", secondary: true),
            A("closecrv", "⬡", "OffeneKurveSchließen", "_CloseCrv", "Offene Kurve durch ein Segment oder Verschieben schließen.", "Bearbeiten", secondary: true),
            A("extend", "⤻", "KurveVerlängern", "_Extend", "Kurve bis zu einer Begrenzung oder um eine Länge verlängern.", "Bearbeiten", secondary: true),
            A("project", "⇓", "AufFlächeProjizieren", "_Project", "Kurve auf Flächen oder Meshes projizieren.", "Erzeugen", secondary: true),
            A("pull", " magnet", "AufFlächeZiehen", "_Pull", "Kurve entlang der Flächennormalen auf eine Fläche ziehen.", "Erzeugen", secondary: true),
            A("divide", "⋯", "Teilen (Punkte)", "_Divide", "Kurve in gleich lange Abschnitte unterteilen.", "Erzeugen", secondary: true),
            A("simplifycrv", "⚡", "KurveVereinfachen", "_SimplifyCrv", "Kurvengeometrie bereinigen und vereinfachen.", "Bearbeiten", secondary: true)
        },

        SelectionKind.PlanarSurface => SurfaceActions(true),
        SelectionKind.Surface => SurfaceActions(false),

        SelectionKind.Mesh => new[]
        {
            A("offsetmesh", "⇆", "PolygonnetzVersetzen", "_OffsetMesh", "Polygonnetz parallel versetzen."),
            A("reduce", "▽", "PolygonnetzReduzieren", "_ReduceMesh", "Polygonanzahl des Polygonnetzes reduzieren."),
            A("quad", "▦", "QuadRemesh", "_QuadRemesh", "Quad-basiertes Polygonnetz erzeugen.", "Konvertieren"),
            A("weld", "⛓", "Verschweißen", "_Weld", "Polygonnetzpunkte entlang gemeinsamer Kanten verschweißen."),
            A("unify", "⇧", "PolygonnetzNormalenVereinheitlichen", "_UnifyMeshNormals", "Normalenrichtungen vereinheitlichen."),
            A("check", "✓", "Prüfen", "_Check", "Polygonnetz auf Fehler prüfen.", "Analyse"),
            // Erweiterte Funktionen (unter "⋯ Mehr")
            A("meshtonurb", "▰", "InNURBSUmwandeln", "_MeshToNURB", "Polygonnetz in einen NURBS-Flächenverband umwandeln.", "Konvertieren", secondary: true),
            A("shrinkwrap", "📦", "ShrinkWrap", "_ShrinkWrap", "Wasserdichtes Polygonnetz um die Geometrie hüllen (Rhino 8).", "Erzeugen", secondary: true),
            A("fillmeshholes", "▰", "AlleLöcherSchließen", "_FillMeshHoles", "Alle offenen Löcher im Polygonnetz füllen.", "Bearbeiten", secondary: true),
            A("meshoutline", "⧉", "UmrisskurveErzeugen", "_MeshOutline", "2D-Silhouettenkurve aus der aktuellen Ansicht erzeugen.", "Erzeugen", secondary: true),
            A("splitdisjointmesh", "⤢", "NichtVerbundeneTeileTrennen", "_SplitDisjointMesh", "Nicht zusammenhängende Mesh-Inseln in Einzelobjekte trennen.", "Bearbeiten", secondary: true)
        },

        SelectionKind.MeshFace => new[]
        {
            A("inset", "▣", "Inset", "_Inset", "Kanten der ausgewählten Polygonnetzfläche nach innen versetzen."),
            A("extrude", "⇧", "PolygonnetzSeitenExtrudieren", "_ExtrudeMeshFaces", "Ausgewählte Polygonnetzfläche extrudieren."),
            A("extract", "⇱", "PolygonnetzSeitenExtrahieren", "_ExtractMeshFaces", "Ausgewählte Polygonnetzfläche lösen."),
            A("delete", "⌫", "PolygonnetzSeitenLöschen", "_DeleteMeshFaces", "Ausgewählte Polygonnetzfläche löschen.", "Kritisch", true)
        },

        SelectionKind.MeshEdge => new[]
        {
            A("split", "✂", "PolygonnetzKanteTeilen", "_SplitMeshEdge", "Ausgewählte Polygonnetzkante teilen."),
            A("swap", "⇄", "PolygonnetzKanteTauschen", "_SwapMeshEdge", "Diagonale einer Polygonnetzfläche tauschen."),
            A("weld", "⛓", "Verschweißen", "_Weld", "Polygonnetz entlang gemeinsamer Kanten verschweißen.")
        },

        SelectionKind.MeshVertex => new[]
        {
            A("move", "✥", "Verschieben", "_Move", "Ausgewählten Polygonnetzpunkt verschieben."),
            A("smooth", "≈", "Glätten", "_Smooth", "Ausgewählte Polygonnetzpunkte glätten."),
            A("align", "≡", "Ausrichten", "_Align", "Ausgewählte Punkte ausrichten.")
        },

        SelectionKind.SubD => new[]
        {
            A("extrude", "⇧", "SubDExtrudieren", "_ExtrudeSubD", "SubD-Komponenten extrudieren."),
            A("insert", "＋", "KanteEinfügen", "_InsertEdge", "Kante in das SubD einfügen."),
            A("crease", "⌁", "Falzen", "_Crease", "SubD-Kanten oder Punkte schärfen."),
            A("smooth", "⌒", "FalzEntfernen", "_RemoveCrease", "Schärfung von SubD-Kanten oder Punkten entfernen."),
            A("reflect", "◇", "Reflektieren", "_Reflect", "Symmetrische SubD-Struktur erzeugen."),
            A("tonurbs", "▰", "InNURBSUmwandeln", "_ToNURBS", "SubD in NURBS-Flächen umwandeln.", "Konvertieren"),
            // Erweiterte Funktionen (unter "⋯ Mehr")
            A("offsetsud", "⇆", "SubDVersetzen", "_OffsetSubD", "SubD parallel versetzen oder zu einem Körper verdicken.", "Erzeugen", secondary: true),
            A("bridge", "≍", "Überbrücken", "_Bridge", "Ausgewählte SubD-Kanten oder -Seiten miteinander verbinden.", "Bearbeiten", secondary: true),
            A("subdivide", "▦", "Unterteilen", "_Subdivide", "SubD-Flächen feiner unterteilen.", "Bearbeiten", secondary: true)
        },

        SelectionKind.SubDFace => new[]
        {
            A("extrude", "⇧", "SubDExtrudieren", "_ExtrudeSubD", "Ausgewählte SubD-Seite extrudieren."),
            A("inset", "▣", "Inset", "_Inset", "Kanten der ausgewählten SubD-Seite nach innen versetzen."),
            A("delete", "⌫", "Löschen", "_Delete", "Ausgewählte SubD-Seite löschen.", "Kritisch", true),
            A("bridge", "≍", "Überbrücken", "_Bridge", "Mit einer anderen SubD-Seite überbrücken.", "Bearbeiten", secondary: true)
        },

        SelectionKind.SubDEdge => new[]
        {
            A("crease", "⌁", "Falzen", "_Crease", "Ausgewählte SubD-Kante schärfen."),
            A("smooth", "⌒", "FalzEntfernen", "_RemoveCrease", "Schärfung der SubD-Kante entfernen."),
            A("insert", "＋", "KanteEinfügen", "_InsertEdge", "Zusätzliche SubD-Kante einsetzen."),
            A("bridge", "≍", "Überbrücken", "_Bridge", "Kanten miteinander überbrücken.", "Bearbeiten", secondary: true),
            A("slide", "↔", "VerschiebenAufSubD", "_Slide", "Kante entlang der SubD-Topologie gleiten lassen.", "Bearbeiten", secondary: true)
        },

        SelectionKind.SubDVertex => new[]
        {
            A("move", "✥", "Verschieben", "_Move", "Ausgewählten SubD-Punkt verschieben."),
            A("crease", "◆", "Falzen", "_Crease", "Ausgewählten SubD-Punkt schärfen."),
            A("smooth", "◇", "FalzEntfernen", "_RemoveCrease", "_RemoveCrease")
        },

        SelectionKind.Block => new[]
        {
            A("unique", "◆", "EinzigartigenBlockErzeugen", "_CreateUniqueBlock", "Eigene Blockdefinition aus der Instanz erzeugen.", "Block"),
            A("edit", "✎", "BlockBearbeiten", "_BlockEditNew", "Blockdefinition bearbeiten (BlockEditNew).", "Block"),
            A("replace", "⇄", "BlockErsetzen", "_ReplaceBlock", "Blockinstanz durch eine andere Definition ersetzen.", "Block", false, "OtherBlocks"),
            A("same", "▦", "BlockinstanzenAuswählen", "_SelBlockInstanceNamed", "Instanzen derselben Blockdefinition auswählen.", "Block"),
            A("manager", "☷", "BlockManager", "_BlockManager", "Blockmanager öffnen.", "Block"),
            A("add", "＋", "ObjekteZuBlockHinzufügen", "_AddObjectsToBlock", "Objekte zur Blockdefinition hinzufügen.", "Block"),
            A("reset", "1:1", "BlockMaßstabZurücksetzen", "_BlockResetScale", "Skalierung der Blockinstanz zurücksetzen.", "Block"),
            A("explode", "⤢", "BlockExplodieren", "_ExplodeBlock", "Blockinstanz vollständig zerlegen.", "Kritisch", true)
        },

        SelectionKind.Multiple or SelectionKind.Mixed => new[]
        {
            A("group", "▦", "Gruppieren", "_Group", "Ausgewählte Objekte gruppieren."),
            A("align", "≡", "Ausrichten", "_Align", "Ausgewählte Objekte ausrichten."),
            A("join", "⛓", "Verbinden", "_Join", "Ausgewählte Kurven oder Flächen verbinden."),
            A("union", "∪", "BoolescheVereinigung", "_BooleanUnion", "Ausgewählte Körper vereinigen.", "Smart", false, "Multiple"),
            A("difference", "−", "BoolescheDifferenz", "_BooleanDifference", "Körper voneinander abziehen.", "Kritisch", true, "Multiple"),
            A("intersection", "∩", "BoolescheSchnittmenge", "_BooleanIntersection", "Gemeinsames Volumen erzeugen.", "Smart", false, "Multiple"),
            // Erweiterte Mehrfach-/Kurven-Funktionen (unter "⋯ Mehr")
            A("loft", "∿", "Loft", "_Loft", "Fläche durch ausgewählte Kurven spannen.", "Erzeugen", false, "Multiple", secondary: true),
            A("sweep1", "↝", "Aufziehen1Leitkurve", "_Sweep1", "Fläche entlang einer Leitkurve (Sweep 1) aufziehen.", "Erzeugen", false, "Multiple", secondary: true),
            A("sweep2", "⇉", "Aufziehen2Leitkurven", "_Sweep2", "Fläche entlang zweier Leitkurven (Sweep 2) aufziehen.", "Erzeugen", false, "Multiple", secondary: true),
            A("networksrf", "🕸", "FlächeAusKurvennetzwerk", "_NetworkSrf", "Fläche aus sich kreuzenden Kurven erzeugen.", "Erzeugen", false, "Multiple", secondary: true),
            A("planarsrf", "▰", "PlanareFläche", "_PlanarSrf", "Planare Fläche aus geschlossenen Kurven erzeugen.", "Erzeugen", false, "Multiple", secondary: true),
            A("curveboolean", "∪", "KurvenBoolesch", "_CurveBoolean", "Überlappende Kurvenbereiche kombinieren.", "Bearbeiten", false, "Multiple", secondary: true),
            A("booleansplit", "◪", "BooleschesTeilen", "_BooleanSplit", "Körper aneinander teilen.", "Bearbeiten", false, "Multiple", secondary: true),
            A("boolean2objects", "⇄", "Boolesch2Objekte", "_Boolean2Objects", "Alle booleschen Varianten zwischen 2 Körpern durchschalten.", "Smart", false, "Multiple", secondary: true),
            A("intersect", "✕", "SchnittkurvenErzeugen", "_Intersect", "Schnittkurven oder Schnittpunkte zwischen den Objekten berechnen.", "Erzeugen", false, "Multiple", secondary: true),
            A("distribute", "⇔", "GleichmäßigVerteilen", "_Distribute", "Objekte mit gleichem Abstand verteilen.", "Transformieren", false, "Multiple", secondary: true),
            A("arraylinear", "⋯", "RechteckigeAnordnung", "_ArrayLinear", "Objekte linear vervielfältigen.", "Transformieren", false, "Multiple", secondary: true),
            A("block", "◆", "BlockDefinieren", "_Block", "Aus den ausgewählten Objekten eine Blockdefinition erstellen.", "Block", false, "Multiple", secondary: true),
            A("ungroup", "⤢", "GruppierungAufheben", "_Ungroup", "Bestehende Gruppierung der Objekte auflösen.", "Bearbeiten", false, "Multiple", secondary: true),
            A("make2d", "◰", "2DZeichnung", "_Make2D", "2D-Zeichnung der ausgewählten Objekte ableiten.", "Dokumentation", false, "Multiple", secondary: true)
        },

        _ => Array.Empty<ContextAction>()
    };

    private static ContextAction[] FaceActions(bool planar)
    {
        List<ContextAction> actions = new List<ContextAction>
        {
            A("fillet", "⌒", "KantenVerrunden", "_FilletEdge _FaceEdges", "Randkanten der ausgewählten Teilfläche abrunden."),
            A("chamfer", "◩", "KantenFasen", "_ChamferEdge _FaceEdges", "Randkanten der ausgewählten Teilfläche fasen."),
            A("inset", "▣", "Inset", "_Inset", "Kanten der ausgewählten Teilfläche nach innen versetzen."),
            A("move", "✥", "SeiteVerschieben", "_MoveFace", "Ausgewählte Seite des Flächenverbands verschieben."),
            A("offset", "⇆", "Parallelfläche", "_OffsetSrf", "Ausgewählte Teilfläche parallel versetzen."),
            A("extract", "⇱", "FlächeLösen", "_ExtractSrf", "Ausgewählte Teilfläche aus dem Flächenverband lösen."),
            A("border", "▣", "RandDuplizieren", "_DupBorder", "Randkurven der ausgewählten Teilfläche erzeugen.", "Erzeugen"),
            A("delete", "⌫", "Löschen", "_Delete", "Ausgewählte Teilfläche löschen.", "Kritisch", true),
            // Erweiterte Funktionen (unter "⋯ Mehr")
            A("extrudesrf", "⇧", "TeilflächeExtrudieren", "_ExtrudeSrf", "Ausgewählte Teilfläche extrudieren.", "Erzeugen", secondary: true),
            A("rotateface", "↻", "SeiteDrehen", "_RotateFace", "Teilfläche um eine Achse kippen/drehen.", "Bearbeiten", secondary: true),
            A("splitface", "✂", "SeiteTeilen", "_SplitFace", "Teilfläche entlang einer Achse oder Kurve teilen.", "Bearbeiten", secondary: true)
        };

        if (planar)
        {
            actions.Insert(2,
                A("pushpull", "↕", "DrückenZiehen", "_PushPull",
                    "Planare Teilfläche interaktiv drücken oder ziehen."));
        }

        return actions.ToArray();
    }

    private static ContextAction[] SurfaceActions(bool planar)
    {
        List<ContextAction> actions = new List<ContextAction>
        {
            A("inset", "▣", "Inset", "_Inset", "Kanten der Fläche nach innen versetzen."),
            A("offset", "⇆", "Parallelfläche", "_OffsetSrf", "Parallele Fläche in einem Abstand erzeugen."),
            A("extrude", "⇧", "FlächeExtrudieren", "_ExtrudeSrf", "Fläche extrudieren.", "Erzeugen"),
            A("border", "▣", "RandDuplizieren", "_DupBorder", "Randkurven der Fläche erzeugen.", "Erzeugen"),
            A("rebuild", "⟳", "NeuAufbauen", "_Rebuild", "Fläche neu aufbauen."),
            A("dir", "➜", "Richtung", "_Dir", "Flächenrichtung anzeigen oder umkehren.", "Analyse"),
            A("zebra", "≋", "Zebra", "_Zebra", "Flächenübergänge mit Zebraanalyse prüfen.", "Analyse"),
            // Erweiterte Funktionen (unter "⋯ Mehr")
            A("extendsrf", "⤻", "FlächeVerlängern", "_ExtendSrf", "Flächenkante verlängern.", "Bearbeiten", secondary: true),
            A("unrollsrf", "◱", "FlächeAbwickeln", "_UnrollSrf", "Fläche flach auf die Konstruktionsebene abwickeln.", "Erzeugen", secondary: true),
            A("shrinktrimmedsrf", "◫", "GetrimmteFlächeSchrumpfen", "_ShrinkTrimmedSrf", "Zugrundeliegende Kontrollpunktfläche auf den Trimmrand verkleinern.", "Bearbeiten", secondary: true),
            A("flip", "⇄", "RichtungUmkehren", "_Flip", "Normalenrichtung der Fläche umdrehen.", "Bearbeiten", secondary: true),
            A("contour", "▤", "Kontur", "_Contour", "Konturschnitte durch die Fläche legen.", "Erzeugen", secondary: true)
        };

        if (planar)
        {
            actions.Insert(0,
                A("pushpull", "↕", "DrückenZiehen", "_PushPull",
                    "Planare Fläche interaktiv drücken oder ziehen."));
        }

        return actions.ToArray();
    }
}
