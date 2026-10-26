namespace RhinoContextAssist;

internal static class ActionCatalog
{
    internal static readonly SelectionKind[] Kinds = Enum.GetValues<SelectionKind>()
        .Where(k => k != SelectionKind.None && k != SelectionKind.MultipleEnvelopeObjects)
        .ToArray();

    private static ContextAction A(string id, string icon, string label, string macro,
        string hint, string category = "Bearbeiten", bool destructive = false,
        string requirement = "Any") =>
        new(id, icon, label, macro, hint, category, destructive, requirement);

    internal static IReadOnlyList<ContextAction> Defaults(SelectionKind kind) => kind switch
    {
        SelectionKind.ClosedPlanarCurve => new[]
        {
            A("solidextrude","⬒","Extrudieren und schließen","_ExtrudeCrv _Solid=_Yes","Geschlossenen Körper erzeugen","Erzeugen"),
            A("planarsrf","▰","Fläche erzeugen","_PlanarSrf","Planare Fläche erzeugen","Erzeugen"),
            A("pipe","◉","Rohr erzeugen","_Pipe","Rohr entlang der Kurve erzeugen","Erzeugen"),
            A("extrudecrv","⇧","Kurve extrudieren","_ExtrudeCrv","Kurve extrudieren","Erzeugen"),
            A("offset","⇆","Kurve versetzen","_Offset","Kurve versetzen"),
            A("hatch","▧","Schraffieren","_Hatch","Schraffur erzeugen","Dokumentation"),
            A("rebuild","⟳","Neu aufbauen","_Rebuild","Kurve neu aufbauen")
        },
        SelectionKind.Curve => new[]
        {
            A("offset","⇆","Kurve versetzen","_Offset","Kurve versetzen"),
            A("pipe","◉","Rohr erzeugen","_Pipe","Rohr erzeugen","Erzeugen"),
            A("extrudecrv","⇧","Kurve extrudieren","_ExtrudeCrv","Kurve extrudieren","Erzeugen"),
            A("rebuild","⟳","Neu aufbauen","_Rebuild","Kurve neu aufbauen"),
            A("length","↔","Länge ermitteln","_Length","Kurvenlänge ermitteln","Analyse"),
            A("dir","➜","Richtung anzeigen","_Dir","Kurvenrichtung anzeigen","Analyse")
        },
        SelectionKind.PlanarBrepFace => FaceActions(true),
        SelectionKind.BrepFace => FaceActions(false),
        SelectionKind.BrepEdge => new[]
        {
            A("filletedge","⌒","Kante abrunden","_FilletEdge","Ausgewählte Kante abrunden"),
            A("chamferedge","◩","Kante fasen","_ChamferEdge","Ausgewählte Kante fasen"),
            A("dupedge","⧉","Kante duplizieren","_DupEdge","Kante als Kurve duplizieren","Erzeugen")
        },
        SelectionKind.PlanarSurface => SurfaceActions(true),
        SelectionKind.Surface => SurfaceActions(false),
        SelectionKind.SolidBrep => new[]
        {
            A("filletedge","⌒","Kanten abrunden","_FilletEdge","Kanten auswählen und abrunden"),
            A("chamferedge","◩","Kanten fasen","_ChamferEdge","Kanten auswählen und fasen"),
            A("shell","◫","Aushöhlen","_Shell","Körper aushöhlen"),
            A("offsetsrf","⇆","Flächen versetzen","_OffsetSrf","Flächen versetzen"),
            A("extractsrf","⇱","Fläche lösen","_ExtractSrf","Teilfläche lösen"),
            A("dupborder","▣","Rand duplizieren","_DupBorder","Randkurven erzeugen","Erzeugen"),
            A("volume","V","Volumen","_Volume","Volumen ermitteln","Analyse"),
            A("area","A","Flächeninhalt","_Area","Flächeninhalt ermitteln","Analyse"),
            A("check","✓","Objekt prüfen","_Check","Objekt prüfen","Analyse")
        },
        SelectionKind.Brep => new[]
        {
            A("explode","⤢","Zerlegen","_Explode","Flächenverband zerlegen","Kritisch",true),
            A("merge","▤","Coplanare Flächen vereinigen","_MergeAllCoplanarFaces","Coplanare Flächen vereinigen","Bearbeiten",false,"Coplanar"),
            A("cap","▰","Öffnungen schließen","_Cap","Planare Öffnungen schließen","Bearbeiten",false,"Open"),
            A("offsetsrf","⇆","Flächen versetzen","_OffsetSrf","Flächen versetzen"),
            A("extractsrf","⇱","Fläche lösen","_ExtractSrf","Teilfläche lösen"),
            A("filletedge","⌒","Kanten abrunden","_FilletEdge","Kanten auswählen und abrunden"),
            A("chamferedge","◩","Kanten fasen","_ChamferEdge","Kanten auswählen und fasen"),
            A("shownaked","⊠","Offene Kanten anzeigen","_ShowEdges _NakedEdges","Offene Kanten anzeigen","Analyse"),
            A("check","✓","Objekt prüfen","_Check","Flächenverband prüfen","Analyse")
        },
        SelectionKind.Mesh => new[]
        {
            A("offsetmesh","⇆","Mesh versetzen","_OffsetMesh","Mesh versetzen"),
            A("reducemesh","▽","Mesh reduzieren","_ReduceMesh","Polygonzahl reduzieren"),
            A("quadremesh","▦","Quad-Mesh erzeugen","_QuadRemesh","Quad-Mesh erzeugen","Konvertieren"),
            A("weld","⛓","Verschweißen","_Weld","Mesh verschweißen"),
            A("unify","⇧","Normalen vereinheitlichen","_UnifyMeshNormals","Normalen vereinheitlichen"),
            A("shownaked","⊠","Offene Kanten anzeigen","_ShowEdges _NakedEdges","Offene Kanten anzeigen","Analyse"),
            A("check","✓","Mesh prüfen","_Check","Mesh prüfen","Analyse")
        },
        SelectionKind.MeshFace => new[]
        {
            A("inset","▣","Inset","_Inset","Mesh-Fläche nach innen versetzen"),
            A("extrudemeshfaces","⇧","Mesh-Fläche extrudieren","_ExtrudeMeshFaces","Mesh-Fläche extrudieren"),
            A("extractmeshfaces","⇱","Mesh-Fläche lösen","_ExtractMeshFaces","Mesh-Fläche lösen"),
            A("deletemeshfaces","⌫","Mesh-Fläche löschen","_DeleteMeshFaces","Mesh-Fläche löschen","Kritisch",true)
        },
        SelectionKind.MeshEdge => new[]
        {
            A("splitmeshedge","✂","Mesh-Kante teilen","_SplitMeshEdge","Mesh-Kante teilen"),
            A("swapmeshedge","⇄","Mesh-Kante drehen","_SwapMeshEdge","Mesh-Kante drehen"),
            A("weld","⛓","Verschweißen","_Weld","Mesh verschweißen")
        },
        SelectionKind.MeshVertex => new[]
        {
            A("move","✥","Punkt verschieben","_Move","Mesh-Punkt verschieben"),
            A("smooth","≈","Glätten","_Smooth","Mesh glätten"),
            A("align","≡","Ausrichten","_Align","Punkte ausrichten")
        },
        SelectionKind.SubD => new[]
        {
            A("extrudesubd","⇧","SubD extrudieren","_ExtrudeSubD","SubD extrudieren"),
            A("insertedge","＋","Kante einsetzen","_InsertEdge","Kante einsetzen"),
            A("crease","⌁","Schärfen","_Crease","SubD schärfen"),
            A("removecrease","⌒","Glätten","_RemoveCrease","Schärfung entfernen"),
            A("reflect","◇","Symmetrie","_Reflect","Symmetrie erzeugen"),
            A("tonurbs","▰","In NURBS umwandeln","_ToNURBS","SubD in NURBS umwandeln","Konvertieren")
        },
        SelectionKind.SubDFace => new[]
        {
            A("extrudesubd","⇧","Fläche extrudieren","_ExtrudeSubD","SubD-Fläche extrudieren"),
            A("inset","▣","Inset","_Inset","Inset erzeugen"),
            A("delete","⌫","Fläche löschen","_Delete","SubD-Fläche löschen","Kritisch",true)
        },
        SelectionKind.SubDEdge => new[]
        {
            A("crease","⌁","Kante schärfen","_Crease","Kante schärfen"),
            A("removecrease","⌒","Kante glätten","_RemoveCrease","Schärfung entfernen"),
            A("insertedge","＋","Kante einsetzen","_InsertEdge","Kante einsetzen")
        },
        SelectionKind.SubDVertex => new[]
        {
            A("move","✥","Punkt verschieben","_Move","SubD-Punkt verschieben"),
            A("crease","◆","Punkt schärfen","_Crease","Punkt schärfen"),
            A("removecrease","◇","Punkt glätten","_RemoveCrease","Schärfung entfernen")
        },
        SelectionKind.Block => new[]
        {
            A("unique","◆","Einzigartigen Block erstellen","_CreateUniqueBlock","Eigene Definition erzeugen","Block"),
            A("edit","✎","Block bearbeiten","_BlockEdit","Blockdefinition bearbeiten","Block"),
            A("replace","⇄","Block ersetzen","_ReplaceBlock","Blockdefinition ersetzen","Block",false,"OtherBlocks"),
            A("selectsame","▦","Gleiche Blöcke auswählen","_SelBlockInstanceNamed","Gleiche Blöcke auswählen","Block"),
            A("manager","☷","Blockmanager öffnen","_BlockManager","Blockmanager öffnen","Block"),
            A("add","＋","Objekte zum Block hinzufügen","_AddObjectsToBlock","Objekte ergänzen","Block"),
            A("resetscale","1:1","Blockmaßstab zurücksetzen","_BlockResetScale","Skalierung zurücksetzen","Block"),
            A("explodeblock","⤢","Block vollständig zerlegen","_ExplodeBlock","Block zerlegen","Kritisch",true)
        },
        SelectionKind.Multiple or SelectionKind.Mixed => new[]
        {
            A("group","▦","Gruppieren","_Group","Objekte gruppieren"),
            A("align","≡","Ausrichten","_Align","Objekte ausrichten"),
            A("join","⛓","Verbinden","_Join","Objekte verbinden"),
            A("union","∪","Vereinigen","_BooleanUnion","Körper vereinigen"),
            A("difference","−","Differenz bilden","_BooleanDifference","Körper abziehen","Kritisch",true),
            A("intersection","∩","Schnittmenge","_BooleanIntersection","Schnittmenge erzeugen")
        },
        _ => Array.Empty<ContextAction>()
    };

