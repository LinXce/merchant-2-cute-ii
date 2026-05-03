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
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Helpers;

namespace Merchant2CuteII;

[ModInitializer("Init")]
public static class Merchant2CuteIIMod
{
	private const string ModName = "Merchant2CuteII";

	public static void Init()
	{
		Harmony harmony = new Harmony(ModName);
		harmony.PatchAll();

		try
		{
			script.ResourcePreloader.PreloadAll();
			TaskHelper.RunSafely(script.ResourcePreloader.PreloadAudioPathsAsync(script.ModConfig.Paths.MerchantVoicePaths));
			// Run heavy spine preloads asynchronously to avoid blocking startup
			TaskHelper.RunSafely(script.ResourcePreloader.PreloadSpinePathsAsync(script.ModConfig.Paths.SpinePaths));
		}
		catch (System.Exception ex)
		{
			GD.PrintErr($"[Merchant2CuteII] Resource preloading failed: {ex.Message}");
		}

		GD.Print("[Merchant2CuteII] Mod initialized");
	}
}