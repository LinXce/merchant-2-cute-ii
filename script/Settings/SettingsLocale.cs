using System;
using Godot;
using MegaCrit.Sts2.Core.Localization;

namespace Merchant2CuteII.script.Settings;

/// <summary>
/// 把游戏当前语言同步到 <see cref="SettingsText.Language"/>。
///
/// 游戏的语言来源是 <c>LocManager.Instance.Language</c>（小写三字母码，如 eng / zhs / zht）。
/// 调用点：模组初始化时一次；每次打开设置页再同步一次（玩家可能中途在游戏设置里换语言）。
/// </summary>
public static class SettingsLocale
{
    public static UiLanguage Current { get; private set; } = UiLanguage.English;

    /// <summary>读游戏语言并应用到界面文案；读不到时保持当前值。</summary>
    public static UiLanguage Sync()
    {
        try
        {
            string? code = LocManager.Instance?.Language;
            if (!string.IsNullOrWhiteSpace(code))
            {
                UiLanguage mapped = Map(code);
                if (mapped != Current)
                {
                    GD.Print($"[Merchant2CuteII] language: '{code}' -> {mapped}");
                }

                Current = mapped;
                SettingsText.Language = mapped;
            }
        }
        catch (Exception ex)
        {
            GD.Print($"[Merchant2CuteII] language: cannot read LocManager ({ex.Message}); keeping {Current}");
        }

        return Current;
    }

    /// <summary>游戏语言码 → 界面语言。未支持的语言回退英文。</summary>
    public static UiLanguage Map(string? code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return UiLanguage.English;
        }

        return code.Trim().ToLowerInvariant() switch
        {
            "zhs" => UiLanguage.SimplifiedChinese,
            "zht" => UiLanguage.TraditionalChinese,
            "eng" => UiLanguage.English,
            _ => UiLanguage.English,
        };
    }
}
