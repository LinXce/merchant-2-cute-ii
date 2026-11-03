using System;
using Godot;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Events.Custom;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Screens.Shops;

namespace Merchant2CuteII.script.Settings;

/// <summary>
/// 手部变体的运行时应用（只放动画，不换骨架）。
///
/// 骨架（身体 / 手部用哪一套）由 <see cref="MerchantSceneSkinner"/> 在**场景进树前**决定；
/// 因为运行期 <c>SetSkeletonDataRes</c> 既会在场景装配期触发
/// <c>add_child</c> 失败（闪退），也会让游戏缓存的 <c>MegaBone/MegaSkeleton</c> 变成陈旧指针。
/// </summary>
public static class SkinApplier
{
    public static MerchantKind? ResolveKind(Node? node)
    {
        Node? current = node;
        int guard = 0;
        while (current != null && guard++ < 64)
        {
            if (current is NFakeMerchant)
            {
                return MerchantKind.Fake;
            }

            if (current is NMerchantRoom)
            {
                return MerchantKind.Real;
            }

            current = current.GetParent();
        }

        return null;
    }

    public static MerchantSkinSettings SettingsFor(MerchantKind kind)
    {
        return SettingsHost.Current.For(kind);
    }

    /// <summary>对 <c>NMerchantHand</c> 套用它所属商人的手部变体。返回是否成功放了动画。</summary>
    public static bool ApplyToHand(Node? handNode)
    {
        if (handNode == null || !GodotObject.IsInstanceValid(handNode))
        {
            return false;
        }

        MerchantKind? kind = ResolveKind(handNode);
        if (kind == null)
        {
            return false;
        }

        Node? host = handNode.GetParent();
        if (host == null)
        {
            return false;
        }

        MerchantSkinSettings skin = SettingsFor(kind.Value);

        try
        {
            var sprite = new MegaSprite(host);
            return ApplyHandVariant(host, sprite, skin.HandVariant);
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[Merchant2CuteII] ApplyToHand failed: {ex.Message}");
            return false;
        }
    }

    private static bool ApplyHandVariant(Node host, MegaSprite sprite, string variant)
    {
        string[] chain = HandVariants.AnimationFallbackChain(variant);

        if (AnimationHelper.ShouldUseLegacyHandSwitching())
        {
            foreach (string name in chain)
            {
                if (!sprite.HasAnimation(name))
                {
                    continue;
                }

                sprite.GetAnimationState().BoundObject.Call("set_animation", name, false, 0);
                return true;
            }

            return false;
        }

        if (!sprite.IsAnimationStateReady())
        {
            host.RunWhenSpineReady(sprite, animState => SetFirstAvailable(animState, sprite, chain));
            return true;
        }

        return SetFirstAvailable(sprite.GetAnimationState(), sprite, chain);
    }

    private static bool SetFirstAvailable(MegaAnimationState animState, MegaSprite sprite, string[] chain)
    {
        if (animState == null)
        {
            return false;
        }

        foreach (string name in chain)
        {
            if (!sprite.HasAnimation(name))
            {
                continue;
            }

            animState.BoundObject.Call("set_animation", name, false, 0);
            return true;
        }

        GD.PrintErr("[Merchant2CuteII] No usable hand animation among: " + string.Join(", ", chain));
        return false;
    }

    /// <summary>预载 4 份骨架资源，避免第一次进商店时卡顿。</summary>
    public static void WarmUp()
    {
        MerchantSceneSkinner.WarmUp();
    }
}
