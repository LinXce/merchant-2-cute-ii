/*
 * Merchant2CuteII - A Slay the Spire 2 Mod
 * Copyright (C) 2026 LinXce
 * 
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Helpers;
using Merchant2CuteII.script;
using Merchant2CuteII.script.Settings;

namespace Merchant2CuteII;

[ModInitializer("Init")]
public static class Merchant2CuteIIMod
{
    private const string ModName = "Merchant2CuteII";

    public static void Init()
    {
        Harmony harmony = new Harmony(ModName);
        PatchAllSafely(harmony);

        try
        {
            // 0) 版本能力位：先确定当前游戏版本，后面所有版本相关分支都查它
            script.Compat.GameVersion.ForceFromEnvironment();
            GD.Print("[Merchant2CuteII] " + script.Compat.VersionProfile.Describe());

            // 1) 先读配置：必须在任何补丁跑起来之前完成（含旧版 schema 1 的迁移）
            SettingsHost.Load();

            // 1.5) 读视觉微调覆盖值（独立文件，不存在即全默认）
            MerchantVisualMotion.Load();

            // 2) 预载两套骨架的 .tres，避免第一次进商店时卡顿
            SkinApplier.WarmUp();

            script.ResourcePreloader.PreloadAll();
            TaskHelper.RunSafely(script.ResourcePreloader.PreloadAudioPathsAsync(script.ModConfig.Paths.MerchantVoicePaths));
            // Run heavy spine preloads asynchronously to avoid blocking startup
            TaskHelper.RunSafely(script.ResourcePreloader.PreloadSpinePathsAsync(script.ModConfig.Paths.SpinePaths));

            // 3) 设置一改，场景里已有的商人立刻跟上
            SettingsHost.Changed += () => MerchantRuntime.RefreshAll();

#if MERCHANT_BASELIB
            // 可选：把同一份设置挂到 BaseLib 的「模组设置」界面里（enum → 下拉，volume → 滑条）
            script.Integration.MerchantBaseLibConfig.Register();
#endif
#if MERCHANT_RITSILIB
            // 可选：同时在 RitsuLib 的设置页里提供一份（AddEnumChoice / AddSlider / AddToggle）
            script.Integration.RitsuLibSettings.Register();
#endif

            GD.Print($"[Merchant2CuteII] Ready. {MerchantRuntime.DescribeCurrent()}");
        }
        catch (System.Exception ex)
        {
            GD.PrintErr($"[Merchant2CuteII] Initialisation failed: {ex}");
        }
    }

    /// <summary>
    /// 逐类打补丁：某个补丁因为游戏 API 变了而失败时，只跳过它并报错，
    /// 不会像 <c>Harmony.PatchAll()</c> 那样抛异常导致整个 mod 初始化失败（设置也读不到）。
    /// </summary>
    private static void PatchAllSafely(Harmony harmony)
    {
        int applied = 0;
        int failed = 0;

        foreach (System.Type type in typeof(Merchant2CuteIIMod).Assembly.GetTypes())
        {
            if (type.GetCustomAttributes(typeof(HarmonyPatch), true).Length == 0)
            {
                continue;
            }

            // 不设最低版本：所有补丁都尝试打；某个补丁的目标 API 在本版本不存在时，
            // Harmony 会抛异常并被下面的 catch 吞掉（只跳过这一个，不影响其它补丁）。
            try
            {
                harmony.CreateClassProcessor(type).Patch();
                applied++;
            }
            catch (System.Exception ex)
            {
                failed++;
                GD.PrintErr($"[Merchant2CuteII] patch skipped for {type.Name}: {ex.Message}");
            }
        }

        GD.Print($"[Merchant2CuteII] Harmony patches: applied={applied}, failed={failed}");
    }
}
