using System;

namespace Merchant2CuteII.script.Compat;

/// <summary>
/// 版本能力位：把"游戏在哪个版本改了什么"集中成一组开关，业务代码只问"能不能"，不问版本号。
///
/// 阈值与"版本取不到时的默认值"都严格沿用仓库历史写法（v1.5.1 / v1.5.3 两次兼容性修复）：
///
/// <list type="table">
/// <item><description>
/// AnimationHelper：<c>NewSpineReadyVersion = 0.108.0</c>，
/// 版本未知时走**新**签名（return false）。
/// </description></item>
/// <item><description>
/// MerchantVisualResolver：<c>MerchantVisualOwnerChangeVersion = 0.110.0</c>，
/// 版本未知时走**旧**查找（return true）。
/// </description></item>
/// </list>
///
/// 注意：本文件**不**用来限制"最低支持版本"——mod 不声明最低版本，
/// 任何版本都会被加载；版本差异只影响走哪条分支。
/// </summary>
public static class VersionProfile
{
    // ---------------------------------------------------------------- 阈值（与历史提交一致）

    /// <summary>0.108.0：新 Spine 绑定就绪，<c>set_animation</c> 走新签名。</summary>
    private const int SpineApiMajor = 0, SpineApiMinor = 108, SpineApiPatch = 0;

    /// <summary>0.110.0：MerchantVisual 的宿主从房间挪到了 NMerchantButton 上。</summary>
    private const int VisualOwnerMajor = 0, VisualOwnerMinor = 110, VisualOwnerPatch = 0;

    // ------------------------------------------------------------ 能力位

    /// <summary>
    /// 旧版用 <c>set_animation(name, loop)</c>；新签名需要 <c>(name, loop, track)</c>。
    /// 版本取不到时按新签名处理（历史行为：<c>return false</c>）。
    /// </summary>
    public static bool LegacyAnimationSignature =>
        GameVersion.Known && GameVersion.Below(SpineApiMajor, SpineApiMinor, SpineApiPatch);

    /// <summary>
    /// 旧版的 MerchantVisual 需要从房间/旧路径查找（0.110.0 起改挂在按钮上）。
    /// 版本取不到时按旧查找处理（历史行为：<c>return true</c>）。
    /// </summary>
    public static bool LegacyMerchantVisualLookup =>
        !GameVersion.Known || GameVersion.Below(VisualOwnerMajor, VisualOwnerMinor, VisualOwnerPatch);

    public static string Describe()
    {
        return string.Join(", ",
            new[]
            {
                "game=" + GameVersion.Describe(),
                "legacyAnimSig=" + Flag(LegacyAnimationSignature),
                "legacyVisualLookup=" + Flag(LegacyMerchantVisualLookup),
            });
    }

    private static string Flag(bool value) => value ? "yes" : "no";
}