    private static ContextAction[] FaceActions(bool planar)
    {
        var actions = new List<ContextAction>
        {
            A("filletfaceedges","⌒","Kanten der Teilfläche abrunden","_FilletEdge _FaceEdges","Randkanten der Teilfläche abrunden"),
            A("chamferfaceedges","◩","Kanten der Teilfläche fasen","_ChamferEdge _FaceEdges","Randkanten der Teilfläche fasen"),
            A("inset","▣","Inset","_Inset","Teilfläche nach innen versetzen"),
            A("moveface","✥","Fläche verschieben","_MoveFace","Teilfläche verschieben"),
            A("offsetsrf","⇆","Fläche versetzen","_OffsetSrf","Teilfläche versetzen"),
            A("extractsrf","⇱","Fläche lösen","_ExtractSrf","Teilfläche lösen"),
            A("dupborder","▣","Rand duplizieren","_DupBorder","Randkurven erzeugen","Erzeugen"),
            A("deleteface","⌫","Fläche löschen","_Delete","Teilfläche löschen","Kritisch",true)
        };
        if (planar)
            actions.Insert(2, A("pushpull","↕","Drücken/Ziehen","_PushPull","Planare Teilfläche drücken oder ziehen"));
        return actions.ToArray();
    }

    private static ContextAction[] SurfaceActions(bool planar)
    {
        var actions = new List<ContextAction>
        {
            A("inset","▣","Inset","_Inset","Flächenkanten nach innen versetzen"),
            A("offsetsrf","⇆","Fläche versetzen","_OffsetSrf","Fläche versetzen"),
            A("extrudesrf","⇧","Fläche extrudieren","_ExtrudeSrf","Fläche extrudieren","Erzeugen"),
            A("dupborder","▣","Rand duplizieren","_DupBorder","Randkurven erzeugen","Erzeugen"),
            A("rebuild","⟳","Neu aufbauen","_Rebuild","Fläche neu aufbauen"),
            A("dir","➜","Richtung anzeigen","_Dir","Flächenrichtung anzeigen","Analyse"),
            A("zebra","≋","Zebraanalyse","_Zebra","Flächen analysieren","Analyse")
        };
        if (planar)
            actions.Insert(0, A("pushpull","↕","Drücken/Ziehen","_PushPull","Planare Fläche drücken oder ziehen"));
        return actions.ToArray();
    }
}
