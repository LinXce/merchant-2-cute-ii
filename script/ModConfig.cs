using Godot;

namespace Merchant2CuteII.script;

public static class ModConfig
{
	public static class Paths
	{
		public static string MerchantLegTexture => "res://animations/customs/merchant/leg.png";
		// kept for backward-compat; use GetMerchantVoicePath in ModConfig to resolve by variant

		public static string[] SpinePaths => new[]
		{
			"res://animations/backgrounds/fake_merchant_room/hand/fake_merchant_hand_skel_data.tres",
			"res://animations/backgrounds/fake_merchant_room/top/fake_merchant_top.tres.tres",
			"res://animations/backgrounds/merchant_room/hand/merchant_hand_skel_data.tres",
			"res://animations/backgrounds/merchant_room/top/shop_merchant_top.tres",
		};
	}

	public static class Options
	{
		private static string _handVariant = "hand";
		private static string _merchantVoiceVariant = "default";

		public static string HandVariant
		{
			get => _handVariant;
			set => _handVariant = NormalizeVariant(value);
		}

		public static bool UseFoot => HandVariant == "foot";

		public static string MerchantVoiceVariant
		{
			get => _merchantVoiceVariant;
			set => _merchantVoiceVariant = NormalizeVoiceVariant(value);
		}

		public static bool UseMerchantJpVoice => MerchantVoiceVariant == "jp";
		public static bool UseMerchantZhVoice => MerchantVoiceVariant == "zh";

		private static string NormalizeVariant(string? variant)
		{
			string normalized = (variant ?? string.Empty).Trim().ToLowerInvariant();
			return normalized == "foot" ? "foot" : "hand";
		}

		private static string NormalizeVoiceVariant(string? variant)
		{
			string normalized = (variant ?? string.Empty).Trim().ToLowerInvariant();
			if (normalized == "jp") return "jp";
			if (normalized == "zh") return "zh";
			return "default";
		}
	}

	public static class Voice
	{
		public static string? GetMerchantVoicePath(string sfxEvent)
		{
			string variant = Options.MerchantVoiceVariant;
			// match the event by substring
			if (variant == "jp")
			{
				if (sfxEvent.Contains("merchant_welcome"))
					return "jp/welcome.mp3";
				if (sfxEvent.Contains("merchant_thank_yous"))
					return "jp/thanks.mp3";
				if (sfxEvent.Contains("merchant_dissapointment") || sfxEvent.Contains("merchant_dissapoint"))
					return "jp/disapointment.mp3";
			}
			else if (variant == "zh")
			{
				if (sfxEvent.Contains("merchant_welcome"))
				{
					// choose between two welcome variants to add variety
					int idx = (int)(System.DateTime.UtcNow.Ticks % 2);
					return idx == 0 ? "zh/welcome1.wav" : "zh/welcome2.wav";
				}
				if (sfxEvent.Contains("merchant_thank_yous"))
					return "zh/thanks.wav";
				if (sfxEvent.Contains("merchant_dissapointment") || sfxEvent.Contains("merchant_dissapoint"))
					return "zh/disappointment.wav";
			}

			// default: no replacement
			return null;
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
