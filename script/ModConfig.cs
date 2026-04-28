using Godot;

namespace Merchant2CuteII.script;

public static class ModConfig
{
	public const float RealMerchantVisualOffsetX = 1260f;
	public const float RealMerchantVisualOffsetY = 620f;
	public const float RealMerchantVisualScaleX = 0.18f;
	public const float RealMerchantVisualScaleY = 0.18f;
	public const float RealPointAtTargetOffsetX = 0f;
	public const float RealPointAtTargetOffsetY = 0f;
	public const float RealMerchantHandScaleX = 0.4f;
	public const float RealMerchantHandScaleY = 0.4f;
	public const float FakeMerchantVisualOffsetX = 1260f;
	public const float FakeMerchantVisualOffsetY = 620f;
	public const float FakeMerchantVisualScaleX = 1.18f;
	public const float FakeMerchantVisualScaleY = 1.18f;
	public const float FakePointAtTargetOffsetX = 0f;
	public const float FakePointAtTargetOffsetY = -100f;
	public const float FakeMerchantHandScaleX = 0.4f;
	public const float FakeMerchantHandScaleY = 0.4f;
	public const string MerchantBodySpinePath = "res://animations/merchant/merchant_body_L.tres";
	public const string MerchantHandSpinePath = "res://animations/merchant/merchant_hand_L.tres";
	public const string FakeMerchantBodySpinePath = "res://animations/fake/fake_merchant_body_L.tres";
	public const string FakeMerchantHandSpinePath = "res://animations/fake/fake_merchant_hand_L.tres";

	// Backward-compatible alias: old config name maps to top model replacement.
	public const string SpineAnimationPath = MerchantBodySpinePath;

	public static Vector2 RealMerchantVisualPositionOffset => new Vector2(RealMerchantVisualOffsetX, RealMerchantVisualOffsetY);

	public static Vector2 RealMerchantVisualScale => new Vector2(RealMerchantVisualScaleX, RealMerchantVisualScaleY);

	public static Vector2 RealMerchantHandScale => new Vector2(RealMerchantHandScaleX, RealMerchantHandScaleY);

	public static Vector2 RealPointAtTargetOffset => new Vector2(RealPointAtTargetOffsetX, RealPointAtTargetOffsetY);

	public static Vector2 FakeMerchantVisualPositionOffset => new Vector2(FakeMerchantVisualOffsetX, FakeMerchantVisualOffsetY);

	public static Vector2 FakeMerchantVisualScale => new Vector2(FakeMerchantVisualScaleX, FakeMerchantVisualScaleY);

	public static Vector2 FakeMerchantHandScale => new Vector2(FakeMerchantHandScaleX, FakeMerchantHandScaleY);

	public static Vector2 FakePointAtTargetOffset => new Vector2(FakePointAtTargetOffsetX, FakePointAtTargetOffsetY);
}