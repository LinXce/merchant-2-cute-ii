using System;
using System.Collections.Generic;
using Godot;
using MegaCrit.Sts2.Core.Nodes.Events.Custom;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Screens.Shops;
using Merchant2CuteII.script.Compat;

namespace Merchant2CuteII.script.Settings;

/// <summary>
/// 在**场景进入节点树之前**把商人骨架换成玩家选的那套。
///
/// 为什么不在运行期换（血泪教训）：
/// <list type="number">
/// <item><c>MegaSprite.SetSkeletonDataRes</c> 内部会 <c>add_child</c>；
/// 在 <c>_Ready</c>/场景装配期间同步调用会报
/// <c>Parent node is busy setting up children</c>，spine 内部状态残缺 → 进商店闪退。</item>
/// <item>就算延后，游戏节点会缓存 skeleton 派生对象
/// （<c>NMerchantHand._bone</c>、<c>NMerchantButton._merchantSkeleton</c>），
/// 换完这些指针指向旧骨架，之后用它们（FindBone / SetSkinByName）同样会崩。</item>
/// </list>
///
/// 正确时机：<c>NSceneContainer.SetCurrentScene</c> 的 Prefix —— 此时场景刚 Instantiate、
/// 尚未进树，spine 还没初始化，直接改节点的 <c>skeleton_data_res</c> 属性最安全；
/// 游戏自己的 <c>RunWhenSpineReady</c> 回调会基于新骨架重新取 bone / skin。
/// </summary>
public static class MerchantSceneSkinner
{
    private enum SceneKind { Real, Fake, Blitz }

    /// <summary>骨架路径 → 已加载的 Godot 资源（保持引用，避免被 GC）。</summary>
    private static readonly Dictionary<string, Resource> ResourceCache = new();

    public static void WarmUp()
    {
        foreach (string path in SkeletonSetAssets.AllTresPaths())
        {
            Load(path);
        }
    }

    /// <summary>在场景进树前应用设置。返回被替换的节点数。</summary>
    public static int ApplyBeforeEnteringTree(Node? root)
    {
        if (root == null || !GodotObject.IsInstanceValid(root))
        {
            return 0;
        }

        SceneKind? sceneKind = DetectKind(root);
        if (sceneKind == null)
        {
            return 0;
        }

        // 极速商人沿用真商人的手部设置；其身体有独立的 Spine 动画与代币槽。
        MerchantKind kind = sceneKind == SceneKind.Fake ? MerchantKind.Fake : MerchantKind.Real;
        MerchantSkinSettings skin = SettingsHost.Current.For(kind);
        int changed = 0;

        string handTarget = SkeletonSetAssets.HandTresPath(kind, skin.HandSet);
        changed += ApplyToHandNodes(root, kind, handTarget);

        if (sceneKind == SceneKind.Blitz)
        {
            // v0.113 路径：只有真商人 own 美术有极速专用骨架。
            // other 仍保留原生身体，不把假商人的整骨架放进极速按钮。
            int bodyChanged = SkeletonSetAssets.IsFakeBodyArt(kind, skin.BodySet)
                ? 0 : ApplyToBlitzBodyNodes(root);
            string bodyStatus = bodyChanged > 0 ? "custom body applied" : "existing body retained";
            GD.Print($"[Merchant2CuteII] blitz merchant: {bodyStatus} ({bodyChanged} body node(s)); "
                     + $"{SkeletonSetAssets.DescribePart(kind, "hand", skin.HandSet)} ({changed} hand node(s) changed)");
            changed += bodyChanged;
            return changed;
        }

        string bodyTarget = SkeletonSetAssets.BodyTresPath(kind, skin.BodySet);
        changed += ApplyToBodyNodes(root, kind, bodyTarget);

        if (changed > 0)
        {
            GD.Print($"[Merchant2CuteII] scene skin applied: {SkeletonSetAssets.DescribePart(kind, "hand", skin.HandSet)}, "
                     + $"{SkeletonSetAssets.DescribePart(kind, "body", skin.BodySet)}  ({changed} node(s))");
        }

        return changed;
    }

    // --------------------------------------------------------------- kinds

    private static SceneKind? DetectKind(Node root)
    {
        // 必须先于 NMerchantRoom 判断：NBlitzMerchantRoom 继承它。
        if (BlitzMerchantCompatibility.IsRoom(root)
            || BlitzMerchantCompatibility.IsButton(root)
            || BlitzMerchantCompatibility.IsVfx(root))
        {
            return SceneKind.Blitz;
        }

        if (root is NMerchantRoom)
        {
            return SceneKind.Real;
        }

        if (root is NFakeMerchant)
        {
            return SceneKind.Fake;
        }

        // 场景根可能是外层容器：向下找商人节点
        foreach (Node child in root.GetChildren())
        {
            SceneKind? kind = DetectKind(child);
            if (kind != null)
            {
                return kind;
            }
        }

        return null;
    }

