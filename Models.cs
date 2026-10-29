using Rhino.Geometry;
namespace RhinoContextAssist;
internal enum SelectionKind{None,Multiple,Mixed,Curve,ClosedPlanarCurve,Surface,PlanarSurface,Brep,SolidBrep,BrepFace,PlanarBrepFace,BrepEdge,Mesh,MeshFace,MeshEdge,MeshVertex,SubD,SubDFace,SubDEdge,SubDVertex,Block}
internal enum HudCorner{Auto,TopLeft,TopRight,BottomLeft,BottomRight}
internal sealed record SelectionContext(SelectionKind Kind,int Count,string Caption,Point3d Anchor,bool IsOpen=false,bool HasMultipleFaces=false);
internal sealed record ContextAction(string Id,string Icon,string Label,string Macro,string Hint,string Category="Bearbeiten",bool Destructive=false);
internal sealed record ActionPreference(string Id,bool Favorite,long Used);
internal sealed record HudHitRegion(System.Drawing.Rectangle Bounds,ContextAction? Action,string Kind);
