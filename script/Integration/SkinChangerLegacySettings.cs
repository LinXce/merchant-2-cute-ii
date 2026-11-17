using Godot;
using MegaCrit.Sts2.Core.Nodes.Screens.Shops;
using Merchant2CuteII.script.Settings;

namespace MegaCrit.Sts2.Core.DevConsole.ConsoleCommands;

// Skin Changer 在加载 DLL 前审核并重写这两个旧接口，注册原命令和配置保存回调。
// 保留真实的本地节点帮助方法与无管理器时的实现，不能用空方法冒充契约。
public partial class MerchantConsoleCmd
{
    public static int ApplyToExistingHands()
    {
        if (Engine.GetMainLoop() is not SceneTree tree) return 0;
        int count = 0;
        FindAndApplyRecursive(tree.Root, ref count);
        return count;
    }

    private static void FindAndApplyRecursive(Node node, ref int count)
    {
        if (node is NMerchantHand hand && TryApplyToHand(hand)) count++;
        foreach (Node child in node.GetChildren()) FindAndApplyRecursive(child, ref count);
    }

    public static bool TryApplyToHand(NMerchantHand hand) => SkinApplier.ApplyToHand(hand);

    public static void UpdateLegVisibility(bool visible)
    {
        if (Engine.GetMainLoop() is SceneTree tree) UpdateLegVisibilityStatic(tree.Root, visible);
    }

    public static void UpdateLegVisibilityStatic(Node node, bool visible)
    {
        if (node is TextureRect leg && leg.Name == "MerchantInventoryLeg"
            && (Merchant2CuteII.script.Compat.BlitzMerchantCompatibility.IsInBlitzShop(leg)
                || Merchant2CuteII.script.Compat.SkinChangerCompatibility.MayApply(leg, MerchantKind.Real)))
            leg.Visible = visible && MerchantRuntime.ShouldShowInventoryLegFor(MerchantKind.Real, leg);
        foreach (Node child in node.GetChildren()) UpdateLegVisibilityStatic(child, visible);
    }
}
