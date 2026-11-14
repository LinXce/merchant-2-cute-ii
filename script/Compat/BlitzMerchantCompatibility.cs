using System;
using Godot;

namespace Merchant2CuteII.script.Compat;

/// <summary>
/// v0.113.0 的极速商人使用独立的身体骨架，但仍继承普通商人节点类型。
/// 通过运行时类型名识别，避免在 v0.111.0 中硬引用不存在的 Blitz 类型。
/// </summary>
public static class BlitzMerchantCompatibility
{
    public const string NativeBodySkeletonPath =
        "res://animations/backgrounds/blitz_merchant_room/top/blitz_merchant_top_skel_data.tres";

    // 使用独立路径：未打包、损坏或契约不匹配时，仍可保留游戏原生资源。
    public const string CustomBodySkeletonPath =
        "res://animations/customs/merchant2cute_ii/blitz_merchant/top/blitz_merchant_top_skel_data.tres";

    private static Resource? _customBody;

    public static bool IsRoom(Node? node) => HasType(node, "NBlitzMerchantRoom");

    public static bool IsButton(Node? node) => HasType(node, "NBlitzMerchantButton");

    public static bool IsVfx(Node? node) => HasType(node, "NBlitzMerchantVfx");

    private static bool HasType(Node? node, string name)
    {
        // 其它 mod 可能派生这些节点，仍然需要保留极速骨架契约。
        for (Type? type = node?.GetType(); type != null; type = type.BaseType)
            if (type.Name == name) return true;
        return false;
    }

    /// <summary>
    /// 在实例入树前检查专用骨架。绝不把普通商人的骨架作为极速模式回退。
    /// 仅在已识别的极速场景调用，因此 v0.111 不会加载新资源。
    /// </summary>
    public static Resource? LoadCustomBody()
    {
        if (_customBody != null && GodotObject.IsInstanceValid(_customBody))
        {
            return _customBody;
        }

        if (!ResourceLoader.Exists(CustomBodySkeletonPath))
        {
            return null;
        }

        try
        {
            Resource? resource = ResourceLoader.Load<Resource>(CustomBodySkeletonPath);
            if (resource == null || !resource.HasMethod("is_skeleton_data_loaded")
                || !resource.Call("is_skeleton_data_loaded").AsBool())
            {
                GD.PrintErr("[Merchant2CuteII] blitz custom body could not be loaded; native body retained");
                return null;
            }

            foreach (string animation in new[] { "animation", "done", "done_loop" })
            {
                if (!HasEntry(resource, "find_animation", animation)) return null;
            }
            foreach (string skin in new[] { "default", "outline" })
            {
                if (!HasEntry(resource, "find_skin", skin)) return null;
            }
            for (int index = 1; index <= 6; index++)
            {
                if (!HasEntry(resource, "find_slot", $"TOKEN_{index}")) return null;
            }

            _customBody = resource;
            return resource;
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[Merchant2CuteII] blitz custom body rejected; native body retained: {ex.Message}");
            return null;
        }
    }

    private static bool HasEntry(Resource resource, string method, string name)
    {
        if (resource.HasMethod(method) && resource.Call(method, name).AsGodotObject() != null)
        {
            return true;
        }

        GD.PrintErr($"[Merchant2CuteII] blitz custom body missing {method}('{name}'); native body retained");
        return false;
    }
}
