using Rhino;

namespace RhinoContextAssist;

internal sealed class ContextAssistSettings
{
    private readonly PersistentSettings _store;

    public bool Enabled { get; set; }
    public int PrimaryCount { get; set; }
    public PopupPositionMode PositionMode { get; set; }
    public bool AutomaticPrioritization { get; set; }

    public PopupUxMode UxMode { get; set; }
    public int SelectionDelayMs { get; set; }
    public int HoverDelayMs { get; set; }
    public int CollapseDelayMs { get; set; }
    public int MoreCollapseDelayMs { get; set; }
    public int AutoHideMs { get; set; }
    public int ProximityRadiusPx { get; set; }
    public int CursorOffsetPx { get; set; }
    public double IdleOpacity { get; set; }
    public double ActiveOpacity { get; set; }

    public Dictionary<SelectionKind, List<ActionPreference>> Preferences { get; } = new();
    public Dictionary<string, UsageEntry> Usage { get; } = new();

    public ContextAssistSettings(PersistentSettings store)
    {
        _store = store;
        Load();
    }

    private void Load()
    {
        Enabled = _store.GetBool("Enabled", true);
        PrimaryCount = Math.Clamp(_store.GetInteger("PrimaryCount", 3), 1, 5);
        PositionMode = _store.GetEnumValue("PositionMode", PopupPositionMode.Mouse);
        AutomaticPrioritization = _store.GetBool("AutomaticPrioritization", true);

        UxMode = _store.GetEnumValue("UxMode", PopupUxMode.Compact);
        SelectionDelayMs = Math.Clamp(_store.GetInteger("UX.SelectionDelayMs", 300), 0, 2000);
        HoverDelayMs = Math.Clamp(_store.GetInteger("UX.HoverDelayMs", 300), 0, 2000);
        CollapseDelayMs = Math.Clamp(_store.GetInteger("UX.CollapseDelayMs", 600), 0, 5000);
        MoreCollapseDelayMs = Math.Clamp(_store.GetInteger("UX.MoreCollapseDelayMs", 1500), 0, 10000);
        AutoHideMs = Math.Clamp(_store.GetInteger("UX.AutoHideMs", 5000), 0, 30000);
        ProximityRadiusPx = Math.Clamp(_store.GetInteger("UX.ProximityRadiusPx", 140), 0, 1000);
        CursorOffsetPx = Math.Clamp(_store.GetInteger("UX.CursorOffsetPx", 24), 0, 200);
        IdleOpacity = Math.Clamp(_store.GetDouble("UX.IdleOpacity", 0.42), 0.15, 1.0);
        ActiveOpacity = Math.Clamp(_store.GetDouble("UX.ActiveOpacity", 1.0), 0.25, 1.0);

        foreach (var kind in ActionCatalog.ConfigurableKinds)
        {
            var defaults = ActionCatalog.Defaults(kind);
            var text = _store.GetString($"Actions.{kind}", string.Empty);
            var known = defaults.ToDictionary(x => x.Id);
            var list = new List<ActionPreference>();

            foreach (var row in text.Split(';', StringSplitOptions.RemoveEmptyEntries))
            {
                var parts = row.Split('|');
                if (parts.Length >= 3 && known.ContainsKey(parts[0]))
                    list.Add(new ActionPreference(parts[0], parts[1] == "1", parts[2] == "1"));
            }

            foreach (var action in defaults)
                if (list.All(x => x.Id != action.Id))
                    list.Add(new ActionPreference(action.Id, true, false));

            Preferences[kind] = list;
        }

        foreach (var row in _store.GetString("Usage", string.Empty)
                     .Split(';', StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = row.Split('|');
            if (parts.Length == 3 &&
                long.TryParse(parts[1], out var count) &&
                long.TryParse(parts[2], out var ticks))
            {
                Usage[parts[0]] = new UsageEntry(
                    parts[0], count, new DateTime(ticks, DateTimeKind.Utc));
            }
        }
    }

    public void Save()
    {
        _store.SetBool("Enabled", Enabled);
        _store.SetInteger("PrimaryCount", PrimaryCount);
        _store.SetEnumValue("PositionMode", PositionMode);
        _store.SetBool("AutomaticPrioritization", AutomaticPrioritization);

        _store.SetEnumValue("UxMode", UxMode);
        _store.SetInteger("UX.SelectionDelayMs", SelectionDelayMs);
        _store.SetInteger("UX.HoverDelayMs", HoverDelayMs);
        _store.SetInteger("UX.CollapseDelayMs", CollapseDelayMs);
        _store.SetInteger("UX.MoreCollapseDelayMs", MoreCollapseDelayMs);
        _store.SetInteger("UX.AutoHideMs", AutoHideMs);
        _store.SetInteger("UX.ProximityRadiusPx", ProximityRadiusPx);
        _store.SetInteger("UX.CursorOffsetPx", CursorOffsetPx);
        _store.SetDouble("UX.IdleOpacity", IdleOpacity);
        _store.SetDouble("UX.ActiveOpacity", ActiveOpacity);

        foreach (var pair in Preferences)
        {
            _store.SetString(
                $"Actions.{pair.Key}",
                string.Join(";", pair.Value.Select(x =>
                    $"{x.Id}|{(x.Enabled ? 1 : 0)}|{(x.Favorite ? 1 : 0)}")));
        }

        _store.SetString(
            "Usage",
            string.Join(";", Usage.Values.Select(x =>
                $"{x.ActionId}|{x.Count}|{x.LastUsedUtc.Ticks}")));
    }

    public IReadOnlyList<ContextAction> Resolve(SelectionKind kind)
    {
        var catalog = ActionCatalog.Defaults(kind).ToDictionary(x => x.Id);
        if (!Preferences.TryGetValue(kind, out var preferences))
            return catalog.Values.ToList();

        var query = preferences
            .Select((preference, index) => new { Preference = preference, Index = index })
            .Where(x => x.Preference.Enabled && catalog.ContainsKey(x.Preference.Id));

        query = AutomaticPrioritization
            ? query.OrderByDescending(x => x.Preference.Favorite)
                .ThenByDescending(x => Usage.TryGetValue(x.Preference.Id, out var usage) ? usage.Count : 0)
                .ThenBy(x => x.Index)
            : query.OrderByDescending(x => x.Preference.Favorite)
                .ThenBy(x => x.Index);

        return query.Select(x => catalog[x.Preference.Id]).ToList();
    }

    public bool IsFavorite(SelectionKind kind, string id) =>
        Preferences.TryGetValue(kind, out var list) &&
        list.Any(x => x.Id == id && x.Favorite);

    public long UsageCount(string id) =>
        Usage.TryGetValue(id, out var usage) ? usage.Count : 0;

    public void RecordUsage(string id)
    {
        var now = DateTime.UtcNow;
        Usage[id] = Usage.TryGetValue(id, out var usage)
            ? usage with { Count = usage.Count + 1, LastUsedUtc = now }
            : new UsageEntry(id, 1, now);
        Save();
    }

    public void ResetUsage()
    {
        Usage.Clear();
        Save();
    }
}
