using System;
using Godot;
using MegaCrit.Sts2.Core.Debug;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Helpers;

namespace Merchant2CuteII.script
{
    internal static class AnimationHelper
    {
        private static readonly SemanticVersion NewSpineReadyVersion = new SemanticVersion(0, 108, 0);

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

                if (ShouldUseLegacyHandSwitching())
                {
                    return ApplyVariantLegacy(ms, variant);
                }

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

        public static bool ShouldUseLegacyHandSwitching()
        {
            SemanticVersion? gameVersion = ReleaseInfoManager.Instance.SemVer;
            if (gameVersion == null)
            {
                return false;
            }

            return gameVersion.CompareTo(NewSpineReadyVersion) < 0;
        }

        private static bool ApplyVariantLegacy(MegaSprite sprite, string variant)
        {
            string animationName = variant == "hand" ? "default" : variant;
            if (sprite.HasAnimation(animationName))
            {
                return TrySetAnimationLegacy(sprite.GetAnimationState(), animationName);
            }

            if ((variant == "white" || variant == "black") && sprite.HasAnimation("foot"))
            {
                return TrySetAnimationLegacy(sprite.GetAnimationState(), "foot");
            }

            if (sprite.HasAnimation("default"))
            {
                return TrySetAnimationLegacy(sprite.GetAnimationState(), "default");
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
                return TrySetAnimationNew(animState, animationName, false);
            }

            if ((variant == "white" || variant == "black") && sprite.HasAnimation("foot"))
            {
                return TrySetAnimationNew(animState, "foot", false);
            }

            if (sprite.HasAnimation("default"))
            {
                return TrySetAnimationNew(animState, "default", false);
            }

            return false;
        }

        private static bool TrySetAnimationLegacy(MegaAnimationState animState, string animationName)
        {
            if (animState == null)
                return false;

            animState.BoundObject.Call("set_animation", animationName);
            return true;
        }

        private static bool TrySetAnimationNew(MegaAnimationState animState, string animationName, bool loop, int trackId = 0)
        {
            if (animState == null)
                return false;

            animState.BoundObject.Call("set_animation", animationName, loop, trackId);
            return true;
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
                    if (ShouldUseLegacyHandSwitching())
                    {
                        return TrySetAnimationLegacy(ms.GetAnimationState(), animationName);
                    }

                    return TrySetAnimationNew(ms.GetAnimationState(), animationName, false);
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
