using Godot;
using System.Diagnostics;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;

namespace Merchant2CuteII.script;

internal static class ResourcePreloader
{
	private static readonly Dictionary<string, Resource?> _cache = new();

	public static void PreloadAll()
	{
		// preload only essential UI textures; voice assets can be large and are loaded on demand
		Preload(ModConfig.Paths.MerchantLegTexture);
	}

	public static async Task PreloadAudioPathsAsync(IEnumerable<string> paths, int perFrame = 2)
	{
		if (paths == null)
			return;

		SceneTree? tree = Engine.GetMainLoop() as SceneTree;
		int batch = 0;
		int loaded = 0;
		Stopwatch sw = Stopwatch.StartNew();

		foreach (string path in paths)
		{
			if (string.IsNullOrEmpty(path) || _cache.ContainsKey(path))
			{
				continue;
			}

			try
			{
				Resource? resource = ResourceLoader.Load<Resource>(path, null, ResourceLoader.CacheMode.Reuse);
				_cache[path] = resource;
				if (resource != null)
				{
					loaded++;
					GD.Print($"[Merchant2CuteII] Preloaded audio resource: {path}");
				}
				else
				{
					GD.PrintErr($"[Merchant2CuteII] Preload audio: resource not found {path}");
				}
			}
			catch (System.Exception ex)
			{
				GD.PrintErr($"[Merchant2CuteII] Preload audio exception for {path}: {ex.Message}");
			}

			batch++;
			if (perFrame > 0 && batch >= perFrame && tree != null)
			{
				batch = 0;
				await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
			}
		}

		GD.Print($"[Merchant2CuteII] PreloadAudioPathsAsync completed: loaded={loaded} timeMs={sw.ElapsedMilliseconds}");
	}

	public static void PreloadSpinePaths(IEnumerable<string> paths)
	{
		if (paths == null)
			return;

		foreach (var p in paths)
		{
			if (string.IsNullOrEmpty(p))
				continue;

			if (_cache.ContainsKey(p))
				continue;

			try
			{
				var raw = ResourceLoader.Load<Resource>(p, null, ResourceLoader.CacheMode.Reuse);
				if (raw != null)
				{
					// wrap to ensure spine binding native resource is initialized where applicable
					try
					{
						var wrapper = new MegaSkeletonDataResource(raw);
						_cache[p] = raw;
						GD.Print($"[Merchant2CuteII] Preloaded spine resource: {p}");
					}
					catch
					{
						_cache[p] = raw;
						GD.Print($"[Merchant2CuteII] Preloaded raw resource (no wrapper): {p}");
					}
				}
				else
				{
					_cache[p] = null;
					GD.PrintErr($"[Merchant2CuteII] Preload spine: resource not found {p}");
				}
			}
			catch (System.Exception ex)
			{
				GD.PrintErr($"[Merchant2CuteII] Preload spine exception for {p}: {ex.Message}");
			}
		}
	}

	public static async Task PreloadSpinePathsAsync(IEnumerable<string> paths, int perFrame = 1)
	{
		if (paths == null)
			return;

		SceneTree? tree = Engine.GetMainLoop() as SceneTree;
		Stopwatch sw = Stopwatch.StartNew();
		int loaded = 0;
		int batch = 0;

		foreach (var p in paths)
		{
			if (string.IsNullOrEmpty(p))
				continue;

			if (_cache.ContainsKey(p))
				continue;

			try
			{
				var raw = ResourceLoader.Load<Resource>(p, null, ResourceLoader.CacheMode.Reuse);
				if (raw != null)
				{
					try
					{
						var wrapper = new MegaSkeletonDataResource(raw);
						_cache[p] = raw;
						loaded++;
						GD.Print($"[Merchant2CuteII] Preloaded spine resource: {p}");
					}
					catch
					{
						_cache[p] = raw;
						loaded++;
						GD.Print($"[Merchant2CuteII] Preloaded raw resource (no wrapper): {p}");
					}
				}
				else
				{
					_cache[p] = null;
					GD.PrintErr($"[Merchant2CuteII] Preload spine: resource not found {p}");
				}
			}
			catch (System.Exception ex)
			{
				GD.PrintErr($"[Merchant2CuteII] Preload spine exception for {p}: {ex.Message}");
			}

			batch++;
			if (perFrame > 0 && batch >= perFrame && tree != null)
			{
				batch = 0;
				await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
			}
		}

		long elapsed = sw.ElapsedMilliseconds;
		GD.Print($"[Merchant2CuteII] PreloadSpinePathsAsync completed: loaded={loaded} timeMs={elapsed}");
	}

	private static void Preload(string path)
	{
		if (string.IsNullOrEmpty(path))
			return;

		if (_cache.ContainsKey(path))
			return;

		try
		{
			var res = ResourceLoader.Load<Resource>(path, null, ResourceLoader.CacheMode.Reuse);
			_cache[path] = res;
			if (res == null)
				GD.PrintErr($"[Merchant2CuteII] Preload failed: {path} returned null");
			else
				GD.Print($"[Merchant2CuteII] Preloaded: {path}");
		}
		catch (System.Exception ex)
		{
			GD.PrintErr($"[Merchant2CuteII] Preload exception for {path}: {ex.Message}");
		}
	}
}
