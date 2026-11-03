using System;
using Godot;
using MegaCrit.Sts2.Core.DevConsole;
using MegaCrit.Sts2.Core.DevConsole.ConsoleCommands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using Merchant2CuteII.script;
using Merchant2CuteII.script.Settings;

namespace MegaCrit.Sts2.Core.DevConsole.ConsoleCommands;

/// <summary>
/// 控制台命令 <c>merchant</c>。
///
/// schema 2 的语法（新增按商人分别设置）：
/// <code>
/// merchant status
/// merchant real|fake hand &lt;hand|foot|white|black|toggle&gt;
/// merchant real|fake set  &lt;own|other|toggle&gt;
/// merchant real|fake leg  &lt;on|off|toggle&gt;      （leg 只对真商人有意义）
/// merchant voice &lt;default|jp|zh|toggle|db &lt;value&gt;&gt;
/// </code>
/// 旧语法仍然可用（会同时改两个商人，与旧版行为一致）：
/// <code>
/// merchant point hand|foot|white|black|toggle|status
/// </code>
/// </summary>
public class MerchantConsoleCmd : AbstractConsoleCmd
{
    public override string CmdName => "merchant";

    public override string Args => "status | real|fake <hand|set|leg> ... | point ... | voice ... ";

    public override string Description => "Configure the cute merchant skin: per-merchant hand variant / skeleton set, voice language and volume";

    public override bool IsNetworked => false;

    static MerchantConsoleCmd()
    {
        try
        {
            ConfigStore.Load();
            SkinApplier.WarmUp();
            MerchantRuntime.RefreshAll();
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[Merchant2CuteII] Console init failed: {ex.Message}");
        }
    }

    public override CmdResult Process(Player? issuingPlayer, string[] args)
    {
        string verb = args.Length > 0 ? args[0].ToLowerInvariant() : "status";

        try
        {
            return verb switch
            {
                "status" => new CmdResult(true, MerchantRuntime.DescribeCurrent()),
                "real" => ProcessMerchant(MerchantKind.Real, args),
                "fake" => ProcessMerchant(MerchantKind.Fake, args),
                "point" => ProcessLegacyPoint(args),
                "voice" => ProcessVoiceCommand(args),
                "tune" => ProcessTuneCommand(args),
                _ => Usage(),
            };
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[Merchant2CuteII] Console command failed: {ex}");
            return new CmdResult(false, "Command failed: " + ex.Message);
        }
    }

    private static CmdResult Usage()
    {
        return new CmdResult(false,
            "Usage: merchant status | merchant real|fake hand <hand|foot|white|black|toggle> | "
            + "merchant real|fake set <own|other|toggle> | merchant real|fake leg <on|off|toggle> | "
            + "merchant tune [offset real|fake own|other <dx> <dy> | scale ... | reset] | ");
    }

    // ------------------------------------------------------- per merchant

    private static CmdResult ProcessMerchant(MerchantKind kind, string[] args)
    {
        string field = args.Length > 1 ? args[1].ToLowerInvariant() : "status";
        MerchantSkinSettings skin = SettingsHost.Current.For(kind);
        string label = kind == MerchantKind.Real ? "real" : "fake";

        switch (field)
        {
            case "status":
                return new CmdResult(true,
                    $"{label}: body={skin.BodySet}, handSet={skin.HandSet}, hand={skin.HandVariant}, leg={(skin.ShowInventoryLeg ? "on" : "off")}");

            case "hand":
            {
                string value = args.Length > 2 ? args[2].ToLowerInvariant() : "toggle";
                string next = value == "toggle" ? HandVariants.NextFor(kind, skin.HandSet, skin.HandVariant) : value;
                if (!HandVariants.IsAllowed(kind, skin.HandSet, next))
                {
                    string allowed = string.Join("|", HandVariants.Allowed(kind, skin.HandSet));
                    return new CmdResult(false,
                        $"Usage: merchant {label} hand {allowed}|toggle  (allowed set depends on hand-set={skin.HandSet})");
                }

                skin.HandVariant = HandVariants.NormalizeFor(kind, skin.HandSet, next);
                return Commit($"{label} hand -> {skin.HandVariant}");
            }

            case "body":
            case "set":
            {
                // set 是旧语义（以前只有一个套），现在等同 body
                if (kind == MerchantKind.Fake)
                {
                    return new CmdResult(false, "假商人的身体美术固定为本套，不能借用商店商人的身体");
                }

                string value = args.Length > 2 ? args[2].ToLowerInvariant() : "toggle";
                skin.BodySet = SkeletonSets.Normalize(value == "toggle" ? SkeletonSets.Next(skin.BodySet) : value);
                return Commit($"{label} body art -> {skin.BodySet} ({SkeletonSetAssets.DescribePart(kind, "body", skin.BodySet)})");
            }

            case "hand-set":
            case "handset":
            {
                string value = args.Length > 2 ? args[2].ToLowerInvariant() : "toggle";
                skin.HandSet = SkeletonSets.Normalize(value == "toggle" ? SkeletonSets.Next(skin.HandSet) : value);
                skin.HandVariant = HandVariants.NormalizeFor(kind, skin.HandSet, skin.HandVariant);
                return Commit($"{label} hand art -> {skin.HandSet} ({SkeletonSetAssets.DescribePart(kind, "hand", skin.HandSet)}), hand={skin.HandVariant}");
            }

            case "leg":
            {
                if (!SettingsJson.CanToggleInventoryLeg(skin, MerchantKind.Real))
                {
                    return new CmdResult(false,
                        "腿部贴图只能在「本套美术 + 手部变体=手」时开关（当前：body=" + skin.BodySet
                        + ", handSet=" + skin.HandSet + ", hand=" + skin.HandVariant + "）");
                }

                string value = args.Length > 2 ? args[2].ToLowerInvariant() : "toggle";
                skin.ShowInventoryLeg = value switch
                {
                    "on" or "true" or "1" => true,
                    "off" or "false" or "0" => false,
                    _ => !skin.ShowInventoryLeg,
                };
                return Commit($"{label} inventory leg -> {(skin.ShowInventoryLeg ? "on" : "off")}");
            }

            default:
                return Usage();
        }
    }

