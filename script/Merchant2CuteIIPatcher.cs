using System.Collections.Generic;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Audio.Debug;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Nodes.Audio;
using MegaCrit.Sts2.Core.Nodes.Events.Custom;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Nodes.Screens.Shops;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.Nodes.Vfx.Utilities;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Debug;
using MegaCrit.Sts2.Core.Helpers;
using Merchant2CuteII.script.Settings;

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

internal static class MerchantVisualResolver
{
    // 阈值 MerchantVisualOwnerChangeVersion = 0.110.0 已集中到 Compat/VersionProfile.cs

    /// <summary>MerchantVisual 宿主变更（0.110.0）判断，统一由 <see cref="Compat.VersionProfile"/> 提供。</summary>
    public static bool ShouldUseLegacyMerchantVisualLookup()
    {
        return Compat.VersionProfile.LegacyMerchantVisualLookup;
    }

    public static Node? TryGetMerchantVisual(Node? node)
    {
        if (node == null)
        {
            return null;
        }

        // Godot 0.110.0 scene ownership changes can make `%MerchantVisual` inaccessible
        // from parent scenes, so fall back to direct child and recursive lookup.
        return node.GetNodeOrNull("%MerchantVisual")
            ?? node.GetNodeOrNull("MerchantVisual")
            ?? node.FindChild("MerchantVisual", true, false);
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

        Node? merchantVisual = MerchantVisualResolver.ShouldUseLegacyMerchantVisualLookup()
            ? __instance.GetNodeOrNull("%MerchantVisual")
            : MerchantVisualResolver.TryGetMerchantVisual(__instance);
        if (merchantVisual == null)
        {
            GD.PrintErr("[Merchant2CuteII] Cannot find MerchantVisual in MerchantButton");
            return true;
        }

        if (merchantVisual is Node2D mv2)
        {
            // 位置 = 节点基准位 + 组合偏移；缩放按「实际使用的美术」。
            // 四种「商人 × 美术」组合各有独立数值，并可在游戏内用 `merchant tune` 实时微调。
            MerchantVisualMotion.Apply(__instance, mv2);
        }

        // 骨架选择（身体/手部用哪套）在场景进树前就已应用，见 MerchantSceneSkinPatch
        return true;
    }

    // 位置/缩放逻辑已集中到 MerchantVisualMotion（按「商人 × 美术」四种组合分别可调，
    // 支持游戏内 `merchant tune` 实时微调）。
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
    /// <summary>
    /// 只负责**切换动画**，原版逻辑一律放行。
    ///
    /// 原版 <c>NMerchantRoom.FoulPotionThrown</c> 只做三件事：
    /// 播放 <c>merchant_thank_yous</c> 音效 → 随机一句台词气泡 → 屏幕震动；
    /// 「给 100 金币」「假商人转到战斗状态」都在事件/卡牌流程里，不在这里。
    ///
    /// 所以这里用 **void Prefix**（不拦截）：
    /// <list type="bullet">
    /// <item>台词、音效、震动照旧由原版执行，绝不重复触发；</item>
    /// <item>金币结算、战斗状态切换完全不受影响；</item>
    /// <item>音效替换由 <see cref="MerchantVoiceSfxPatch"/> 在 SfxCmd 层处理，不需要在这里再放一次。</item>
    /// </list>
    /// </summary>
    [HarmonyPrefix]
    public static void Prefix(NMerchantRoom __instance)
    {
        try
        {
            if (__instance == null)
            {
                return;
            }

            Node? merchantVisual = MerchantVisualResolver.ShouldUseLegacyMerchantVisualLookup()
                ? __instance.GetNodeOrNull("%MerchantVisual")
                : MerchantVisualResolver.TryGetMerchantVisual(__instance.MerchantButton)
                    ?? MerchantVisualResolver.TryGetMerchantVisual(__instance);
            if (merchantVisual == null)
            {
                GD.PrintErr("[Merchant2CuteII] Cannot find MerchantVisual in MerchantRoom or MerchantButton");
                return;
            }

            if (!AnimationHelper.TrySetFirstAvailableAnimation(merchantVisual, AnimationLogic.FoulPotionCandidates()))
            {
                GD.PrintErr($"[Merchant2CuteII] Merchant animation '{AnimationLogic.FoulPotionAnimation}' does not exist or is not ready");
            }
        }
        catch (System.Exception ex)
        {
            GD.PrintErr($"[Merchant2CuteII] Error switching foul-potion animation: {ex.Message}");
        }
    }

