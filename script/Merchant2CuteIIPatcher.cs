using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Nodes.Events.Custom;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Screens.Shops;
using MegaCrit.Sts2.Core.Random;

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
}

[HarmonyPatch(typeof(NMerchantButton), "_Ready")]
public static class MerchantButtonPatch
{
	[HarmonyPrefix]
	public static bool Prefix(NMerchantButton __instance)
	{
		if (__instance == null)
			return true;

		Node merchantVisual = __instance.GetNodeOrNull("%MerchantVisual");
		if (merchantVisual == null)
		{
			GD.PrintErr("[Merchant2CuteII] Cannot find %MerchantVisual in MerchantButton");
			return true;
		}

		if (merchantVisual is Node2D mv2)
		{
			mv2.Position += GetMerchantVisualPositionOffset(__instance);
			mv2.Scale = GetMerchantVisualScale(__instance);
		}

		return true;
	}

	private static Vector2 GetMerchantVisualPositionOffset(NMerchantButton button)
	{
		return MerchantContextResolver.IsFakeMerchantButton(button)
			? ModConfig.FakeMerchant.VisualPositionOffset
			: ModConfig.Merchant.VisualPositionOffset;
	}

	private static Vector2 GetMerchantVisualScale(NMerchantButton button)
	{
		return MerchantContextResolver.IsFakeMerchantButton(button)
			? ModConfig.FakeMerchant.VisualScale
			: ModConfig.Merchant.VisualScale;
	}
}

// !已弃用
// [HarmonyPatch(typeof(NMerchantHand), "PointAtTarget")]
// public static class MerchantHandPointAtTargetPatch
// {
// 	[HarmonyPrefix]
// 	public static void Prefix(NMerchantHand __instance, Control target, ref Vector2 offset)
// 	{
// 		if (__instance == null || (!MerchantContextResolver.IsRealMerchantHand(__instance) && !MerchantContextResolver.IsFakeMerchantHand(__instance)))
// 		{
// 			return;
// 		}

// 		offset += GetPointAtTargetOffset(__instance);
// 	}

// 	private static Vector2 GetPointAtTargetOffset(NMerchantHand hand)
// 	{
// 		return MerchantContextResolver.IsFakeMerchantHand(hand)
// 		? ModConfig.FakeMerchant.PointAtTargetOffset
// 		: ModConfig.Merchant.PointAtTargetOffset;
// 	}
// }

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

[HarmonyPatch(typeof(NMerchantCharacter), "PlayAnimation")]
public static class MerchantCharacterPlayAnimationPatch
{
	[HarmonyPrefix]
	public static bool Prefix(NMerchantCharacter __instance, string anim, bool loop)
	{
		if (__instance == null)
		{
			return false;
		}

		try
		{
			Node? spineNode = TryGetMerchantSpineNode(__instance);
			if (spineNode == null)
			{
				GD.PrintErr("[Merchant2CuteII] Cannot find a compatible merchant spine node.");
				return false;
			}

			MegaSprite megaSprite = new MegaSprite(spineNode);
			MegaTrackEntry? megaTrackEntry = megaSprite.GetAnimationState().SetAnimation(anim, loop);
			if (loop && megaTrackEntry != null)
			{
				megaTrackEntry.SetTrackTime(megaTrackEntry.GetAnimationEnd() * Rng.Chaotic.NextFloat());
			}

			return false;
		}
		catch (System.Exception ex)
		{
			GD.PrintErr($"[Merchant2CuteII] Safe merchant animation failed: {ex.Message}");
			return false;
		}
	}

	private static Node? TryGetMerchantSpineNode(NMerchantCharacter merchantCharacter)
	{
		foreach (Node child in merchantCharacter.GetChildren())
		{
			try
			{
				_ = new MegaSprite(child);
				return child;
			}
			catch
			{
			}
		}

		if (merchantCharacter.GetChildCount() > 0)
		{
			return merchantCharacter.GetChild(0);
		}

		return null;
	}
}

