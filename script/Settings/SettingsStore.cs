using System;
using Godot;

namespace Merchant2CuteII.script.Settings;

/// <summary>
/// 配置文件读写。文件位置与旧版完全一样：<c>user://merchant2cute_config.json</c>。
/// 解析/迁移/序列化的纯逻辑在 <see cref="SettingsJson"/>（可单测），这里只负责 Godot 文件 IO 与日志。
/// </summary>
public static class SettingsStore
{
    public const string ConfigPath = "user://merchant2cute_config.json";

    /// <summary>最近一次读到的文件是否来自旧版本（schema 1）。</summary>
    public static bool LastLoadWasLegacy { get; private set; }

    public static ModSettings Load()
    {
        LastLoadWasLegacy = false;

        string? json = ReadAllText();
        if (json == null)
        {
            return new ModSettings();
        }

        ModSettings settings = SettingsJson.Parse(json, out bool wasLegacy);
        LastLoadWasLegacy = wasLegacy;

        if (wasLegacy)
        {
            GD.Print("[Merchant2CuteII] Legacy config (schema 1) detected and migrated to schema "
                + ModSettings.CurrentSchema + ".");
        }

        return settings;
    }

    public static bool Save(ModSettings settings)
    {
        try
        {
            string json = SettingsJson.Serialize(settings);
            using Godot.FileAccess? file = Godot.FileAccess.Open(ConfigPath, Godot.FileAccess.ModeFlags.Write);
            if (file == null)
            {
                GD.PrintErr("[Merchant2CuteII] Cannot open config for writing: " + ConfigPath);
                return false;
            }

            file.StoreString(json);
            return true;
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[Merchant2CuteII] Failed to save settings: {ex.Message}");
            return false;
        }
    }

    public static bool IsFootLike(string handVariant)
    {
        return SettingsJson.IsFootLike(handVariant);
    }

    private static string? ReadAllText()
    {
        try
        {
            if (!Godot.FileAccess.FileExists(ConfigPath))
            {
                return null;
            }

            using Godot.FileAccess? file = Godot.FileAccess.Open(ConfigPath, Godot.FileAccess.ModeFlags.Read);
            if (file == null)
            {
                return null;
            }

            string json = file.GetAsText();
            return string.IsNullOrWhiteSpace(json) ? null : json;
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[Merchant2CuteII] Failed to read settings: {ex.Message}");
            return null;
        }
    }
}
