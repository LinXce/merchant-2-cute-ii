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
    private const string VoiceVolumeButtonName = "Merchant2CuteII_VoiceVolume";

    private static readonly float[] VoiceVolumePresets = new[] { -12f, -6f, -3f, 0f, 3f, 6f };

    [HarmonyPostfix]
    public static void Postfix(NModInfoContainer __instance, Mod mod)
    {
        if (__instance == null)
            return;

        bool isOurMod = string.Equals(mod?.manifest?.id, ModId, StringComparison.OrdinalIgnoreCase);

        var pointBtn = __instance.GetNodeOrNull<Button>(PointButtonName);
        var voiceBtn = __instance.GetNodeOrNull<Button>(VoiceButtonName);
        var volumeBtn = __instance.GetNodeOrNull<Button>(VoiceVolumeButtonName);

        if (!isOurMod)
        {
            if (pointBtn != null)
                pointBtn.Visible = false;
            if (voiceBtn != null)
                voiceBtn.Visible = false;
            if (volumeBtn != null)
                volumeBtn.Visible = false;
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
                    RefreshButtonText(pointBtn, voiceBtn, volumeBtn);
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
                    RefreshButtonText(pointBtn, voiceBtn, volumeBtn);
                }
                catch (Exception ex)
                {
                    GD.PrintErr($"[Merchant2CuteII] Failed to toggle voice variant: {ex.Message}");
                }
            };
            __instance.AddChild(voiceBtn);
        }

        if (volumeBtn == null)
        {
            volumeBtn = CreateButton(VoiceVolumeButtonName);
            volumeBtn.Pressed += () =>
            {
                try
                {
                    ToggleVoiceVolume();
                    RefreshButtonText(pointBtn, voiceBtn, volumeBtn);
                }
                catch (Exception ex)
                {
                    GD.PrintErr($"[Merchant2CuteII] Failed to toggle voice volume: {ex.Message}");
                }
            };
            __instance.AddChild(volumeBtn);
        }

        RefreshButtonText(pointBtn, voiceBtn, volumeBtn);
        UpdateButtonPositions(__instance, pointBtn, voiceBtn, volumeBtn);

        pointBtn.Visible = true;
        voiceBtn.Visible = true;
        volumeBtn.Visible = true;
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

    private static void RefreshButtonText(Button? pointBtn, Button? voiceBtn, Button? volumeBtn)
    {
        string point = Merchant2CuteII.script.ModConfig.Options.HandVariant;
        string voice = Merchant2CuteII.script.ModConfig.Options.MerchantVoiceVariant;
        float voiceDb = Merchant2CuteII.script.ModConfig.Options.ExtraDb;
        float voiceScale = Merchant2CuteII.script.ModConfig.GetMerchantVoiceVolumeLinear();

        if (pointBtn != null)
            pointBtn.Text = $"指向：{point}（点击切换）";
        if (voiceBtn != null)
            voiceBtn.Text = $"语音：{voice}（点击切换）";
        if (volumeBtn != null)
            volumeBtn.Text = $"语音音量：{voiceDb:+0.##;-0.##;0} dB ({voiceScale:0.00}x)";
    }

    private static void UpdateButtonPositions(Control container, Button pointBtn, Button voiceBtn, Button volumeBtn)
    {
        float marginLeft = 22;
        float marginBottom = 18;
        float spacing = 8;

        Vector2 size = pointBtn.Size;
        float totalHeight = size.Y + spacing + voiceBtn.Size.Y + spacing + volumeBtn.Size.Y;

        float yBase = container.Size.Y > 0 ? container.Size.Y - totalHeight - marginBottom : 930;
        yBase = MathF.Max(0, yBase);

        pointBtn.Position = new Vector2(marginLeft, yBase);
        voiceBtn.Position = new Vector2(marginLeft, yBase + size.Y + spacing);
        volumeBtn.Position = new Vector2(marginLeft, yBase + (size.Y + spacing) * 2);
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

    private static void ToggleVoiceVolume()
    {
        float current = Merchant2CuteII.script.ModConfig.Options.ExtraDb;
        int index = Array.IndexOf(VoiceVolumePresets, current);
        if (index < 0)
        {
            index = 0;
        }

        int nextIndex = (index + 1) % VoiceVolumePresets.Length;
        Merchant2CuteII.script.ModConfig.Options.ExtraDb = VoiceVolumePresets[nextIndex];
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
