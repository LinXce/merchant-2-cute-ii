using System;
using Godot;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;

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
				string animationName = variant == "hand" ? "default" : variant;
				if (ms.HasAnimation(animationName))
				{
					ms.GetAnimationState().SetAnimation(animationName);
					return true;
				}
				else if (ms.HasAnimation("default"))
				{
					ms.GetAnimationState().SetAnimation("default");
					return true;
				}
			}
			catch (Exception ex)
			{
				GD.PrintErr($"[Merchant2CuteII] AnimationHelper.TryApplyVariantToHandNode failed: {ex.Message}");
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
