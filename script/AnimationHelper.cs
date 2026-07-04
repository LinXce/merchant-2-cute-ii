using System;
using Godot;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Helpers;

namespace Merchant2CuteII.script
{
    internal static class AnimationHelper
    {
        public static bool TryApplyVariantToHandNode(Node handNode)
        {
            if (handNode == null)
                return false;

            Node? parent = handNode.GetParent();
            if (parent == null)
                return false;

            try
            {
                MegaSprite ms = new MegaSprite(parent);
                string variant = ModConfig.Options.HandVariant;

                if (!ms.IsAnimationStateReady())
                {
                    handNode.RunWhenSpineReady(ms, animState => ApplyVariantToAnimationState(animState, ms, variant));
                    return true;
                }

                return ApplyVariantToAnimationState(ms.GetAnimationState(), ms, variant);
            }
            catch (Exception ex)
            {
                GD.PrintErr($"[Merchant2CuteII] AnimationHelper.TryApplyVariantToHandNode failed: {ex.Message}");
            }

            return false;
        }

        private static bool ApplyVariantToAnimationState(MegaAnimationState animState, MegaSprite sprite, string variant)
        {
            if (animState == null)
                return false;

            string animationName = variant == "hand" ? "default" : variant;
            if (sprite.HasAnimation(animationName))
            {
                animState.SetAnimation(animationName);
                return true;
            }

            if ((variant == "white" || variant == "black") && sprite.HasAnimation("foot"))
            {
                animState.SetAnimation("foot");
                return true;
            }

            if (sprite.HasAnimation("default"))
            {
                animState.SetAnimation("default");
                return true;
            }

            return false;
        }

        public static bool TrySetAnimationOnTarget(Node target, string animationName)
        {
            if (target == null || string.IsNullOrEmpty(animationName))
                return false;

            try
            {
                MegaSprite ms = new MegaSprite(target);
                if (ms.HasAnimation(animationName))
                {
                    ms.GetAnimationState().SetAnimation(animationName);
                    return true;
                }
            }
            catch (Exception ex)
            {
                GD.PrintErr($"[Merchant2CuteII] AnimationHelper.TrySetAnimationOnTarget failed: {ex.Message}");
            }

            return false;
        }
    }
}
