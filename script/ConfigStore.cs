using Merchant2CuteII.script.Settings;

namespace Merchant2CuteII.script;

/// <summary>
/// 兼容层：旧代码里的 <c>ConfigStore.Load()/Save()</c> 仍然可用，
/// 但数据模型已经升级到 <see cref="ModSettings"/>（schema 2，见 <see cref="SettingsStore"/>）。
/// </summary>
public static class ConfigStore
{
    public static ModSettings Settings => SettingsHost.Current;

    public static void Load()
    {
        SettingsHost.Load();
    }

    public static void Save()
    {
        // 兼容壳：旧代码调 Save() 时也要广播，否则场景不刷新、BaseLib 的 cfg 也不会同步
        SettingsHost.SaveAndNotify();
    }
}
