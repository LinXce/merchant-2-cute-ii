using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Nodes.Events.Custom;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Screens.Shops;

namespace Merchant2CuteII.script;

internal static class MerchantContextResolver
{
	public static bool IsRealMerchantButton(NMerchantButton button)
	{
		return TryGetMerchantSceneRoot(button) is NMerchantRoom;
	}

	public static bool IsFakeMerchantButton(NMerchantButton button)
	{
		return TryGetMerchantSceneRoot(button) is NFakeMerchant;
	}

	public static bool IsRealMerchantHand(NMerchantHand hand)
	{
		return TryGetMerchantInventoryRoot(hand) is NMerchantInventory inventory && inventory is not NFakeMerchantInventory;
	}

	public static bool IsFakeMerchantHand(NMerchantHand hand)
	{
		return TryGetMerchantInventoryRoot(hand) is NFakeMerchantInventory;
	}

	private static Node? TryGetMerchantSceneRoot(Node node)
	{
		Node? current = node;
		while (current != null)
		{
			if (current is NMerchantRoom || current is NFakeMerchant)
			{
				return current;
			}

			current = current.GetParent();
		}

		return null;
	}

	private static NMerchantInventory? TryGetMerchantInventoryRoot(Node node)
	{
		Node? current = node;
		while (current != null)
		{
			if (current is NMerchantInventory inventory)
			{
				return inventory;
			}

			current = current.GetParent();
		}

		return null;
	}
}

[HarmonyPatch(typeof(NMerchantButton), "_Ready")]
public static class RealMerchantButtonPatch
{
	[HarmonyPrefix]
	public static bool Prefix(NMerchantButton __instance)
	{
		try
		{
			if (__instance == null || !MerchantContextResolver.IsRealMerchantButton(__instance))
			{
				return true;
			}

			MegaSkeletonDataResource merchantSkeleton = ResourceLoader.Load<MegaSkeletonDataResource>(ModConfig.MerchantTopSpinePath, null, ResourceLoader.CacheMode.Reuse);
			if (merchantSkeleton == null)
			{
				GD.PrintErr($"[Merchant2CuteII] Cannot load top model: {ModConfig.MerchantTopSpinePath}");
				return true;
			}

			Node merchantVisual = __instance.GetNodeOrNull("%MerchantVisual");
			if (merchantVisual == null)
			{
				GD.PrintErr("[Merchant2CuteII] Cannot find %MerchantVisual in MerchantButton");
				return true;
			}

			if (merchantVisual is Node2D merchantVisual2D)
			{
				merchantVisual2D.Position += ModConfig.MerchantVisualPositionOffset;
				merchantVisual2D.Scale = ModConfig.MerchantVisualScale;
			}

			MegaSprite megaSprite = new MegaSprite(merchantVisual);
			megaSprite.SetSkeletonDataRes(merchantSkeleton);
			return true;
		}
		catch (System.Exception ex)
		{
			GD.PrintErr($"[Merchant2CuteII] Error in MerchantButton._Ready prefix: {ex.Message}");
			return true;
		}
	}
}

[HarmonyPatch(typeof(NMerchantHand), "_Ready")]
public static class RealMerchantHandPatch
{
	[HarmonyPrefix]
	public static void Prefix(NMerchantHand __instance)
	{
		try
		{
			if (__instance == null || !MerchantContextResolver.IsRealMerchantHand(__instance))
			{
				return;
			}

			if (__instance.GetParent() is Node2D merchantHandParent)
			{
				MegaSkeletonDataResource merchantHandSkeleton = ResourceLoader.Load<MegaSkeletonDataResource>(ModConfig.MerchantHandSpinePath, null, ResourceLoader.CacheMode.Reuse);
				if (merchantHandSkeleton != null)
				{
					MegaSprite handSprite = new MegaSprite(merchantHandParent);
					handSprite.SetSkeletonDataRes(merchantHandSkeleton);
				}
				else
				{
					GD.PrintErr($"[Merchant2CuteII] Cannot load hand model: {ModConfig.MerchantHandSpinePath}");
				}

				merchantHandParent.Scale = ModConfig.MerchantHandScale;
				GD.Print($"[Merchant2CuteII] Set merchant hand scale: {ModConfig.MerchantHandScale}");
			}
		}
		catch (System.Exception ex)
		{
			GD.PrintErr($"[Merchant2CuteII] Error in NMerchantHand._Ready prefix: {ex.Message}");
		}
	}
}

[HarmonyPatch(typeof(NMerchantHand), "PointAtTarget")]
public static class RealMerchantHandPointAtTargetPatch
{
	[HarmonyPrefix]
	public static void Prefix(NMerchantHand __instance, Control target, ref Vector2 offset)
	{
		if (__instance == null || !MerchantContextResolver.IsRealMerchantHand(__instance))
		{
			return;
		}

		offset += ModConfig.PointAtTargetOffset;
	}
}

[HarmonyPatch(typeof(NMerchantRoom), "FoulPotionThrown")]
public static class MerchantRoomFoulPotionPatch
{
	[HarmonyPostfix]
	public static void Postfix(NMerchantRoom __instance)
	{
		try
		{
			if (__instance == null)
			{
				return;
			}

			Node merchantVisual = __instance.GetNodeOrNull("%MerchantVisual");
			if (merchantVisual == null)
			{
				GD.PrintErr("[Merchant2CuteII] Cannot find %MerchantVisual in MerchantRoom");
				return;
			}

			MegaSprite megaSprite = new MegaSprite(merchantVisual);
			if (!megaSprite.HasAnimation("poison"))
			{
				GD.PrintErr("[Merchant2CuteII] Poison animation does not exist on merchant skeleton");
				return;
			}

			megaSprite.GetAnimationState().SetAnimation("poison");
			GD.Print("[Merchant2CuteII] Set merchant animation to poison");
		}
		catch (System.Exception ex)
		{
			GD.PrintErr($"[Merchant2CuteII] Error adjusting FoulPotionThrown: {ex.Message}");
		}
	}
}