    /// <summary>投毒动画的候选链由 <see cref="AnimationLogic"/> 统一决定（不暴露给玩家）。</summary>
    private static string[] FoulAnimationCandidates()
    {
        return AnimationLogic.FoulPotionCandidates();
    }
}


[HarmonyPatch(typeof(SfxCmd), nameof(SfxCmd.Play), new[] { typeof(string), typeof(float) })]
public static class MerchantVoiceSfxPatch
{
    private static readonly System.Collections.Generic.HashSet<string> LoggedUnmapped = new();
    private static readonly System.Collections.Generic.HashSet<string> LoggedMapped = new();

    [HarmonyPrefix]
    public static bool Prefix(string __0, float __1)
    {
        return !MerchantVoiceRouter.TryReplace(__0, __1, "SfxCmd");
    }

    public static bool TryPlayVoice(string voicePath, float volume, float extraDb = 0f)
    {
        float adjustedVolume = Mathf.Max(0f, volume * Mathf.DbToLinear(extraDb));

        if (NDebugAudioManager.Instance != null)
        {
            NDebugAudioManager.Instance.Play(voicePath, adjustedVolume);
            return true;
        }

        AudioStream? stream = ResourceLoader.Load<AudioStream>($"res://debug_audio/{voicePath}");
        if (stream == null)
        {
            GD.PrintErr($"[Merchant2CuteII] Merchant voice stream not found: res://debug_audio/{voicePath}");
            return false;
        }

        SceneTree? tree = Engine.GetMainLoop() as SceneTree;
        if (tree?.Root == null)
        {
            GD.PrintErr("[Merchant2CuteII] Merchant voice fallback failed: SceneTree root is null");
            return false;
        }

        AudioStreamPlayer player = new AudioStreamPlayer
        {
            Stream = stream,
            VolumeLinear = adjustedVolume,
            Bus = "SFX",
            Name = "MerchantVoiceFallbackPlayer"
        };

        tree.Root.AddChild(player);
        player.Finished += player.QueueFree;
        player.Play();
        return true;
    }
}

/// <summary>
/// 真正的音频入口：<c>NAudioManager.PlayOneShot(string, float)</c>。
///
/// 为什么需要它：<c>SfxCmd.Play</c> 只有两行，很容易被 JIT **内联**到调用方，
/// 内联之后 Harmony 对 <c>SfxCmd.Play</c> 的补丁对该调用点就失效了
/// （表现为"有时替换成功、有时还是原版声音"）。而 <c>NAudioManager.PlayOneShot</c>
/// 是实例方法且有一定体积，不会被内联，所有音频最终都会经过它。
/// </summary>
[HarmonyPatch(typeof(NAudioManager), nameof(NAudioManager.PlayOneShot), new[] { typeof(string), typeof(float) })]
public static class MerchantVoiceAudioPatch
{
    [HarmonyPrefix]
    public static bool Prefix(string __0, float __1)
    {
        return !MerchantVoiceRouter.TryReplace(__0, __1, "NAudioManager");
    }
}

/// <summary>带 FMOD 参数的重载同样要拦（<c>SfxCmd.Play(sfx, param, val, volume)</c> 走这条）。</summary>
[HarmonyPatch(typeof(NAudioManager), nameof(NAudioManager.PlayOneShot),
    new[] { typeof(string), typeof(System.Collections.Generic.Dictionary<string, float>), typeof(float) })]
public static class MerchantVoiceAudioParamsPatch
{
    [HarmonyPrefix]
    // 该重载是 (string path, Dictionary<string,float> parameters, float volume)
    // 只取第 0 个和第 2 个实参，中间的参数字典不需要。
    public static bool Prefix(string __0, float __2)
    {
        return !MerchantVoiceRouter.TryReplace(__0, __2, "NAudioManager(param)");
    }
}

/// <summary>
/// 语音替换的统一路由：两个入口（SfxCmd / NAudioManager）共用同一套映射与日志，
/// 避免逻辑分叉，也避免同一事件被记录两次。
/// </summary>
internal static class MerchantVoiceRouter
{
    private static readonly System.Collections.Generic.HashSet<string> LoggedMapped = new();
    private static readonly System.Collections.Generic.HashSet<string> LoggedUnmapped = new();

