using System;

namespace Merchant2CuteII.script.Settings;

/// <summary>
/// 设置界面用的枚举（BaseLib 的 SimpleModConfig 与 RitsuLib 的 AddEnumChoice 都靠 enum 生成下拉框）。
///
/// 说明：手部变体的**可用集合随「手部来源」变化**（真商人手部骨架有 white/black，
/// 假商人那副没有），而库的下拉框只能从一个固定的 enum 生成，所以这里统一列 4 项，
/// 由模型层的 <see cref="HandVariants.NormalizeFor"/> 按当前手部来源夹取（白/黑 → 脚）。
/// 我们自带的面板则会按当前手部来源动态只列出真正可用的项。
/// </summary>
public enum HandVariantOption
{
    Hand = 0,
    Foot = 1,
    White = 2,
    Black = 3,
}

public enum SkeletonSetOption
{
    /// <summary>商店商人那套美术。</summary>
    ShopArt = 0,

    /// <summary>假商人那套美术。</summary>
    EventArt = 1,
}

public enum VoiceLanguageOption
{
    Vanilla = 0,
    Japanese = 1,
    Chinese = 2,
}

public static class SettingsOptions
{
    public static string ToValue(HandVariantOption option) => option switch
    {
        HandVariantOption.Foot => HandVariants.Foot,
        HandVariantOption.White => HandVariants.White,
        HandVariantOption.Black => HandVariants.Black,
        _ => HandVariants.Hand,
    };

    public static HandVariantOption ToHandOption(string? value) => HandVariants.Normalize(value) switch
    {
        HandVariants.Foot => HandVariantOption.Foot,
        HandVariants.White => HandVariantOption.White,
        HandVariants.Black => HandVariantOption.Black,
        _ => HandVariantOption.Hand,
    };

    public static string ToValue(SkeletonSetOption option)
        => option == SkeletonSetOption.EventArt ? SkeletonSets.Other : SkeletonSets.Own;

    public static SkeletonSetOption ToSetOption(string? value)
        => SkeletonSets.Normalize(value) == SkeletonSets.Other ? SkeletonSetOption.EventArt : SkeletonSetOption.ShopArt;

    public static string ToValue(VoiceLanguageOption option) => option switch
    {
        VoiceLanguageOption.Japanese => VoiceLanguages.Japanese,
        VoiceLanguageOption.Chinese => VoiceLanguages.Chinese,
        _ => VoiceLanguages.Default,
    };

    public static VoiceLanguageOption ToLanguageOption(string? value) => VoiceLanguages.Normalize(value) switch
    {
        VoiceLanguages.Japanese => VoiceLanguageOption.Japanese,
        VoiceLanguages.Chinese => VoiceLanguageOption.Chinese,
        _ => VoiceLanguageOption.Vanilla,
    };
}

/// <summary>枚举 → 下拉框显示文字。</summary>
public static class SettingsOptionLabels
{
    public static string Hand(HandVariantOption option) => option switch
    {
        HandVariantOption.Foot => "Foot / 脚",
        HandVariantOption.White => "White / 白（需真商人手部美术）",
        HandVariantOption.Black => "Black / 黑（需真商人手部美术）",
        _ => "Hand / 手",
    };

    /// <summary>美术来源下拉项文字：直接写这套美术属于谁，避免"本套/另一套"这种相对说法。</summary>
    public static string Set(MerchantKind kind, string part, SkeletonSetOption option)
    {
        string set = SettingsOptions.ToValue(option);
        bool fakeArt = part == "body"
            ? SkeletonSetAssets.IsFakeBodyArt(kind, set)
            : SkeletonSetAssets.IsFakeHandArt(kind, set);
        return SettingsText.ArtOwner(fakeArt);
    }
}
