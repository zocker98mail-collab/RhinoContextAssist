namespace RhinoContextAssist;
internal static class ActionService {
 static readonly ContextAction Envelope=new("boundingboxcurves","⬚","Linien-Hüllkörper erzeugen","_BoundingBox _CoordinateSystem=_World _Cumulative=_Yes _Output=_Curves","Linienhülle erzeugen","Erzeugen");
 internal static IReadOnlyList<ContextAction> Get(SelectionContext c,ContextAssistSettings s){var kind=c.Kind==SelectionKind.MultipleEnvelopeObjects?SelectionKind.Multiple:c.Kind;var all=ActionCatalog.Defaults(kind).Concat(s.CustomActions.Where(x=>x.Requirement==c.Kind.ToString()||x.Requirement=="Any")).ToList();all=all.Where(x=>Applicable(x,c)).ToList();if(c.Kind is SelectionKind.SolidBrep or SelectionKind.Brep or SelectionKind.Mesh or SelectionKind.MultipleEnvelopeObjects)all.Add(Envelope);return s.Order(c.Kind,all);}
 static bool Applicable(ContextAction a,SelectionContext c)=>a.Requirement switch{"Multiple"=>c.Count>1,"Open"=>c.IsOpen,"Coplanar"=>c.HasMultipleCoplanarFaces,"OtherBlocks"=>c.HasOtherBlockDefinitions,_=>true};
}
