namespace RhinoContextAssist;
internal static class ContextActionPolicy
{
    private const string EnvelopeId = "boundingboxcurves";
    private static readonly ContextAction Envelope = new(EnvelopeId, "⬚", "Linien-Hüllkörper erzeugen", "_BoundingBox _CoordinateSystem=_World _Cumulative=_Yes _Output=_Curves", "Gemeinsame weltachsenorientierte Linienhülle erzeugen");
    internal static IReadOnlyList<ContextAction> Apply(SelectionContext context, IReadOnlyList<ContextAction> source)
    {
        var result = source.Where(x => x.Id != EnvelopeId).ToList();
        if (context.Kind is SelectionKind.SolidBrep or SelectionKind.Brep or SelectionKind.Mesh or SelectionKind.MultipleEnvelopeObjects)
            result.Add(Envelope);
        return result;
    }
}
