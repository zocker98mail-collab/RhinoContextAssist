using System.Text.Json;
using Rhino;

namespace RhinoContextAssist;

internal sealed class ContextAssistSettings
{
    private readonly PersistentSettings _store;

    public bool Enabled { get; set; }
    public bool AutomaticPrioritization { get; set; }
    public int PrimaryCount { get; set; }
    public PopupUxMode UxMode { get; set; }
    public PopupPositionMode PositionMode { get; set; }
    public int SelectionDelayMs { get; set; }
    public int HoverDelayMs { get; set; }
    public int CollapseDelayMs { get; set; }
    public int MoreCollapseDelayMs { get; set; }
    public int AutoHideMs { get; set; }
    public int CornerMarginPx { get; set; }
    public int IdleOpacity { get; set; }
    public int ActiveOpacity { get; set; }

    // Neue Option für den leichten Hybrid-Anker-Conduit
    public bool ShowViewportAnchor { get; set; }
    public bool ShowTetherLine { get; set; }

    public List<ContextAction> CustomActions { get; } = new();
    public Dictionary<SelectionKind, List<ActionPreference>> Preferences { get; } = new();
    public Dictionary<string, UsageEntry> Usage { get; } = new();

    public ContextAssistSettings(PersistentSettings store)
    {
        _store = store;
        Load();
    }

    public void Load()
    {
        Enabled = _store.GetBool("Enabled", true);
        AutomaticPrioritization = _store.GetBool("Auto", true);
        PrimaryCount = Math.Clamp(_store.GetInteger("Primary", 5), 1, 8);
        UxMode = _store.GetEnumValue("Ux", PopupUxMode.Compact);
        PositionMode = _store.GetEnumValue("Position", PopupPositionMode.AutoViewportCorner);

        SelectionDelayMs = Math.Clamp(_store.GetInteger("SelectionDelay", 300), 50, 2000);
        HoverDelayMs = Math.Clamp(_store.GetInteger("HoverDelay", 300), 50, 2000);
        CollapseDelayMs = Math.Clamp(_store.GetInteger("Collapse", 600), 100, 5000);
        MoreCollapseDelayMs = Math.Clamp(_store.GetInteger("MoreCollapse", 1500), 200, 10000);
        AutoHideMs = Math.Clamp(_store.GetInteger("AutoHide", 5000), 0, 30000);
        CornerMarginPx = Math.Clamp(_store.GetInteger("Margin", 90), 20, 350);
        IdleOpacity = Math.Clamp(_store.GetInteger("Idle", 42), 15, 100);
        ActiveOpacity = Math.Clamp(_store.GetInteger("Active", 100), 30, 100);

        ShowViewportAnchor = _store.GetBool("ShowViewportAnchor", true);
        ShowTetherLine = _store.GetBool("ShowTetherLine", false);

        CustomActions.Clear();
        try
        {
            var customJson = _store.GetString("Custom", "[]");
            var loadedCustom = JsonSerializer.Deserialize<List<ContextAction>>(customJson);
            if (loadedCustom != null)
                CustomActions.AddRange(loadedCustom);
        }
        catch
        {
            // Ignoriere beschädigtes JSON und nutze leere Liste
        }

        Usage.Clear();
        try
        {
            var usageJson = _store.GetString("Usage", "{}");
            var loadedUsage = JsonSerializer.Deserialize<Dictionary<string, UsageEntry>>(usageJson);
            if (loadedUsage != null)
            {
                foreach (var kv in loadedUsage)
                    Usage[kv.Key] = kv.Value;
            }
        }
        catch
        {
            // Ignoriere beschädigtes JSON
        }

        Preferences.Clear();
        foreach (var kind in ActionCatalog.Kinds)
        {
            var defaults = ActionCatalog.Defaults(kind)
                .Select(x => new ActionPreference(x.Id, true, false))
                .ToList();

            // Envelope-Aktion für passende Typen vormerken, damit auch diese favorisiert/sortiert werden kann
            if (kind is SelectionKind.SolidBrep or SelectionKind.Brep or SelectionKind.Mesh or SelectionKind.Multiple)
            {
                if (defaults.All(x => x.Id != "boundingboxcurves"))
                    defaults.Add(new ActionPreference("boundingboxcurves", true, false));
            }

            try
            {
                var prefJson = _store.GetString("Preferences." + kind, string.Empty);
                if (!string.IsNullOrWhiteSpace(prefJson))
                {
                    var savedPrefs = JsonSerializer.Deserialize<List<ActionPreference>>(prefJson);
                    if (savedPrefs is { Count: > 0 })
                    {
                        // Behalte die gespeicherte Reihenfolge & Favoriten bei und ergänze evtl. neue Standardaktionen am Ende
                        var merged = new List<ActionPreference>(savedPrefs);
                        foreach (var def in defaults)
                        {
                            if (merged.All(p => p.Id != def.Id))
                                merged.Add(def);
                        }
                        Preferences[kind] = merged;
                        continue;
                    }
                }
            }
            catch
            {
                // Fallback auf Standardwerte
            }

            Preferences[kind] = defaults;
        }
    }

