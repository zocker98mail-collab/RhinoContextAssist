namespace RhinoContextAssist;
internal sealed record ContextAction(string Label,string Macro,string Hint);
internal static class ActionResolver{
 public static IReadOnlyList<ContextAction> Resolve(SelectionContext c)=>c.Kind switch{
 SelectionKind.PlanarBrepFace=>A(("Drücken/Ziehen","_PushPull","Teilfläche direkt drücken oder ziehen"),("Fläche verschieben","_MoveFace","Teilfläche verschieben"),("Fläche lösen","_ExtractSrf","Teilfläche lösen"),("Rand duplizieren","_DupBorder","Randkurven erzeugen")),
 SelectionKind.BrepFace=>A(("Fläche verschieben","_MoveFace","Teilfläche verschieben"),("Fläche lösen","_ExtractSrf","Teilfläche lösen"),("Rand duplizieren","_DupBorder","Randkurven erzeugen")),
 SelectionKind.BrepEdge=>A(("Kante abrunden","_FilletEdge","Kante abrunden"),("Kante fasen","_ChamferEdge","Kante fasen"),("Kante duplizieren","_DupEdge","Kante als Kurve duplizieren")),
 SelectionKind.ClosedPlanarCurve=>A(("Kurve extrudieren","_ExtrudeCrv","Kurve extrudieren"),("Fläche erzeugen","_PlanarSrf","Planare Fläche erzeugen"),("Kurve versetzen","_Offset","Parallele Kurve erzeugen"),("Schraffieren","_Hatch","Schraffur erzeugen")),
 SelectionKind.Curve=>A(("Kurve versetzen","_Offset","Kurve versetzen"),("Rohr erzeugen","_Pipe","Rohr erzeugen"),("Kurve extrudieren","_ExtrudeCrv","Kurve extrudieren"),("Neu aufbauen","_Rebuild","Kurve neu aufbauen")),
 SelectionKind.PlanarSurface=>A(("Drücken/Ziehen","_PushPull","Fläche drücken oder ziehen"),("Fläche extrudieren","_ExtrudeSrf","Fläche extrudieren"),("Fläche versetzen","_OffsetSrf","Fläche versetzen"),("Rand duplizieren","_DupBorder","Randkurven erzeugen")),
 SelectionKind.Surface=>A(("Fläche versetzen","_OffsetSrf","Fläche versetzen"),("Fläche lösen","_ExtractSrf","Fläche lösen"),("Rand duplizieren","_DupBorder","Randkurven erzeugen"),("Neu aufbauen","_Rebuild","Fläche neu aufbauen")),
 SelectionKind.SolidBrep=>A(("Vereinigen","_BooleanUnion","Körper vereinigen"),("Differenz bilden","_BooleanDifference","Körper abziehen"),("Kanten abrunden","_FilletEdge","Kanten abrunden"),("Kanten fasen","_ChamferEdge","Kanten fasen")),
 SelectionKind.Brep=>A(("Verbinden","_Join","Objekte verbinden"),("Öffnungen schließen","_Cap","Öffnungen schließen"),("Flächen versetzen","_OffsetSrf","Flächen versetzen")),
 SelectionKind.Mesh=>A(("Mesh versetzen","_OffsetMesh","Mesh versetzen"),("Mesh reduzieren","_ReduceMesh","Polygonzahl reduzieren"),("Quad-Mesh erzeugen","_QuadRemesh","Quad-Mesh erzeugen")),
 SelectionKind.SubD=>A(("SubD extrudieren","_ExtrudeSubD","SubD extrudieren"),("Kante einsetzen","_InsertEdge","Kante einsetzen"),("Kante schärfen","_Crease","Kante schärfen"),("Symmetrie","_Reflect","Symmetrie erzeugen")),
 SelectionKind.Multiple or SelectionKind.Mixed=>A(("Gruppieren","_Group","Objekte gruppieren"),("Ausrichten","_Align","Objekte ausrichten"),("Vereinigen","_BooleanUnion","Körper vereinigen"),("Verbinden","_Join","Objekte verbinden")),_=>Array.Empty<ContextAction>()};
 private static IReadOnlyList<ContextAction>A(params(string label,string macro,string hint)[]x)=>x.Select(i=>new ContextAction(i.label,i.macro,i.hint)).ToList();}