    /// <summary>返回 true 表示"已用我们的音频替代，调用方应拦下原版播放"。</summary>
    public static bool TryReplace(string sfx, float volume, string source)
    {
        // 防御：不处理我们自己的资源路径，避免自我递归
        if (string.IsNullOrEmpty(sfx) || sfx.StartsWith("res://", System.StringComparison.Ordinal))
        {
            return false;
        }

        string? voicePath = ModConfig.Voice.GetMerchantVoicePath(sfx);
        if (string.IsNullOrEmpty(voicePath))
        {
            if (ModConfig.Options.MerchantVoiceVariant != "default"
                && sfx.Contains("merchant", System.StringComparison.OrdinalIgnoreCase)
                && LoggedUnmapped.Add(sfx))
            {
                GD.Print($"[Merchant2CuteII] voice: unmapped merchant sfx '{sfx}' (kept vanilla sound)");
            }

            return false;
        }

        try
        {
            if (MerchantVoiceSfxPatch.TryPlayVoice(voicePath, volume, ModConfig.Options.ExtraDb))
            {
                if (LoggedMapped.Add(sfx))
                {
                    GD.Print($"[Merchant2CuteII] voice[{source}]: '{sfx}' -> {voicePath}");
                }

                return true;
            }
        }
        catch (System.Exception ex)
        {
            GD.PrintErr($"[Merchant2CuteII] Merchant voice replacement failed: {ex.Message}");
        }

        return false;
    }
}

[HarmonyPatch(typeof(NMerchantCharacter), "PlayAnimation")]
public static class MerchantCharacterPlayAnimationPatch
{
    /// <summary>
    /// **最小介入**：动画存在时完全交给原版（原版自己会设置动画并随机起始时间）；
    /// 只有换了美术之后动画名不存在时，才由我们挑一个降级动画播放并拦下原版。
    /// </summary>
    [HarmonyPrefix]
    public static bool Prefix(NMerchantCharacter __instance, string __0, bool __1)
    {
        string anim = __0;
        bool loop = __1;

        if (__instance == null)
        {
            return true;
        }

        try
        {
            Node? spineNode = TryGetMerchantSpineNode(__instance);
            if (spineNode == null)
            {
                return true; // 找不到 spine 就让原版自己试着播
            }

            MegaSprite megaSprite = new MegaSprite(spineNode);

            // 1) 正常情况：动画存在 → 原版执行，不做任何干预
            if (AnimationHelper.HasAnimation(megaSprite, anim))
            {
                return true;
            }

            // 2) 换过美术导致动画名不存在（例：借来的骨架没有 poison）→ 降级播放
            foreach (string candidate in FallbackAnimationCandidates(anim))
            {
                if (!AnimationHelper.HasAnimation(megaSprite, candidate))
                {
                    continue;
                }

                MegaAnimationState animationState = megaSprite.GetAnimationState();
                if (AnimationHelper.ShouldUseLegacyHandSwitching())
                {
                    animationState.BoundObject.Call("set_animation", candidate, loop);
                }
                else
                {
                    animationState.BoundObject.Call("set_animation", candidate, loop, 0);
                }

                if (loop)
                {
                    MegaTrackEntry? trackEntry = animationState.GetCurrent(0);
                    trackEntry?.SetTrackTime(trackEntry.GetAnimationEnd() * Rng.Chaotic.NextFloat());
                }

                GD.Print($"[Merchant2CuteII] Animation '{anim}' missing on current skeleton -> fallback '{candidate}'");
                return false;
            }

            GD.Print($"[Merchant2CuteII] Animation '{anim}' not present on current skeleton, skipped.");
            return false;
        }
        catch (System.Exception ex)
        {
            GD.PrintErr($"[Merchant2CuteII] Safe merchant animation failed: {ex.Message}");
            return true; // 出异常也不拦原版
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

    /// <summary>换骨架后动画名可能不存在：降级链由 <see cref="AnimationLogic"/> 统一决定。</summary>
    private static string[] FallbackAnimationCandidates(string requested)
    {
        return AnimationLogic.FallbackFor(requested);
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
            Visible = MerchantRuntime.ShouldShowInventoryLeg(MerchantKind.Real),
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