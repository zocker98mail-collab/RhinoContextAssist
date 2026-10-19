using Rhino;
using Rhino.DocObjects;
using Rhino.Geometry;

namespace RhinoContextAssist;

internal enum SelectionKind
{
    None, Multiple, Mixed, Curve, ClosedPlanarCurve, Surface, PlanarSurface,
    Brep, SolidBrep, BrepFace, PlanarBrepFace, BrepEdge, Mesh, SubD
}

internal sealed record SelectionContext(SelectionKind Kind, int Count, string Caption);

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
                if (first.ComponentIndexType == ComponentIndexType.BrepFace && obj.Geometry is Brep brep)
                {
                    var face = brep.Faces[first.Index];
                    return face.TryGetPlane(out _)
                        ? new(SelectionKind.PlanarBrepFace, 1, "Planare Flaeche")
                        : new(SelectionKind.BrepFace, 1, "Brep-Flaeche");
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
        switch (obj.Geometry)
        {
            case Curve c:
                return c.IsClosed && c.IsPlanar()
                    ? new(SelectionKind.ClosedPlanarCurve, 1, "Geschlossene planare Kurve")
                    : new(SelectionKind.Curve, 1, "Kurve");
            case Brep b:
                if (b.Faces.Count == 1)
                    return b.Faces[0].TryGetPlane(out _)
                        ? new(SelectionKind.PlanarSurface, 1, "Planare Flaeche")
                        : new(SelectionKind.Surface, 1, "Flaeche");
                return b.IsSolid
                    ? new(SelectionKind.SolidBrep, 1, "Geschlossener Brep")
                    : new(SelectionKind.Brep, 1, "Brep");
            case Extrusion e:
                return e.IsSolid
                    ? new(SelectionKind.SolidBrep, 1, "Geschlossene Extrusion")
                    : new(SelectionKind.Surface, 1, "Extrusion");
            case Mesh:
                return new(SelectionKind.Mesh, 1, "Mesh");
            case SubD:
                return new(SelectionKind.SubD, 1, "SubD");
            default:
                return new(SelectionKind.Multiple, 1, obj.ObjectType.ToString());
        }
    }
}
