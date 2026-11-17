using Godot;
using HarmonyLib;
using System.Linq;
using System.Reflection;
using MegaCrit.Sts2.Core.Nodes.Screens.Shops;
using Merchant2CuteII.script.Compat;
using Merchant2CuteII.script.Settings;

namespace Merchant2CuteII.script.Patches;

// 1.0.36 已负责房间入场前的 ApplyBeforeEnteringTree。
// 热切换会另外实例化库存以提取手部；补齐这个不经过房间入口的窄路径。
[HarmonyPatch]
public static class ManagedMerchantInstantiationPatch
{
    public static bool Prepare() => VersionProfile.SkinChangerCooperation;
    public static MethodBase TargetMethod() => typeof(PackedScene).GetMethods(BindingFlags.Instance | BindingFlags.Public)
        .Single(method => method.Name == nameof(PackedScene.Instantiate) && !method.IsGenericMethod
            && method.GetParameters().Length == 1
            && method.GetParameters()[0].ParameterType == typeof(PackedScene.GenEditState));
    public static void Postfix(Node __result)
    {
        if (__result is NMerchantInventory && SkinChangerCompatibility.IsManaged)
            MerchantSceneSkinner.ApplyBeforeEnteringTree(__result);
    }
}
