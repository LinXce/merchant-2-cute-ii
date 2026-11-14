using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace Merchant2CuteII.script.Settings;

/// <summary>
/// 配置文件路径解析：桌面版与**手机移植版**都能找到/写入配置文件。
///
/// 背景：桌面版 `user://` 一定可写；但手机移植版可能是重新打包的，`user://`
/// 可能不可写、或指向另一个目录，导致"读不到配置"。因此这里按优先级尝试多组路径：
///
/// <list type="number">
/// <item><description><c>user://merchant2cute_config.json</c> —— 老版本位置，优先保持</description></item>
/// <item><description><c>user://Merchant2CuteII/merchant2cute_config.json</c></description></item>
/// <item><description>游戏用户目录（<c>OS.GetUserDataDir()</c> / <c>OS.GetDataDir()</c>）</description></item>
/// <item><description>可执行文件同目录（移植版常把游戏解包到可写目录）</description></item>
/// <item><description>可执行文件目录下的 <c>mods/Merchant2CuteII/</c>（配置跟 mod 放一起）</description></item>
/// </list>
///
/// 读：第一个"存在且非空"的；写：优先写回读到的那条，否则写第一个可写目录。
/// 全过程都会打日志，方便手机用户把日志贴出来定位。
/// </summary>
public static class ConfigPaths
{
    public const string FileName = "merchant2cute_config.json";

    private static string? _resolvedRead;
    private static string? _resolvedWrite;
    private static readonly HashSet<string> FailedWrites = new();

    /// <summary>当前正在使用的配置文件路径（读优先，其次写）。</summary>
    public static string ActivePath => _resolvedWrite ?? _resolvedRead ?? DefaultPath;

    public static string DefaultPath => "user://" + FileName;

    /// <summary>候选路径（按优先级，已去重）。</summary>
    public static List<string> Candidates()
    {
        var list = new List<string>();
        void Add(string? p)
        {
            if (string.IsNullOrWhiteSpace(p))
            {
                return;
            }

            string normalized = p.Replace('\\', '/');
            if (!list.Contains(normalized))
            {
                list.Add(normalized);
            }
        }

        Add(DefaultPath);
        Add("user://Merchant2CuteII/" + FileName);

        Add(CombineSafe(SafeGet(OS.GetUserDataDir), FileName));
        Add(CombineSafe(SafeGet(OS.GetDataDir), FileName));

        string? exeDir = ExecutableDirectory();
        Add(CombineSafe(exeDir, FileName));
        Add(CombineSafe(exeDir, "mods/Merchant2CuteII/" + FileName));

        // ---- 由运行时真实值推导（不靠猜文件夹名）----
        // Android 上 user:// 通常是 /data/user/0/<pkg>/files，
        // 对应的"外部专属目录"是 /storage/emulated/0/Android/data/<pkg>/files。
        foreach (string dataDir in new[] { SafeGet(OS.GetDataDir), SafeGet(OS.GetUserDataDir) })
        {
            Add(CombineSafe(dataDir, FileName));

            string? pkg = PackageNameFromDataDir(dataDir);
            if (pkg != null)
            {
                Add(CombineSafe($"/storage/emulated/0/Android/data/{pkg}/files", FileName));
                Add(CombineSafe($"/sdcard/Android/data/{pkg}/files", FileName));
                Add(CombineSafe($"/storage/emulated/0/Android/data/{pkg}/files/mods/Merchant2CuteII", FileName));
            }
        }

        // user:// 的实际绝对路径（移植版可能被重定向）
        Add(CombineSafe(SafeGlobalize("user://"), FileName));

        return list;
    }

    /// <summary>读取用路径：第一个存在且非空的文件；都不存在返回 null。</summary>
    public static string? ResolveReadPath()
    {
        if (_resolvedRead != null)
        {
            return _resolvedRead;
        }

        List<string> candidates = Candidates();
        // 备用目录可能是上次实际保存的位置；按有效文件的新旧排序，避免一直读旧主文件。
        string? chosen = candidates.Where(ExistsAndNotEmpty)
            .OrderByDescending(Godot.FileAccess.GetModifiedTime).FirstOrDefault();
        if (chosen != null)
        {
            _resolvedRead = chosen;
            GD.Print($"[Merchant2CuteII] config: reading from {chosen}");
            return chosen;
        }

        // 全都找不到时做一次**有限扫描**：只在几个常见根目录下、限深度/数量地找同名文件，
        // 这样移植版把文件放在奇怪位置时也能被找到（而不是靠猜）。
        string? scanned = ScanForConfig();
        if (scanned != null)
        {
            _resolvedRead = scanned;
            GD.Print($"[Merchant2CuteII] config: found by scan at {scanned}");
            return scanned;
        }

        GD.Print("[Merchant2CuteII] config: not found in any candidate path");
        foreach (string path in candidates)
        {
            GD.Print($"[Merchant2CuteII] config:   tried {path} ({Describe(path)})");
        }

        return null;
    }

