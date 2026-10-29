using System.Drawing;
namespace RhinoContextAssist;
internal sealed class HudState{
 internal SelectionContext Context{get;set;}=new(SelectionKind.None,0,"Keine Auswahl",Rhino.Geometry.Point3d.Unset);
 internal List<ContextAction> Actions{get;}=new();internal List<HudHitRegion> Regions{get;}=new();internal int HoverIndex{get;set;}=-1;internal bool Expanded{get;set;}=true;internal DateTime HoverSince{get;set;}=DateTime.MaxValue;internal Rectangle Bounds{get;set;}=Rectangle.Empty;internal uint ViewSerial{get;set;}
}