[HarmonyPatch(typeof(NMerchantHand), "_Ready")]
public static class MerchantHandReadyPatch
{
	[HarmonyPostfix]
	public static void Postfix(NMerchantHand __instance)
	{
		if (__instance == null)
		{
			return;
		}

		try
		{
			AnimationHelper.TryApplyVariantToHandNode(__instance);
		}
		catch (System.Exception ex)
		{
			GD.PrintErr($"[Merchant2CuteII] Error applying merchant hand variant on ready: {ex.Message}");
		}
	}
}

[HarmonyPatch(typeof(NMerchantInventory), "_Ready")]
public static class MerchantInventoryLegPatch
{
	[HarmonyPrefix]
	public static void Prefix(NMerchantInventory __instance)
	{
		if (__instance == null)
		{
			return;
		}

		if (__instance is NFakeMerchantInventory)
		{
			return;
		}

		Control? inventoryRoot = __instance;
		Control? slotsContainer = __instance.GetNodeOrNull<Control>("%SlotsContainer");
		if (slotsContainer == null)
		{
			GD.PrintErr("[Merchant2CuteII] Cannot find %SlotsContainer in MerchantInventory");
			return;
		}

		if (slotsContainer.GetNodeOrNull<TextureRect>("MerchantInventoryLeg") != null)
		{
			return;
		}

		Texture2D? texture = GD.Load<Texture2D>(ModConfig.Paths.MerchantLegTexture);
		if (texture == null)
		{
			GD.PrintErr($"[Merchant2CuteII] Cannot load merchant leg texture: {ModConfig.Paths.MerchantLegTexture}");
			return;
		}

		Vector2 legPosition = new Vector2(
		slotsContainer.Position.X + ModConfig.Merchant.LegPosition.X,
		slotsContainer.Position.Y + ModConfig.Merchant.LegPosition.Y
		);

		TextureRect decoration = new TextureRect
		{
			Name = "MerchantInventoryLeg",
			Texture = texture,
			Position = legPosition,
			RotationDegrees = ModConfig.Merchant.LegRotationDegrees,
			Scale = ModConfig.Merchant.LegScale,
			MouseFilter = Control.MouseFilterEnum.Ignore,
			Visible = !ModConfig.Options.UseFoot,
			ZIndex = 0,
			ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
			StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
			CustomMinimumSize = texture.GetSize()
		};
		inventoryRoot.AddChild(decoration);
		inventoryRoot.MoveChild(decoration, slotsContainer.GetIndex() + 1);
	}
}

[HarmonyPatch(typeof(NMerchantInventory), "Open")]
public static class MerchantInventoryLegOpenPatch
{
	[HarmonyPostfix]
	public static void Postfix(NMerchantInventory __instance)
	{
		MerchantInventoryLegMotion.TweenLegY(__instance, 0.7f, Tween.TransitionType.Quint, Tween.EaseType.Out, 80f);
	}
}

[HarmonyPatch(typeof(NMerchantInventory), "Close")]
public static class MerchantInventoryLegClosePatch
{
	[HarmonyPostfix]
	public static void Postfix(NMerchantInventory __instance)
	{
		MerchantInventoryLegMotion.TweenLegY(__instance, 0.5f, Tween.TransitionType.Cubic, Tween.EaseType.Out, -1000f);
	}
}

internal static class MerchantInventoryLegMotion
{
	public static void TweenLegY(NMerchantInventory inventory, double duration, Tween.TransitionType transition, Tween.EaseType ease, float slotsTargetY)
	{
		if (inventory == null)
		{
			return;
		}

		Control? slotsContainer = inventory.GetNodeOrNull<Control>("%SlotsContainer");
		TextureRect? leg = inventory.GetNodeOrNull<TextureRect>("MerchantInventoryLeg");
		if (slotsContainer == null || leg == null)
		{
			return;
		}

		Vector2 targetPosition = new Vector2(
		slotsContainer.Position.X + ModConfig.Merchant.LegPosition.X,
		slotsTargetY + ModConfig.Merchant.LegPosition.Y
		);

		Tween tween = inventory.CreateTween();
		tween.TweenProperty(leg, "position", targetPosition, duration)
		.SetEase(ease)
		.SetTrans(transition)
		.FromCurrent();
	}
}