    /// <summary>写入用路径：优先读到的那条，否则第一个可写位置。</summary>
    public static string? ResolveWritePath()
    {
        if (_resolvedWrite != null)
        {
            return _resolvedWrite;
        }

        List<string> candidates = Candidates();

        // 1) 已经存在（且可写）的那条最安全：不会产生两份配置
        if (_resolvedRead != null && !FailedWrites.Contains(_resolvedRead))
        {
            if (TryEnsureWritable(_resolvedRead))
            {
                _resolvedWrite = _resolvedRead;
                GD.Print($"[Merchant2CuteII] config: saving to {_resolvedWrite} (same file we read)");
                return _resolvedWrite;
            }

            GD.PrintErr($"[Merchant2CuteII] config: {_resolvedRead} is not writable, looking for fallback");
        }

        // 2) 退而求其次：第一个可写路径
        string? fallback = PathChoice.FirstWritable(candidates,
            path => !FailedWrites.Contains(path) && TryEnsureWritable(path));
        if (fallback != null)
        {
            _resolvedWrite = fallback;
            GD.Print($"[Merchant2CuteII] config: saving to {_resolvedWrite}");
            return _resolvedWrite;
        }

        GD.PrintErr("[Merchant2CuteII] config: no writable path found; settings will not persist");
        foreach (string path in candidates)
        {
            GD.PrintErr($"[Merchant2CuteII] config:   {path} ({Describe(path)})");
        }

        return null;
    }

    /// <summary>
    /// 有限扫描：在常见根目录下按广度优先找 <see cref="FileName"/>。
    /// 限制深度 4、最多访问 400 个目录，避免手机卡顿。
    /// </summary>
    private static string? ScanForConfig()
    {
        string[] roots =
        {
            "/storage/emulated/0/Android/data",
            "/sdcard/Android/data",
            "/storage/emulated/0",
            "/sdcard",
        };

        int visited = 0;
        foreach (string root in roots)
        {
            try
            {
                if (!DirAccess.DirExistsAbsolute(root))
                {
                    continue;
                }
            }
            catch (Exception)
            {
                continue;
            }

            var queue = new Queue<(string Dir, int Depth)>();
            queue.Enqueue((root, 0));
            while (queue.Count > 0 && visited < 400)
            {
                (string dir, int depth) = queue.Dequeue();
                visited++;

                string candidate = dir.TrimEnd('/') + "/" + FileName;
                if (dir.Length > 0 && ExistsAndNotEmpty(candidate))
                {
                    return candidate;
                }

                if (depth >= 4)
                {
                    continue;
                }

                try
                {
                    using DirAccess? access = DirAccess.Open(dir);
                    if (access == null)
                    {
                        continue;
                    }

                    foreach (string sub in access.GetDirectories())
                    {
                        queue.Enqueue((dir.TrimEnd('/') + "/" + sub, depth + 1));
                    }
                }
                catch (Exception)
                {
                    // 无权限的目录直接跳过
                }
            }
        }

        return null;
    }

    /// <summary>环境信息（手机用户报问题时贴这段就够）。</summary>
    public static string DescribeEnvironment()
    {
        var lines = new List<string>
        {
            "os=" + SafeGet(OS.GetName),
            "exe=" + SafeGet(OS.GetExecutablePath),
            "exeDir=" + (ExecutableDirectory() ?? "<null>"),
            "dataDir=" + SafeGet(OS.GetDataDir),
            "userDataDir=" + SafeGet(OS.GetUserDataDir),
            "user:// -> " + SafeGlobalize("user://"),
        };

        foreach (string path in Candidates())
        {
            lines.Add($"{Describe(path)}  {path}");
        }

        lines.Add("active=" + ActivePath);
        return string.Join("\n", lines);
    }

    /// <summary>测试/诊断用：清掉缓存，下次重新解析。</summary>
    public static void Reset()
    {
        _resolvedRead = null;
        _resolvedWrite = null;
        FailedWrites.Clear();
    }

