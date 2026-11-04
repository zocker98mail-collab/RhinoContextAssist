using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using Rhino;

namespace RhinoContextAssist;

internal sealed class ContextAssistSettings
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

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

    // Hybrid-Conduit & Dynamische Positionierung
    public bool ShowViewportAnchor { get; set; }
    public bool ShowTetherLine { get; set; }
    public int TetherLineOpacity { get; set; }
    public double TetherLineThickness { get; set; }
    public bool FollowCameraMovement { get; set; }
    public bool AutoHideWhenAnchorOffscreen { get; set; }
    public int DynamicOffsetPx { get; set; }
    public bool EnableEdgeSnapping { get; set; }
    public int EdgeSnapDistancePx { get; set; }

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
        TetherLineOpacity = Math.Clamp(_store.GetInteger("TetherLineOpacity", 115), 20, 255);
        TetherLineThickness = Math.Clamp(_store.GetDouble("TetherLineThickness", 1.5), 0.5, 5.0);
        FollowCameraMovement = _store.GetBool("FollowCameraMovement", true);
        AutoHideWhenAnchorOffscreen = _store.GetBool("AutoHideWhenAnchorOffscreen", true);
        DynamicOffsetPx = Math.Clamp(_store.GetInteger("DynamicOffsetPx", 48), 0, 200);
        EnableEdgeSnapping = _store.GetBool("EnableEdgeSnapping", true);
        EdgeSnapDistancePx = Math.Clamp(_store.GetInteger("EdgeSnapDistancePx", 28), 8, 80);

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
            // Ignoriere beschädigtes JSON
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
            var defaults = BuildDefaultPreferencesForKind(kind);

            try
            {
                var prefJson = _store.GetString("Preferences." + kind, string.Empty);
                if (!string.IsNullOrWhiteSpace(prefJson))
                {
                    var savedPrefs = JsonSerializer.Deserialize<List<ActionPreference>>(prefJson);
                    if (savedPrefs is { Count: > 0 })
                    {
                        Preferences[kind] = MergePreferencesWithDefaults(savedPrefs, defaults);
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

    private static List<ActionPreference> BuildDefaultPreferencesForKind(SelectionKind kind)
    {
        var defaults = ActionCatalog.Defaults(kind)
            .Select(x => new ActionPreference(x.Id, true, false))
            .ToList();

        if (kind is SelectionKind.SolidBrep or SelectionKind.Brep or SelectionKind.Mesh or SelectionKind.Multiple)
        {
            if (defaults.All(x => x.Id != "boundingboxcurves"))
                defaults.Add(new ActionPreference("boundingboxcurves", true, false));
        }

        return defaults;
    }

    private static List<ActionPreference> MergePreferencesWithDefaults(
        List<ActionPreference> savedPrefs,
        List<ActionPreference> defaults)
    {
        var merged = new List<ActionPreference>(savedPrefs);
        foreach (var def in defaults)
        {
            if (merged.All(p => p.Id != def.Id))
                merged.Add(def);
        }
        return merged;
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
        _store.SetInteger("TetherLineOpacity", TetherLineOpacity);
        _store.SetDouble("TetherLineThickness", TetherLineThickness);
        _store.SetBool("FollowCameraMovement", FollowCameraMovement);
        _store.SetBool("AutoHideWhenAnchorOffscreen", AutoHideWhenAnchorOffscreen);
        _store.SetInteger("DynamicOffsetPx", DynamicOffsetPx);
        _store.SetBool("EnableEdgeSnapping", EnableEdgeSnapping);
        _store.SetInteger("EdgeSnapDistancePx", EdgeSnapDistancePx);

        _store.SetString("Custom", JsonSerializer.Serialize(CustomActions));
        _store.SetString("Usage", JsonSerializer.Serialize(Usage));

        foreach (var p in Preferences)
            _store.SetString("Preferences." + p.Key, JsonSerializer.Serialize(p.Value));

        ContextAssistPlugIn.Instance?.SaveSettings();
    }

    public void ExportToFile(string filePath)
    {
        var prefDict = Preferences.ToDictionary(k => k.Key.ToString(), v => v.Value);
        var dto = new SettingsExportDto(
            Version: "2.2-StableHybrid",
            ExportedUtc: DateTime.UtcNow,
            Enabled: Enabled,
            AutomaticPrioritization: AutomaticPrioritization,
            PrimaryCount: PrimaryCount,
            UxMode: UxMode,
            PositionMode: PositionMode,
            SelectionDelayMs: SelectionDelayMs,
            HoverDelayMs: HoverDelayMs,
            CollapseDelayMs: CollapseDelayMs,
            MoreCollapseDelayMs: MoreCollapseDelayMs,
            AutoHideMs: AutoHideMs,
            CornerMarginPx: CornerMarginPx,
            IdleOpacity: IdleOpacity,
            ActiveOpacity: ActiveOpacity,
            ShowViewportAnchor: ShowViewportAnchor,
            ShowTetherLine: ShowTetherLine,
            TetherLineOpacity: TetherLineOpacity,
            TetherLineThickness: TetherLineThickness,
            FollowCameraMovement: FollowCameraMovement,
            AutoHideWhenAnchorOffscreen: AutoHideWhenAnchorOffscreen,
            DynamicOffsetPx: DynamicOffsetPx,
            EnableEdgeSnapping: EnableEdgeSnapping,
            EdgeSnapDistancePx: EdgeSnapDistancePx,
            CustomActions: CustomActions.ToList(),
            Preferences: prefDict,
            Usage: new Dictionary<string, UsageEntry>(Usage));

        string json = JsonSerializer.Serialize(dto, JsonOptions);
        File.WriteAllText(filePath, json);
    }

    public bool ImportFromFile(string filePath, out string message)
    {
        try
        {
            if (!File.Exists(filePath))
            {
                message = "Datei wurde nicht gefunden.";
                return false;
            }

            string json = File.ReadAllText(filePath);
            var dto = JsonSerializer.Deserialize<SettingsExportDto>(json, JsonOptions);
            if (dto == null)
            {
                message = "Ungültiges Einstellungsformat.";
                return false;
            }

            Enabled = dto.Enabled;
            AutomaticPrioritization = dto.AutomaticPrioritization;
            PrimaryCount = Math.Clamp(dto.PrimaryCount, 1, 8);
            UxMode = dto.UxMode;
            PositionMode = dto.PositionMode;

            SelectionDelayMs = Math.Clamp(dto.SelectionDelayMs, 50, 2000);
            HoverDelayMs = Math.Clamp(dto.HoverDelayMs, 50, 2000);
            CollapseDelayMs = Math.Clamp(dto.CollapseDelayMs, 100, 5000);
            MoreCollapseDelayMs = Math.Clamp(dto.MoreCollapseDelayMs, 200, 10000);
            AutoHideMs = Math.Clamp(dto.AutoHideMs, 0, 30000);
            CornerMarginPx = Math.Clamp(dto.CornerMarginPx, 20, 350);
            IdleOpacity = Math.Clamp(dto.IdleOpacity, 15, 100);
            ActiveOpacity = Math.Clamp(dto.ActiveOpacity, 30, 100);

            ShowViewportAnchor = dto.ShowViewportAnchor;
            ShowTetherLine = dto.ShowTetherLine;
            TetherLineOpacity = Math.Clamp(dto.TetherLineOpacity > 0 ? dto.TetherLineOpacity : 115, 20, 255);
            TetherLineThickness = Math.Clamp(dto.TetherLineThickness > 0 ? dto.TetherLineThickness : 1.5, 0.5, 5.0);
            FollowCameraMovement = dto.FollowCameraMovement;
            AutoHideWhenAnchorOffscreen = dto.AutoHideWhenAnchorOffscreen;
            DynamicOffsetPx = Math.Clamp(dto.DynamicOffsetPx, 0, 200);
            EnableEdgeSnapping = dto.EnableEdgeSnapping;
            EdgeSnapDistancePx = Math.Clamp(dto.EdgeSnapDistancePx > 0 ? dto.EdgeSnapDistancePx : 28, 8, 80);

            CustomActions.Clear();
            if (dto.CustomActions != null)
                CustomActions.AddRange(dto.CustomActions);

            Usage.Clear();
            if (dto.Usage != null)
            {
                foreach (var kv in dto.Usage)
                    Usage[kv.Key] = kv.Value;
            }

            foreach (var kind in ActionCatalog.Kinds)
            {
                var defaults = BuildDefaultPreferencesForKind(kind);
                if (dto.Preferences != null &&
                    dto.Preferences.TryGetValue(kind.ToString(), out var importedPrefs) &&
                    importedPrefs is { Count: > 0 })
                {
                    Preferences[kind] = MergePreferencesWithDefaults(importedPrefs, defaults);
                }
                else
                {
                    Preferences[kind] = defaults;
                }
            }

            Save();
            message = "Einstellungen erfolgreich importiert und gespeichert.";
            return true;
        }
        catch (Exception ex)
        {
            message = $"Fehler beim Importieren: {ex.Message}";
            return false;
        }
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
