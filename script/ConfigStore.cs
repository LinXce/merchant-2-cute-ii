using System;
using System.IO;
using System.Text.Json;
using Godot;

namespace Merchant2CuteII.script;

public static class ConfigStore
{
    private const string ConfigPath = "user://merchant2cute_config.json";

    private class ConfigData
    {
        public string HandVariant { get; set; } = "hand";
        public string MerchantVoiceVariant { get; set; } = "default";
        public float ExtraDb { get; set; } = 4f;
        public string FoulPotionAnimation { get; set; } = "poison";
    }

    public static void Load()
    {
        try
        {
            var fa = Godot.FileAccess.Open(ConfigPath, Godot.FileAccess.ModeFlags.Read);
            if (fa == null)
                return;
            string json = fa.GetAsText();
            fa.Close();
            if (string.IsNullOrEmpty(json))
                return;
            var cfg = JsonSerializer.Deserialize<ConfigData>(json);
            if (cfg == null)
                return;
            Merchant2CuteII.script.ModConfig.Options.HandVariant = cfg.HandVariant;
            Merchant2CuteII.script.ModConfig.Options.MerchantVoiceVariant = cfg.MerchantVoiceVariant;
            Merchant2CuteII.script.ModConfig.Options.ExtraDb = cfg.ExtraDb;
            Merchant2CuteII.script.ModConfig.Options.FoulPotionAnimation = cfg.FoulPotionAnimation;
        }
        catch (Exception)
        {
            // ignore
        }
    }

    public static void Save()
    {
        try
        {
            var cfg = new ConfigData
            {
                HandVariant = Merchant2CuteII.script.ModConfig.Options.HandVariant,
                MerchantVoiceVariant = Merchant2CuteII.script.ModConfig.Options.MerchantVoiceVariant,
                ExtraDb = Merchant2CuteII.script.ModConfig.Options.ExtraDb,
                FoulPotionAnimation = Merchant2CuteII.script.ModConfig.Options.FoulPotionAnimation
            };
            string json = JsonSerializer.Serialize(cfg, new JsonSerializerOptions { WriteIndented = true });
            var fa = Godot.FileAccess.Open(ConfigPath, Godot.FileAccess.ModeFlags.Write);
            if (fa == null)
                return;
            fa.StoreString(json);
            fa.Close();
        }
        catch (Exception)
        {
            // ignore
        }
    }
}
