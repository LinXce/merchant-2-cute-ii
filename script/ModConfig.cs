using Godot;

namespace Merchant2CuteII.script;

public static class ModConfig
{
	public static class Paths
	{
		public static string MerchantLegTexture => "res://animations/customs/merchant/leg.png";

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

		public static string HandVariant
		{
			get => _handVariant;
			set => _handVariant = NormalizeVariant(value);
		}

		public static bool UseFoot => HandVariant == "foot";

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
