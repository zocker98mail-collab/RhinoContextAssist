using System.Drawing;
using Rhino.Display;
using Rhino.DocObjects;
using Rhino.Geometry;

namespace RhinoContextAssist;

internal sealed class ContextAssistAnchorConduit : DisplayConduit
{
    public Point3d AnchorWorld { get; set; } = Point3d.Unset;
    public Point2d? ToolbarScreenAnchor { get; set; }
    public bool ShowAnchorDot { get; set; } = true;
    public bool ShowTetherLine { get; set; }
    public int TetherOpacity { get; set; } = 115;
    public float TetherThickness { get; set; } = 1.5f;
    public Guid ActiveViewportId { get; set; } = Guid.Empty;
    public bool IsAnchorVisibleInViewport { get; private set; } = true;
    
    // Enthält temporär den Namen des Macros für Hover-Highlights
    public string? HighlightMode { get; set; }

    public static bool CheckAnchorVisible(RhinoViewport viewport, Point3d anchorWorld, out Point2d clientPoint)
    {
        clientPoint = Point2d.Unset;
        if (!anchorWorld.IsValid) return false;
        var xform = viewport.GetTransform(CoordinateSystem.World, CoordinateSystem.Clip);
        Point3d clipPt = xform * anchorWorld;
        if (clipPt.Z < -1.0 || clipPt.Z > 1.0) return false;
        var screenPt3d = viewport.WorldToClient(anchorWorld);
        var bounds = viewport.Bounds;
        clientPoint = new Point2d(screenPt3d.X, screenPt3d.Y);
        return screenPt3d.X >= 0 && screenPt3d.Y >= 0 && screenPt3d.X <= bounds.Width && screenPt3d.Y <= bounds.Height;
    }

    protected override void DrawForeground(DrawEventArgs e)
    {
        if (!AnchorWorld.IsValid) return;
        if (ActiveViewportId != Guid.Empty && e.Viewport.Id != ActiveViewportId) return;

        bool visible = CheckAnchorVisible(e.Viewport, AnchorWorld, out var anchor2d);
        IsAnchorVisibleInViewport = visible;
        if (!visible) return;

        if (ShowTetherLine && ToolbarScreenAnchor.HasValue)
        {
            var target = ToolbarScreenAnchor.Value;
            e.Display.Draw2dLine(
                new PointF((float)anchor2d.X, (float)anchor2d.Y),
                new PointF((float)target.X, (float)target.Y),
                Color.FromArgb(Math.Clamp(TetherOpacity, 20, 255), 130, 195, 255),
                Math.Clamp(TetherThickness, 0.5f, 5.0f));
        }

        if (ShowAnchorDot)
        {
            // Wenn über einen Button gehovert wird, leuchtet der Anker hell auf (oder die Kanten, falls erweitert)
            bool isHovered = !string.IsNullOrEmpty(HighlightMode);
            Color outlineColor = isHovered ? Color.FromArgb(255, 255, 120, 0) : Color.FromArgb(200, 70, 165, 255);
            float radius = isHovered ? 8.0f : 6.0f;
            float border = isHovered ? 2.5f : 1.5f;

            e.Display.DrawPoint(
                AnchorWorld, PointStyle.RoundControlPoint,
                Color.FromArgb(245, 255, 255, 255), outlineColor,
                radius, border, 0.0f, 0.0f, true, true);
        }
    }
}
