using Rhino; using Rhino.DocObjects; using Rhino.Geometry;
namespace RhinoContextAssist;
internal static class SelectionAnalyzer
{
    public static SelectionContext Analyze(RhinoDoc doc)
    {
        var selected=doc.Objects.GetSelectedObjects(false,false).ToList();
        if(selected.Count==0) selected=doc.Objects.Where(o=>o.GetSelectedSubObjects()?.Length>0).ToList();
        if(selected.Count==0) return new(SelectionKind.None,0,"Keine Auswahl",Point3d.Unset);
        var anchor=UnionBox(selected).Center;
        if(selected.Count==1)
        {
            var obj=selected[0]; var sub=obj.GetSelectedSubObjects();
            if(sub is {Length:>0}) return AnalyzeSubObject(obj,sub[0],anchor);
            return AnalyzeObject(obj,anchor);
        }
        var kinds=selected.Select(o=>AnalyzeObject(o,anchor).Kind).Distinct().ToList();
        return kinds.Count==1?new(SelectionKind.Multiple,selected.Count,$"{selected.Count} Objekte",anchor):new(SelectionKind.Mixed,selected.Count,$"{selected.Count} gemischte Objekte",anchor);
    }
    private static SelectionContext AnalyzeSubObject(RhinoObject obj,ComponentIndex ci,Point3d fallback)
    {
        var type=ci.ComponentIndexType.ToString();
        var brep=obj.Geometry switch { Brep b=>b, Extrusion e=>e.ToBrep(), _=>null };
        if(type=="BrepFace"&&brep!=null&&ci.Index>=0&&ci.Index<brep.Faces.Count)
        { var f=brep.Faces[ci.Index];var a=f.GetBoundingBox(true).Center;return f.TryGetPlane(out _)?new(SelectionKind.PlanarBrepFace,1,"Planare Teilfläche",a):new(SelectionKind.BrepFace,1,"Teilfläche",a); }
        if(type=="BrepEdge"&&brep!=null&&ci.Index>=0&&ci.Index<brep.Edges.Count)
            return new(SelectionKind.BrepEdge,1,"Brep-Kante",brep.Edges[ci.Index].GetBoundingBox(true).Center);
        if(obj.Geometry is Mesh mesh)
        {
            if(type.Contains("Face",StringComparison.OrdinalIgnoreCase)) return new(SelectionKind.MeshFace,1,"Mesh-Fläche",MeshFaceCenter(mesh,ci.Index,fallback));
            if(type.Contains("TopologyEdge",StringComparison.OrdinalIgnoreCase)||type.Contains("Edge",StringComparison.OrdinalIgnoreCase)) return new(SelectionKind.MeshEdge,1,"Mesh-Kante",fallback);
            if(type.Contains("Vertex",StringComparison.OrdinalIgnoreCase)) return new(SelectionKind.MeshVertex,1,"Mesh-Vertex",MeshVertex(mesh,ci.Index,fallback));
        }
        if(obj.Geometry is SubD)
        {
            if(type.Contains("Face",StringComparison.OrdinalIgnoreCase)) return new(SelectionKind.SubDFace,1,"SubD-Fläche",fallback);
            if(type.Contains("Edge",StringComparison.OrdinalIgnoreCase)) return new(SelectionKind.SubDEdge,1,"SubD-Kante",fallback);
            if(type.Contains("Vertex",StringComparison.OrdinalIgnoreCase)) return new(SelectionKind.SubDVertex,1,"SubD-Vertex",fallback);
        }
        return AnalyzeObject(obj,fallback);
    }
    private static Point3d MeshFaceCenter(Mesh m,int i,Point3d fallback)
    { if(i<0||i>=m.Faces.Count)return fallback;var f=m.Faces[i];var pts=new List<Point3d>{m.Vertices.Point3dAt(f.A),m.Vertices.Point3dAt(f.B),m.Vertices.Point3dAt(f.C)};if(f.IsQuad)pts.Add(m.Vertices.Point3dAt(f.D));return new Point3d(pts.Average(p=>p.X),pts.Average(p=>p.Y),pts.Average(p=>p.Z)); }
    private static Point3d MeshVertex(Mesh m,int i,Point3d fallback)=>i>=0&&i<m.Vertices.Count?m.Vertices.Point3dAt(i):fallback;
    private static BoundingBox UnionBox(List<RhinoObject> objects){var b=BoundingBox.Empty;foreach(var o in objects)b.Union(o.Geometry.GetBoundingBox(true));return b;}
    private static SelectionContext AnalyzeObject(RhinoObject o,Point3d a)=>o.Geometry switch
    { Curve c when c.IsClosed&&c.IsPlanar()=>new(SelectionKind.ClosedPlanarCurve,1,"Geschlossene planare Kurve",a),Curve=>new(SelectionKind.Curve,1,"Kurve",a),Brep b when b.Faces.Count==1&&b.Faces[0].TryGetPlane(out _)=>new(SelectionKind.PlanarSurface,1,"Planare Fläche",a),Brep b when b.Faces.Count==1=>new(SelectionKind.Surface,1,"Fläche",a),Brep b when b.IsSolid=>new(SelectionKind.SolidBrep,1,"Geschlossener Körper",a),Brep=>new(SelectionKind.Brep,1,"Flächenverband",a),Extrusion e when e.IsSolid=>new(SelectionKind.SolidBrep,1,"Geschlossene Extrusion",a),Extrusion=>new(SelectionKind.Surface,1,"Extrusion",a),Mesh=>new(SelectionKind.Mesh,1,"Mesh",a),SubD=>new(SelectionKind.SubD,1,"SubD",a),_=>new(SelectionKind.Multiple,1,o.ObjectType.ToString(),a)};
}
