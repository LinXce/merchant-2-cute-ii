using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Merchant2CuteII.script.Settings;

/// <summary>
/// 纯 JSON 逻辑（不依赖 Godot），方便单独跑测试：
/// 旧版 schema 1 → schema 2 的迁移、宽松读取、双写序列化都在这里。
///
/// schema 2 的结构（**身体与手部分开选来源**）：
/// <code>
/// {
///   "schema": 2,
///   "realMerchant": { "bodySet": "own", "handSet": "other", "handVariant": "foot", "showInventoryLeg": true },
///   "fakeMerchant": { "bodySet": "own", "handSet": "own",  "handVariant": "hand", "showInventoryLeg": true },
///   "voice": { "language": "jp", "volumeDb": -3 },
///   "HandVariant": "...", "MerchantVoiceVariant": "...", "ExtraDb": 0   // 旧版字段镜像
/// }
/// </code>
/// </summary>
public static class SettingsJson
{
    public const string LegacyKeyHandVariant = "HandVariant";
    public const string LegacyKeyVoiceVariant = "MerchantVoiceVariant";
    public const string LegacyKeyExtraDb = "ExtraDb";

    /// <summary>schema 2 草稿期用过的单一位段键（已拆成 bodySet/handSet），仅用于读取兼容。</summary>
    private const string LegacySkinSetKey = "skeletonSet";

    private static readonly JsonSerializerOptions WriteOptions = new() { WriteIndented = true };

    public static ModSettings Parse(string? json, out bool wasLegacy)
    {
        TryParse(json, out ModSettings settings, out wasLegacy);
        return settings;
    }

    public static bool TryParse(string? json, out ModSettings settings, out bool wasLegacy)
    {
        wasLegacy = false;
        settings = new ModSettings();

        if (string.IsNullOrWhiteSpace(json))
        {
            return false;
        }

        JsonObject? root;
        try
        {
            root = JsonNode.Parse(json, documentOptions: new JsonDocumentOptions
            {
                AllowTrailingCommas = true,
                CommentHandling = JsonCommentHandling.Skip,
            }) as JsonObject;
        }
        catch
        {
            return false;
        }

        if (root == null)
        {
            return false;
        }

        int schema = ReadInt(root, "schema") ?? 1;
        JsonObject? real = Find(root, "realMerchant") as JsonObject;
        JsonObject? fake = Find(root, "fakeMerchant") as JsonObject;
        JsonObject? voice = Find(root, "voice") as JsonObject;
        bool hasNewShape = real != null || fake != null || voice != null;

        if (hasNewShape)
        {
            settings.RealMerchant = ReadSkin(real, MerchantKind.Real);
            settings.FakeMerchant = ReadSkin(fake, MerchantKind.Fake);
            settings.Voice = ReadVoice(voice, settings.Voice);
        }

        if (schema < ModSettings.CurrentSchema || !hasNewShape)
        {
            wasLegacy = true;
        }

        // 旧键兜底始终跑：新结构优先，旧键只在「新结构没给」时补位
        ApplyLegacyMigration(root, settings, hasNewShape);
        settings.Schema = ModSettings.CurrentSchema;
        settings.Normalize();
        return true;
    }

    public static string Serialize(ModSettings settings)
    {
        settings.Normalize();

        var root = new JsonObject
        {
            ["schema"] = ModSettings.CurrentSchema,
            ["realMerchant"] = WriteSkin(settings.RealMerchant),
            ["fakeMerchant"] = WriteSkin(settings.FakeMerchant),
            ["voice"] = WriteVoice(settings.Voice),

            // ↓ 旧版字段镜像（PascalCase）：HandVariant 只写真商人的变体，
            //   旧版读取时会把它同时作用于两个商人（旧行为）。
            [LegacyKeyHandVariant] = settings.RealMerchant.HandVariant,
            [LegacyKeyVoiceVariant] = settings.Voice.Language,
            [LegacyKeyExtraDb] = Math.Round(settings.Voice.VolumeDb, 2),
        };

        return root.ToJsonString(WriteOptions);
    }

    // ------------------------------------------------------------- migration

