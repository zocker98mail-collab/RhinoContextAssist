using System.Drawing;
using Rhino.Display;
using Rhino.Geometry;

namespace RhinoContextAssist;

/// <summary>
/// Leichter DisplayConduit für die Stable-Hybrid-Architektur.
/// Zeichnet ausschließlich einen dezenten 3D/2D-Ankerpunkt an der Auswahl
/// sowie optional eine dezente Verbindungslinie zur WPF-Toolbar.
/// Prüft zudem, ob der Ankerpunkt im aktuellen Kamerasichtfeld liegt.
/// Zeichnet ausdrücklich KEINE Buttons und führt KEIN Mouse-Hit-Testing durch.
/// </summary>
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

    public static bool CheckAnchorVisible(RhinoViewport viewport, Point3d anchorWorld, out Point2d clientPoint)
    {
        clientPoint = Point2d.Unset;
        if (!anchorWorld.IsValid)
            return false;

        // Prüfe, ob der Punkt vor der Kamera liegt (WorldToClip Z-Check)
        var xform = viewport.GetTransform(CoordinateSystem.World, CoordinateSystem.Clip);
        Point3d clipPt = xform * anchorWorld;
        if (clipPt.Z < -1.0 || clipPt.Z > 1.0)
            return false;

        var screenPt3d = viewport.WorldToClient(anchorWorld);
        var bounds = viewport.Bounds;
        clientPoint = new Point2d(screenPt3d.X, screenPt3d.Y);

        return screenPt3d.X >= 0 &&
               screenPt3d.Y >= 0 &&
               screenPt3d.X <= bounds.Width &&
               screenPt3d.Y <= bounds.Height;
    }

    protected override void DrawForeground(DrawEventArgs e)
    {
        if (!AnchorWorld.IsValid)
            return;

        if (ActiveViewportId != Guid.Empty && e.Viewport.Id != ActiveViewportId)
            return;

        bool visible = CheckAnchorVisible(e.Viewport, AnchorWorld, out var anchor2d);
        IsAnchorVisibleInViewport = visible;
        if (!visible)
            return;

        if (ShowTetherLine && ToolbarScreenAnchor.HasValue)
        {
            var target = ToolbarScreenAnchor.Value;
            int alpha = Math.Clamp(TetherOpacity, 20, 255);
            float thickness = Math.Clamp(TetherThickness, 0.5f, 5.0f);

            e.Display.Draw2dLine(
                new PointF((float)anchor2d.X, (float)anchor2d.Y),
                new PointF((float)target.X, (float)target.Y),
                Color.FromArgb(alpha, 130, 195, 255),
                thickness);
        }

        if (ShowAnchorDot)
        {
            e.Display.DrawPoint(
                AnchorWorld,
                PointStyle.RoundControlPoint,
                Color.FromArgb(235, 255, 255, 255),
                Color.FromArgb(200, 70, 165, 255),
                6.0f,
                1.5f,
                0.0f,
                0.0f,
                true,
                true);
        }
    }
}
