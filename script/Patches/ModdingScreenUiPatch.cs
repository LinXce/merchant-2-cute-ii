using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Nodes.Screens.ModdingScreen;
using Merchant2CuteII.script.Settings;

namespace Merchant2CuteII.script.Patches;

// 保留 Skin Changer 的设置页契约。它会独立调用此入口，即使本皮肤尚未被选中。
[HarmonyPatch(typeof(NModInfoContainer), nameof(NModInfoContainer.Fill))]
public static class ModdingScreenUiPatch
{
    public static void Postfix(NModInfoContainer __instance, Mod mod)
    {
        const string name = "Merchant2CuteIISettingsButton";
        bool ours = mod.manifest?.id == "Merchant2CuteII";
        var button = __instance.GetNodeOrNull<Button>(name);
        if (!ours)
        {
            if (button != null) button.Hide();
            return;
        }
        SettingsHost.EnsureLoaded();
        MerchantSettingsAvailability.EnsureAvailable();
        SettingsLocale.Sync();
        if (button == null)
        {
            button = new Button { Name = name, Text = "商人外观 / Merchant appearance" };
            __instance.AddChild(button);
            button.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.BottomRight);
            button.Position = new Vector2(-320, -64);
            button.Size = new Vector2(300, 48);
            button.Pressed += () => SettingsPanel.Open(__instance);
        }
        button.Show();
    }
}
