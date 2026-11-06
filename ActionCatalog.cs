namespace RhinoContextAssist;

internal static class ActionCatalog
{
    internal static readonly SelectionKind[] Kinds = Enum.GetValues<SelectionKind>()
        .Where(kind => kind != SelectionKind.None && kind != SelectionKind.MultipleEnvelopeObjects)
        .ToArray();

    private static ContextAction A(
        string id, string icon, string germanCommand, string macro,
        string description, string category = "Bearbeiten",
        bool destructive = false, string requirement = "Any") =>
        new(id, icon, germanCommand, macro, description, category, destructive, requirement);

    internal static IReadOnlyList<ContextAction> Defaults(SelectionKind kind) => kind switch
    {
        SelectionKind.PlanarBrepFace => FaceActions(true),
        SelectionKind.BrepFace => FaceActions(false),

        SelectionKind.BrepEdge => new[]
        {
            A("fillet", "⌒", "KantenVerrunden", "_FilletEdge", "Ausgewählte Kante abrunden."),
            A("chamfer", "◩", "KantenFasen", "_ChamferEdge", "Ausgewählte Kante fasen."),
            A("dupedge", "⧉", "KanteDuplizieren", "_DupEdge", "Ausgewählte Kante als Kurve duplizieren.", "Erzeugen")
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
            A("check", "✓", "Prüfen", "_Check", "Objekt auf geometrische Fehler prüfen.", "Analyse")
        },

        SelectionKind.Brep => new[]
        {
            A("cap", "▰", "PlanareÖffnungenSchließen", "_Cap", "Planare Öffnungen des Flächenverbands schließen.", "Smart", false, "Open"),
            A("merge", "▤", "AlleKoplanarenSeitenVereinigen", "_MergeAllCoplanarFaces", "Koplanare Seiten des Flächenverbands vereinigen.", "Smart", false, "MultipleFaces"),
            A("fillet", "⌒", "KantenVerrunden", "_FilletEdge", "Kanten des Flächenverbands abrunden."),
            A("chamfer", "◩", "KantenFasen", "_ChamferEdge", "Kanten des Flächenverbands fasen."),
            A("offsetsrf", "⇆", "Parallelfläche", "_OffsetSrf", "Flächen parallel versetzen."),
            A("shownaked", "⊠", "KantenAnzeigen", "_ShowEdges _NakedEdges", "Offene Kanten des Flächenverbands anzeigen.", "Analyse"),
            A("explode", "⤢", "Explodieren", "_Explode", "Flächenverband in einzelne Flächen zerlegen.", "Kritisch", true)
        },

        SelectionKind.ClosedPlanarCurve => new[]
        {
            A("solidextrude", "⬒", "KurveExtrudieren", "_ExtrudeCrv _Solid=_Yes", "Geschlossene Kurve als Volumenkörper extrudieren.", "Erzeugen"),
            A("planarsrf", "▰", "PlanareFläche", "_PlanarSrf", "Aus der geschlossenen Kurve eine planare Fläche erzeugen.", "Erzeugen"),
            A("pipe", "◉", "Rohr", "_Pipe", "Rohr entlang der Kurve erzeugen.", "Erzeugen"),
            A("offset", "⇆", "Versetzen", "_Offset", "Parallele Kurve in einem Abstand erzeugen."),
            A("hatch", "▧", "Schraffierung", "_Hatch", "Schraffierung innerhalb der Kurve erzeugen.", "Dokumentation")
        },

        SelectionKind.Curve => new[]
        {
            A("offset", "⇆", "Versetzen", "_Offset", "Parallele Kurve in einem Abstand erzeugen."),
            A("pipe", "◉", "Rohr", "_Pipe", "Rohr entlang der Kurve erzeugen.", "Erzeugen"),
            A("extrude", "⇧", "KurveExtrudieren", "_ExtrudeCrv", "Kurve zu einer Fläche extrudieren.", "Erzeugen"),
            A("rebuild", "⟳", "NeuAufbauen", "_Rebuild", "Kurve mit neuer Punkt- und Gradverteilung neu aufbauen."),
            A("length", "↔", "Länge", "_Length", "Länge der Kurve ermitteln.", "Analyse"),
            A("dir", "➜", "Richtung", "_Dir", "Richtung der Kurve anzeigen oder umkehren.", "Analyse")
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
            A("check", "✓", "Prüfen", "_Check", "Polygonnetz auf Fehler prüfen.", "Analyse")
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
            A("tonurbs", "▰", "InNURBSUmwandeln", "_ToNURBS", "SubD in NURBS-Flächen umwandeln.", "Konvertieren")
        },

        SelectionKind.SubDFace => new[]
        {
            A("extrude", "⇧", "SubDExtrudieren", "_ExtrudeSubD", "Ausgewählte SubD-Seite extrudieren."),
            A("inset", "▣", "Inset", "_Inset", "Kanten der ausgewählten SubD-Seite nach innen versetzen."),
            A("delete", "⌫", "Löschen", "_Delete", "Ausgewählte SubD-Seite löschen.", "Kritisch", true)
        },

        SelectionKind.SubDEdge => new[]
        {
            A("crease", "⌁", "Falzen", "_Crease", "Ausgewählte SubD-Kante schärfen."),
            A("smooth", "⌒", "FalzEntfernen", "_RemoveCrease", "Schärfung der SubD-Kante entfernen."),
            A("insert", "＋", "KanteEinfügen", "_InsertEdge", "Zusätzliche SubD-Kante einsetzen.")
        },

        SelectionKind.SubDVertex => new[]
        {
            A("move", "✥", "Verschieben", "_Move", "Ausgewählten SubD-Punkt verschieben."),
            A("crease", "◆", "Falzen", "_Crease", "Ausgewählten SubD-Punkt schärfen."),
            A("smooth", "◇", "FalzEntfernen", "_RemoveCrease", "Schärfung des SubD-Punkts entfernen.")
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
            A("intersection", "∩", "BoolescheSchnittmenge", "_BooleanIntersection", "Gemeinsames Volumen erzeugen.", "Smart", false, "Multiple")
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
            A("delete", "⌫", "Löschen", "_Delete", "Ausgewählte Teilfläche löschen.", "Kritisch", true)
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
            A("zebra", "≋", "Zebra", "_Zebra", "Flächenübergänge mit Zebraanalyse prüfen.", "Analyse")
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
