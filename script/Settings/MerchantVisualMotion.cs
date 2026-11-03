using System;
using System.Collections.Generic;
using Godot;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Screens.Shops;

namespace Merchant2CuteII.script.Settings;

/// <summary>
/// 商人视觉节点的「位置 + 缩放」应用与实时微调。
///
/// 为什么要表：位置取决于**房间布局**（真商人节点锚在左上角，需要 +1260,620；
/// 假商人节点本来就在位），缩放取决于**美术**（本套 0.18 / 另一套 0.36），
/// 而跨用时两套美术叠在对方房间里可能仍需 1~2 px 级微调，所以按
/// 「商人 × 美术」四种组合各存一份数值。
///
/// 位置用「基准位 + 偏移」记录：节点首次被处理时把原始 Position 记成基准，
/// 之后每次应用都从基准重算，因此可以反复微调而不会累积漂移。
/// </summary>
public static class MerchantVisualMotion
{
    private static readonly Dictionary<ulong, Vector2> BasePositions = new();
    private static readonly Dictionary<ulong, string> LoggedKeys = new();

    private static readonly Dictionary<string, Vector2> OffsetOverrides = new();
    private static readonly Dictionary<string, float> ScaleOverrides = new();

    /// <summary>
    /// 组合 key = 商人 + **玩家选择的美术来源**（own = 本套，other = 借用）。
    ///
    /// 注意：这里绝不能拿"美术是不是假的"来填第二段 ——
    /// 对真商人两者恰好等价（other ⇒ 假美术），但对假商人正好相反
    /// （fake + other = 借来的**真**美术）。之前用 ArtIsFake 做判据，
    /// 导致 `fake/other` 永远用不上，且 `merchant tune ... fake other` 完全无效。
    /// </summary>
    public static string Key(MerchantKind kind, MerchantSkinSettings skin)
    {
        return string.Concat(kind == MerchantKind.Fake ? "fake" : "real", "/", SkeletonSets.Normalize(skin.BodySet));
    }

    /// <summary>对某个商人按钮应用位置与缩放（幂等，可反复调用）。</summary>
    public static void Apply(NMerchantButton button, Node2D visual)
    {
        if (button == null || visual == null || !GodotObject.IsInstanceValid(visual))
        {
            return;
        }

        MerchantKind kind = SkinApplier.ResolveKind(button) ?? MerchantKind.Real;
        string key = Key(kind, SettingsHost.Current.For(kind));

        ulong id = visual.GetInstanceId();
        if (!BasePositions.TryGetValue(id, out Vector2 basePosition))
        {
            basePosition = visual.Position;
            BasePositions[id] = basePosition;
        }

        Vector2 offset = GetOffset(key);
        float scale = GetScale(key);
        visual.Position = basePosition + offset;
        visual.Scale = new Vector2(scale, scale);

        if (!LoggedKeys.TryGetValue(id, out string? logged) || logged != key)
        {
            LoggedKeys[id] = key;
            GD.Print($"[Merchant2CuteII] visual {key} on {visual.GetPath()}: offset=({offset.X:0.#},{offset.Y:0.#}) scale={scale:0.###}");
        }
    }

    public static Vector2 GetOffset(string key)
    {
        if (OffsetOverrides.TryGetValue(key, out Vector2 value))
        {
            return value;
        }

        return Defaults(key).Offset;
    }

    public static float GetScale(string key)
    {
        if (ScaleOverrides.TryGetValue(key, out float value))
        {
            return value;
        }

        return Defaults(key).Scale;
    }

    /// <summary>
    /// 四种「商人 × 美术」组合的默认值（游戏内实测调好的数值，2026-11-03）。
    ///
    /// 规律：位置取决于房间布局（真商人节点锚左上角 → +1260/620；假商人本来就位），
    ///       缩放取决于美术本身；跨用时再按实测补一点位移/缩放。
    /// </summary>
    private static (Vector2 Offset, float Scale) Defaults(string key)
    {
        switch (key)
        {
            case "real/own":
                // 商店商人 + 本套美术（原始数值，未改动）
                return (ModConfig.Merchant.VisualPositionOffset, ModConfig.Merchant.VisualScale.X);

            case "real/other":
                // 商店商人 + 另一套美术：下移 40 盖住地毯阴影，缩放 0.32（实测）
                return (ModConfig.Merchant.VisualPositionOffset + new Vector2(0f, 40f), 0.32f);

            case "fake/own":
                // 假商人房间 + 本套美术（原始数值，未改动）
                return (ModConfig.FakeMerchant.VisualPositionOffset, ModConfig.FakeMerchant.VisualScale.X);

            case "fake/other":
                // 假商人房间 + 借来的真美术：上移 30、缩放 0.22（2026-11-03 游戏内实测）
                return (ModConfig.FakeMerchant.VisualPositionOffset + new Vector2(0f, -30f), 0.22f);

            default:
                return (ModConfig.Merchant.VisualPositionOffset, ModConfig.Merchant.VisualScale.X);
        }
    }

    /// <summary>诊断：列出当前场景里找到的商人视觉节点与套用的数值。</summary>
    public static string ProbeScene()
    {
        SceneTree? tree = Engine.GetMainLoop() as SceneTree;
        if (tree?.Root == null)
        {
            return "no scene tree";
        }

        var lines = new List<string>();
        WalkWithReport(tree.Root, lines);
        return lines.Count == 0 ? "no merchant visual found in current scene" : string.Join(" | ", lines);
    }

