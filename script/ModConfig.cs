using Godot;

namespace Merchant2CuteII.script;

public static class ModConfig
{
	public static class Paths
	{
		public const string MerchantBodySpine = "res://animations/merchant/merchant_body_L.tres";
		public const string MerchantHandSpine = "res://animations/merchant/merchant_hand_L.tres";
		public const string FakeMerchantBodySpine = "res://animations/fake/fake_merchant_body_L.tres";
		public const string FakeMerchantHandSpine = "res://animations/fake/fake_merchant_hand_L.tres";
		public const string MerchantLegTexture = "res://animations/merchant/leg.png";
	}

	public static class Merchant
	{
		public static Vector2 VisualPositionOffset => new Vector2(1260f, 620f);
		public static Vector2 VisualScale => new Vector2(0.18f, 0.18f);
		public static Vector2 HandScale => new Vector2(0.4f, 0.4f);
		public static Vector2 PointAtTargetOffset => Vector2.Zero;
		public static Vector2 LegPosition => new Vector2(50f, -810f);
		public static Vector2 LegScale => new Vector2(0.75f, 0.75f);
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