    private static void ApplyLegacyMigration(JsonObject root, ModSettings settings, bool hasNewShape)
    {
        string? legacyHand = ReadString(root, LegacyKeyHandVariant);
        if (legacyHand != null)
        {
            string variant = HandVariants.Normalize(legacyHand);
            if (!hasNewShape)
            {
                // 旧版只有一个全局 HandVariant，两个商人都用它；
                // 假商人手部骨架没有 white/black，落到 foot（与旧版降级一致）。
                settings.RealMerchant.BodySet = SkeletonSets.Own;
                settings.RealMerchant.HandSet = SkeletonSets.Own;
                settings.RealMerchant.HandVariant =
                    HandVariants.NormalizeFor(MerchantKind.Real, SkeletonSets.Own, variant);
                settings.RealMerchant.ShowInventoryLeg = true; // 玩家偏好，实际显示由 ShouldShowInventoryLeg 决定

                settings.FakeMerchant.BodySet = SkeletonSets.Own;
                settings.FakeMerchant.HandSet = SkeletonSets.Own;
                settings.FakeMerchant.HandVariant =
                    HandVariants.NormalizeFor(MerchantKind.Fake, SkeletonSets.Own, variant);
            }
            else
            {
                // 旧字段只补缺项；显式选择 hand/default/0 同样是有效的新设置。
                if (Find(Find(root, "realMerchant") as JsonObject ?? new JsonObject(), "handVariant") == null)
                    settings.RealMerchant.HandVariant = HandVariants.NormalizeFor(MerchantKind.Real, settings.RealMerchant.HandSet, variant);
                if (Find(Find(root, "fakeMerchant") as JsonObject ?? new JsonObject(), "handVariant") == null)
                    settings.FakeMerchant.HandVariant = HandVariants.NormalizeFor(MerchantKind.Fake, settings.FakeMerchant.HandSet, variant);
            }
        }

        string? legacyVoice = ReadString(root, LegacyKeyVoiceVariant);
        JsonObject voice = Find(root, "voice") as JsonObject ?? new JsonObject();
        if (legacyVoice != null && Find(voice, "language") == null)
        {
            settings.Voice.Language = VoiceLanguages.Normalize(legacyVoice);
        }

        float? legacyDb = ReadFloat(root, LegacyKeyExtraDb);
        if (legacyDb.HasValue && Find(voice, "volumeDb") == null)
        {
            settings.Voice.VolumeDb = legacyDb.Value;
        }
    }

    public static bool IsFootLike(string handVariant)
    {
        string v = HandVariants.Normalize(handVariant);
        return v is HandVariants.Foot or HandVariants.White or HandVariants.Black;
    }

    /// <summary>
    /// 商店界面那条额外贴图（腿）是否显示。
    ///
    /// 规则：它只在「手部变体 = hand（默认）」时跟着一起出现；
    /// foot / white / black 一律不出现；同时玩家的开关可以整体关掉。
    /// 注意 <c>ShowInventoryLeg</c> 是**玩家偏好**，切换变体时不会被改写，
    /// 所以从 foot 切回 hand 时它能按玩家的开关恢复显示。
    /// </summary>
    /// <summary>
    /// 腿贴图这一项**是否可用**：必须整套用本套美术，且手部变体必须是「手」
    /// （裸足 / 白丝 / 黑丝 时它固定不出现，开关也就没有意义）。
    /// </summary>
    public static bool CanToggleInventoryLeg(MerchantSkinSettings skin, MerchantKind kind)
    {
        return UsesOwnArt(skin, kind) && !IsFootLike(skin.HandVariant);
    }

    public static bool ShouldShowInventoryLeg(MerchantSkinSettings skin, MerchantKind kind)
    {
        // 开关关掉 → 不显示
        if (!skin.ShowInventoryLeg)
        {
            return false;
        }

        // foot / white / black → 不显示
        if (IsFootLike(skin.HandVariant))
        {
            return false;
        }

        // leg.png 属于「本套（商店商人）美术」的一部分：一旦借用了另一套美术，
        // 它和看到的模型对不上，直接不显示（设置界面里也会同步禁用这个开关）。
        if (SkeletonSetAssets.IsFakeBodyArt(kind, skin.BodySet))
        {
            return false;
        }

        if (SkeletonSetAssets.IsFakeHandArt(kind, skin.HandSet))
        {
            return false;
        }

        return true;
    }