    private static void WalkWithReport(Node node, List<string> lines)
    {
        foreach (Node child in node.GetChildren())
        {
            if (child == null)
            {
                continue;
            }

            if (child is NMerchantButton button)
            {
                Node? visual = button.GetNodeOrNull("%MerchantVisual")
                    ?? button.GetNodeOrNull("MerchantVisual")
                    ?? button.FindChild("MerchantVisual", true, false);
                if (visual is Node2D node2D)
                {
                    MerchantKind kind = SkinApplier.ResolveKind(button) ?? MerchantKind.Real;
                    string key = Key(kind, SettingsHost.Current.For(kind));
                    Vector2 offset = GetOffset(key);
                    float scale = GetScale(key);
                    lines.Add($"{button.GetPath()}: key={key} offset=({offset.X:0.#},{offset.Y:0.#}) scale={scale:0.###} pos=({node2D.Position.X:0.#},{node2D.Position.Y:0.#})");
                }
                else if (visual != null)
                {
                    lines.Add($"{button.GetPath()}: visual found but not Node2D ({visual.GetType().Name})");
                }
                else
                {
                    lines.Add($"{button.GetPath()}: MerchantVisual NOT found");
                }
            }

            WalkWithReport(child, lines);
        }
    }

    // ------------------------------------------------------------ 持久化
    // 微调值单独存一个文件，避免和主配置的 schema 迁移纠缠（文件不存在就代表全默认）。
    private const string TuningPath = "user://merchant2cute_visual.json";

    public static void Load()
    {
        try
        {
            if (!Godot.FileAccess.FileExists(TuningPath))
            {
                return;
            }

            using Godot.FileAccess? file = Godot.FileAccess.Open(TuningPath, Godot.FileAccess.ModeFlags.Read);
            if (file == null)
            {
                return;
            }

            Variant parsed = Godot.Json.ParseString(file.GetAsText());
            if (parsed.VariantType != Variant.Type.Dictionary)
            {
                return;
            }

            Godot.Collections.Dictionary root = parsed.AsGodotDictionary();
            foreach (Variant keyVariant in root.Keys)
            {
                string key = keyVariant.AsString();
                var entry = root[keyVariant].AsGodotDictionary();
                if (entry.ContainsKey("x") && entry.ContainsKey("y"))
                {
                    OffsetOverrides[key] = new Vector2((float)entry["x"].AsDouble(), (float)entry["y"].AsDouble());
                }

                if (entry.ContainsKey("scale"))
                {
                    ScaleOverrides[key] = (float)entry["scale"].AsDouble();
                }
            }

            GD.Print("[Merchant2CuteII] visual tuning loaded: " + Describe());
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[Merchant2CuteII] tuning load failed: {ex.Message}");
        }
    }

    public static void Save()
    {
        try
        {
            var root = new Godot.Collections.Dictionary();
            foreach (string key in new[] { "real/own", "real/other", "fake/own", "fake/other" })
            {
                bool hasOffset = OffsetOverrides.ContainsKey(key);
                bool hasScale = ScaleOverrides.ContainsKey(key);
                if (!hasOffset && !hasScale)
                {
                    continue;
                }

                Vector2 offset = GetOffset(key);
                var entry = new Godot.Collections.Dictionary
                {
                    ["x"] = offset.X,
                    ["y"] = offset.Y,
                    ["scale"] = GetScale(key),
                };
                root[key] = entry;
            }

            using Godot.FileAccess? file = Godot.FileAccess.Open(TuningPath, Godot.FileAccess.ModeFlags.Write);
            if (file == null)
            {
                return;
            }

            file.StoreString(Godot.Json.Stringify(root, "  "));
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[Merchant2CuteII] tuning save failed: {ex.Message}");
        }
    }

    // ------------------------------------------------------------ tuning

    public static void AddOffsetDelta(string key, Vector2 delta)
    {
        OffsetOverrides[key] = GetOffset(key) + delta;
    }

    public static void SetOffset(string key, Vector2 value)
    {
        OffsetOverrides[key] = value;
    }

    public static void SetScale(string key, float value)
    {
        ScaleOverrides[key] = Mathf.Clamp(value, 0.02f, 3f);
    }

    public static void Reset(string? key = null)
    {
        if (key == null)
        {
            OffsetOverrides.Clear();
            ScaleOverrides.Clear();
            return;
        }

        OffsetOverrides.Remove(key);
        ScaleOverrides.Remove(key);
    }

    public static string Describe()
    {
        var keys = new[] { "real/own", "real/other", "fake/own", "fake/other" };
        var lines = new List<string>();
        foreach (string key in keys)
        {
            Vector2 offset = GetOffset(key);
            float scale = GetScale(key);
            bool overridden = OffsetOverrides.ContainsKey(key) || ScaleOverrides.ContainsKey(key);
            lines.Add($"{key}: offset=({offset.X:0.#},{offset.Y:0.#}) scale={scale:0.###}{(overridden ? " *" : "")}");
        }

        return string.Join(" | ", lines) + "   (* = 已手动微调)";
    }

    /// <summary>清掉已释放节点的基准位，避免字典无限增长。</summary>
    public static void Prune()
    {
        BasePositions.Clear();
    }

    public static void ReapplyAll()
    {
        SceneTree? tree = Engine.GetMainLoop() as SceneTree;
        if (tree?.Root == null)
        {
            return;
        }

        Walk(tree.Root);
    }

    private static void Walk(Node node)
    {
        foreach (Node child in node.GetChildren())
        {
            if (child == null)
            {
                continue;
            }

            if (child is NMerchantButton button)
            {
                Node? visual = button.GetNodeOrNull("%MerchantVisual")
                    ?? button.GetNodeOrNull("MerchantVisual")
                    ?? button.FindChild("MerchantVisual", true, false);
                if (visual is Node2D node2D)
                {
                    Apply(button, node2D);
                }
            }

            Walk(child);
        }
    }
}
