using System;
using Godot;
using MegaCrit.Sts2.Core.DevConsole;
using MegaCrit.Sts2.Core.DevConsole.ConsoleCommands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Screens.Shops;

namespace MegaCrit.Sts2.Core.DevConsole.ConsoleCommands;

public class MerchantConsoleCmd : AbstractConsoleCmd
{
    public override string CmdName => "merchant";

    public override string Args => "<hand|foot|toggle|status|voice>";

    public override string Description => "Switch merchant skeleton or voice variant";

    public override bool IsNetworked => false;

    private const string VariantFile = "user://merchant2cute_variant.txt";
    private const string VoiceFile = "user://merchant2cute_voice.txt";

    static MerchantConsoleCmd()
    {
        try
        {
            LoadPersistedSetting(VariantFile, v => Merchant2CuteII.script.ModConfig.Options.HandVariant = v, "hand", "foot");
            LoadPersistedSetting(VoiceFile, v => Merchant2CuteII.script.ModConfig.Options.MerchantVoiceVariant = v, "default", "jp", "zh");

            // apply at startup
            ApplyToExistingHands();
            UpdateLegVisibility(!Merchant2CuteII.script.ModConfig.Options.UseFoot);
        }
        catch { }
    }

    public override CmdResult Process(Player? issuingPlayer, string[] args)
    {
        string verb = args.Length > 0 ? args[0].ToLowerInvariant() : "status";

        if (verb == "status")
        {
            return new CmdResult(success: true, msg: $"Merchant variant: {Merchant2CuteII.script.ModConfig.Options.HandVariant}, voice: {Merchant2CuteII.script.ModConfig.Options.MerchantVoiceVariant}");
        }

        if (verb == "voice")
        {
            return ProcessVoiceCommand(args);
        }

        if (verb != "hand" && verb != "foot" && verb != "toggle")
        {
            return new CmdResult(success: false, msg: "Usage: merchant hand|foot|toggle|status|voice default|jp|toggle|status");
        }

        string current = Merchant2CuteII.script.ModConfig.Options.HandVariant;
        string next = verb == "toggle" ? (current == "hand" ? "foot" : "hand") : verb;

        Merchant2CuteII.script.ModConfig.Options.HandVariant = next;

        // persist
        try
        {
            var fa = Godot.FileAccess.Open(VariantFile, Godot.FileAccess.ModeFlags.Write);
            if (fa != null)
            {
                fa.StoreLine(next);
                fa.Close();
            }
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[Merchant2CuteII] Failed to save variant: {ex.Message}");
        }

        int updatedCount = ApplyToExistingHands();
        TaskHelper.RunSafely(ApplyToExistingHandsNextFrame());
        try
        {
            bool showLeg = !Merchant2CuteII.script.ModConfig.Options.UseFoot;
            UpdateLegVisibility(showLeg);
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[Merchant2CuteII] Error updating leg visibility: {ex.Message}");
        }

        return new CmdResult(success: true, msg: $"Set merchant variant to {next}. Updated {updatedCount} node(s).");
    }

    private static CmdResult ProcessVoiceCommand(string[] args)
    {
        string verb = args.Length > 1 ? args[1].ToLowerInvariant() : "status";

        if (verb == "status")
        {
            return new CmdResult(success: true, msg: $"Merchant voice: {Merchant2CuteII.script.ModConfig.Options.MerchantVoiceVariant}");
        }

        if (verb != "default" && verb != "jp" && verb != "zh" && verb != "toggle")
        {
            return new CmdResult(success: false, msg: "Usage: merchant voice default|jp|zh|toggle|status");
        }

        string current = Merchant2CuteII.script.ModConfig.Options.MerchantVoiceVariant;
        string next;
        if (verb == "toggle")
        {
            // cycle: default -> jp -> zh -> default
            next = current == "default" ? "jp" : current == "jp" ? "zh" : "default";
        }
        else
        {
            next = verb;
        }
        Merchant2CuteII.script.ModConfig.Options.MerchantVoiceVariant = next;

        try
        {
            var fa = Godot.FileAccess.Open(VoiceFile, Godot.FileAccess.ModeFlags.Write);
            if (fa != null)
            {
                fa.StoreLine(next);
                fa.Close();
            }
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[Merchant2CuteII] Failed to save voice variant: {ex.Message}");
        }

        return new CmdResult(success: true, msg: $"Set merchant voice to {next}.");
    }

    private static int ApplyToExistingHands()
    {
        SceneTree? tree = Engine.GetMainLoop() as SceneTree;
        if (tree == null || tree.Root == null)
            return 0;

        int updated = 0;
        FindAndApplyRecursive(tree.Root, ref updated);
        return updated;
    }

    private static async System.Threading.Tasks.Task ApplyToExistingHandsNextFrame()
    {
        SceneTree? tree = Engine.GetMainLoop() as SceneTree;
        if (tree == null || tree.Root == null)
            return;

        await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        ApplyToExistingHands();
        UpdateLegVisibility(!Merchant2CuteII.script.ModConfig.Options.UseFoot);
    }

    private static void FindAndApplyRecursive(Node root, ref int updated)
    {
        if (root == null)
            return;

        foreach (Node child in root.GetChildren())
        {
            try
            {
                if (child is NMerchantHand merchantHand)
                {
                    if (TryApplyToHand(merchantHand))
                    {
                        updated++;
                    }
                }
            }
            catch { }

            FindAndApplyRecursive(child, ref updated);
        }
    }

    private static bool TryApplyToHand(NMerchantHand hand)
    {
        try
        {
            return Merchant2CuteII.script.AnimationHelper.TryApplyVariantToHandNode(hand);
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[Merchant2CuteII] AnimationHelper failed: {ex.Message}");
        }

        return false;
    }


    private static void UpdateLegVisibility(bool show)
    {
        SceneTree? tree = Engine.GetMainLoop() as SceneTree;
        if (tree == null || tree.Root == null)
            return;

        UpdateLegVisibilityStatic(tree.Root, show);
    }

    private static void UpdateLegVisibilityStatic(Node root, bool show)
    {
        if (root == null)
            return;

        foreach (Node child in root.GetChildren())
        {
            try
            {
                if (child is TextureRect tr && tr.Name == "MerchantInventoryLeg")
                {
                    tr.Visible = show;
                }
            }
            catch { }

            UpdateLegVisibilityStatic(child, show);
        }
    }

    private static void LoadPersistedSetting(string filePath, Action<string> setter, params string[] allowedValues)
    {
        var fa = Godot.FileAccess.Open(filePath, Godot.FileAccess.ModeFlags.Read);
        if (fa == null)
        {
            return;
        }

        try
        {
            string value = fa.GetLine().Trim().ToLowerInvariant();
            foreach (string allowed in allowedValues)
            {
                if (value == allowed)
                {
                    setter(value);
                    break;
                }
            }
        }
        finally
        {
            fa.Close();
        }
    }

}

