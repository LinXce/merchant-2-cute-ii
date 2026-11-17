using Godot;
using MegaCrit.Sts2.Core.Nodes.Screens.Settings;
using Merchant2CuteII.script.Compat;

namespace Merchant2CuteII.script.Settings;

/// <summary>设置是常驻服务，不随皮肤提供者的 Harmony 补丁停用。</summary>
public static class MerchantSettingsAvailability
{
    private static SceneTree? _tree;

    public static void EnsureAvailable()
    {
        if (!VersionProfile.SkinChangerCooperation || !SkinChangerCompatibility.IsManaged
            || Engine.GetMainLoop() is not SceneTree tree) return;
        if (ReferenceEquals(tree, _tree)) return;
        if (_tree != null && GodotObject.IsInstanceValid(_tree)) _tree.NodeAdded -= OnNodeAdded;
        _tree = tree;
        tree.NodeAdded += OnNodeAdded;
        VisitExisting(tree.Root);
    }

    private static void VisitExisting(Node node)
    {
        OnNodeAdded(node);
        foreach (Node child in node.GetChildren()) VisitExisting(child);
    }

    private static void OnNodeAdded(Node node)
    {
        if (node is not NSettingsScreen screen) return;
        Callable.From(() =>
        {
            if (GodotObject.IsInstanceValid(screen) && !screen.IsQueuedForDeletion() && screen.IsInsideTree())
                MerchantSettingsEntry.Inject(screen);
        }).CallDeferred();
    }
}
