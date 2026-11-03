using System;

namespace Merchant2CuteII.script.Settings;

/// <summary>
/// 玩家可以在设置里挑选的「手部变体」。
/// 注意：这些值最终会映射成 Spine 动画名，而不是贴图路径 ——
/// 贴图是靠 PCK 同名路径覆盖的，变体之间共用同一张 atlas。
/// </summary>
public static class HandVariants
{
    /// <summary>跟随骨架默认（= Spine 动画 <c>default</c>）。</summary>
    public const string Hand = "hand";

    public const string Foot = "foot";

    /// <summary>只有 merchant_room 的手部骨架有。</summary>
    public const string White = "white";

    /// <summary>只有 merchant_room 的手部骨架有。</summary>
    public const string Black = "black";

    /// <summary>merchant_room 手部骨架支持的完整集合。</summary>
    public static readonly string[] FullSet = { Hand, Foot, White, Black };

    /// <summary>fake_merchant_room 手部骨架只有这两个。</summary>
    public static readonly string[] HandAndFoot = { Hand, Foot };

    public static string Normalize(string? value)
    {
        string normalized = (value ?? string.Empty).Trim().ToLowerInvariant();
        return normalized switch
        {
            Foot => Foot,
            White => White,
            Black => Black,
            _ => Hand,
        };
    }

    public static bool IsKnown(string? value)
    {
        if (value == null)
        {
            return false;
        }

        string normalized = value.Trim().ToLowerInvariant();
        return normalized is Hand or Foot or White or Black;
    }

    /// <summary>变体 → Spine 动画名。历史原因 <c>hand</c> 实际对应动画 <c>default</c>。</summary>
    public static string ToAnimationName(string variant)
    {
        return variant == Hand ? "default" : variant;
    }

    /// <summary>
    /// 逐级降级的候选动画名。真商人骨架没有的变体退回 foot → default，
    /// 与旧版 <c>AnimationHelper</c> 的降级行为一致。
    /// </summary>
    public static string[] AnimationFallbackChain(string variant)
    {
        string primary = ToAnimationName(variant);
        return variant switch
        {
            White or Black => new[] { primary, Foot, "default" },
            Foot => new[] { Foot, "default" },
            _ => new[] { "default" },
        };
    }

    public static string DisplayName(string variant)
    {
        return variant switch
        {
            Foot => "Foot",
            White => "White",
            Black => "Black",
            _ => "Hand",
        };
    }

    // ----------------------------------------------------- 按实际骨架限制选项

    /// <summary>
    /// 某商人 + 某「手部来源」实际能用的变体。
    ///
    /// 这里按**真正加载的那副手部骨架**判定，而不是按商人身份：
    /// 真商人借了假商人的手（handSet=other）→ 只剩 hand/foot；
    /// 假商人借了真商人的手（handSet=other）→ 反而能用 white/black。
    /// </summary>
    public static string[] Allowed(MerchantKind kind, string handSet)
    {
        return SkeletonSetAssets.IsFakeHandArt(kind, handSet) ? HandAndFoot : FullSet;
    }

    public static bool IsAllowed(MerchantKind kind, string handSet, string? value)
    {
        return Array.IndexOf(Allowed(kind, handSet), Normalize(value)) >= 0;
    }

    /// <summary>把不适用于当前手部骨架的变体夹回 <c>foot</c>（旧版也是这个降级方向）。</summary>
    public static string NormalizeFor(MerchantKind kind, string handSet, string? value)
    {
        string normalized = Normalize(value);
        return Array.IndexOf(Allowed(kind, handSet), normalized) >= 0 ? normalized : Foot;
    }

    /// <summary>按当前可选集合循环（4 态或 2 态）。</summary>
    public static string NextFor(MerchantKind kind, string handSet, string current)
    {
        string[] allowed = Allowed(kind, handSet);
        int index = Array.IndexOf(allowed, NormalizeFor(kind, handSet, current));
        return allowed[(index + 1 + allowed.Length) % allowed.Length];
    }
}

/// <summary>美术来源切换开关（身体、手部各自独立）。</summary>
public static class SkeletonSets
{
    /// <summary>用这个部位「本来」的那套美术（真商人 = merchant_room，假商人 = fake_merchant_room）。</summary>
    public const string Own = "own";

    /// <summary>借用另一套美术（真商人借假商人的，假商人借真商人的）。</summary>
    public const string Other = "other";

    public static readonly string[] All = { Own, Other };

    public static string Normalize(string? value)
    {
        string normalized = (value ?? string.Empty).Trim().ToLowerInvariant();
        return normalized is Other or "fake" or "real" or "cross" ? Other : Own;
    }

    public static string DisplayName(string set)
    {
        return Normalize(set) == Other ? "Other" : "Own";
    }

