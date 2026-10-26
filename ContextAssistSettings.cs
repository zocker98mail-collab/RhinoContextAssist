using Rhino;
namespace RhinoContextAssist;
internal sealed class ContextAssistSettings
{
    readonly PersistentSettings store;
    public bool Enabled{get;set;} public int PrimaryCount{get;set;} public PopupPositionMode PositionMode{get;set;} public bool AutomaticPrioritization{get;set;}
    public PopupUxMode UxMode{get;set;} public int SelectionDelayMs{get;set;} public int HoverDelayMs{get;set;} public int CollapseDelayMs{get;set;} public int MoreCollapseDelayMs{get;set;} public int AutoHideMs{get;set;} public int ProximityRadiusPx{get;set;} public int CursorOffsetPx{get;set;} public double IdleOpacity{get;set;} public double ActiveOpacity{get;set;}
    public Dictionary<SelectionKind,List<ActionPreference>> Preferences{get;}=new(); public Dictionary<string,UsageEntry> Usage{get;}=new();
    public ContextAssistSettings(PersistentSettings s){store=s;Load();}
    void Load(){
        Enabled=store.GetBool("Enabled",true); AutomaticPrioritization=store.GetBool("AutomaticPrioritization",true);
        UxMode=store.GetEnumValue("UxMode",PopupUxMode.Compact); PositionMode=store.GetEnumValue("PositionMode",PopupPositionMode.Mouse);
        PrimaryCount=Math.Clamp(store.GetInteger("PrimaryCount",5),1,5);
        SelectionDelayMs=Math.Clamp(store.GetInteger("UX.SelectionDelayMs",300),0,2000); HoverDelayMs=Math.Clamp(store.GetInteger("UX.HoverDelayMs",300),0,2000);
        CollapseDelayMs=Math.Clamp(store.GetInteger("UX.CollapseDelayMs",600),0,5000); MoreCollapseDelayMs=Math.Clamp(store.GetInteger("UX.MoreCollapseDelayMs",1500),0,10000);
        AutoHideMs=Math.Clamp(store.GetInteger("UX.AutoHideMs",5000),0,30000); ProximityRadiusPx=Math.Clamp(store.GetInteger("UX.ProximityRadiusPx",0),0,1000);
        CursorOffsetPx=Math.Clamp(store.GetInteger("UX.CursorOffsetPx",5),0,200); IdleOpacity=Math.Clamp(store.GetDouble("UX.IdleOpacity",0.42),0.15,1.0); ActiveOpacity=Math.Clamp(store.GetDouble("UX.ActiveOpacity",1.0),0.25,1.0);
        foreach(var k in ActionCatalog.ConfigurableKinds){var d=ActionCatalog.Defaults(k);var text=store.GetString($"Actions.{k}","");var known=d.ToDictionary(x=>x.Id);var list=new List<ActionPreference>();foreach(var row in text.Split(';',StringSplitOptions.RemoveEmptyEntries)){var p=row.Split('|');if(p.Length>=3&&known.ContainsKey(p[0]))list.Add(new(p[0],p[1]=="1",p[2]=="1"));}foreach(var a in d)if(list.All(x=>x.Id!=a.Id))list.Add(new(a.Id,true,false));Preferences[k]=list;}
        foreach(var row in store.GetString("Usage","").Split(';',StringSplitOptions.RemoveEmptyEntries)){var p=row.Split('|');if(p.Length==3&&long.TryParse(p[1],out var c)&&long.TryParse(p[2],out var t))Usage[p[0]]=new(p[0],c,new DateTime(t,DateTimeKind.Utc));}
    }
    public void Save(){store.SetBool("Enabled",Enabled);store.SetBool("AutomaticPrioritization",AutomaticPrioritization);store.SetEnumValue("UxMode",UxMode);store.SetEnumValue("PositionMode",PositionMode);store.SetInteger("PrimaryCount",PrimaryCount);store.SetInteger("UX.SelectionDelayMs",SelectionDelayMs);store.SetInteger("UX.HoverDelayMs",HoverDelayMs);store.SetInteger("UX.CollapseDelayMs",CollapseDelayMs);store.SetInteger("UX.MoreCollapseDelayMs",MoreCollapseDelayMs);store.SetInteger("UX.AutoHideMs",AutoHideMs);store.SetInteger("UX.ProximityRadiusPx",ProximityRadiusPx);store.SetInteger("UX.CursorOffsetPx",CursorOffsetPx);store.SetDouble("UX.IdleOpacity",IdleOpacity);store.SetDouble("UX.ActiveOpacity",ActiveOpacity);foreach(var x in Preferences)store.SetString($"Actions.{x.Key}",string.Join(";",x.Value.Select(v=>$"{v.Id}|{(v.Enabled?1:0)}|{(v.Favorite?1:0)}")));store.SetString("Usage",string.Join(";",Usage.Values.Select(x=>$"{x.ActionId}|{x.Count}|{x.LastUsedUtc.Ticks}")));}
    public IReadOnlyList<ContextAction> Resolve(SelectionKind k){var cat=ActionCatalog.Defaults(k).ToDictionary(x=>x.Id);if(!Preferences.TryGetValue(k,out var p))return cat.Values.ToList();var q=p.Select((x,i)=>new{P=x,I=i}).Where(x=>x.P.Enabled&&cat.ContainsKey(x.P.Id));q=AutomaticPrioritization?q.OrderByDescending(x=>x.P.Favorite).ThenByDescending(x=>Usage.TryGetValue(x.P.Id,out var u)?u.Count:0).ThenBy(x=>x.I):q.OrderByDescending(x=>x.P.Favorite).ThenBy(x=>x.I);return q.Select(x=>cat[x.P.Id]).ToList();}
    public bool IsFavorite(SelectionKind k,string id)=>Preferences.TryGetValue(k,out var p)&&p.Any(x=>x.Id==id&&x.Favorite);public long UsageCount(string id)=>Usage.TryGetValue(id,out var u)?u.Count:0;public void RecordUsage(string id){var n=DateTime.UtcNow;Usage[id]=Usage.TryGetValue(id,out var u)?u with{Count=u.Count+1,LastUsedUtc=n}:new(id,1,n);Save();}public void ResetUsage(){Usage.Clear();Save();}
}
