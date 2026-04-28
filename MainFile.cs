using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Modding;

namespace Merchant2CuteII;

[ModInitializer("Init")]
public static class Merchant2CuteIIMod
{
	private const string ModName = "Merchant2CuteII";

	public static void Init()
	{
		Harmony harmony = new Harmony(ModName);
		harmony.PatchAll();

		// Preload merchant spine resources to avoid hitch when swapping at runtime.
		try
		{
			// safe if paths missing - MerchantSpineLoader.Preload will handle nulls/errors
			script.MerchantSpineLoader.Preload(script.ModConfig.MerchantBodySpinePath);
			script.MerchantSpineLoader.Preload(script.ModConfig.MerchantHandSpinePath);
			script.MerchantSpineLoader.Preload(script.ModConfig.FakeMerchantBodySpinePath);
			script.MerchantSpineLoader.Preload(script.ModConfig.FakeMerchantHandSpinePath);
		}
		catch (System.Exception ex)
		{
			GD.PrintErr($"[Merchant2CuteII] Preload call failed: {ex.Message}");
		}

		GD.Print("[Merchant2CuteII] Mod initialized");
	}
}