    // -------------------------------------------------------------- legacy

    /// <summary>旧命令：同时改两个商人（旧版只有一个全局 HandVariant）。</summary>
    private static CmdResult ProcessLegacyPoint(string[] args)
    {
        string verb = args.Length > 1 ? args[1].ToLowerInvariant() : "status";

        if (verb == "status")
        {
            return new CmdResult(true,
                $"point (both): {SettingsHost.Current.RealMerchant.HandVariant}");
        }

        string current = SettingsHost.Current.RealMerchant.HandVariant;
        // 旧版 toggle 只有 hand/foot 两态，这里保持原语义（四态轮换是给新语法用的）
        string next = verb == "toggle"
            ? (current == HandVariants.Hand ? HandVariants.Foot : HandVariants.Hand)
            : verb;
        if (!HandVariants.IsKnown(next))
        {
            return new CmdResult(false, "Usage: merchant point hand|foot|white|black|toggle|status");
        }

        ModSettings settings = SettingsHost.Current;
        settings.RealMerchant.HandVariant =
            HandVariants.NormalizeFor(MerchantKind.Real, settings.RealMerchant.HandSet, next);
        settings.FakeMerchant.HandVariant =
            HandVariants.NormalizeFor(MerchantKind.Fake, settings.FakeMerchant.HandSet, next);


        return Commit($"both merchants hand -> {HandVariants.Normalize(next)} (legacy behaviour)");
    }

    private static CmdResult ProcessVoiceCommand(string[] args)
    {
        string verb = args.Length > 1 ? args[1].ToLowerInvariant() : "status";

        if (verb == "status")
        {
            return new CmdResult(true,
                $"voice: {SettingsHost.Current.Voice.Language}, db: {SettingsHost.Current.Voice.VolumeDb:+0.##;-0.##;0}");
        }

        if (verb == "db")
        {
            if (args.Length < 3)
            {
                return new CmdResult(false, "Usage: merchant voice db <value>");
            }

            if (!float.TryParse(args[2], System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out float parsed))
            {
                return new CmdResult(false, "Invalid db value");
            }

            SettingsHost.Current.Voice.VolumeDb = Mathf.Clamp(parsed, -12f, 12f);
            return Commit($"voice volume -> {SettingsHost.Current.Voice.VolumeDb} dB");
        }

        if (verb != "toggle" && verb != VoiceLanguages.Default && verb != VoiceLanguages.Japanese
            && verb != VoiceLanguages.Chinese && verb != "cn" && verb != "ja")
        {
            return new CmdResult(false, "Usage: merchant voice default|jp|zh|toggle|status|db <value>");
        }

        string current = SettingsHost.Current.Voice.Language;
        string next = verb switch
        {
            "toggle" => current == VoiceLanguages.Default
                ? VoiceLanguages.Japanese
                : current == VoiceLanguages.Japanese ? VoiceLanguages.Chinese : VoiceLanguages.Default,
            _ => VoiceLanguages.Normalize(verb),
        };

        SettingsHost.Current.Voice.Language = next;
        return Commit($"voice language -> {next}");
    }

