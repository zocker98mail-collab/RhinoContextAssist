using System.Drawing;
using Rhino.Display;
using Rhino.Geometry;

namespace RhinoContextAssist;

/// <summary>
/// Leichter DisplayConduit für die Stable-Hybrid-Architektur.
/// Zeichnet ausschließlich einen dezenten 3D/2D-Ankerpunkt an der Auswahl
/// sowie optional eine dezente Verbindungslinie zur WPF-Toolbar.
/// Zeichnet ausdrücklich KEINE Buttons und führt KEIN Mouse-Hit-Testing durch.
/// </summary>
internal sealed class ContextAssistAnchorConduit : DisplayConduit
{
    public Point3d AnchorWorld { get; set; } = Point3d.Unset;
    public Point2d? ToolbarScreenAnchor { get; set; }
    public bool ShowAnchorDot { get; set; } = true;
    public bool ShowTetherLine { get; set; }
    public Guid ActiveViewportId { get; set; } = Guid.Empty;

    protected override void DrawForeground(DrawEventArgs e)
    {
        if (!AnchorWorld.IsValid)
            return;

        if (ActiveViewportId != Guid.Empty && e.Viewport.Id != ActiveViewportId)
            return;

        var screenPt3d = e.Viewport.WorldToClient(AnchorWorld);
        var bounds = e.Viewport.Bounds;
        if (screenPt3d.X < 0 || screenPt3d.Y < 0 || screenPt3d.X > bounds.Width || screenPt3d.Y > bounds.Height)
            return;

        var anchor2d = new Point2d(screenPt3d.X, screenPt3d.Y);

        if (ShowTetherLine && ToolbarScreenAnchor.HasValue)
        {
            var target = ToolbarScreenAnchor.Value;
            e.Display.Draw2dLine(
                new PointF((float)anchor2d.X, (float)anchor2d.Y),
                new PointF((float)target.X, (float)target.Y),
                Color.FromArgb(110, 130, 195, 255),
                1.5f);
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
