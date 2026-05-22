using System;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.DevConsole.ConsoleCommands;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Nodes.Screens.ModdingScreen;

namespace Merchant2CuteII.script.Patches;

[HarmonyPatch(typeof(NModInfoContainer), nameof(NModInfoContainer.Fill))]
public static class ModdingScreenUiPatch
{
    private const string ModId = "Merchant2CuteII";

    private const string PointButtonName = "Merchant2CuteII_PointVariant";
    private const string VoiceButtonName = "Merchant2CuteII_VoiceVariant";

    [HarmonyPostfix]
    public static void Postfix(NModInfoContainer __instance, Mod mod)
    {
        if (__instance == null)
            return;

        bool isOurMod = string.Equals(mod?.manifest?.id, ModId, StringComparison.OrdinalIgnoreCase);

        var pointBtn = __instance.GetNodeOrNull<Button>(PointButtonName);
        var voiceBtn = __instance.GetNodeOrNull<Button>(VoiceButtonName);

        if (!isOurMod)
        {
            if (pointBtn != null)
                pointBtn.Visible = false;
            if (voiceBtn != null)
                voiceBtn.Visible = false;
            return;
        }

        if (pointBtn == null)
        {
            pointBtn = CreateButton(PointButtonName);
            pointBtn.Pressed += () =>
            {
                try
                {
                    TogglePointVariant();
                    MerchantConsoleCmd.ApplyPointVariantNow();
                    RefreshButtonText(pointBtn, voiceBtn);
                }
                catch (Exception ex)
                {
                    GD.PrintErr($"[Merchant2CuteII] Failed to toggle point variant: {ex.Message}");
                }
            };
            __instance.AddChild(pointBtn);
        }

        if (voiceBtn == null)
        {
            voiceBtn = CreateButton(VoiceButtonName);
            voiceBtn.Pressed += () =>
            {
                try
                {
                    ToggleVoiceVariant();
                    RefreshButtonText(pointBtn, voiceBtn);
                }
                catch (Exception ex)
                {
                    GD.PrintErr($"[Merchant2CuteII] Failed to toggle voice variant: {ex.Message}");
                }
            };
            __instance.AddChild(voiceBtn);
        }

        RefreshButtonText(pointBtn, voiceBtn);
        UpdateButtonPositions(__instance, pointBtn, voiceBtn);

        pointBtn.Visible = true;
        voiceBtn.Visible = true;
    }

    private static Button CreateButton(string name)
    {
        return new Button
        {
            Name = name,
            FocusMode = Control.FocusModeEnum.All,
            Size = new Vector2(280, 44),
            ZIndex = 100,
            ZAsRelative = true,
        };
    }

    private static void RefreshButtonText(Button? pointBtn, Button? voiceBtn)
    {
        string point = Merchant2CuteII.script.ModConfig.Options.HandVariant;
        string voice = Merchant2CuteII.script.ModConfig.Options.MerchantVoiceVariant;

        if (pointBtn != null)
            pointBtn.Text = $"指向：{point}（点击切换）";
        if (voiceBtn != null)
            voiceBtn.Text = $"语音：{voice}（点击切换）";
    }

    private static void UpdateButtonPositions(Control container, Button pointBtn, Button voiceBtn)
    {
        float marginLeft = 22;
        float marginBottom = 18;
        float spacing = 8;

        Vector2 size = pointBtn.Size;
        float totalHeight = size.Y + spacing + voiceBtn.Size.Y;

        float yBase = container.Size.Y > 0 ? container.Size.Y - totalHeight - marginBottom : 930;
        yBase = MathF.Max(0, yBase);

        pointBtn.Position = new Vector2(marginLeft, yBase);
        voiceBtn.Position = new Vector2(marginLeft, yBase + size.Y + spacing);
    }

    private static void TogglePointVariant()
    {
        string current = Merchant2CuteII.script.ModConfig.Options.HandVariant;
        string next = current switch
        {
            "hand" => "foot",
            "foot" => "white",
            "white" => "black",
            _ => "hand",
        };

        Merchant2CuteII.script.ModConfig.Options.HandVariant = next;
        TrySaveConfig();
    }

    private static void ToggleVoiceVariant()
    {
        string current = Merchant2CuteII.script.ModConfig.Options.MerchantVoiceVariant;
        string next = current == "default" ? "jp" : current == "jp" ? "zh" : "default";

        Merchant2CuteII.script.ModConfig.Options.MerchantVoiceVariant = next;
        TrySaveConfig();
    }

    private static void TrySaveConfig()
    {
        try
        {
            Merchant2CuteII.script.ConfigStore.Save();
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[Merchant2CuteII] Failed to save config: {ex.Message}");
        }
    }
}