    // -------------------------------------------------------------- shared

    // -------------------------------------------------------------- tuning

    /// <summary>
    /// 视觉微调（会话内生效，调好后请把数值反馈给作者固化进常量）：
    /// <code>
    /// merchant tune                                  列出四种组合当前数值
    /// merchant tune offset real|fake own|other dx dy 在当前位置上叠加偏移
    /// merchant tune set    real|fake own|other x y   直接设定偏移
    /// merchant tune scale  real|fake own|other value 设定缩放
    /// merchant tune reset [real|fake own|other]      恢复默认
    /// </code>
    /// </summary>
    private static CmdResult ProcessTuneCommand(string[] args)
    {
        if (args.Length <= 1)
        {
            return new CmdResult(true, MerchantVisualMotion.Describe());
        }

        if (string.Equals(args[1], "probe", StringComparison.OrdinalIgnoreCase))
        {
            return new CmdResult(true, MerchantVisualMotion.ProbeScene());
        }

        string action = args[1].ToLowerInvariant();

        if (action == "reset")
        {
            if (args.Length >= 4)
            {
                MerchantVisualMotion.Reset(ParseKey(args[2], args[3]));
            }
            else
            {
                MerchantVisualMotion.Reset();
            }

            MerchantVisualMotion.Save();
            MerchantVisualMotion.ReapplyAll();
            return new CmdResult(true, "reset -> " + MerchantVisualMotion.Describe());
        }

        if (args.Length < 4)
        {
            return new CmdResult(false, "Usage: merchant tune offset|set|scale real|fake own|other <values>");
        }

        string key = ParseKey(args[2], args[3]);
        if (key.Length == 0)
        {
            return new CmdResult(false, "Usage: merchant tune <offset|set|scale> real|fake own|other ...");
        }

        if (action is "offset" or "set")
        {
            if (args.Length < 6
                || !float.TryParse(args[4], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float dx)
                || !float.TryParse(args[5], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float dy))
            {
                return new CmdResult(false, "Usage: merchant tune " + action + " " + key + " <dx> <dy>");
            }

            if (action == "offset")
            {
                MerchantVisualMotion.AddOffsetDelta(key, new Vector2(dx, dy));
            }
            else
            {
                MerchantVisualMotion.SetOffset(key, new Vector2(dx, dy));
            }

            MerchantVisualMotion.Save();
            MerchantVisualMotion.ReapplyAll();
            return new CmdResult(true, key + " -> " + MerchantVisualMotion.Describe());
        }

        if (action == "scale")
        {
            if (args.Length < 5
                || !float.TryParse(args[4], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float scale))
            {
                return new CmdResult(false, "Usage: merchant tune scale " + key + " <value>");
            }

            MerchantVisualMotion.SetScale(key, scale);
            MerchantVisualMotion.Save();
            MerchantVisualMotion.ReapplyAll();
            return new CmdResult(true, key + " -> " + MerchantVisualMotion.Describe());
        }

        return new CmdResult(false, "Usage: merchant tune [offset|set|scale|reset] ...");
    }

    private static string ParseKey(string merchant, string art)
    {
        string m = merchant.ToLowerInvariant();
        string a = art.ToLowerInvariant();
        if (m != "real" && m != "fake")
        {
            return string.Empty;
        }

        if (a != "own" && a != "other")
        {
            return string.Empty;
        }

        return m + "/" + a;
    }

    private static CmdResult Commit(string message)
    {
        // Save + NotifyChanged：NotifyChanged 会触发 Changed 订阅（场景刷新 + BaseLib cfg 同步），
        // 保证控制台改的东西和其他入口完全一致。
        SettingsHost.SaveAndNotify();
        TaskHelper.RunSafely(MerchantRuntime.RefreshAllNextFrame());
        return new CmdResult(true, msg: message);
    }

    /// <summary>设置界面按钮用：改完立刻刷新场景里的商人。</summary>
    public static int ApplyNow()
    {
        SettingsHost.SaveAndNotify();
        TaskHelper.RunSafely(MerchantRuntime.RefreshAllNextFrame());
        return 0; // 具体更新数量由 RefreshAll 的日志给出
    }

    /// <summary>旧 UI 补丁仍在用的兼容入口。</summary>
    internal static int ApplyPointVariantNow()
    {
        return ApplyNow();
    }
}
