using Rhino;
using Rhino.DocObjects;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RhinoContextAssist;

internal static class SelectionAnalyzer
{
    private const int MaxAnalyzedObjects = 250;
    private static readonly HashSet<Guid> TrackedObjectIds = new();

    internal static void TrackSelectedObjects(IEnumerable<RhinoObject>? objects)
    {
        if (objects == null) return;
        foreach (var o in objects)
        {
            if (o != null && o.Id != Guid.Empty)
                TrackedObjectIds.Add(o.Id);
        }
    }

    internal static void ClearTrackedObjects() => TrackedObjectIds.Clear();

    internal static List<RhinoObject> GetEffectiveSelectedObjects(RhinoDoc doc)
    {
        var s = doc.Objects.GetSelectedObjects(includeLights: false, includeGrips: false)
            .Where(o => o != null && o.IsValid && !o.IsDeleted)
            .ToList();

        if (s.Count > 0)
        {
            foreach (var o in s) TrackedObjectIds.Add(o.Id);
            return s;
        }

        // 1. O(1) Fast-Path: Prüfe zuerst die über SelectObjects gemerkten Objekt-IDs auf Sub-Objekt-Auswahl
        if (TrackedObjectIds.Count > 0)
        {
            var deadIds = new List<Guid>();
            foreach (var id in TrackedObjectIds)
            {
                var obj = doc.Objects.FindId(id);
                if (obj == null || !obj.IsValid || obj.IsDeleted)
                {
                    deadIds.Add(id);
                    continue;
                }
                if (obj.IsSelected(checkSubObjects: true) > 0 || obj.GetSelectedSubObjects() is { Length: > 0 })
                {
                    s.Add(obj);
                }
                else
                {
                    deadIds.Add(id);
                }
            }
            foreach (var dead in deadIds) TrackedObjectIds.Remove(dead);
            if (s.Count > 0) return s;
        }

        // 2. Fallback: Nur sichtbare, entsperrte NormalObjects über IsSelected(true) prüfen
        var settings = new ObjectEnumeratorSettings
        {
            NormalObjects = true,
            LockedObjects = false,
            HiddenObjects = false,
            ReferenceObjects = true,
            IncludeLights = false,
            IncludeGrips = false
        };

        foreach (var o in doc.Objects.GetObjectList(settings))
        {
            if (o != null && o.IsValid && !o.IsDeleted && o.IsSelected(checkSubObjects: true) > 0)
            {
                s.Add(o);
                TrackedObjectIds.Add(o.Id);
            }
        }

        return s;
    }

    internal static bool HasAnySelectionFast(RhinoDoc doc)
    {
        if (doc.Objects.GetSelectedObjects(false, false).Any())
            return true;

        if (TrackedObjectIds.Count > 0)
        {
            foreach (var id in TrackedObjectIds.ToList())
            {
                var obj = doc.Objects.FindId(id);
                if (obj != null && obj.IsValid && !obj.IsDeleted &&
                    (obj.IsSelected(checkSubObjects: true) > 0 || obj.GetSelectedSubObjects() is { Length: > 0 }))
                {
                    return true;
                }
                TrackedObjectIds.Remove(id);
            }
        }

        return false;
    }

    internal static SelectionContext Analyze(RhinoDoc doc)
    {
        if (doc == null)
            return new(SelectionKind.None, ContextLevel.Basic, 0, "Keine Auswahl", Point3d.Unset);

        var s = GetEffectiveSelectedObjects(doc);

        if (s.Count == 0)
            return new(SelectionKind.None, ContextLevel.Basic, 0, "Keine Auswahl", Point3d.Unset);

        int totalCount = s.Count;
        var sample = totalCount > MaxAnalyzedObjects ? s.Take(MaxAnalyzedObjects).ToList() : s;

        var anchor = Box(sample).Center;
        if (totalCount == 1)
        {
            var o = sample[0];
            var sub = o.GetSelectedSubObjects();
            if (sub is { Length: > 0 })
                return Sub(doc, o, sub[0], anchor);
            return Obj(doc, o, anchor);
        }

        var cs = sample.Select(o => Obj(doc, o, anchor)).ToList();
        if (cs.All(x => x.Kind is SelectionKind.SolidBrep or SelectionKind.Brep or SelectionKind.Mesh))
            return new(SelectionKind.MultipleEnvelopeObjects, ContextLevel.Multiple, totalCount, $"{totalCount} Körper/Flächenverbände/Meshes", anchor);

        var ks = cs.Select(x => x.Kind).Distinct().ToList();
        return new(
            ks.Count == 1 ? SelectionKind.Multiple : SelectionKind.Mixed,
            ContextLevel.Multiple,
            totalCount,
            ks.Count == 1 ? $"{totalCount} Objekte" : $"{totalCount} gemischte Objekte",
            anchor);
    }

    static SelectionContext Sub(RhinoDoc d, RhinoObject o, ComponentIndex ci, Point3d a)
    {
        var n = ci.ComponentIndexType.ToString();
        Brep? tempBrep = null;
        try
        {
            Brep? b = null;
            if (o.Geometry is Brep existingBrep)
            {
                b = existingBrep;
            }
            else if (o.Geometry is Extrusion ext)
            {
                tempBrep = ext.ToBrep();
                b = tempBrep;
            }

            if (n == "BrepFace" && b != null && ci.Index >= 0 && ci.Index < b.Faces.Count)
            {
                var f = b.Faces[ci.Index];
                var p = f.GetBoundingBox(false).Center;
                return f.TryGetPlane(out _)
                    ? new(SelectionKind.PlanarBrepFace, ContextLevel.Planar, 1, "Planare Teilfläche", p)
                    : new(SelectionKind.BrepFace, ContextLevel.SubObject, 1, "Teilfläche", p);
            }

            if (n == "BrepEdge")
            {
                var edgeAnchor = (b != null && ci.Index >= 0 && ci.Index < b.Edges.Count)
                    ? b.Edges[ci.Index].GetBoundingBox(false).Center
                    : a;
                return new(SelectionKind.BrepEdge, ContextLevel.SubObject, 1, "Brep-Kante", edgeAnchor);
            }
        }
        finally
        {
            tempBrep?.Dispose();
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

        var geom = o.Geometry;
        if (geom == null)
            return new(SelectionKind.Mixed, ContextLevel.Basic, 1, o.ObjectType.ToString(), a);

        return geom switch
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
        {
            try
            {
                if (o?.Geometry != null)
                {
                    var ob = o.Geometry.GetBoundingBox(false);
                    if (ob.IsValid) b.Union(ob);
                }
            }
            catch { }
        }
        return b;
    }
}