    /// <summary>当前商人是否整套都用自己那套美术（决定腿开关是否可用）。</summary>
    public static bool UsesOwnArt(MerchantSkinSettings skin, MerchantKind kind)
    {
        return !SkeletonSetAssets.IsFakeBodyArt(kind, skin.BodySet)
            && !SkeletonSetAssets.IsFakeHandArt(kind, skin.HandSet);
    }

    // ------------------------------------------------------------------ json

    private static MerchantSkinSettings ReadSkin(JsonObject? node, MerchantKind kind)
    {
        var skin = new MerchantSkinSettings();
        if (node == null)
        {
            return skin;
        }

        // 兼容 schema 2 草稿期：那时只有一个 skeletonSet，同时管身体和手部
        string? sharedSet = ReadString(node, LegacySkinSetKey);

        skin.BodySet = SkeletonSets.Normalize(ReadString(node, "bodySet") ?? sharedSet);
        skin.HandSet = SkeletonSets.Normalize(ReadString(node, "handSet") ?? sharedSet);
        skin.HandVariant = HandVariants.NormalizeFor(kind, skin.HandSet,
            ReadString(node, "handVariant") ?? HandVariants.Hand);
        skin.ShowInventoryLeg = ReadBool(node, "showInventoryLeg") ?? true;
        return skin;
    }

    private static VoiceSettings ReadVoice(JsonObject? node, VoiceSettings fallback)
    {
        if (node == null)
        {
            return fallback;
        }

        return new VoiceSettings
        {
            Language = ReadString(node, "language") ?? fallback.Language,
            VolumeDb = ReadFloat(node, "volumeDb") ?? fallback.VolumeDb,
        };
    }

    private static JsonObject WriteSkin(MerchantSkinSettings skin)
    {
        return new JsonObject
        {
            ["bodySet"] = skin.BodySet,
            ["handSet"] = skin.HandSet,
            ["handVariant"] = skin.HandVariant,
            ["showInventoryLeg"] = skin.ShowInventoryLeg,
        };
    }

    private static JsonObject WriteVoice(VoiceSettings voice)
    {
        return new JsonObject
        {
            ["language"] = voice.Language,
            ["volumeDb"] = Math.Round(voice.VolumeDb, 2),
        };
    }

    // 大小写宽松读取（旧文件 PascalCase，新文件 camelCase）
    private static JsonNode? Find(JsonObject node, string key)
    {
        if (node.TryGetPropertyValue(key, out JsonNode? direct))
        {
            return direct;
        }

        foreach (KeyValuePair<string, JsonNode?> pair in node)
        {
            if (string.Equals(pair.Key, key, StringComparison.OrdinalIgnoreCase))
            {
                return pair.Value;
            }
        }

        return null;
    }

    private static string? ReadString(JsonObject node, string key)
    {
        try
        {
            string? value = Find(node, key)?.GetValue<string>();
            return string.IsNullOrWhiteSpace(value) ? null : value;
        }
        catch
        {
            return null;
        }
    }

    private static int? ReadInt(JsonObject node, string key)
    {
        try
        {
            return Find(node, key)?.GetValue<int>();
        }
        catch
        {
            try
            {
                return (int)(Find(node, key)?.GetValue<double>() ?? 0);
            }
            catch
            {
                return null;
            }
        }
    }

    private static float? ReadFloat(JsonObject node, string key)
    {
        JsonNode? value = Find(node, key);
        if (value == null)
        {
            return null;
        }

        try
        {
            return value.GetValue<float>();
        }
        catch
        {
            // 容忍字符串 / 整数形式的数值（玩家手改配置很常见）
        }

        try
        {
            return (float)value.GetValue<double>();
        }
        catch
        {
            // ignore
        }

        try
        {
            string? text = value.GetValue<string>();
            if (text != null && float.TryParse(text, System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out float parsed))
            {
                return parsed;
            }
        }
        catch
        {
            // ignore
        }

        return null;
    }

    private static bool? ReadBool(JsonObject node, string key)
    {
        try
        {
            return Find(node, key)?.GetValue<bool>();
        }
        catch
        {
            return null;
        }
    }
}
