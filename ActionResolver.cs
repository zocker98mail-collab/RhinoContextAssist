namespace RhinoContextAssist;

internal sealed record ContextAction(string Label, string Macro, string Hint);

internal static class ActionResolver
{
    public static IReadOnlyList<ContextAction> Resolve(SelectionContext c) => c.Kind switch
    {
        SelectionKind.PlanarBrepFace => A(
            ("Drücken/Ziehen", "_PushPull", "Teilfläche direkt drücken oder ziehen"),
            ("Fläche verschieben", "_MoveFace", "Ausgewählte Teilfläche verschieben"),
            ("Fläche lösen", "_ExtractSrf", "Teilfläche aus dem Körper lösen"),
            ("Rand duplizieren", "_DupBorder", "Randkurven der Fläche erzeugen")),
        SelectionKind.BrepFace => A(
            ("Fläche verschieben", "_MoveFace", "Ausgewählte Teilfläche verschieben"),
            ("Fläche lösen", "_ExtractSrf", "Teilfläche aus dem Körper lösen"),
            ("Rand duplizieren", "_DupBorder", "Randkurven der Fläche erzeugen")),
        SelectionKind.BrepEdge => A(
            ("Kante abrunden", "_FilletEdge", "Rundung an der ausgewählten Kante erzeugen"),
            ("Kante fasen", "_ChamferEdge", "Fase an der ausgewählten Kante erzeugen"),
            ("Kante duplizieren", "_DupEdge", "Kante als Kurve duplizieren")),
        SelectionKind.ClosedPlanarCurve => A(
            ("Kurve extrudieren", "_ExtrudeCrv", "Geschlossene Kurve extrudieren"),
            ("Fläche erzeugen", "_PlanarSrf", "Planare Fläche aus der Kurve erzeugen"),
            ("Kurve versetzen", "_Offset", "Parallele Kurve erzeugen"),
            ("Schraffieren", "_Hatch", "Schraffur innerhalb der Kurve erzeugen")),
        SelectionKind.Curve => A(
            ("Kurve versetzen", "_Offset", "Parallele Kurve erzeugen"),
            ("Rohr erzeugen", "_Pipe", "Rohr entlang der Kurve erzeugen"),
            ("Kurve extrudieren", "_ExtrudeCrv", "Kurve extrudieren"),
            ("Neu aufbauen", "_Rebuild", "Kurve neu aufbauen")),
        SelectionKind.PlanarSurface => A(
            ("Drücken/Ziehen", "_PushPull", "Fläche direkt drücken oder ziehen"),
            ("Fläche extrudieren", "_ExtrudeSrf", "Fläche extrudieren"),
            ("Fläche versetzen", "_OffsetSrf", "Parallele Fläche erzeugen"),
            ("Rand duplizieren", "_DupBorder", "Randkurven erzeugen")),
        SelectionKind.Surface => A(
            ("Fläche versetzen", "_OffsetSrf", "Parallele Fläche erzeugen"),
            ("Fläche lösen", "_ExtractSrf", "Fläche lösen"),
            ("Rand duplizieren", "_DupBorder", "Randkurven erzeugen"),
            ("Neu aufbauen", "_Rebuild", "Fläche neu aufbauen")),
        SelectionKind.SolidBrep => A(
            ("Vereinigen", "_BooleanUnion", "Geeignete Körper vereinigen"),
            ("Differenz bilden", "_BooleanDifference", "Körper voneinander abziehen"),
            ("Kanten abrunden", "_FilletEdge", "Kanten des Körpers abrunden"),
            ("Kanten fasen", "_ChamferEdge", "Kanten des Körpers fasen")),
        SelectionKind.Brep => A(
            ("Verbinden", "_Join", "Flächen oder Kurven verbinden"),
            ("Öffnungen schließen", "_Cap", "Planare Öffnungen schließen"),
            ("Flächen versetzen", "_OffsetSrf", "Flächen versetzen")),
        SelectionKind.Mesh => A(
            ("Mesh versetzen", "_OffsetMesh", "Versetztes Mesh erzeugen"),
            ("Mesh reduzieren", "_ReduceMesh", "Polygonzahl reduzieren"),
            ("Quad-Mesh erzeugen", "_QuadRemesh", "Quad-Mesh erzeugen")),
        SelectionKind.SubD => A(
            ("SubD extrudieren", "_ExtrudeSubD", "SubD-Komponenten extrudieren"),
            ("Kante einsetzen", "_InsertEdge", "SubD-Kante einsetzen"),
            ("Kante schärfen", "_Crease", "SubD-Kante schärfen"),
            ("Symmetrie", "_Reflect", "Gespiegelte Symmetrie erzeugen")),
        SelectionKind.Multiple or SelectionKind.Mixed => A(
            ("Gruppieren", "_Group", "Objekte gruppieren"),
            ("Ausrichten", "_Align", "Objekte ausrichten"),
            ("Vereinigen", "_BooleanUnion", "Geeignete Körper vereinigen"),
            ("Verbinden", "_Join", "Geeignete Objekte verbinden")),
        _ => Array.Empty<ContextAction>()
    };

    private static IReadOnlyList<ContextAction> A(params (string label, string macro, string hint)[] items) =>
        items.Select(x => new ContextAction(x.label, x.macro, x.hint)).ToList();
}
