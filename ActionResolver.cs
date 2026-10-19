namespace RhinoContextAssist;

internal sealed record ContextAction(string Label, string Macro, string Hint);

internal static class ActionResolver
{
    public static IReadOnlyList<ContextAction> Resolve(SelectionContext c) => c.Kind switch
    {
        SelectionKind.PlanarBrepFace => A(
            ("PushPull", "_PushPull", "Flaeche direkt druecken/ziehen"),
            ("MoveFace", "_MoveFace", "Flaeche verschieben"),
            ("Inset", "_Inset", "Inset starten, falls fuer den Typ verfuegbar"),
            ("ExtractSrf", "_ExtractSrf", "Flaeche herausloesen"),
            ("DupBorder", "_DupBorder", "Randkurve erzeugen")),
        SelectionKind.BrepFace => A(
            ("OffsetSrf", "_OffsetSrf", "Flaeche versetzen"),
            ("MoveFace", "_MoveFace", "Flaeche verschieben"),
            ("ExtractSrf", "_ExtractSrf", "Flaeche herausloesen"),
            ("DupBorder", "_DupBorder", "Randkurve erzeugen")),
        SelectionKind.BrepEdge => A(
            ("FilletEdge", "_FilletEdge", "Kante abrunden"),
            ("ChamferEdge", "_ChamferEdge", "Kante fasen"),
            ("DupEdge", "_DupEdge", "Kante duplizieren")),
        SelectionKind.ClosedPlanarCurve => A(
            ("ExtrudeCrv", "_ExtrudeCrv", "Kurve extrudieren"),
            ("PlanarSrf", "_PlanarSrf", "Planare Flaeche erzeugen"),
            ("Offset", "_Offset", "Kurve versetzen"),
            ("Hatch", "_Hatch", "Schraffur erzeugen")),
        SelectionKind.Curve => A(
            ("Offset", "_Offset", "Kurve versetzen"),
            ("Pipe", "_Pipe", "Rohr erzeugen"),
            ("ExtrudeCrv", "_ExtrudeCrv", "Kurve extrudieren"),
            ("Rebuild", "_Rebuild", "Kurve neu aufbauen")),
        SelectionKind.PlanarSurface => A(
            ("PushPull", "_PushPull", "Druecken/ziehen"),
            ("ExtrudeSrf", "_ExtrudeSrf", "Flaeche extrudieren"),
            ("OffsetSrf", "_OffsetSrf", "Flaeche versetzen"),
            ("DupBorder", "_DupBorder", "Rand duplizieren")),
        SelectionKind.Surface => A(
            ("OffsetSrf", "_OffsetSrf", "Flaeche versetzen"),
            ("ExtractSrf", "_ExtractSrf", "Flaeche herausloesen"),
            ("DupBorder", "_DupBorder", "Rand duplizieren"),
            ("Rebuild", "_Rebuild", "Flaeche neu aufbauen")),
        SelectionKind.SolidBrep => A(
            ("Shell", "_Shell", "Koerper aushoehlen"),
            ("BooleanUnion", "_BooleanUnion", "Vereinigung"),
            ("BooleanDifference", "_BooleanDifference", "Differenz"),
            ("Cap", "_Cap", "Planare Oeffnungen schliessen")),
        SelectionKind.Brep => A(
            ("Join", "_Join", "Verbinden"),
            ("Cap", "_Cap", "Oeffnungen schliessen"),
            ("OffsetSrf", "_OffsetSrf", "Versetzen")),
        SelectionKind.Mesh => A(
            ("OffsetMesh", "_OffsetMesh", "Mesh versetzen"),
            ("ReduceMesh", "_ReduceMesh", "Polygonzahl reduzieren"),
            ("QuadRemesh", "_QuadRemesh", "Quad-Mesh erzeugen")),
        SelectionKind.SubD => A(
            ("ExtrudeSubD", "_ExtrudeSubD", "SubD extrudieren"),
            ("InsertEdge", "_InsertEdge", "Kante einsetzen"),
            ("Crease", "_Crease", "Kante schaerfen"),
            ("Reflect", "_Reflect", "Symmetrie")),
        SelectionKind.Multiple or SelectionKind.Mixed => A(
            ("Group", "_Group", "Gruppieren"),
            ("Align", "_Align", "Ausrichten"),
            ("BooleanUnion", "_BooleanUnion", "Vereinigung, wenn moeglich"),
            ("Join", "_Join", "Verbinden, wenn moeglich")),
        _ => Array.Empty<ContextAction>()
    };

    private static IReadOnlyList<ContextAction> A(params (string label, string macro, string hint)[] items) =>
        items.Select(x => new ContextAction(x.label, x.macro, x.hint)).ToList();
}