    public static string Next(string set)
    {
        return Normalize(set) == Other ? Own : Other;
    }

    /// <summary>
    /// 某个商人允许选择的**身体**来源。
    /// 假商人房间只允许本套身体：借用商店商人的身体会连带动画/骨骼/尺寸一起错位，
    /// 实测调不出可接受的效果，故直接禁用（手部仍允许借用）。
    /// </summary>
    public static string[] AllowedBodySets(MerchantKind kind)
    {
        return kind == MerchantKind.Fake ? new[] { Own } : new[] { Own, Other };
    }
}

public static class VoiceLanguages
{
    public const string Default = "default";
    public const string Japanese = "jp";
    public const string Chinese = "zh";

    public static readonly string[] All = { Default, Japanese, Chinese };

    public static string Normalize(string? value)
    {
        string normalized = (value ?? string.Empty).Trim().ToLowerInvariant();
        return normalized switch
        {
            Japanese or "ja" => Japanese,
            Chinese or "cn" or "zho" => Chinese,
            _ => Default,
        };
    }
}

public enum MerchantKind
{
    Real = 0,
    Fake = 1,
}

/// <summary>
/// 单个商人的外观设置。
///
/// **身体和手部是分开选的**：这样才支持"身体用本套、手用另一套"这种混搭，
/// 而两副骨架的可用动画不同（手：merchant_room 有 white/black，fake 没有；
/// 身体：merchant_room 有 poison/nsfw，fake 只有 idle），所以必须分部位记录来源。
/// </summary>
public sealed class MerchantSkinSettings
{
    /// <summary>身体 / 购物界面骨架的来源（<see cref="SkeletonSets"/>）。</summary>
    public string BodySet { get; set; } = SkeletonSets.Own;

    /// <summary>手部骨架的来源（<see cref="SkeletonSets"/>）。</summary>
    public string HandSet { get; set; } = SkeletonSets.Own;

    /// <summary><see cref="HandVariants"/> 之一；实际可用集合取决于 <see cref="HandSet"/>。</summary>
    public string HandVariant { get; set; } = HandVariants.Hand;

    /// <summary>购物界面那条腿的装饰是否显示（只对真商人有意义）。</summary>
    public bool ShowInventoryLeg { get; set; } = true;

    public MerchantSkinSettings Clone()
    {
        return new MerchantSkinSettings
        {
            BodySet = BodySet,
            HandSet = HandSet,
            HandVariant = HandVariant,
            ShowInventoryLeg = ShowInventoryLeg,
        };
    }

    public void Normalize(MerchantKind kind)
    {
        BodySet = SkeletonSets.Normalize(BodySet);

        // 假商人的身体固定为本套（借用商店商人的身体会整体错位，已禁用）
        if (kind == MerchantKind.Fake)
        {
            BodySet = SkeletonSets.Own;
        }

        HandSet = SkeletonSets.Normalize(HandSet);
        HandVariant = HandVariants.NormalizeFor(kind, HandSet, HandVariant);
    }
}

public sealed class VoiceSettings
{
    /// <summary><see cref="VoiceLanguages"/> 之一。</summary>
    public string Language { get; set; } = VoiceLanguages.Default;

    /// <summary>相对原版商人的音量增量（dB），-12 ~ +12。</summary>
    public float VolumeDb { get; set; }

    public void Normalize()
    {
        Language = VoiceLanguages.Normalize(Language);
        VolumeDb = System.Math.Clamp(VolumeDb, -12f, 12f);
    }

    public float LinearGain => (float)System.Math.Pow(10d, VolumeDb / 20d);

    public VoiceSettings Clone()
    {
        return new VoiceSettings { Language = Language, VolumeDb = VolumeDb };
    }
}

/// <summary>配置文件根结构（schema 2）。</summary>
public sealed class ModSettings
{
    public const int CurrentSchema = 2;

    public int Schema { get; set; } = CurrentSchema;

    public MerchantSkinSettings RealMerchant { get; set; } = new();

    public MerchantSkinSettings FakeMerchant { get; set; } = new();

    public VoiceSettings Voice { get; set; } = new();

    public MerchantSkinSettings For(MerchantKind kind)
    {
        return kind == MerchantKind.Fake ? FakeMerchant : RealMerchant;
    }

    public ModSettings Clone()
    {
        return new ModSettings
        {
            Schema = Schema,
            RealMerchant = RealMerchant.Clone(),
            FakeMerchant = FakeMerchant.Clone(),
            Voice = Voice.Clone(),
        };
    }

    public void Normalize()
    {
        RealMerchant.Normalize(MerchantKind.Real);
        FakeMerchant.Normalize(MerchantKind.Fake);
        Voice.Normalize();
    }
}
