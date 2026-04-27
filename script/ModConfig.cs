using Godot;

namespace Merchant2CuteII.script;

public static class ModConfig
{
	public const float MerchantVisualOffsetX = 1280f;
	public const float MerchantVisualOffsetY = 540f;
	public const float MerchantVisualScaleX = 0.72f;
	public const float MerchantVisualScaleY = 0.72f;
	public const float MerchantHandScaleX = 0.4f;
	public const float MerchantHandScaleY = 0.4f;
	public const float PointAtTargetOffsetX = 0f;
	public const float PointAtTargetOffsetY = 0f;
	public const string MerchantTopSpinePath = "res://animations/merchant_top_L.tres";
	public const string MerchantHandSpinePath = "res://animations/merchant_hand_L.tres";

	// Backward-compatible alias: old config name maps to top model replacement.
	public const string SpineAnimationPath = MerchantTopSpinePath;

	public static Vector2 MerchantVisualPositionOffset => new Vector2(MerchantVisualOffsetX, MerchantVisualOffsetY);

	public static Vector2 MerchantVisualScale => new Vector2(MerchantVisualScaleX, MerchantVisualScaleY);

	public static Vector2 MerchantHandScale => new Vector2(MerchantHandScaleX, MerchantHandScaleY);

	public static Vector2 PointAtTargetOffset => new Vector2(PointAtTargetOffsetX, PointAtTargetOffsetY);
}