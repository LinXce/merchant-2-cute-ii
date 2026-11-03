using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace Merchant2CuteII.script.Settings;

/// <summary>
/// 设置行的构造工具（原版设置界面的排版：居中标题、分组 + 分隔线、
/// 左字段名 + 右控件、行下灰色小字）。
///
/// 供子菜单页 <see cref="MerchantSettingsSubmenu"/> 使用；控件外观继承所在场景的 Theme。
/// </summary>
public static class SettingsRows
{
    public static void AddTitle(VBoxContainer column, string text)
    {
        var title = new Label
        {
            Text = text,
            HorizontalAlignment = HorizontalAlignment.Center,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
        };
        SettingsTheme.ApplyTitle(title);
        column.AddChild(title);
        column.AddChild(new Control { CustomMinimumSize = new Vector2(0, 8) });
    }

    public static void AddSection(VBoxContainer column, string title, string? hint)
    {
        column.AddChild(new Control { CustomMinimumSize = new Vector2(0, 10) });

        var label = new Label { Text = title, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        SettingsTheme.ApplySection(label);
        column.AddChild(label);

        if (!string.IsNullOrEmpty(hint))
        {
            column.AddChild(HintLabel(hint));
        }

        column.AddChild(new HSeparator());
    }

    public static OptionButton AddDropdownRow(
        VBoxContainer column,
        string label,
        Func<string[]> optionsProvider,
        Func<string, string> optionLabelProvider,
        Func<string> readCurrent,
        Action<string> onSelected,
        string? hint,
        List<Action>? refreshers = null)
    {
        var row = new HBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        row.AddThemeConstantOverride("separation", 12);

        var text = new Label
        {
            Text = label,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            VerticalAlignment = VerticalAlignment.Center,
        };
        SettingsTheme.ApplyField(text);

        var dropdown = new OptionButton
        {
            CustomMinimumSize = new Vector2(300, 40),
            SizeFlagsVertical = Control.SizeFlags.ShrinkCenter,
        };

        var built = new List<string>();
        void Rebuild(string[] options)
        {
            dropdown.Clear();
            built.Clear();
            for (int i = 0; i < options.Length; i++)
            {
                dropdown.AddItem(optionLabelProvider(options[i]), i);
                built.Add(options[i]);
            }
        }

        Rebuild(optionsProvider());
        dropdown.Selected = Math.Max(0, built.IndexOf(readCurrent()));
        dropdown.ItemSelected += index => onSelected(built[(int)index]);

        refreshers?.Add(() =>
        {
            if (!GodotObject.IsInstanceValid(dropdown))
            {
                return;
            }

            string[] options = optionsProvider();
            if (!built.SequenceEqual(options))
            {
                Rebuild(options);
            }

            int index = built.IndexOf(readCurrent());
            if (index >= 0)
            {
                dropdown.Selected = index;
            }
        });

        row.AddChild(text);
        row.AddChild(dropdown);
        column.AddChild(row);

        if (!string.IsNullOrEmpty(hint))
        {
            column.AddChild(HintLabel(hint));
        }

        column.AddChild(new HSeparator());
        return dropdown;
    }

    public static void AddStaticRow(VBoxContainer column, string label, string value, string? hint)
    {
        var row = new HBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        row.AddThemeConstantOverride("separation", 12);

        var text = new Label
        {
            Text = label,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            VerticalAlignment = VerticalAlignment.Center,
        };
        SettingsTheme.ApplyField(text);

        var valueLabel = new Label
        {
            Text = value,
            CustomMinimumSize = new Vector2(300, 0),
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Center,
        };
        SettingsTheme.ApplyValue(valueLabel);

        row.AddChild(text);
        row.AddChild(valueLabel);
        column.AddChild(row);

        if (!string.IsNullOrEmpty(hint))
        {
            column.AddChild(HintLabel(hint));
        }

        column.AddChild(new HSeparator());
    }

    public static CheckBox AddToggleRow(
        VBoxContainer column,
        string label,
        Func<bool> readValue,
        Func<bool> readEnabled,
        Action<bool> onToggled,
        Func<string> hintProvider,
        List<Action>? refreshers = null)
    {
        var row = new HBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        row.AddThemeConstantOverride("separation", 12);

        var text = new Label
        {
            Text = label,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            VerticalAlignment = VerticalAlignment.Center,
        };
        SettingsTheme.ApplyField(text);

        var check = new CheckBox
        {
            ButtonPressed = readValue(),
            Disabled = !readEnabled(),
            CustomMinimumSize = new Vector2(300, 40),
            SizeFlagsVertical = Control.SizeFlags.ShrinkCenter,
        };
        check.Toggled += pressed => onToggled(pressed);

        row.AddChild(text);
        row.AddChild(check);
        column.AddChild(row);

        Label hintLabel = HintLabel(hintProvider());
        column.AddChild(hintLabel);
        column.AddChild(new HSeparator());

        refreshers?.Add(() =>
        {
            if (!GodotObject.IsInstanceValid(check))
            {
                return;
            }

            bool enabled = readEnabled();
            check.Disabled = !enabled;

            // 显示的是**玩家偏好**（默认开启），禁用只表示"当前组合下不生效"，
            // 不要因为不可用就显示成未勾选，否则看起来像默认关闭。
            check.SetPressedNoSignal(readValue());
            hintLabel.Text = hintProvider();
        });

        return check;
    }

    public static void AddSliderRow(
        VBoxContainer column,
        string label,
        float min,
        float max,
        float step,
        Func<float> readValue,
        Func<float, string> formatter,
        Action<float> onChanged,
        List<Action>? refreshers = null)
    {
        var row = new HBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        row.AddThemeConstantOverride("separation", 12);

        var text = new Label
        {
            Text = label,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            VerticalAlignment = VerticalAlignment.Center,
        };
        SettingsTheme.ApplyField(text);

        var slider = new HSlider
        {
            MinValue = min,
            MaxValue = max,
            Step = step,
            Value = readValue(),
            CustomMinimumSize = new Vector2(240, 28),
            SizeFlagsVertical = Control.SizeFlags.ShrinkCenter,
        };

        var valueLabel = new Label
        {
            Text = formatter(readValue()),
            CustomMinimumSize = new Vector2(130, 0),
            VerticalAlignment = VerticalAlignment.Center,
        };

        slider.ValueChanged += v =>
        {
            valueLabel.Text = formatter((float)v);
            onChanged((float)v);
        };

        row.AddChild(text);
        row.AddChild(slider);
        row.AddChild(valueLabel);
        column.AddChild(row);
        column.AddChild(new HSeparator());

        refreshers?.Add(() =>
        {
            if (!GodotObject.IsInstanceValid(slider))
            {
                return;
            }

            float v = readValue();
            slider.SetValueNoSignal(v);
            valueLabel.Text = formatter(v);
        });
    }

    public static Label HintLabel(string hint)
    {
        var label = new Label
        {
            Text = hint,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
        };
        SettingsTheme.ApplyHint(label);
        return label;
    }

    /// <summary>把某个商人的所有设置行加进容器。</summary>
    public static void AddMerchantRows(VBoxContainer column, MerchantKind kind, List<Action>? refreshers = null)
    {
        if (SettingsText.BodySetOptions(kind).Length > 1)
        {
            AddDropdownRow(column, SettingsText.Field("bodySet"),
                () => SettingsText.BodySetOptions(kind),
                set => SettingsText.ArtOwner(SkeletonSetAssets.IsFakeBodyArt(kind, set)),
                () => SettingsHost.Current.For(kind).BodySet,
                value => Mutate(() => SettingsHost.Current.For(kind).BodySet = SkeletonSets.Normalize(value)),
                SettingsText.Hint("bodySet"), refreshers);
        }
        else
        {
            AddStaticRow(column, SettingsText.Field("bodySet"),
                SettingsText.ArtOwner(SkeletonSetAssets.IsFakeBodyArt(kind, SkeletonSets.Own)),
                SettingsText.Hint("bodyLocked"));
        }

        AddDropdownRow(column, SettingsText.Field("handSet"),
            () => SettingsText.HandSetOptions(kind),
            set => SettingsText.ArtOwner(SkeletonSetAssets.IsFakeHandArt(kind, set)),
            () => SettingsHost.Current.For(kind).HandSet,
            value => Mutate(() =>
            {
                MerchantSkinSettings skin = SettingsHost.Current.For(kind);
                skin.HandSet = SkeletonSets.Normalize(value);
                skin.HandVariant = HandVariants.NormalizeFor(kind, skin.HandSet, skin.HandVariant);
            }),
            SettingsText.Hint("handSet"), refreshers);

        AddDropdownRow(column, SettingsText.Field("handVariant"),
            () => SettingsText.HandVariantOptions(kind, SettingsHost.Current.For(kind).HandSet),
            v => SettingsText.Option(v),
            () => HandVariants.NormalizeFor(kind,
                SettingsHost.Current.For(kind).HandSet,
                SettingsHost.Current.For(kind).HandVariant),
            value => Mutate(() =>
            {
                MerchantSkinSettings skin = SettingsHost.Current.For(kind);
                skin.HandVariant = HandVariants.NormalizeFor(kind, skin.HandSet, value);
            }),
            SettingsText.Hint("handVariant"), refreshers);

        if (kind == MerchantKind.Real)
        {
            MerchantSkinSettings skin = SettingsHost.Current.RealMerchant;
            bool usable = SettingsJson.UsesOwnArt(skin, MerchantKind.Real);
            AddToggleRow(column, SettingsText.Field("showLeg"),
                () => SettingsHost.Current.RealMerchant.ShowInventoryLeg,
                () => SettingsJson.CanToggleInventoryLeg(SettingsHost.Current.RealMerchant, MerchantKind.Real),
                pressed => Mutate(() =>
                {
                    // 只有"手 + 本套美术"时才允许写入；其它组合保持玩家偏好不变
                    MerchantSkinSettings realSkin = SettingsHost.Current.RealMerchant;
                    if (SettingsJson.CanToggleInventoryLeg(realSkin, MerchantKind.Real))
                    {
                        realSkin.ShowInventoryLeg = pressed;
                    }
                }),
                () => !SettingsJson.UsesOwnArt(SettingsHost.Current.RealMerchant, MerchantKind.Real)
                    ? SettingsText.Hint("legUnavailable")
                    : SettingsJson.IsFootLike(SettingsHost.Current.RealMerchant.HandVariant)
                        ? SettingsText.Hint("legNeedsHand")
                        : SettingsText.Hint("leg"),
                refreshers);
        }
    }

    public static void AddVoiceRows(VBoxContainer column, List<Action>? refreshers = null)
    {
        AddDropdownRow(column, SettingsText.Field("language"),
            () => SettingsText.VoiceLanguageOptions,
            v => SettingsText.Option(v),
            () => SettingsHost.Current.Voice.Language,
            value => Mutate(() => SettingsHost.Current.Voice.Language = VoiceLanguages.Normalize(value)),
            null, refreshers);

        AddSliderRow(column, SettingsText.Field("volume"), -12, 12, 1,
            () => SettingsHost.Current.Voice.VolumeDb,
            db => $"{db:+0;-0;0} dB ({(float)Math.Pow(10d, db / 20d):0.00}x)",
            value => Mutate(() => SettingsHost.Current.Voice.VolumeDb = value),
            refreshers);
    }

    private static void Mutate(Action mutate)
    {
        try
        {
            SettingsHost.Mutate(_ => mutate());
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[Merchant2CuteII] applying setting failed: {ex}");
        }
    }
}
