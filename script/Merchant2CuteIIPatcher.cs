using Godot;
using HarmonyLib;
using System.Runtime.CompilerServices;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Combat;
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

internal static class MerchantSpineLoader
{
	private static readonly Dictionary<string, Resource> _cache = new();

	public static void Preload(string path)
	{
		if (string.IsNullOrEmpty(path))
		{
			return;
		}

		if (_cache.ContainsKey(path))
		{
			return;
		}

		try
		{
			Resource? r = ResourceLoader.Load<Resource>(path, null, ResourceLoader.CacheMode.Reuse);
			if (r != null)
			{
				_cache[path] = r;
			}
		}
		catch (System.Exception ex)
		{
			GD.PrintErr($"[Merchant2CuteII] Preload failed for {path}: {ex.Message}");
		}
	}

	public static MegaSkeletonDataResource? Load(string path)
	{
		Resource? rawResource = null;

		if (!string.IsNullOrEmpty(path) && _cache.TryGetValue(path, out var cached))
		{
			rawResource = cached;
		}
		else
		{
			rawResource = ResourceLoader.Load<Resource>(path, null, ResourceLoader.CacheMode.Reuse);
		}

		if (rawResource == null)
		{
			return null;
		}

		return new MegaSkeletonDataResource(rawResource);
	}

	public static Resource? GetRaw(string path)
	{
		if (string.IsNullOrEmpty(path))
		{
			return null;
		}

		if (_cache.TryGetValue(path, out var cached))
		{
			return cached;
		}

		try
		{
			return ResourceLoader.Load<Resource>(path, null, ResourceLoader.CacheMode.Reuse);
		}
		catch
		{
			return null;
		}
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

		string? spinePath = GetButtonSpinePath(__instance);
		if (spinePath == null)
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

		MegaSkeletonDataResource? skeleton = MerchantSpineLoader.Load(spinePath);
		if (!SkeletonAssigner.TryAssign(merchantVisual, spinePath, skeleton))
		{
			GD.PrintErr($"[Merchant2CuteII] Failed to assign skeleton for {spinePath}");
		}

		return true;
	}