    public static void RejectWritePath(string path)
    {
        FailedWrites.Add(path);
        _resolvedWrite = null;
    }

    // ------------------------------------------------------------- helpers

    private static string Describe(string path)
    {
        bool exists = ExistsAndNotEmpty(path);
        string state = exists ? "exists" : "missing";
        return $"{state}, {SafeGlobalize(path)}";
    }

    private static bool ExistsAndNotEmpty(string path)
    {
        try
        {
            if (!Godot.FileAccess.FileExists(path))
            {
                return false;
            }

            using Godot.FileAccess? file = Godot.FileAccess.Open(path, Godot.FileAccess.ModeFlags.Read);
            if (file == null)
            {
                return false;
            }

            return file.GetLength() > 2 && SettingsJson.TryParse(file.GetAsText(), out _, out _);
        }
        catch (Exception)
        {
            return false;
        }
    }

    /// <summary>
    /// 确保目标可写。注意：**绝不能**用 <c>ModeFlags.Write</c> 探测已存在的文件——
    /// 那会把用户已有配置截断。文件已存在时用 <c>ReadWrite</c>（不截断）验证；
    /// 文件不存在时才尝试创建。
    /// </summary>
    private static bool TryEnsureWritable(string path)
    {
        try
        {
            bool exists = Godot.FileAccess.FileExists(path);

            if (!exists)
            {
                string? dir = ParentDirectory(SafeGlobalize(path));
                if (dir != null && !DirAccess.DirExistsAbsolute(dir))
                {
                    Error mkdir = DirAccess.MakeDirRecursiveAbsolute(dir);
                    if (mkdir != Error.Ok && mkdir != Error.AlreadyExists)
                    {
                        return false;
                    }
                }
            }

            // 探测同目录临时文件，不创建空配置，也不触碰用户原文件。
            string probePath = path + ".probe";
            using (Godot.FileAccess? probe = Godot.FileAccess.Open(probePath, Godot.FileAccess.ModeFlags.Write))
            {
                if (probe == null) return false;
            }
            DirAccess.RemoveAbsolute(SafeGlobalize(probePath));
            return true;
        }
        catch (Exception ex)
        {
            GD.Print($"[Merchant2CuteII] config: probe failed for {path}: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 从 Android 的 dataDir（形如 <c>/data/user/0/<pkg>/files</c> 或 <c>/data/data/<pkg>/files</c>）里取包名。
    /// 取不到返回 null，调用方就不会加那些候选。
    /// </summary>
    private static string? PackageNameFromDataDir(string? dataDir)
    {
        if (string.IsNullOrEmpty(dataDir))
        {
            return null;
        }

        string[] parts = dataDir.Replace('\\', '/').Split('/', StringSplitOptions.RemoveEmptyEntries);
        for (int i = 0; i + 1 < parts.Length; i++)
        {
            if (string.Equals(parts[i], "data", StringComparison.OrdinalIgnoreCase)
                || string.Equals(parts[i], "0", StringComparison.Ordinal))
            {
                continue;
            }
        }

        // 优先匹配 "..." 中形如 a.b.c 的段（包名一定含点）
        foreach (string part in parts)
        {
            if (part.Contains('.') && !part.Equals("files", StringComparison.OrdinalIgnoreCase))
            {
                return part;
            }
        }

        return null;
    }

    private static string? ExecutableDirectory()
    {
        try
        {
            string exe = OS.GetExecutablePath();
            if (string.IsNullOrEmpty(exe))
            {
                return null;
            }

            return ParentDirectory(exe.Replace('\\', '/'));
        }
        catch (Exception)
        {
            return null;
        }
    }

    private static string? ParentDirectory(string path)
    {
        int idx = path.LastIndexOf('/');
        return idx <= 0 ? null : path.Substring(0, idx);
    }

    private static string? CombineSafe(string? dir, string relative)
    {
        if (string.IsNullOrEmpty(dir))
        {
            return null;
        }

        return dir.Replace('\\', '/').TrimEnd('/') + "/" + relative;
    }

    private static string SafeGet(Func<string> getter)
    {
        try
        {
            return getter() ?? string.Empty;
        }
        catch (Exception)
        {
            return string.Empty;
        }
    }

    private static string SafeGlobalize(string path)
    {
        try
        {
            return ProjectSettings.GlobalizePath(path) ?? path;
        }
        catch (Exception)
        {
            return path;
        }
    }
}

