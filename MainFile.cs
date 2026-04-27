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
		GD.Print("[Merchant2CuteII] Mod initialized");
	}
}