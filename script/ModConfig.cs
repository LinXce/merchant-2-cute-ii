using Godot;
using Merchant2CuteII.script.Settings;

namespace Merchant2CuteII.script;

/// <summary>
/// 资源路径与几何常量 + **旧 API 兼容层**。
///
/// schema 2 之后，真正的设置数据在 <see cref="SettingsHost"/>.Current 里（分真/假商人）。
/// 这里保留 <c>ModConfig.Options.*</c> 是因为控制台命令、Patches 与旧版本配置文件都还在用它；
/// 每个属性都是对新结构的读写代理，旧的语义（"一个全局 HandVariant 同时管两个商人"）被保留。
/// </summary>
public static class ModConfig
{
    public static class Paths
    {
        public static string MerchantLegTexture => "res://animations/customs/merchant/leg.png";

        public static string[] MerchantVoicePaths => new[]
        {
            "res://debug_audio/jp/welcome.mp3",
            "res://debug_audio/jp/thanks.mp3",
            "res://debug_audio/jp/disapointment.mp3",
            "res://debug_audio/zh/welcome1.wav",
            "res://debug_audio/zh/welcome2.wav",
            "res://debug_audio/zh/thanks.wav",
            "res://debug_audio/zh/disappointment.wav",
        };

        public static string[] SpinePaths => new[]
        {
            "res://animations/backgrounds/fake_merchant_room/hand/fake_merchant_hand_skel_data.tres",
            "res://animations/backgrounds/fake_merchant_room/top/fake_merchant_top.tres",
            "res://animations/backgrounds/merchant_room/hand/merchant_hand_skel_data.tres",
            "res://animations/backgrounds/merchant_room/top/shop_merchant_top.tres",
        };
    }

    /// <summary>旧 API。读写的是「真商人」那一份设置；写的时候两个商人一起改（旧行为）。</summary>
    public static class Options
    {
        public static string HandVariant
        {
            get => SettingsHost.Current.RealMerchant.HandVariant;
            set
            {
                MerchantSkinSettings real = SettingsHost.Current.RealMerchant;
                MerchantSkinSettings fake = SettingsHost.Current.FakeMerchant;
                real.HandVariant = HandVariants.NormalizeFor(MerchantKind.Real, real.HandSet, value);
                fake.HandVariant = HandVariants.NormalizeFor(MerchantKind.Fake, fake.HandSet, value);
            }
        }

        /// <summary>真商人是否选了「脚」系变体。</summary>
        public static bool UseFoot => HandVariant == HandVariants.Foot;

        /// <summary>脚 / 白 / 黑 都算「脚系」（旧版用来决定购物界面的腿要不要显示）。</summary>
        public static bool UseFootLike => SettingsStore.IsFootLike(HandVariant);

        public static string MerchantVoiceVariant
        {
            get => SettingsHost.Current.Voice.Language;
            set => SettingsHost.Current.Voice.Language = VoiceLanguages.Normalize(value);
        }

        public static bool UseMerchantJpVoice => MerchantVoiceVariant == VoiceLanguages.Japanese;

        public static bool UseMerchantZhVoice => MerchantVoiceVariant == VoiceLanguages.Chinese;

        public static float ExtraDb
        {
            get => SettingsHost.Current.Voice.VolumeDb;
            set => SettingsHost.Current.Voice.VolumeDb = Mathf.Clamp(value, -12f, 12f);
        }
    }

    public static float GetMerchantVoiceVolumeLinear()
    {
        return SettingsHost.Current.Voice.LinearGain;
    }

    public static class Voice
    {
        /// <summary>
        /// 事件名 → 本 mod 的音频相对路径（相对 <c>res://debug_audio/</c>）。
        /// 返回 null 表示"不替换，用原版音效"。
        /// </summary>
        public static string? GetMerchantVoicePath(string sfxEvent)
        {
            string language = SettingsHost.Current.Voice.Language;
            string[] candidates = language switch
            {
                VoiceLanguages.Japanese => GetJpCandidates(sfxEvent),
                VoiceLanguages.Chinese => GetZhCandidates(sfxEvent),
                _ => System.Array.Empty<string>(),
            };

            if (candidates.Length == 0)
            {
                return null;
            }

            foreach (string candidate in candidates)
            {
                if (Godot.ResourceLoader.Exists($"res://debug_audio/{candidate}"))
                {
                    return candidate;
                }
            }

            // 文件缺失也返回第一个，让日志里能看到问题
            return candidates[0];
        }

        private static string[] GetJpCandidates(string sfxEvent)
        {
            // ---- 假商人（reverse_merchant_*）：同样全部替换成我们的语音 ----
            if (sfxEvent.Contains("reverse_merchant"))
            {
                if (sfxEvent.Contains("laugh"))
                {
                    return new[] { "jp/welcome.mp3" };
                }

                // hurt / die 等战斗音效统一用"不满"语音，避免战斗中突然冒出原版男声
                return new[] { "jp/disapointment.mp3" };
            }

            // ---- 商店商人（merchant_*）----
            if (sfxEvent.Contains("merchant_welcome"))
            {
                return new[] { "jp/welcome.mp3" };
            }

            if (sfxEvent.Contains("merchant_thank_yous") || sfxEvent.Contains("merchant_passive"))
            {
                return new[] { "jp/thanks.mp3" };
            }

            if (sfxEvent.Contains("merchant_dissapointment") || sfxEvent.Contains("merchant_dissapoint"))
            {
                return new[] { "jp/disapointment.mp3" };
            }

            return System.Array.Empty<string>();
        }

        private static string[] GetZhCandidates(string sfxEvent)
        {
            // ---- 假商人（reverse_merchant_*）----
            if (sfxEvent.Contains("reverse_merchant"))
            {
                if (sfxEvent.Contains("laugh"))
                {
                    // 中文有两段欢迎语音，第二段拿来做笑声
                    return new[] { "zh/welcome2.wav", "zh/welcome1.wav" };
                }

                return new[] { "zh/disappointment.wav" };
            }

            // ---- 商店商人（merchant_*）----
            if (sfxEvent.Contains("merchant_welcome"))
            {
                return new[] { "zh/welcome1.wav", "zh/welcome2.wav" };
            }

            if (sfxEvent.Contains("merchant_thank_yous") || sfxEvent.Contains("merchant_passive"))
            {
                return new[] { "zh/thanks.wav" };
            }

            if (sfxEvent.Contains("merchant_dissapointment") || sfxEvent.Contains("merchant_dissapoint"))
            {
                return new[] { "zh/disappointment.wav" };
            }

            return System.Array.Empty<string>();
        }
    }

    public static class Merchant
    {
        public static Vector2 VisualPositionOffset => new Vector2(1260f, 620f);
        public static Vector2 VisualScale => new Vector2(0.18f, 0.18f);
        public static Vector2 LegPosition => new Vector2(0f, -900f);
        public static Vector2 LegScale => new Vector2(0.8f, 0.8f);
        public static float LegRotationDegrees => -6f;
    }

    public static class FakeMerchant
    {
        public static Vector2 VisualPositionOffset => Vector2.Zero;
        public static Vector2 VisualScale => new Vector2(0.36f, 0.36f);
    }
}
