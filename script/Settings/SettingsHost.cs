using System;

namespace Merchant2CuteII.script.Settings;

/// <summary>全局设置实例（进程内唯一）。任何改动之后调用 <see cref="Save"/> + <see cref="NotifyChanged"/>。</summary>
public static class SettingsHost
{
    private static ModSettings _current = new();
    private static bool _loaded;

    public static ModSettings Current => _current;

    /// <summary>设置发生变更（UI 按钮、控制台命令都会触发）。</summary>
    public static event Action? Changed;

    public static void Load()
    {
        _current = SettingsStore.Load();
        _loaded = true;
    }

    public static void EnsureLoaded()
    {
        if (!_loaded) Load();
    }

    public static bool Save()
    {
        EnsureLoaded();
        return SettingsStore.Save(_current);
    }

    /// <summary>归一化 + 保存 + 广播。给「已经直接改过 Current 上的值」的调用方用（如控制台命令）。</summary>
    public static void SaveAndNotify()
    {
        EnsureLoaded();
        _current.Normalize();
        Save();
        NotifyChanged();
    }

    public static void NotifyChanged()
    {
        if (Changed == null) return;
        foreach (Action handler in Changed.GetInvocationList())
        {
            try { handler(); }
            catch (Exception ex) { Godot.GD.PrintErr($"[Merchant2CuteII] settings listener failed: {ex.Message}"); }
        }
    }

    /// <summary>读 + 改 + 存 的便捷写法。</summary>
    public static void Mutate(Action<ModSettings> mutate, bool save = true)
    {
        EnsureLoaded();
        mutate(_current);
        _current.Normalize();
        if (save)
        {
            Save();
        }

        NotifyChanged();
    }
}
