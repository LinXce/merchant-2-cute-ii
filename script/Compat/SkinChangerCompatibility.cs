using System;
using System.Collections;
using System.Reflection;
using Godot;
using Merchant2CuteII.script.Settings;

namespace Merchant2CuteII.script.Compat;

/// <summary>可选协作接口，无外部 DLL 引用；选项和资源归属仍由管理器决定。</summary>
public static class SkinChangerCompatibility
{
    private const BindingFlags Static = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
    private static Type? _loader, _service, _appearance, _settingsApi;
    private static bool _reported;

    private static void Discover()
    {
        if (!VersionProfile.SkinChangerCooperation || _loader != null) return;
        foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            var loader = assembly.GetType("STS2SkinChanger.Core.ManagedSkinModLoader");
            if (loader == null) continue;
            _loader = loader;
            _service = assembly.GetType("STS2SkinChanger.Core.SkinService");
            _appearance = assembly.GetType("STS2SkinChanger.Ui.MerchantRuntimeAppearance");
            _settingsApi = assembly.GetType("STS2SkinChanger.ProviderSettingsApi");
            break;
        }
    }

    public static bool IsManaged
    {
        get
        {
            if (!VersionProfile.SkinChangerCooperation) return false;
            Discover();
            if (_loader?.GetMethod("IsManagedProviderAssembly", Static, new[] { typeof(Assembly) }) is { } query)
                return query.Invoke(null, new object[] { typeof(SkinChangerCompatibility).Assembly }) is true;
            // 注册表保留已切走的提供者，避免延迟回调在停用后回退到全局刷新。
            return _loader?.GetField("ProviderIdsByAssembly", Static)?.GetValue(null) is IDictionary providers
                && providers.Contains(typeof(SkinChangerCompatibility).Assembly);
        }
    }

    private static string Group(Node? node, MerchantKind kind) =>
        (node == null ? null : _appearance?.GetMethod("ResolveMerchantGroupId", Static,
            new[] { typeof(Node) })?.Invoke(null, new object[] { node }) as string)
        ?? (kind == MerchantKind.Fake ? "fake_merchant_monster" : "merchant");

    public static bool MayApply(Node? node, MerchantKind kind)
    {
        if (!IsManaged) return true;
        try
        {
            string group = Group(node, kind);
            string? selected = _service?.GetMethod("GetSelectedRuntimeProvider", Static,
                new[] { typeof(string) })?.Invoke(null, new object[] { group }) as string;
            selected ??= _service?.GetMethod("GetSelectedFullRuntimeProvider", Static,
                new[] { typeof(string) })?.Invoke(null, new object[] { group }) as string;
            if (selected != null && _loader?.GetMethod("ResolveBehaviorProviderId", Static,
                    new[] { typeof(string) }) is { } alias)
                selected = alias.Invoke(null, new object[] { selected }) as string;
            return selected != null && _loader?.GetMethod("IsProviderAssemblyFor", Static,
                new[] { typeof(string), typeof(Assembly) })?.Invoke(null,
                new object[] { selected, typeof(SkinChangerCompatibility).Assembly }) is true;
        }
        catch (Exception ex) { Report(ex); return false; }
    }

    public static bool TryRefresh(out int count)
    {
        count = 0;
        if (!IsManaged) return false;
        try
        {
            count = (int?)_settingsApi?.GetMethod("Refresh", Static, new[] { typeof(Type) })
                ?.Invoke(null, new object[] { typeof(SkinChangerCompatibility) }) ?? 0;
        }
        catch (Exception ex) { Report(ex); }
        return true; // 接管后接口缺失也不回退到全树刷新。
    }

    public static Resource? LoadResource(string path, Node? context, MerchantKind kind)
    {
        if (!IsManaged) return ResourceLoader.Load<Resource>(path);
        if (!MayApply(context, kind)) return null;
        try
        {
            return _service?.GetMethod("GetOrLoadRuntimeResource", Static,
                new[] { typeof(string), typeof(string), typeof(bool) })?.Invoke(null,
                new object[] { Group(context, kind), path, true }) as Resource;
        }
        catch (Exception ex) { Report(ex); return null; }
    }

    private static void Report(Exception ex)
    {
        if (_reported) return;
        _reported = true;
        GD.PrintErr("[Merchant2CuteII] Skin Changer bridge unavailable; selected skin retained: "
                    + ex.GetBaseException().Message);
    }
}
