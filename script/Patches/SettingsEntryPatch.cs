using System;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Screens.Settings;
using Merchant2CuteII.script.Settings;

namespace Merchant2CuteII.script.Patches;

/// <summary>
/// 把设置入口挂到**游戏设置界面**（设置 → 游戏设置），紧跟原版「Modding」行之后。
///
/// 挂在 <see cref="NSettingsScreen._Ready"/> 的 Postfix：此时原版已经把 %Modding 行
/// 与 %ModdingDivider 设为可见（见 NSettingsScreen._Ready 里对 UniqueName 的访问），
/// 我们复制这一行即可得到完全一致的样式。
/// </summary>
[HarmonyPatch(typeof(NSettingsScreen), "_Ready")]
public static class SettingsEntryPatch
{
    [HarmonyPostfix]
    public static void Postfix(NSettingsScreen __instance)
    {
        try
        {
            // 等场景装配完成，避免 _Ready 内向正在创建子节点的容器 AddChild。
            Callable.From(() => MerchantSettingsEntry.Inject(__instance)).CallDeferred();
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[Merchant2CuteII] settings entry patch failed: {ex}");
        }
    }
}
