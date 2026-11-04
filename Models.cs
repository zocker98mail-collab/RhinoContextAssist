using Rhino.Geometry;

namespace RhinoContextAssist;

internal enum SelectionKind
{
    None,
    Multiple,
    MultipleEnvelopeObjects,
    Mixed,
    Curve,
    ClosedPlanarCurve,
    Surface,
    PlanarSurface,
    Brep,
    SolidBrep,
    BrepFace,
    PlanarBrepFace,
    BrepEdge,
    Mesh,
    MeshFace,
    MeshEdge,
    MeshVertex,
    SubD,
    SubDFace,
    SubDEdge,
    SubDVertex,
    Block
}

internal enum ContextLevel
{
    Basic,
    Planar,
    Open,
    Closed,
    SubObject,
    Multiple,
    BlockInstance
}

internal enum PopupPositionMode
{
    Mouse,
    Geometry,
    GeometryOffset,
    AutoViewportCorner,
    TopLeft,
    TopRight,
    BottomLeft,
    BottomRight
}

internal enum PopupUxMode
{
    Direct,
    Compact,
    Manual
}

internal sealed record SelectionContext(
    SelectionKind Kind,
    ContextLevel Level,
    int Count,
    string Caption,
    Point3d Anchor,
    bool IsOpen = false,
    bool HasMultipleFaces = false,
    bool HasOtherBlocks = false);

internal sealed record ContextAction(
    string Id,
    string Icon,
    string Label,
    string Macro,
    string Hint,
    string Category = "Bearbeiten",
    bool Destructive = false,
    string Requirement = "Any",
    bool Custom = false);

internal sealed record ActionPreference(
    string Id,
    bool Enabled,
    bool Favorite);

internal sealed record UsageEntry(
    string ActionId,
    long Count,
    DateTime LastUsedUtc);
