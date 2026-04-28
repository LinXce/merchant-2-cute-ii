using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Helpers;
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
			return ModConfig.MerchantBodySpinePath;
		}

		if (MerchantContextResolver.IsFakeMerchantButton(button))
		{
			return ModConfig.FakeMerchantBodySpinePath;
		}

		return null;
	}

	private static Vector2 GetMerchantVisualPositionOffset(NMerchantButton button)
	{
		return MerchantContextResolver.IsFakeMerchantButton(button)
			? ModConfig.FakeMerchantVisualPositionOffset
			: ModConfig.RealMerchantVisualPositionOffset;
	}

	private static Vector2 GetMerchantVisualScale(NMerchantButton button)
	{
		return MerchantContextResolver.IsFakeMerchantButton(button)
			? ModConfig.FakeMerchantVisualScale
			: ModConfig.RealMerchantVisualScale;
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
			return ModConfig.MerchantHandSpinePath;
		}

		if (MerchantContextResolver.IsFakeMerchantHand(hand))
		{
			return ModConfig.FakeMerchantHandSpinePath;
		}

		return null;
	}

	private static Vector2 GetMerchantHandScale(NMerchantHand hand)
	{
		return MerchantContextResolver.IsFakeMerchantHand(hand)
			? ModConfig.FakeMerchantHandScale
			: ModConfig.RealMerchantHandScale;
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
		for (int i = 0; i < 4; i++)
		{
			await merchantHandParent.ToSignal(merchantHandParent.GetTree(), SceneTree.SignalName.ProcessFrame);
		}

		merchantHandParent.Scale = GetMerchantHandScale(hand);

		// 替换后重绑 NMerchantHand 内部字段，避免 _Process 持有旧骨骼引用并恢复手部正常逻辑
		if (GodotObject.IsInstanceValid(hand))
		{
			TryRebindHandInternals(hand, merchantHandParent);
		}
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

			newAnimController.GetAnimationState().SetAnimation("default");
		}
		catch (System.Exception ex)
		{
			GD.PrintErr($"[Merchant2CuteII] Hand internals rebind failed: {ex.Message}");
		}
	}
}

[HarmonyPatch(typeof(NMerchantHand), "PointAtTarget")]
public static class MerchantHandPointAtTargetPatch
{
	[HarmonyPrefix]
	public static void Prefix(NMerchantHand __instance, Control target, ref Vector2 offset)
	{
		if (__instance == null || (!MerchantContextResolver.IsRealMerchantHand(__instance) && !MerchantContextResolver.IsFakeMerchantHand(__instance)))
		{
			return;
		}

		offset += GetPointAtTargetOffset(__instance);
	}

	private static Vector2 GetPointAtTargetOffset(NMerchantHand hand)
	{
		return MerchantContextResolver.IsFakeMerchantHand(hand)
			? ModConfig.FakePointAtTargetOffset
			: ModConfig.RealPointAtTargetOffset;
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