using System;
using Godot;
using MegaCrit.Sts2.Core.Debug;
using MegaCrit.Sts2.Core.Helpers;

namespace Merchant2CuteII.script.Compat;

/// <summary>
/// 游戏版本读取与比较（唯一入口）。
///
/// 版本来源是游戏自己的 <see cref="ReleaseInfoManager.Instance"/>.SemVer —— 不靠猜、不读文件。
/// 所有"某个版本之后 API/场景结构变了"的判断都必须经过这里，避免阈值散落在各处。
/// </summary>
public static class GameVersion
{
    private const string EnvOverride = "MERCHANT2CUTE_FAKE_GAME_VERSION";

    private static string? _forced;

    /// <summary>当前游戏版本；取不到时为 null（此时各 <see cref="VersionProfile"/> 能力位走保守默认值）。</summary>
    public static SemanticVersion? Current
    {
        get
        {
            if (_forced != null)
            {
                return SemanticVersion.TryFromString(_forced, out SemanticVersion? forced) ? forced : null;
            }

            try
            {
                return ReleaseInfoManager.Instance?.SemVer;
            }
            catch (Exception)
            {
                return null;
            }
        }
    }

    public static bool Known => Current != null;

    /// <summary>用于测试：强制指定版本（也可用环境变量 <c>MERCHANT2CUTE_FAKE_GAME_VERSION</c>）。</summary>
    public static void Force(string? version)
    {
        _forced = string.IsNullOrWhiteSpace(version) ? null : version.Trim();
    }

    public static void ForceFromEnvironment()
    {
        string? env = System.Environment.GetEnvironmentVariable(EnvOverride);
        if (!string.IsNullOrWhiteSpace(env))
        {
            Force(env);
        }
    }

    public static bool AtLeast(int major, int minor, int patch)
    {
        SemanticVersion? current = Current;
        return current != null && current.CompareTo(new SemanticVersion(major, minor, patch)) >= 0;
    }

    public static bool AtLeast(string version)
    {
        if (!SemanticVersion.TryFromString(version, out SemanticVersion? target) || target == null)
        {
            return false;
        }

        SemanticVersion? current = Current;
        return current != null && current.CompareTo(target) >= 0;
    }

    public static bool Below(int major, int minor, int patch)
    {
        SemanticVersion? current = Current;
        return current == null || current.CompareTo(new SemanticVersion(major, minor, patch)) < 0;
    }

    public static string Describe()
    {
        SemanticVersion? current = Current;
        string suffix = _forced != null ? " (forced)" : string.Empty;
        return current == null ? "unknown" + suffix : current + suffix;
    }
}
