using Rhino;
namespace RhinoContextAssist;
internal sealed class ContextAssistSettings
{
    private readonly PersistentSettings _store;
    public bool Enabled { get; set; }
    public int PrimaryCount { get; set; }
    public PopupPositionMode PositionMode { get; set; }
    public bool AutomaticPrioritization { get; set; }
    public Dictionary<SelectionKind,List<ActionPreference>> Preferences { get; } = new();
    public Dictionary<string,UsageEntry> Usage { get; } = new();

    public ContextAssistSettings(PersistentSettings store) { _store=store; Load(); }
    public void Load()
    {
        Enabled=_store.GetBool("Enabled",true);
        PrimaryCount=Math.Clamp(_store.GetInteger("PrimaryCount",3),1,5);
        PositionMode=_store.GetEnumValue("PositionMode",PopupPositionMode.Mouse);
        AutomaticPrioritization=_store.GetBool("AutomaticPrioritization",true);
        foreach(var kind in ActionCatalog.ConfigurableKinds)
        {
            var defaults=ActionCatalog.Defaults(kind);
            var text=_store.GetString($"Actions.{kind}",string.Join(";",defaults.Select(a=>$"{a.Id}|1|0")));
            Preferences[kind]=ParsePreferences(text,defaults);
        }
        var usage=_store.GetString("Usage","");
        foreach(var row in usage.Split(';',StringSplitOptions.RemoveEmptyEntries))
        {
            var p=row.Split('|');
            if(p.Length==3 && long.TryParse(p[1],out var count) && long.TryParse(p[2],out var ticks))
                Usage[p[0]]=new(p[0],count,new DateTime(ticks,DateTimeKind.Utc));
        }
    }
    private static List<ActionPreference> ParsePreferences(string text,IReadOnlyList<ContextAction> defaults)
    {
        var known=defaults.ToDictionary(x=>x.Id); var result=new List<ActionPreference>();
        foreach(var row in text.Split(';',StringSplitOptions.RemoveEmptyEntries))
        { var p=row.Split('|'); if(p.Length>=3&&known.ContainsKey(p[0])) result.Add(new(p[0],p[1]=="1",p[2]=="1")); }
        foreach(var a in defaults) if(result.All(x=>x.Id!=a.Id)) result.Add(new(a.Id,true,false));
        return result;
    }
    public void Save()
    {
        _store.SetBool("Enabled",Enabled); _store.SetInteger("PrimaryCount",PrimaryCount);
        _store.SetEnumValue("PositionMode",PositionMode); _store.SetBool("AutomaticPrioritization",AutomaticPrioritization);
        foreach(var pair in Preferences)
            _store.SetString($"Actions.{pair.Key}",string.Join(";",pair.Value.Select(x=>$"{x.Id}|{(x.Enabled?1:0)}|{(x.Favorite?1:0)}")));
        _store.SetString("Usage",string.Join(";",Usage.Values.Select(x=>$"{x.ActionId}|{x.Count}|{x.LastUsedUtc.Ticks}")));
    }
    public IReadOnlyList<ContextAction> Resolve(SelectionKind kind)
    {
        var catalog=ActionCatalog.Defaults(kind).ToDictionary(x=>x.Id);
        if(!Preferences.TryGetValue(kind,out var prefs)) return catalog.Values.ToList();
        var indexed=prefs.Select((p,i)=>new{Pref=p,Index=i}).Where(x=>x.Pref.Enabled&&catalog.ContainsKey(x.Pref.Id));
        if(AutomaticPrioritization)
            indexed=indexed.OrderByDescending(x=>x.Pref.Favorite).ThenByDescending(x=>Usage.TryGetValue(x.Pref.Id,out var u)?u.Count:0).ThenBy(x=>x.Index);
        else indexed=indexed.OrderByDescending(x=>x.Pref.Favorite).ThenBy(x=>x.Index);
        return indexed.Select(x=>catalog[x.Pref.Id]).ToList();
    }
    public bool IsFavorite(SelectionKind kind,string id)=>Preferences.TryGetValue(kind,out var p)&&p.Any(x=>x.Id==id&&x.Favorite);
    public long UsageCount(string id)=>Usage.TryGetValue(id,out var u)?u.Count:0;
    public void RecordUsage(string id){var now=DateTime.UtcNow;Usage[id]=Usage.TryGetValue(id,out var u)?u with{Count=u.Count+1,LastUsedUtc=now}:new(id,1,now);Save();}
    public void ResetUsage(){Usage.Clear();Save();}
}
