using Rhino;
namespace RhinoContextAssist;
internal sealed class ContextAssistSettings{
 readonly PersistentSettings store; public bool Enabled{get;set;} public int PrimaryCount{get;set;} public PopupPositionMode PositionMode{get;set;} public Dictionary<SelectionKind,List<ActionPreference>> Preferences{get;}=new();
 public ContextAssistSettings(PersistentSettings s){store=s;Load();}
 public void Load(){Enabled=store.GetBool("Enabled",true);PrimaryCount=Math.Clamp(store.GetInteger("PrimaryCount",3),1,5);PositionMode=store.GetEnumValue("PositionMode",PopupPositionMode.Mouse);foreach(var kind in ActionCatalog.ConfigurableKinds){var defaults=ActionCatalog.Defaults(kind);var order=store.GetString($"Actions.{kind}",string.Join(";",defaults.Select(a=>$"{a.Id}|1|0")));Preferences[kind]=Parse(order,defaults);}}
 static List<ActionPreference> Parse(string text,IReadOnlyList<ContextAction> defaults){var known=defaults.ToDictionary(x=>x.Id);var result=new List<ActionPreference>();foreach(var part in text.Split(';',StringSplitOptions.RemoveEmptyEntries)){var p=part.Split('|');if(p.Length>=3&&known.ContainsKey(p[0]))result.Add(new(p[0],p[1]=="1",p[2]=="1"));}foreach(var a in defaults)if(result.All(x=>x.Id!=a.Id))result.Add(new(a.Id,true,false));return result;}
 public void Save(){store.SetBool("Enabled",Enabled);store.SetInteger("PrimaryCount",PrimaryCount);store.SetEnumValue("PositionMode",PositionMode);foreach(var pair in Preferences)store.SetString($"Actions.{pair.Key}",string.Join(";",pair.Value.Select(x=>$"{x.Id}|{(x.Enabled?1:0)}|{(x.Favorite?1:0)}")));}
 public IReadOnlyList<ContextAction> Resolve(SelectionKind kind){var actions=ActionCatalog.Defaults(kind).ToDictionary(x=>x.Id);if(!Preferences.TryGetValue(kind,out var prefs))return actions.Values.ToList();return prefs.Where(x=>x.Enabled&&actions.ContainsKey(x.Id)).OrderByDescending(x=>x.Favorite).Select(x=>actions[x.Id]).ToList();}
 public bool IsFavorite(SelectionKind kind,string id)=>Preferences.TryGetValue(kind,out var p)&&p.Any(x=>x.Id==id&&x.Favorite);
}
