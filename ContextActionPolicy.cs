namespace RhinoContextAssist;
internal static class ContextActionPolicy
{
    private const string EnvelopeId = "boundingboxcurves";
    private static ContextAction EnvelopeAction => new(EnvelopeId,"⬚","Linien-Hüllkörper erzeugen","_BoundingBox _CoordinateSystem=_World _Cumulative=_Yes _Output=_Curves","Gemeinsame weltachsenorientierte Linienhülle erzeugen");

    public static IReadOnlyList<ContextAction> Apply(SelectionContext context,IReadOnlyList<ContextAction> source,ContextAssistSettings settings)
    {
        var result=source.Where(x=>x.Id!=EnvelopeId).ToList();
        var eligible=context.Kind is SelectionKind.SolidBrep or SelectionKind.Brep or SelectionKind.Mesh or SelectionKind.MultipleEnvelopeObjects;
        if(eligible)result.Add(EnvelopeAction);
        return result;
    }
}
