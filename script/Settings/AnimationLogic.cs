using System.Collections.Generic;

namespace Merchant2CuteII.script.Settings;

/// <summary>
/// 动画选择是**实现细节**，不对玩家暴露（不进设置界面、不进控制台）。
/// 这里集中放「代码决定用哪个动画」的规则，方便以后调整时只改一处。
///
/// 背景：Spine 动画名是按骨架不同而不同的 ——
///   真商人身体：idle / idle_loop / poison / nsfw / *_ol
///   假商人身体：只有 idle / idle_loop
/// 所以任何"某个动画不存在"的场景都必须有降级链，而不是让玩家去选动画名。
/// </summary>
public static class AnimationLogic
{
    /// <summary>投掷有害药水时使用的动画（游戏原始资源里存在的那一个）。</summary>
    public const string FoulPotionAnimation = "poison";

    /// <summary>身体骨架的通用兜底动画（按优先级）。</summary>
    public static readonly string[] BodyFallbackChain = { "idle_loop", "idle" };

    /// <summary>
    /// 投毒动画的候选链：<c>poison</c> → <c>poison_ol</c> → <c>idle_loop</c> → <c>idle</c>。
    /// 借用了假商人身体骨架时（没有 poison）会自动落到 idle。
    /// </summary>
    public static string[] FoulPotionCandidates()
    {
        return Build(FoulPotionAnimation, FoulPotionAnimation + "_ol", "idle_loop", "idle");
    }

    /// <summary>任意动画名不存在时的降级链（换骨架后最常见）。</summary>
    public static string[] FallbackFor(string requested)
    {
        return Build(requested, requested + "_ol", "idle_loop", "idle");
    }

    private static string[] Build(params string[] candidates)
    {
        var result = new List<string>(candidates.Length);
        foreach (string candidate in candidates)
        {
            if (!string.IsNullOrEmpty(candidate) && !result.Contains(candidate))
            {
                result.Add(candidate);
            }
        }

        return result.ToArray();
    }
}
