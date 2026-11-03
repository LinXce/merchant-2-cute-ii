using System;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes;
using Merchant2CuteII.script.Settings;

namespace Merchant2CuteII.script.Patches;

/// <summary>
/// 在场景进入节点树**之前**应用骨架选择（详见 <see cref="MerchantSceneSkinner"/>）。
///
/// 选 <c>NSceneContainer.SetCurrentScene</c> 作为注入点的原因：
/// 它是商店/事件房间上屏的唯一入口，此时 scene 已 Instantiate 但尚未 <c>AddChild</c>，
/// spine 还没初始化 —— 改 <c>skeleton_data_res</c> 不会触发内部重建，也不会有陈旧指针。
/// </summary>
[HarmonyPatch(typeof(NSceneContainer), nameof(NSceneContainer.SetCurrentScene))]
public static class MerchantSceneSkinPatch
{
    [HarmonyPrefix]
    // 用**位置参数** __0 绑定第一个实参，而不是按名字：
    // 游戏换过参数名（control → node 就发生过），按名字绑定会在版本间直接失效，
    // 位置绑定则对参数改名免疫。
    public static void Prefix(Control __0)
    {
        try
        {
            MerchantSceneSkinner.ApplyBeforeEnteringTree(__0);
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[Merchant2CuteII] pre-tree skin apply failed: {ex}");
        }
    }
}