	private static string? GetButtonSpinePath(NMerchantButton button)
	{
		if (MerchantContextResolver.IsRealMerchantButton(button))
		{
			return ModConfig.Paths.MerchantBodySpine;
		}

		if (MerchantContextResolver.IsFakeMerchantButton(button))
		{
			return ModConfig.Paths.FakeMerchantBodySpine;
		}

		return null;
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

[HarmonyPatch(typeof(NMerchantHand), "_Ready")]
public static class MerchantHandPatch
{
	[HarmonyPrefix]
	public static void Prefix(NMerchantHand __instance)
	{
		if (__instance == null)
			return;

		string? spinePath = GetHandSpinePath(__instance);
		if (spinePath == null)
			return;

		if (__instance.GetParent() is Node2D merchantHandParent)
		{
			MegaSkeletonDataResource? merchantHandSkeleton = MerchantSpineLoader.Load(spinePath);
			Resource? raw = MerchantSpineLoader.GetRaw(spinePath);

			if (merchantHandSkeleton == null && raw == null)
			{
				GD.PrintErr($"[Merchant2CuteII] Cannot load hand model: {spinePath}");
			}
			else
			{
				// schedule replacement (we pass path and skeleton; helper will prefer raw)
				TaskHelper.RunSafely(ApplyHandReplacementAsync(__instance, merchantHandParent, spinePath, merchantHandSkeleton));
			}

			merchantHandParent.Scale = GetMerchantHandScale(__instance);
		}
	}

	private static string? GetHandSpinePath(NMerchantHand hand)
	{
		if (MerchantContextResolver.IsRealMerchantHand(hand))
		{
			return ModConfig.Paths.MerchantHandSpine;
		}

		if (MerchantContextResolver.IsFakeMerchantHand(hand))
		{
			return ModConfig.Paths.FakeMerchantHandSpine;
		}

		return null;
	}

	private static Vector2 GetMerchantHandScale(NMerchantHand hand)
	{
		return MerchantContextResolver.IsFakeMerchantHand(hand)
			? ModConfig.FakeMerchant.HandScale
			: ModConfig.Merchant.HandScale;
	}

	private static async System.Threading.Tasks.Task ApplyHandReplacementAsync(NMerchantHand hand, Node2D merchantHandParent, string handPath, MegaSkeletonDataResource? merchantHandSkeleton)
	{
		await merchantHandParent.ToSignal(merchantHandParent.GetTree(), SceneTree.SignalName.ProcessFrame);

		if (!GodotObject.IsInstanceValid(merchantHandParent) || merchantHandParent.GetParent() == null)
		{
			return;
		}
		// 尝试一次性赋值（优先 raw，然后绑定）
		bool ok = SkeletonAssigner.TryAssign(merchantHandParent, handPath, merchantHandSkeleton);
		if (!ok)
		{
			GD.PrintErr($"[Merchant2CuteII] Failed to apply hand skeleton for {handPath}");
			merchantHandParent.Scale = GetMerchantHandScale(hand);
			return;
		}

		// 等待少量帧以便 spine native 侧初始化，避免在随后的帧中产生大量 "Native Spine object not set." 日志
		for (int i = 0; i < 8; i++)
		{
			await merchantHandParent.ToSignal(merchantHandParent.GetTree(), SceneTree.SignalName.ProcessFrame);
		}

		merchantHandParent.Scale = GetMerchantHandScale(hand);

		// 替换后重绑 NMerchantHand 内部字段，避免 _Process 持有旧骨骼引用并恢复手部正常逻辑
		if (GodotObject.IsInstanceValid(hand))
		{
			TryRebindHandInternals(hand, merchantHandParent);
			ApplyVariantAnimation(merchantHandParent);
		}
	}

	internal static void RebindHandInternalsForVariantSwitch(NMerchantHand hand, Node2D merchantHandParent)
	{
		TryRebindHandInternals(hand, merchantHandParent);
	}

	private static void TryRebindHandInternals(NMerchantHand hand, Node2D merchantHandParent)
	{
		try
		{
			MegaSprite newAnimController = new MegaSprite(merchantHandParent);
			MegaBone? newBone = newAnimController.GetSkeleton()?.FindBone("rotate_me");

			var animField = AccessTools.Field(typeof(NMerchantHand), "_animController");
			var boneField = AccessTools.Field(typeof(NMerchantHand), "_bone");

			animField?.SetValue(hand, newAnimController);
			boneField?.SetValue(hand, newBone);

			ApplyVariantAnimation(merchantHandParent);
		}
		catch (System.Exception ex)
		{
			GD.PrintErr($"[Merchant2CuteII] Hand internals rebind failed: {ex.Message}");
		}
	}

	private static void ApplyVariantAnimation(Node2D merchantHandParent)
	{
		try
		{
			MegaSprite ms = new MegaSprite(merchantHandParent);
			string variant = ModConfig.Options.HandVariant;
			string animationName = variant == "hand" ? "default" : variant;
			if (ms.HasAnimation(animationName))
			{
				ms.GetAnimationState().SetAnimation(animationName);
			}
			else if (ms.HasAnimation("default"))
			{
				ms.GetAnimationState().SetAnimation("default");
			}
		}
		catch { }
	}
}

[HarmonyPatch(typeof(NMerchantHand), "PointAtTarget")]
public static class MerchantHandPointAtTargetPatch
{
	[HarmonyPrefix]
	public static void Prefix(NMerchantHand __instance, ref Vector2 pos)
	{
		if (__instance == null || (!MerchantContextResolver.IsRealMerchantHand(__instance) && !MerchantContextResolver.IsFakeMerchantHand(__instance)))
		{
			return;
		}

		pos += GetPointAtTargetOffset(__instance);
	}

	private static Vector2 GetPointAtTargetOffset(NMerchantHand hand)
	{
		return MerchantContextResolver.IsFakeMerchantHand(hand)
			? ModConfig.FakeMerchant.PointAtTargetOffset
			: ModConfig.Merchant.PointAtTargetOffset;
	}
}

[HarmonyPatch(typeof(NMerchantHand), "_Process")]
public static class MerchantHandProcessPatch
{
	private static readonly System.Reflection.FieldInfo? StartPosField = AccessTools.Field(typeof(NMerchantHand), "_startPos");
	private static readonly System.Reflection.FieldInfo? TargetPosField = AccessTools.Field(typeof(NMerchantHand), "_targetPos");
	private static readonly System.Reflection.FieldInfo? BoneField = AccessTools.Field(typeof(NMerchantHand), "_bone");
	private static readonly System.Reflection.FieldInfo? NoiseField = AccessTools.Field(typeof(NMerchantHand), "_noise");
	private static readonly System.Reflection.FieldInfo? TimeField = AccessTools.Field(typeof(NMerchantHand), "_time");
	private static readonly System.Reflection.FieldInfo? RugField = AccessTools.Field(typeof(NMerchantHand), "_rug");
	private static readonly System.Reflection.FieldInfo? ParentField = AccessTools.Field(typeof(NMerchantHand), "_parent");
	private static readonly System.Reflection.FieldInfo? AnimControllerField = AccessTools.Field(typeof(NMerchantHand), "_animController");
	private static readonly ConditionalWeakTable<NMerchantHand, SafeBoneState> BoneStates = new();

	[HarmonyPrefix]
	public static bool Prefix(NMerchantHand __instance, double delta)
	{
		if (__instance == null || (!MerchantContextResolver.IsRealMerchantHand(__instance) && !MerchantContextResolver.IsFakeMerchantHand(__instance)))
		{
			return true;
		}

		try
		{
			RunSafeMerchantHandProcess(__instance, delta);
		}
		catch (System.Exception ex)
		{
			GD.PrintErr($"[Merchant2CuteII] Safe hand process failed: {ex.Message}");
		}

		return false;
	}

	private static void RunSafeMerchantHandProcess(NMerchantHand hand, double delta)
	{
		Node2D? parent = ParentField?.GetValue(hand) as Node2D ?? hand.GetParent() as Node2D;
		Control? rug = RugField?.GetValue(hand) as Control ?? parent?.GetParent() as Control;
		FastNoiseLite? noise = NoiseField?.GetValue(hand) as FastNoiseLite;
		if (parent == null || rug == null || noise == null)
		{
			return;
		}

		float time = TimeField?.GetValue(hand) as float? ?? 0f;
		time += (float)delta;
		TimeField?.SetValue(hand, time);

		Vector2 targetPos = TargetPosField?.GetValue(hand) as Vector2? ?? StartPosField?.GetValue(hand) as Vector2? ?? parent.GlobalPosition;
		float noiseX = noise.GetNoise1D(time * 0.1f) + 0.4f;
		float noiseY = noise.GetNoise1D((time + 0.25f) * 0.1f) - 0.5f;
		parent.GlobalPosition = parent.GlobalPosition.Lerp(targetPos + new Vector2(noiseX, noiseY) * 100f, (float)delta * 4f);

		float desiredRotation = Mathf.Lerp(-10f, 10f, (parent.Position.X - rug.Size.X * 0.5f - 50f) * 0.01f);
		TrySetSafeRotateMeRotation(hand, parent, desiredRotation);
	}

	private static void TrySetSafeRotateMeRotation(NMerchantHand hand, Node2D parent, float desiredRotation)
	{
		SafeBoneState state = BoneStates.GetOrCreateValue(hand);
		if (state.Disabled)
		{
			return;
		}

		if (state.FramesUntilRetry > 0)
		{
			state.FramesUntilRetry--;
			return;
		}

		MegaBone? bone = state.Bone;
		if (bone == null)
		{
			bone = TryFindRotateMeBone(parent);
			state.Bone = bone;
			if (bone == null)
			{
				state.FramesUntilRetry = 12;
				return;
			}
		}

		try
		{
			bone.SetRotation(desiredRotation);
			state.Failures = 0;
		}
		catch
		{
			state.Bone = null;
			state.Failures++;
			state.FramesUntilRetry = 12;
			if (state.Failures >= 3)
			{
				state.Disabled = true;
			}
		}
	}

	private static MegaBone? TryFindRotateMeBone(Node2D parent)
	{
		try
		{
			MegaSprite animController = new MegaSprite(parent);
			return animController.GetSkeleton()?.FindBone("rotate_me");
		}
		catch
		{
			return null;
		}
	}

	private sealed class SafeBoneState
	{
		public MegaBone? Bone;
		public int FramesUntilRetry;
		public int Failures;
		public bool Disabled;
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

[HarmonyPatch(typeof(NCreatureVisuals), "_Ready")]
public static class FakeMerchantMonsterPatch
{
	[HarmonyPrefix]
	public static void Prefix(NCreatureVisuals __instance)
	{
		if (__instance == null)
		{
			return;
		}

		if (__instance.Name.ToString() != "FakeMerchantMonster")
		{
			return;
		}

		Node2D? visuals = __instance.GetNodeOrNull<Node2D>("%Visuals");
		if (visuals == null)
		{
			GD.PrintErr("[Merchant2CuteII] Cannot find %Visuals in FakeMerchantMonster");
			return;
		}

		MegaSkeletonDataResource? skeleton = MerchantSpineLoader.Load(ModConfig.Paths.FakeMerchantBodySpine);
		if (skeleton == null && MerchantSpineLoader.GetRaw(ModConfig.Paths.FakeMerchantBodySpine) == null)
		{
			GD.PrintErr($"[Merchant2CuteII] Cannot load fake merchant battle model: {ModConfig.Paths.FakeMerchantBodySpine}");
			return;
		}

		if (!SkeletonAssigner.TryAssign(visuals, ModConfig.Paths.FakeMerchantBodySpine, skeleton))
		{
			GD.PrintErr("[Merchant2CuteII] Failed to assign fake merchant battle model");
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

		Texture2D? texture = GD.Load<Texture2D>(ModConfig.Paths.MerchantLegTexture)
			?? GD.Load<Texture2D>(ModConfig.Paths.MerchantLegTexture);
		if (texture == null)
		{
			GD.PrintErr($"[Merchant2CuteII] Cannot load merchant leg texture: {ModConfig.Paths.MerchantLegTexture} or {ModConfig.Paths.MerchantLegTexture}");
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
			// ZAsRelative = false,
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

internal static class SkeletonAssigner
{
	// 尝试将 skeleton 资源安全地赋给目标节点：优先 raw，否则使用 MegaSprite 绑定；返回是否成功
	public static bool TryAssign(Node target, string? path, MegaSkeletonDataResource? skeleton)
	{
		if (target == null)
			return false;

		// prefer to operate on the actual Spine node if one exists under the given target
		Node assignTarget = FindSpineNode(target) ?? target;

		Resource? raw = null;
		if (!string.IsNullOrEmpty(path))
		{
			raw = MerchantSpineLoader.GetRaw(path);
		}

		if (raw != null)
		{
			try
			{
				assignTarget.Set("skeleton_data_res", raw);
				return true;
			}
			catch
			{
				// fallthrough to binding
			}
		}

		if (skeleton != null)
		{
			try
			{
				MegaSprite ms = new MegaSprite(assignTarget);
				ms.SetSkeletonDataRes(skeleton);
				return true;
			}
			catch
			{
				// failed to bind
			}
		}

		return false;
	}

	private static Node? FindSpineNode(Node root)
	{
		if (root == null)
			return null;

		// check self
		try
		{
			string cls = root.GetClass().ToString().ToLowerInvariant();
			if (cls.Contains("spine") || root.Name.ToString().ToLowerInvariant().Contains("spine") || root.HasMethod("set_skeleton_data_res"))
				return root;
		}
		catch { }

		// BFS up to depth 3
		var q = new Queue<Node>();
		q.Enqueue(root);
		int depth = 0;
		while (q.Count > 0 && depth < 3)
		{
			int n = q.Count;
			for (int i = 0; i < n; i++)
			{
				var node = q.Dequeue();
				foreach (Node child in node.GetChildren())
				{
					try
					{
						string cls = child.GetClass().ToString().ToLowerInvariant();
						if (cls.Contains("spine") || child.Name.ToString().ToLowerInvariant().Contains("spine") || child.HasMethod("set_skeleton_data_res"))
							return child;
					}
					catch { }

					q.Enqueue(child);
				}
			}
			depth++;
		}

		return null;
	}
}
