using Rhino.Geometry;
namespace RhinoContextAssist;
internal enum SelectionKind{None,Multiple,Mixed,Curve,ClosedPlanarCurve,Surface,PlanarSurface,Brep,SolidBrep,BrepFace,PlanarBrepFace,BrepEdge,Mesh,SubD}
internal enum PopupPositionMode{Mouse,Geometry}
internal sealed record SelectionContext(SelectionKind Kind,int Count,string Caption,Point3d Anchor){public string Signature=>$"{Kind}:{Count}:{Caption}";}
internal sealed record ContextAction(string Id,string Label,string Macro,string Hint);
internal sealed record ActionPreference(string Id,bool Enabled,bool Favorite);
