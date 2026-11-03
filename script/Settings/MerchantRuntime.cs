using System;
using System.Collections.Generic;
using Godot;
using MegaCrit.Sts2.Core.Nodes.Events.Custom;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Screens.Shops;

namespace Merchant2CuteII.script.Settings;

/// <summary>
/// 「设置改了之后，把已经出现在场景里的商人重新刷一遍」。
/// 控制台命令、设置界面按钮、以及启动时都会走这里。
/// </summary>
public static class MerchantRuntime
{
    /// <summary>购物界面那条腿要不要显示：玩家开关 && 当前不是脚系变体。</summary>
    public static bool ShouldShowInventoryLeg(MerchantKind kind)
    {
        return SettingsJson.ShouldShowInventoryLeg(SettingsHost.Current.For(kind), kind);
    }

    /// <summary>重新套用所有设置到当前场景树。返回受影响的手部节点数量。</summary>
    public static int RefreshAll()
    {
        // 注意：这里**不能** SkinApplier.ForgetAll()。
        // 缓存记录的是「这根骨架当前被我换成了哪套」；清掉之后，玩家从"另一套"切回"本套"时
        // NeedsSkeletonSwap 会因为"没动过 + 选的是 own"而跳过，导致画面停留在借来的美术上。
        SceneTree? tree = Engine.GetMainLoop() as SceneTree;
        Node? sceneRoot = tree?.Root;
        if (sceneRoot == null)
        {
            return 0;
        }

        int hands = 0;
        var visited = new HashSet<ulong>();
        Walk(sceneRoot, node =>
        {
            if (node is NMerchantHand hand)
            {
                if (SkinApplier.ApplyToHand(hand))
                {
                    hands++;
                }
            }
            else if (node is TextureRect rect && rect.Name == "MerchantInventoryLeg")
            {
                rect.Visible = ShouldShowInventoryLeg(MerchantKind.Real);
            }
            // 注意：这里刻意不换骨架。骨架在场景进树前（MerchantSceneSkinPatch）就选好了；
            // 运行期再 SetSkeletonDataRes 会打断 spine 并使游戏缓存的 bone/skeleton 指针失效。
        });

        GD.Print($"[Merchant2CuteII] Settings refreshed: {hands} hand node(s) updated.");
        return hands;
    }

    /// <summary>下一帧再刷一次（节点 _Ready 顺序可能导致当帧找不到子节点）。</summary>
    public static async System.Threading.Tasks.Task RefreshAllNextFrame()
    {
        SceneTree? tree = Engine.GetMainLoop() as SceneTree;
        if (tree == null)
        {
            return;
        }

        await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        RefreshAll();
    }

    public static int CountHands(Node root)
    {
        int count = 0;
        Walk(root, node =>
        {
            if (node is NMerchantHand)
            {
                count++;
            }
        });
        return count;
    }

    private static void Walk(Node root, Action<Node> visit)
    {
        if (root == null)
        {
            return;
        }

        foreach (Node child in root.GetChildren())
        {
            if (child == null)
            {
                continue;
            }

            try
            {
                visit(child);
            }
            catch (Exception ex)
            {
                GD.PrintErr($"[Merchant2CuteII] Runtime visit failed on {child.Name}: {ex.Message}");
            }

            Walk(child, visit);
        }
    }

    /// <summary>给状态命令用的一行摘要。</summary>
    public static string DescribeCurrent()
    {
        ModSettings s = SettingsHost.Current;
        return string.Format(
            "real[body={0}, handSet={1}, hand={2}, leg={3}] fake[body={4}, handSet={5}, hand={6}] voice[lang={7}, db={8:+0.##;-0.##;0}]",
            s.RealMerchant.BodySet, s.RealMerchant.HandSet, s.RealMerchant.HandVariant,
            s.RealMerchant.ShowInventoryLeg ? "on" : "off",
            s.FakeMerchant.BodySet, s.FakeMerchant.HandSet, s.FakeMerchant.HandVariant,
            s.Voice.Language, s.Voice.VolumeDb);
    }
}