    public void Save()
    {
        _store.SetBool("Enabled", Enabled);
        _store.SetBool("Auto", AutomaticPrioritization);
        _store.SetInteger("Primary", PrimaryCount);
        _store.SetEnumValue("Ux", UxMode);
        _store.SetEnumValue("Position", PositionMode);

        _store.SetInteger("SelectionDelay", SelectionDelayMs);
        _store.SetInteger("HoverDelay", HoverDelayMs);
        _store.SetInteger("Collapse", CollapseDelayMs);
        _store.SetInteger("MoreCollapse", MoreCollapseDelayMs);
        _store.SetInteger("AutoHide", AutoHideMs);
        _store.SetInteger("Margin", CornerMarginPx);
        _store.SetInteger("Idle", IdleOpacity);
        _store.SetInteger("Active", ActiveOpacity);

        _store.SetBool("ShowViewportAnchor", ShowViewportAnchor);
        _store.SetBool("ShowTetherLine", ShowTetherLine);

        _store.SetString("Custom", JsonSerializer.Serialize(CustomActions));
        _store.SetString("Usage", JsonSerializer.Serialize(Usage));

        foreach (var p in Preferences)
            _store.SetString("Preferences." + p.Key, JsonSerializer.Serialize(p.Value));

        RhinoApp.WriteLine(); // Stellt sicher, dass Rhino die Plug-in-Settings im Speicher aktualisiert
    }

    internal IReadOnlyList<ContextAction> Order(SelectionKind k, List<ContextAction> a)
    {
        var prefKind = k == SelectionKind.MultipleEnvelopeObjects ? SelectionKind.Multiple : k;
        if (!Preferences.TryGetValue(prefKind, out var p))
            return a;

        var map = p
            .GroupBy(x => x.Id)
            .Select((g, i) => new { g.First().Id, g.First().Enabled, g.First().Favorite, I = i })
            .ToDictionary(x => x.Id);

        return a
            .Where(x => !map.ContainsKey(x.Id) || map[x.Id].Enabled)
            .OrderByDescending(x => map.TryGetValue(x.Id, out var m) && m.Favorite)
            .ThenByDescending(x => AutomaticPrioritization && Usage.TryGetValue(x.Id, out var u) ? u.Count : 0)
            .ThenBy(x => map.TryGetValue(x.Id, out var m) ? m.I : int.MaxValue)
            .ToList();
    }

    public bool IsFavorite(SelectionKind k, string id)
    {
        var prefKind = k == SelectionKind.MultipleEnvelopeObjects ? SelectionKind.Multiple : k;
        return Preferences.TryGetValue(prefKind, out var p) && p.Any(x => x.Id == id && x.Favorite);
    }

    public void ToggleFavorite(SelectionKind k, string id)
    {
        var prefKind = k == SelectionKind.MultipleEnvelopeObjects ? SelectionKind.Multiple : k;
        if (!Preferences.TryGetValue(prefKind, out var p))
            return;

        var i = p.FindIndex(x => x.Id == id);
        if (i >= 0)
            p[i] = p[i] with { Favorite = !p[i].Favorite };
        else
            p.Add(new ActionPreference(id, true, true));

        Save();
    }

    public long UsageCount(string id) => Usage.TryGetValue(id, out var u) ? u.Count : 0;

    public void RecordUsage(string id)
    {
        var n = DateTime.UtcNow;
        Usage[id] = Usage.TryGetValue(id, out var u)
            ? u with { Count = u.Count + 1, LastUsedUtc = n }
            : new(id, 1, n);
        Save();
    }
}
