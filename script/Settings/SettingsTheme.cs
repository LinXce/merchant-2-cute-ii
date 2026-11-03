using Godot;

namespace Merchant2CuteII.script.Settings;

/// <summary>
/// 设置页配色 —— 取自游戏本身的暗金黄色
/// （`Color(0.937, 0.784, 0.318)` 亮金 / `Color(0.73, 0.543, 0.139)` 暗金，均在原版 tscn 中采样）。
/// </summary>
public static class SettingsTheme
{
    /// <summary>标题、分组标题用的亮金。</summary>
    public static readonly Color GoldBright = new(0.937f, 0.784f, 0.318f);

    /// <summary>字段名等正文用暗金。</summary>
    public static readonly Color Gold = new(0.73f, 0.543f, 0.139f);

    /// <summary>说明文字：灰白（与金色名称区分开，便于阅读）。</summary>
    public static Color Hint => new(0.84f, 0.84f, 0.82f, 0.92f);

    /// <summary>只读值（同色系，略暗）。</summary>
    public static Color Value => new(0.73f, 0.543f, 0.139f, 0.9f);

    public static void ApplyTitle(Label label)
    {
        label.AddThemeFontSizeOverride("font_size", 30);
        label.AddThemeColorOverride("font_color", GoldBright);
    }

    public static void ApplySection(Label label)
    {
        label.AddThemeFontSizeOverride("font_size", 21);
        label.AddThemeColorOverride("font_color", GoldBright);
    }

    public static void ApplyField(Label label)
    {
        label.AddThemeFontSizeOverride("font_size", 18);
        label.AddThemeColorOverride("font_color", Gold);
    }

    public static void ApplyHint(Label label)
    {
        label.AddThemeFontSizeOverride("font_size", 14);
        label.AddThemeColorOverride("font_color", Hint);
    }

    public static void ApplyValue(Label label)
    {
        label.AddThemeFontSizeOverride("font_size", 16);
        label.AddThemeColorOverride("font_color", Value);
    }
}
