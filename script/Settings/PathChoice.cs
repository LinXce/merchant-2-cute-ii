using System;
using System.Collections.Generic;

namespace Merchant2CuteII.script.Settings;

/// <summary>
/// 纯逻辑的路径挑选（不依赖 Godot），便于单测覆盖。
/// </summary>
public static class PathChoice
{
    /// <summary>按顺序返回第一个满足条件的路径；都没有返回 null。</summary>
    public static string? FirstReadable(IReadOnlyList<string> candidates, Func<string, bool> isReadable)
    {
        if (candidates == null || isReadable == null)
        {
            return null;
        }

        foreach (string candidate in candidates)
        {
            if (isReadable(candidate))
            {
                return candidate;
            }
        }

        return null;
    }

    /// <summary>按顺序返回第一个可写的路径；都没有返回 null。</summary>
    public static string? FirstWritable(IReadOnlyList<string> candidates, Func<string, bool> canWrite)
    {
        return FirstReadable(candidates, canWrite);
    }
}