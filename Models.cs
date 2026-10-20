using Rhino.Geometry;
namespace RhinoContextAssist;
internal enum SelectionKind { None, Multiple, Mixed, Curve, ClosedPlanarCurve, Surface, PlanarSurface, Brep, SolidBrep, BrepFace, PlanarBrepFace, BrepEdge, Mesh, MeshFace, MeshEdge, MeshVertex, SubD, SubDFace, SubDEdge, SubDVertex }
internal enum PopupPositionMode { Mouse, Geometry }
internal sealed record SelectionContext(SelectionKind Kind, int Count, string Caption, Point3d Anchor);
internal sealed record ContextAction(string Id, string Icon, string Label, string Macro, string Hint);
internal sealed record ActionPreference(string Id, bool Enabled, bool Favorite);
internal sealed record UsageEntry(string ActionId, long Count, DateTime LastUsedUtc);
