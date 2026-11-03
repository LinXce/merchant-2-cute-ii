using System;

namespace Merchant2CuteII.script.Settings;

/// <summary>
/// 「哪套美术」在磁盘上对应哪几个 Spine 资源。
/// 这些路径必须与 PCK 里的实际文件一致（mod 用同名路径覆盖游戏本体资源）。
///
/// 共 4 份资源，按「部位 × 来源」组合：
/// 身体：merchant_room/top（真）/ fake_merchant_room/top（假）
/// 手部：merchant_room/hand（真）/ fake_merchant_room/hand（假）
/// </summary>
public static class SkeletonSetAssets
{
    // 真商人（娘化版）自己那套
    private const string RealHandTres = "res://animations/backgrounds/merchant_room/hand/merchant_hand_skel_data.tres";
    private const string RealBodyTres = "res://animations/backgrounds/merchant_room/top/shop_merchant_top.tres";

    // 假商人那套
    private const string FakeHandTres = "res://animations/backgrounds/fake_merchant_room/hand/fake_merchant_hand_skel_data.tres";
    private const string FakeBodyTres = "res://animations/backgrounds/fake_merchant_room/top/fake_merchant_top.tres";

    /// <summary>手部骨架路径（body 与 hand 各自独立选择，互不影响）。</summary>
    public static string HandTresPath(MerchantKind kind, string handSet)
    {
        return IsFakeHandArt(kind, handSet) ? FakeHandTres : RealHandTres;
    }

    /// <summary>身体 / 购物界面骨架路径。</summary>
    public static string BodyTresPath(MerchantKind kind, string bodySet)
    {
        return IsFakeBodyArt(kind, bodySet) ? FakeBodyTres : RealBodyTres;
    }

    /// <summary>这个「手部来源」组合是否用假商人的手部美术。</summary>
    public static bool IsFakeHandArt(MerchantKind kind, string handSet)
    {
        bool other = SkeletonSets.Normalize(handSet) == SkeletonSets.Other;
        return kind == MerchantKind.Real ? other : !other;
    }

    /// <summary>这个「身体来源」组合是否用假商人的身体美术。</summary>
    public static bool IsFakeBodyArt(MerchantKind kind, string bodySet)
    {
        bool other = SkeletonSets.Normalize(bodySet) == SkeletonSets.Other;
        return kind == MerchantKind.Real ? other : !other;
    }

    /// <summary>给 UI/日志用的名字，例如 <c>real.hand=fake_art</c>。</summary>
    public static string DescribePart(MerchantKind kind, string part, string set)
    {
        bool fakeArt = part == "hand" ? IsFakeHandArt(kind, set) : IsFakeBodyArt(kind, set);
        string owner = kind == MerchantKind.Real ? "real" : "fake";
        return string.Format("{0}.{1}={2}", owner, part, fakeArt ? "fake_art" : "merchant_art");
    }

    public static string Describe(MerchantKind kind, string set)
    {
        return DescribePart(kind, "hand", set);
    }

    public static string[] AllTresPaths()
    {
        return new[] { RealHandTres, RealBodyTres, FakeHandTres, FakeBodyTres };
    }
}
