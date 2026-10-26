namespace RhinoContextAssist;
internal static class ActionCatalog
{
    internal static readonly SelectionKind[] ConfigurableKinds =
    {
        SelectionKind.PlanarBrepFace, SelectionKind.BrepFace, SelectionKind.BrepEdge,
        SelectionKind.ClosedPlanarCurve, SelectionKind.Curve, SelectionKind.PlanarSurface,
        SelectionKind.Surface, SelectionKind.SolidBrep, SelectionKind.Brep,
        SelectionKind.Mesh, SelectionKind.MeshFace, SelectionKind.MeshEdge, SelectionKind.MeshVertex,
        SelectionKind.SubD, SelectionKind.SubDFace, SelectionKind.SubDEdge, SelectionKind.SubDVertex,
        SelectionKind.Block, SelectionKind.Multiple, SelectionKind.Mixed
    };
    private static ContextAction A(string id, string icon, string label, string macro, string hint) => new(id, icon, label, macro, hint);
    internal static IReadOnlyList<ContextAction> Defaults(SelectionKind kind) => kind switch
    {
        SelectionKind.PlanarBrepFace => new[] { A("pushpull","↕","Drücken/Ziehen","_PushPull","Teilfläche drücken oder ziehen"), A("inset","▣","Inset","_Inset","Innere Teilfläche erzeugen"), A("moveface","✥","Fläche verschieben","_MoveFace","Teilfläche verschieben"), A("extractsrf","⇱","Fläche lösen","_ExtractSrf","Teilfläche lösen"), A("dupborder","▣","Rand duplizieren","_DupBorder","Randkurven erzeugen") },
        SelectionKind.BrepFace => new[] { A("inset","▣","Inset","_Inset","Flächenkanten nach innen versetzen"), A("moveface","✥","Fläche verschieben","_MoveFace","Teilfläche verschieben"), A("offsetsrf","⇆","Fläche versetzen","_OffsetSrf","Fläche versetzen"), A("extractsrf","⇱","Fläche lösen","_ExtractSrf","Teilfläche lösen"), A("dupborder","▣","Rand duplizieren","_DupBorder","Randkurven erzeugen") },
        SelectionKind.BrepEdge => new[] { A("filletedge","⌒","Kante abrunden","_FilletEdge","Kante abrunden"), A("chamferedge","◩","Kante fasen","_ChamferEdge","Kante fasen"), A("dupedge","⧉","Kante duplizieren","_DupEdge","Kante duplizieren") },
        SelectionKind.ClosedPlanarCurve => new[] { A("extrudecrvsolid","⬒","Extrudieren und schließen","_ExtrudeCrv _Solid=_Yes","Geschlossenen Körper extrudieren"), A("planarsrf","▰","Fläche erzeugen","_PlanarSrf","Planare Fläche erzeugen"), A("pipe","◉","Rohr erzeugen","_Pipe","Rohr erzeugen"), A("extrudecrv","⇧","Kurve extrudieren","_ExtrudeCrv","Kurve extrudieren"), A("offset","⇆","Kurve versetzen","_Offset","Kurve versetzen"), A("hatch","▧","Schraffieren","_Hatch","Schraffur erzeugen") },
        SelectionKind.Curve => new[] { A("offset","⇆","Kurve versetzen","_Offset","Kurve versetzen"), A("pipe","◉","Rohr erzeugen","_Pipe","Rohr erzeugen"), A("extrudecrv","⇧","Kurve extrudieren","_ExtrudeCrv","Kurve extrudieren"), A("rebuild","⟳","Neu aufbauen","_Rebuild","Kurve neu aufbauen") },
        SelectionKind.PlanarSurface => new[] { A("pushpull","↕","Drücken/Ziehen","_PushPull","Fläche drücken oder ziehen"), A("inset","▣","Inset","_Inset","Innere Teilfläche erzeugen"), A("extrudesrf","⇧","Fläche extrudieren","_ExtrudeSrf","Fläche extrudieren"), A("offsetsrf","⇆","Fläche versetzen","_OffsetSrf","Fläche versetzen"), A("dupborder","▣","Rand duplizieren","_DupBorder","Randkurven erzeugen") },
        SelectionKind.Surface => new[] { A("inset","▣","Inset","_Inset","Flächenkanten nach innen versetzen"), A("offsetsrf","⇆","Fläche versetzen","_OffsetSrf","Fläche versetzen"), A("extractsrf","⇱","Fläche lösen","_ExtractSrf","Fläche lösen"), A("dupborder","▣","Rand duplizieren","_DupBorder","Randkurven erzeugen"), A("rebuild","⟳","Neu aufbauen","_Rebuild","Fläche neu aufbauen") },
        SelectionKind.SolidBrep => new[] { A("union","∪","Vereinigen","_BooleanUnion","Körper vereinigen"), A("difference","−","Differenz bilden","_BooleanDifference","Körper abziehen"), A("filletedge","⌒","Kanten abrunden","_FilletEdge","Kanten abrunden"), A("chamferedge","◩","Kanten fasen","_ChamferEdge","Kanten fasen") },
        SelectionKind.Brep => new[] { A("join","⛓","Verbinden","_Join","Objekte verbinden"), A("explode","⤢","Zerlegen","_Explode","Flächenverband zerlegen"), A("mergeallcoplanarfaces","▤","Coplanare Flächen vereinigen","_MergeAllCoplanarFaces","Coplanare Flächen vereinigen"), A("cap","▰","Öffnungen schließen","_Cap","Öffnungen schließen"), A("offsetsrf","⇆","Flächen versetzen","_OffsetSrf","Flächen versetzen") },
        SelectionKind.Mesh => new[] { A("offsetmesh","⇆","Mesh versetzen","_OffsetMesh","Mesh versetzen"), A("reducemesh","▽","Mesh reduzieren","_ReduceMesh","Polygonzahl reduzieren"), A("quadremesh","▦","Quad-Mesh erzeugen","_QuadRemesh","Quad-Mesh erzeugen") },
        SelectionKind.MeshFace => new[] { A("inset","▣","Inset","_Inset","Mesh-Fläche nach innen versetzen"), A("extractmeshfaces","⇱","Mesh-Fläche lösen","_ExtractMeshFaces","Mesh-Fläche lösen"), A("deletemeshfaces","⌫","Mesh-Fläche löschen","_DeleteMeshFaces","Mesh-Fläche löschen"), A("meshfaceextrude","⇧","Mesh-Fläche extrudieren","_ExtrudeMeshFaces","Mesh-Fläche extrudieren") },
        SelectionKind.MeshEdge => new[] { A("meshedgesplit","✂","Mesh-Kante teilen","_SplitMeshEdge","Mesh-Kante teilen"), A("meshedgeswap","⇄","Mesh-Kante drehen","_SwapMeshEdge","Mesh-Kante drehen"), A("weld","⛓","Verschweißen","_Weld","Mesh verschweißen") },
        SelectionKind.MeshVertex => new[] { A("move","✥","Punkt verschieben","_Move","Mesh-Vertex verschieben"), A("smooth","≈","Glätten","_Smooth","Mesh glätten"), A("align","≡","Ausrichten","_Align","Punkte ausrichten") },
        SelectionKind.SubD => new[] { A("extrudesubd","⇧","SubD extrudieren","_ExtrudeSubD","SubD extrudieren"), A("insertedge","＋","Kante einsetzen","_InsertEdge","Kante einsetzen"), A("crease","⌁","Kante schärfen","_Crease","Kante schärfen"), A("reflect","◇","Symmetrie","_Reflect","Symmetrie erzeugen") },
        SelectionKind.SubDFace => new[] { A("extrudesubd","⇧","Fläche extrudieren","_ExtrudeSubD","SubD-Fläche extrudieren"), A("inset","▣","Inset","_Inset","Inset erzeugen"), A("deletesubd","⌫","Fläche löschen","_Delete","SubD-Fläche löschen") },
        SelectionKind.SubDEdge => new[] { A("crease","⌁","Kante schärfen","_Crease","Kante schärfen"), A("uncrease","⌒","Kante glätten","_RemoveCrease","Schärfung entfernen"), A("insertedge","＋","Kante einsetzen","_InsertEdge","Kante einsetzen") },
        SelectionKind.SubDVertex => new[] { A("move","✥","Punkt verschieben","_Move","SubD-Punkt verschieben"), A("crease","◆","Punkt schärfen","_Crease","Punkt schärfen"), A("uncrease","◇","Punkt glätten","_RemoveCrease","Schärfung entfernen") },
        SelectionKind.Block => new[] { A("createuniqueblock","◆","Einzigartigen Block erstellen","_CreateUniqueBlock","Eigene Blockdefinition erzeugen"), A("explodeblock","⤢","Block in Teilobjekte zerlegen","_ExplodeBlock","Block zerlegen"), A("replaceblock","⇄","Block ersetzen","_ReplaceBlock","Blockdefinition ersetzen"), A("blockedit","✎","Block bearbeiten","_BlockEdit","Block bearbeiten") },
        SelectionKind.Multiple or SelectionKind.Mixed => new[] { A("group","▦","Gruppieren","_Group","Objekte gruppieren"), A("align","≡","Ausrichten","_Align","Objekte ausrichten"), A("union","∪","Vereinigen","_BooleanUnion","Körper vereinigen"), A("join","⛓","Verbinden","_Join","Objekte verbinden") },
        _ => Array.Empty<ContextAction>()
    };
}
