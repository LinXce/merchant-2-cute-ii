using Godot;

namespace Merchant2CuteII.script;

public static class ModConfig
{
	public static class Paths
	{
		public static string MerchantBodySpine => "res://animations/merchant/merchant_body_L.tres";
		public static string MerchantHandSpine => "res://animations/merchant/merchant_hand_L.tres";
		public static string MerchantFootSpine => string.Empty;
		public static string FakeMerchantBodySpine => "res://animations/fake/fake_merchant_body_L.tres";
		public static string FakeMerchantHandSpine => "res://animations/fake/fake_merchant_hand_L.tres";
		public static string FakeMerchantFootSpine => string.Empty;
		public static string MerchantLegTexture => "res://animations/merchant/leg.png";
	}

	public static class Options
	{
		private static string _handVariant = "hand";

		public static string HandVariant
		{
			get => _handVariant;
			set => _handVariant = NormalizeVariant(value);
		}

		public static bool UseFoot => HandVariant == "foot";

		public static string GetActiveMerchantHandPath()
		{
			return UseFoot && !string.IsNullOrEmpty(Paths.MerchantFootSpine)
				? Paths.MerchantFootSpine
				: Paths.MerchantHandSpine;
		}

		public static string GetActiveFakeMerchantHandPath()
		{
			return UseFoot && !string.IsNullOrEmpty(Paths.FakeMerchantFootSpine)
				? Paths.FakeMerchantFootSpine
				: Paths.FakeMerchantHandSpine;
		}

		private static string NormalizeVariant(string? variant)
		{
			string normalized = (variant ?? string.Empty).Trim().ToLowerInvariant();
			return normalized == "foot" ? "foot" : "hand";
		}
	}

	public static class Merchant
	{
		public static Vector2 VisualPositionOffset => new Vector2(1260f, 620f);
		public static Vector2 VisualScale => new Vector2(0.18f, 0.18f);
		public static Vector2 HandScale => new Vector2(0.4f, 0.4f);
		public static Vector2 PointAtTargetOffset => Vector2.Zero;
		public static Vector2 LegPosition => new Vector2(0f, -900f);
		public static Vector2 LegScale => new Vector2(0.8f, 0.8f);
		public static float LegRotationDegrees => -6f;
	}

	public static class FakeMerchant
	{
		public static Vector2 VisualPositionOffset => Vector2.Zero;
		public static Vector2 VisualScale => new Vector2(0.36f, 0.36f);
		public static Vector2 HandScale => new Vector2(0.4f, 0.4f);
		public static Vector2 PointAtTargetOffset => new Vector2(0f, -100f);
	}
}
