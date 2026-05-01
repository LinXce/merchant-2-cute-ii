/*
 * Merchant2CuteII - A Slay the Spire 2 Mod
 * Copyright (C) 2026 LinXce
 * 
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */

// ?Merchant2CuteII - A Slay the Spire 2 Mod
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
			script.MerchantSpineLoader.Preload(script.ModConfig.Paths.MerchantBodySpine);
			script.MerchantSpineLoader.Preload(script.ModConfig.Paths.MerchantHandSpine);
			script.MerchantSpineLoader.Preload(script.ModConfig.Paths.FakeMerchantBodySpine);
			script.MerchantSpineLoader.Preload(script.ModConfig.Paths.FakeMerchantHandSpine);
		}
		catch (System.Exception ex)
		{
			GD.PrintErr($"[Merchant2CuteII] Preload call failed: {ex.Message}");
		}

		GD.Print("[Merchant2CuteII] Mod initialized");
	}
}