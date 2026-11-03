using System;
using System.Collections.Generic;
using Godot;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Debug;
using MegaCrit.Sts2.Core.Helpers;
using Merchant2CuteII.script.Settings;

namespace Merchant2CuteII.script
{
    /// <summary>
    /// Spine 动画操作的小工具。变体选择与骨架切换已挪到 <see cref="SkinApplier"/>；
    /// 这里保留「按名字放一段动画」的通用能力，供投毒动画等场景使用。
    /// </summary>
    internal static class AnimationHelper
    {
        // 阈值 NewSpineReadyVersion = 0.108.0 已集中到 Compat/VersionProfile.cs

        /// <summary>对 <c>NMerchantHand</c> 套用「它所在那个商人」的变体设置。</summary>
        public static bool TryApplyVariantToHandNode(Node handNode)
        {
            return SkinApplier.ApplyToHand(handNode);
        }

        /// <summary>旧版 set_animation 签名判断，统一由 <see cref="Compat.VersionProfile"/> 提供。</summary>
        public static bool ShouldUseLegacyHandSwitching()
        {
            return Compat.VersionProfile.LegacyAnimationSignature;
        }

        /// <summary>在目标节点上播放指定动画；不存在则返回 false（不会乱塞不存在的动画名）。</summary>
        public static bool TrySetAnimationOnTarget(Node target, string animationName)
        {
            if (target == null || string.IsNullOrEmpty(animationName))
                return false;

            try
            {
                var ms = new MegaSprite(target);
                if (!HasAnimation(ms, animationName))
                {
                    return false;
                }

                MegaAnimationState state = ms.GetAnimationState();
                return Play(state, animationName);
            }
            catch (Exception ex)
            {
                GD.PrintErr($"[Merchant2CuteII] AnimationHelper.TrySetAnimationOnTarget failed: {ex.Message}");
            }

            return false;
        }

        /// <summary>按候选链播放第一个存在的动画（例如 white → foot → default）。</summary>
        public static bool TrySetFirstAvailableAnimation(Node target, IReadOnlyList<string> candidates)
        {
            if (target == null || candidates == null || candidates.Count == 0)
                return false;

            try
            {
                var ms = new MegaSprite(target);
                MegaAnimationState state = ms.GetAnimationState();
                foreach (string name in candidates)
                {
                    if (HasAnimation(ms, name) && Play(state, name))
                    {
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                GD.PrintErr($"[Merchant2CuteII] AnimationHelper.TrySetFirstAvailableAnimation failed: {ex.Message}");
            }

            return false;
        }

        public static bool HasAnimation(MegaSprite sprite, string animationName)
        {
            try
            {
                return sprite.HasAnimation(animationName);
            }
            catch
            {
                return false;
            }
        }

        private static bool Play(MegaAnimationState state, string animationName, bool loop = false, int track = 0)
        {
            if (state == null)
            {
                return false;
            }

            state.BoundObject.Call("set_animation", animationName, loop, track);
            return true;
        }
    }
}