    // ---------------------------------------------------------------- parts

    private static int ApplyToBlitzBodyNodes(Node root)
    {
        Resource? body = BlitzMerchantCompatibility.LoadCustomBody();
        if (body == null)
        {
            return 0;
        }

        int changed = 0;
        foreach (Node node in WalkIncludingRoot(root))
        {
            bool isVfx = BlitzMerchantCompatibility.IsVfx(node);
            if (!isVfx && !BlitzMerchantCompatibility.IsButton(node))
            {
                continue;
            }

            Node? visual = node.GetNodeOrNull("%MerchantVisual")
                ?? node.GetNodeOrNull("MerchantVisual")
                ?? node.FindChild("MerchantVisual", true, false);
            // 独立 VFX 场景把 SpineSprite 放在脚本节点的同一父节点下。
            if (visual == null && isVfx)
            {
                visual = node.GetParent()?.GetNodeOrNull("SpineSprite");
            }

            if (visual != null && TrySetSkeletonData(visual,
                    BlitzMerchantCompatibility.CustomBodySkeletonPath, "blitz body", body))
            {
                changed++;
            }
        }

        return changed;
    }

    private static int ApplyToHandNodes(Node root, MerchantKind kind, string target)
    {
        int changed = 0;
        foreach (Node node in Walk(root))
        {
            if (node is not NMerchantHand hand)
            {
                continue;
            }

            // NMerchantHand 的父节点就是承载 spine 的那层
            Node? host = hand.GetParent();
            if (host != null && TrySetSkeletonData(host, target, "hand"))
            {
                changed++;
            }
        }

        return changed;
    }

    private static int ApplyToBodyNodes(Node root, MerchantKind kind, string target)
    {
        int changed = 0;
        foreach (Node node in Walk(root))
        {
            if (node is not NMerchantButton button)
            {
                continue;
            }

            // 即使未来从外层容器调用本方法，也不能把普通骨架塞给 Blitz 按钮。
            if (BlitzMerchantCompatibility.IsButton(button))
            {
                continue;
            }

            Node? visual = button.GetNodeOrNull("%MerchantVisual")
                ?? button.GetNodeOrNull("MerchantVisual")
                ?? button.FindChild("MerchantVisual", true, false);
            if (visual != null && TrySetSkeletonData(visual, target, "body"))
            {
                changed++;
            }
        }

        return changed;
    }

    /// <summary>设置节点的 skeleton_data_res；与目标一致时跳过。返回是否真的改了。</summary>
    private static bool TrySetSkeletonData(Node spineNode, string targetPath, string part, Resource? validatedTarget = null)
    {
        try
        {
            // 只有在「节点还没进树 / 还没 Ready」时才换：那时 spine 尚未初始化，
            // 改属性不会触发内部重建，也不会留下陈旧指针。已经 Ready 的节点一律跳过（安全优先）。
            if (spineNode.IsInsideTree())
            {
                GD.Print($"[Merchant2CuteII] skip {part} skeleton swap on '{spineNode.Name}': already in tree");
                return false;
            }

            Resource? target = validatedTarget ?? Load(targetPath);
            if (target == null)
            {
                GD.PrintErr($"[Merchant2CuteII] skeleton resource missing: {targetPath}");
                return false;
            }

            Variant current = spineNode.Get("skeleton_data_res");
            GodotObject? currentObj = current.AsGodotObject();
            if (currentObj != null && currentObj.GetInstanceId() == target.GetInstanceId())
            {
                return false;
            }

            // 场景尚未进树，此时设置是最安全的时机
            spineNode.Set("skeleton_data_res", target);

            Variant after = spineNode.Get("skeleton_data_res");
            if (after.AsGodotObject() == null || after.AsGodotObject()!.GetInstanceId() != target.GetInstanceId())
            {
                GD.PrintErr($"[Merchant2CuteII] failed to set skeleton_data_res on {part} node '{spineNode.Name}'");
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[Merchant2CuteII] setting {part} skeleton failed on '{spineNode.Name}': {ex.Message}");
            return false;
        }
    }

    // --------------------------------------------------------------- utils

    private static Resource? Load(string path)
    {
        if (ResourceCache.TryGetValue(path, out Resource? cached))
        {
            return cached;
        }

        var resource = ResourceLoader.Load<Resource>(path, null, ResourceLoader.CacheMode.Reuse);
        if (resource != null)
        {
            ResourceCache[path] = resource;
        }

        return resource;
    }

    private static IEnumerable<Node> WalkIncludingRoot(Node root)
    {
        yield return root;
        foreach (Node node in Walk(root))
        {
            yield return node;
        }
    }

    private static IEnumerable<Node> Walk(Node root)
    {
        foreach (Node child in root.GetChildren())
        {
            if (child == null)
            {
                continue;
            }

            yield return child;

            foreach (Node grandChild in Walk(child))
            {
                yield return grandChild;
            }
        }
    }
}
