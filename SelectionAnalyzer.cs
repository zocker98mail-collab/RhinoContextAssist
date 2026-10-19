using Rhino;
using Rhino.DocObjects;
using Rhino.Geometry;

namespace RhinoContextAssist;

internal enum SelectionKind
{
    None, Multiple, Mixed, Curve, ClosedPlanarCurve, Surface, PlanarSurface,
    Brep, SolidBrep, BrepFace, PlanarBrepFace, BrepEdge, Mesh, SubD
}

internal sealed record SelectionContext(SelectionKind Kind, int Count, string Caption)
{
    public string Signature => $"{Kind}:{Count}:{Caption}";
}

internal static class SelectionAnalyzer
{
    public static SelectionContext Analyze(RhinoDoc doc)
    {
        var selected = doc.Objects.GetSelectedObjects(false, false).ToList();
        if (selected.Count == 0)
            selected = doc.Objects.Where(o => o.GetSelectedSubObjects()?.Length > 0).ToList();
        if (selected.Count == 0) return new(SelectionKind.None, 0, "Keine Auswahl");

        if (selected.Count == 1)
        {
            var obj = selected[0];
            var sub = obj.GetSelectedSubObjects();
            if (sub is { Length: > 0 })
            {
                var first = sub[0];
                if (first.ComponentIndexType == ComponentIndexType.BrepFace && obj.Geometry is Brep brep && first.Index >= 0 && first.Index < brep.Faces.Count)
                {
                    var face = brep.Faces[first.Index];
                    return face.TryGetPlane(out _)
                        ? new(SelectionKind.PlanarBrepFace, 1, "Planare Teilfläche")
                        : new(SelectionKind.BrepFace, 1, "Teilfläche");
                }
                if (first.ComponentIndexType == ComponentIndexType.BrepEdge)
                    return new(SelectionKind.BrepEdge, 1, "Brep-Kante");
            }
            return AnalyzeObject(obj);
        }

        var kinds = selected.Select(AnalyzeObject).Select(x => x.Kind).Distinct().ToList();
        return kinds.Count == 1
            ? new(SelectionKind.Multiple, selected.Count, $"{selected.Count} Objekte")
            : new(SelectionKind.Mixed, selected.Count, $"{selected.Count} gemischte Objekte");
    }

    private static SelectionContext AnalyzeObject(RhinoObject obj)
    {
        return obj.Geometry switch
        {
            Curve c when c.IsClosed && c.IsPlanar() => new(SelectionKind.ClosedPlanarCurve, 1, "Geschlossene planare Kurve"),
            Curve => new(SelectionKind.Curve, 1, "Kurve"),
            Brep b when b.Faces.Count == 1 && b.Faces[0].TryGetPlane(out _) => new(SelectionKind.PlanarSurface, 1, "Planare Fläche"),
            Brep b when b.Faces.Count == 1 => new(SelectionKind.Surface, 1, "Fläche"),
            Brep b when b.IsSolid => new(SelectionKind.SolidBrep, 1, "Geschlossener Körper"),
            Brep => new(SelectionKind.Brep, 1, "Flächenverband"),
            Extrusion e when e.IsSolid => new(SelectionKind.SolidBrep, 1, "Geschlossene Extrusion"),
            Extrusion => new(SelectionKind.Surface, 1, "Extrusion"),
            Mesh => new(SelectionKind.Mesh, 1, "Mesh"),
            SubD => new(SelectionKind.SubD, 1, "SubD"),
            _ => new(SelectionKind.Multiple, 1, obj.ObjectType.ToString())
        };
    }
}
