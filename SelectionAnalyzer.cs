using Rhino;
using Rhino.DocObjects;
using Rhino.Geometry;

namespace RhinoContextAssist;

internal static class SelectionAnalyzer
{
    internal static SelectionContext Analyze(RhinoDoc doc)
    {
        var s = doc.Objects.GetSelectedObjects(false, false).ToList();
        if (s.Count == 0)
            s = doc.Objects.Where(o => o.GetSelectedSubObjects()?.Length > 0).ToList();
        if (s.Count == 0)
            return new(SelectionKind.None, ContextLevel.Basic, 0, "Keine Auswahl", Point3d.Unset);

        var anchor = Box(s).Center;
        if (s.Count == 1)
        {
            var o = s[0];
            var sub = o.GetSelectedSubObjects();
            if (sub is { Length: > 0 })
                return Sub(doc, o, sub[0], anchor);
            return Obj(doc, o, anchor);
        }

        var cs = s.Select(o => Obj(doc, o, anchor)).ToList();
        if (cs.All(x => x.Kind is SelectionKind.SolidBrep or SelectionKind.Brep or SelectionKind.Mesh))
            return new(SelectionKind.MultipleEnvelopeObjects, ContextLevel.Multiple, s.Count, $"{s.Count} Körper/Flächenverbände/Meshes", anchor);

        var ks = cs.Select(x => x.Kind).Distinct().ToList();
        return new(
            ks.Count == 1 ? SelectionKind.Multiple : SelectionKind.Mixed,
            ContextLevel.Multiple,
            s.Count,
            ks.Count == 1 ? $"{s.Count} Objekte" : $"{s.Count} gemischte Objekte",
            anchor);
    }

    static SelectionContext Sub(RhinoDoc d, RhinoObject o, ComponentIndex ci, Point3d a)
    {
        var n = ci.ComponentIndexType.ToString();
        var b = o.Geometry switch
        {
            Brep x => x,
            Extrusion e => e.ToBrep(),
            _ => null
        };

        if (n == "BrepFace" && b != null && ci.Index >= 0 && ci.Index < b.Faces.Count)
        {
            var f = b.Faces[ci.Index];
            var p = f.GetBoundingBox(true).Center;
            return f.TryGetPlane(out _)
                ? new(SelectionKind.PlanarBrepFace, ContextLevel.Planar, 1, "Planare Teilfläche", p)
                : new(SelectionKind.BrepFace, ContextLevel.SubObject, 1, "Teilfläche", p);
        }

        if (n == "BrepEdge")
        {
            var edgeAnchor = (b != null && ci.Index >= 0 && ci.Index < b.Edges.Count)
                ? b.Edges[ci.Index].GetBoundingBox(true).Center
                : a;
            return new(SelectionKind.BrepEdge, ContextLevel.SubObject, 1, "Brep-Kante", edgeAnchor);
        }

        if (o.Geometry is Mesh)
        {
            if (n.Contains("Face"))
                return new(SelectionKind.MeshFace, ContextLevel.SubObject, 1, "Mesh-Fläche", a);
            if (n.Contains("Edge"))
                return new(SelectionKind.MeshEdge, ContextLevel.SubObject, 1, "Mesh-Kante", a);
            return new(SelectionKind.MeshVertex, ContextLevel.SubObject, 1, "Mesh-Vertex", a);
        }

        if (o.Geometry is SubD)
        {
            if (n.Contains("Face"))
                return new(SelectionKind.SubDFace, ContextLevel.SubObject, 1, "SubD-Fläche", a);
            if (n.Contains("Edge"))
                return new(SelectionKind.SubDEdge, ContextLevel.SubObject, 1, "SubD-Kante", a);
            return new(SelectionKind.SubDVertex, ContextLevel.SubObject, 1, "SubD-Vertex", a);
        }

        return Obj(d, o, a);
    }

    static SelectionContext Obj(RhinoDoc d, RhinoObject o, Point3d a)
    {
        if (o is InstanceObject)
            return new(SelectionKind.Block, ContextLevel.BlockInstance, 1, "Blockinstanz", a, HasOtherBlocks: d.InstanceDefinitions.ActiveCount > 1);

        return o.Geometry switch
        {
            Curve c when c.IsClosed && c.IsPlanar() => new(SelectionKind.ClosedPlanarCurve, ContextLevel.Planar, 1, "Geschlossene planare Kurve", a),
            Curve => new(SelectionKind.Curve, ContextLevel.Basic, 1, "Kurve", a),
            Brep b when b.IsSolid => new(SelectionKind.SolidBrep, ContextLevel.Closed, 1, "Geschlossener Körper", a, HasMultipleFaces: b.Faces.Count > 1),
            Brep b when b.Faces.Count == 1 && b.Faces[0].TryGetPlane(out _) => new(SelectionKind.PlanarSurface, ContextLevel.Planar, 1, "Planare Fläche", a),
            Brep b when b.Faces.Count == 1 => new(SelectionKind.Surface, ContextLevel.Basic, 1, "Fläche", a),
            Brep b => new(SelectionKind.Brep, ContextLevel.Open, 1, "Offener Flächenverband", a, true, b.Faces.Count > 1),
            Extrusion e when e.IsSolid => new(SelectionKind.SolidBrep, ContextLevel.Closed, 1, "Geschlossene Extrusion", a),
            Extrusion => new(SelectionKind.Surface, ContextLevel.Basic, 1, "Extrusion", a),
            Mesh => new(SelectionKind.Mesh, ContextLevel.Basic, 1, "Mesh", a),
            SubD => new(SelectionKind.SubD, ContextLevel.Basic, 1, "SubD", a),
            _ => new(SelectionKind.Mixed, ContextLevel.Basic, 1, o.ObjectType.ToString(), a)
        };
    }

    static BoundingBox Box(List<RhinoObject> s)
    {
        var b = BoundingBox.Empty;
        foreach (var o in s)
            b.Union(o.Geometry.GetBoundingBox(true));
        return b;
    }
}
