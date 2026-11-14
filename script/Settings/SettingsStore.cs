using System;
using Godot;

namespace Merchant2CuteII.script.Settings;

/// <summary>
/// 配置文件读写。
///
/// 路径不再写死：通过 <see cref="ConfigPaths"/> 在多个候选位置中挑选
/// （桌面版仍是 <c>user://merchant2cute_config.json</c>，手机移植版会自动回退到可写位置）。
/// 解析/迁移/序列化的纯逻辑在 <see cref="SettingsJson"/>（可单测），这里只负责 Godot 文件 IO 与日志。
/// </summary>
public static class SettingsStore
{
    /// <summary>老版本使用的固定路径（保持向后兼容；实际使用 <see cref="ConfigPaths.ActivePath"/>）。</summary>
    public const string ConfigPath = "user://" + ConfigPaths.FileName;

    /// <summary>最近一次读到的文件是否来自旧版本（schema 1）。</summary>
    public static bool LastLoadWasLegacy { get; private set; }

    /// <summary>上一次成功读取/写入的实际路径。</summary>
    public static string ActivePath => ConfigPaths.ActivePath;
    public static bool? LastSaveSucceeded { get; private set; }
    public static string? LastError { get; private set; }

    public static ModSettings Load()
    {
        LastLoadWasLegacy = false;
        LastSaveSucceeded = null;
        LastError = null;
        ConfigPaths.Reset();

        string? path = ConfigPaths.ResolveReadPath();
        if (path == null)
        {
            // 找不到也要把"应该在哪些位置找"打到日志里，手机用户可直接对照放置文件
            GD.Print("[Merchant2CuteII] config: using defaults\n" + ConfigPaths.DescribeEnvironment());
            EnsureFileExists();
            return new ModSettings();
        }

        string? json = ReadAllText(path);
        if (json == null)
        {
            EnsureFileExists();
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
        LastSaveSucceeded = false;
        LastError = null;
        try
        {
            string json = SettingsJson.Serialize(settings);
            for (int attempt = 0; attempt <= ConfigPaths.Candidates().Count; attempt++)
            {
                string? path = ConfigPaths.ResolveWritePath();
                if (path == null) break;
                string temporary = path + ".tmp";
                try
                {
                    using (Godot.FileAccess? file = Godot.FileAccess.Open(temporary, Godot.FileAccess.ModeFlags.Write))
                    {
                        if (file == null) throw new System.IO.IOException(Godot.FileAccess.GetOpenError().ToString());
                        file.StoreString(json);
                        file.Flush();
                        if (file.GetError() != Error.Ok) throw new System.IO.IOException(file.GetError().ToString());
                    }
                    if (ReadAllText(temporary) != json) throw new System.IO.IOException("Config verification failed");

                    // 同目录替换：写入/验证失败时保留上一份完整配置。
                    Error renamed = DirAccess.RenameAbsolute(ProjectSettings.GlobalizePath(temporary),
                        ProjectSettings.GlobalizePath(path));
                    if (renamed != Error.Ok) throw new System.IO.IOException(renamed.ToString());
                    LastSaveSucceeded = true;
                    LastError = null;
                    GD.Print("[Merchant2CuteII] config: saved and verified " + path);
                    return true;
                }
                catch (Exception ex)
                {
                    LastError = ex.Message;
                    GD.PrintErr($"[Merchant2CuteII] config write failed at {path}: {ex.Message}; trying fallback");
                    DirAccess.RemoveAbsolute(ProjectSettings.GlobalizePath(temporary));
                    ConfigPaths.RejectWritePath(path);
                }
            }
            LastError ??= "No writable config path";
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            GD.PrintErr($"[Merchant2CuteII] Failed to save settings: {ex.Message}");
        }
        return false;
    }

    /// <summary>
    /// 首次运行时落一份默认配置，让用户能"看到文件、知道该改哪"。
    /// 手机移植版尤其需要：否则用户根本不知道该把配置文件放哪里。
    /// </summary>
    public static bool EnsureFileExists()
    {
        try
        {
            string? path = ConfigPaths.ResolveWritePath();
            if (path == null)
            {
                return false;
            }

            // 先关闭读句柄，Windows 下否则可能锁住随后需要替换的目标文件。
            if (ReadAllText(path) is string existing && SettingsJson.TryParse(existing, out _, out _))
            {
                return true;
            }

            bool written = Save(new ModSettings());
            if (written)
            {
                GD.Print("[Merchant2CuteII] config: created default config at " + path);
            }

            return written;
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[Merchant2CuteII] Failed to create default config: {ex.Message}");
            return false;
        }
    }

    public static bool IsFootLike(string handVariant)
    {
        return SettingsJson.IsFootLike(handVariant);
    }

    private static string? ReadAllText(string path)
    {
        try
        {
            using Godot.FileAccess? file = Godot.FileAccess.Open(path, Godot.FileAccess.ModeFlags.Read);
            if (file == null)
            {
                GD.PrintErr("[Merchant2CuteII] Cannot open config for reading: " + path
                    + " (" + Godot.FileAccess.GetOpenError() + ")");
                return null;
            }

            string json = file.GetAsText();
            return string.IsNullOrWhiteSpace(json) ? null : json;
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[Merchant2CuteII] Failed to read settings from {path}: {ex.Message}");
            return null;
        }
    }
}
