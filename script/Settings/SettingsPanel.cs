using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace Merchant2CuteII.script.Settings;

/// <summary>
/// 游戏内设置面板。
///
/// 视觉：尽量贴近原版设置界面 —— 直接**继承游戏自带的 Theme**（从当前场景里任一
/// 带 Theme 的 Control 取），排版参考原版：居中标题、分组标题 + 分隔线、
/// 左侧说明文字 + 右侧控件、下方灰色小字提示。
///
/// 说明：原版的设置行控件（<c>NSettingsDropdown</c> / <c>NSettingsSlider</c>）是
/// abstract 且内容写死在各子类里，无法在运行时复用；因此这里用同一套 Theme 下的
/// 标准控件复刻排版。
///
/// 同步：读写 <see cref="SettingsHost"/>.Current，订阅 <see cref="SettingsHost.Changed"/>
/// 做静默刷新（不触发信号，避免回声）。
/// </summary>
public static class SettingsPanel
{
    public const string PanelName = "Merchant2CuteII_SettingsPanel";

    private static readonly List<Action> Refreshers = new();
    private static bool _subscribed;

    public static void Toggle(Node host)
    {
        if (host == null)
        {
            return;
        }

        Node? existing = host.GetTree()?.Root?.FindChild(PanelName, true, false);
        if (existing != null)
        {
            existing.QueueFree();
            Refreshers.Clear();
            return;
        }

        Open(host);
    }

    public static void Open(Node host)
    {
        try
        {
            EnsureSubscribed();
            Refreshers.Clear();

            Window window = Build(host);
            window.TreeExited += () => Refreshers.Clear();

            Node root = host.GetTree()?.Root ?? host;
            root.AddChild(window);
            window.PopupCentered(new Vector2I(1000, 820));
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[Merchant2CuteII] Failed to open settings panel: {ex}");
        }
    }

    private static void EnsureSubscribed()
    {
        if (_subscribed)
        {
            return;
        }

        SettingsHost.Changed += RefreshOpenPanel;
        _subscribed = true;
    }

    private static void RefreshOpenPanel()
    {
        if (Refreshers.Count == 0)
        {
            return;
        }

        var alive = new List<Action>(Refreshers.Count);
        foreach (Action refresh in Refreshers)
        {
            try
            {
                refresh();
                alive.Add(refresh);
            }
            catch (Exception ex)
            {
                GD.PrintErr($"[Merchant2CuteII] Panel refresh failed: {ex.Message}");
            }
        }

        Refreshers.Clear();
        Refreshers.AddRange(alive);
    }

    // ---------------------------------------------------------------- build

    private static Window Build(Node host)
    {
        var window = new AcceptDialog
        {
            Name = PanelName,
            Title = string.Empty,
            Size = new Vector2I(980, 800),
            Exclusive = false,
            Theme = FindVanillaTheme(host),
        };

        var margin = new MarginContainer { CustomMinimumSize = new Vector2(960, 770) };
        margin.AddThemeConstantOverride("margin_left", 44);
        margin.AddThemeConstantOverride("margin_right", 44);
        margin.AddThemeConstantOverride("margin_top", 28);
        margin.AddThemeConstantOverride("margin_bottom", 28);

        var scroll = new ScrollContainer
        {
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            SizeFlagsVertical = Control.SizeFlags.ExpandFill,
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
        };

        var column = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        column.AddThemeConstantOverride("separation", 6);
        scroll.AddChild(column);
        margin.AddChild(scroll);
        window.AddChild(margin);

        // 居中大标题（对齐原版设置页的标题排版）
        var title = new Label
        {
            Text = SettingsText.Title,
            HorizontalAlignment = HorizontalAlignment.Center,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
        };
        SettingsTheme.ApplyTitle(title);
        column.AddChild(title);
        column.AddChild(new Control { CustomMinimumSize = new Vector2(0, 10) });

        AddSection(column, SettingsText.Section("real"), SettingsText.Hint("real"));
        AddMerchantRows(column, MerchantKind.Real);

        AddSection(column, SettingsText.Section("fake"), SettingsText.Hint("fake"));
        AddMerchantRows(column, MerchantKind.Fake);

        AddSection(column, SettingsText.Section("voice"), SettingsText.Hint("voice"));
        AddVoiceRows(column);

        column.AddChild(new Control { CustomMinimumSize = new Vector2(0, 8) });

        return window;
    }

    private static void AddSection(VBoxContainer column, string title, string hint)
    {
        column.AddChild(new Control { CustomMinimumSize = new Vector2(0, 14) });

        var label = new Label { Text = title, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        SettingsTheme.ApplySection(label);
        column.AddChild(label);

        if (!string.IsNullOrEmpty(hint))
        {
            column.AddChild(HintLabel(hint));
        }

        column.AddChild(new HSeparator());
    }

    private static void AddMerchantRows(VBoxContainer column, MerchantKind kind)
    {
        // 身体美术：假商人的身体固定（借用已禁用）
        if (SettingsText.BodySetOptions(kind).Length > 1)
        {
            AddRow(column, SettingsText.Field("bodySet"),
                () => SettingsText.BodySetOptions(kind),
                set => SettingsText.ArtOwner(SkeletonSetAssets.IsFakeBodyArt(kind, set)),
                () => SettingsHost.Current.For(kind).BodySet,
                value => Apply(() => SettingsHost.Current.For(kind).BodySet = SkeletonSets.Normalize(value)),
                SettingsText.Hint("bodySet"));
        }
        else
        {
            AddStaticRow(column, SettingsText.Field("bodySet"),
                SettingsText.ArtOwner(SkeletonSetAssets.IsFakeBodyArt(kind, SkeletonSets.Own)),
                SettingsText.Hint("bodyLocked"));
        }

        // 手部美术
        AddRow(column, SettingsText.Field("handSet"),
            () => SettingsText.HandSetOptions(kind),
            set => SettingsText.ArtOwner(SkeletonSetAssets.IsFakeHandArt(kind, set)),
            () => SettingsHost.Current.For(kind).HandSet,
            value => Apply(() =>
            {
                MerchantSkinSettings skin = SettingsHost.Current.For(kind);
                skin.HandSet = SkeletonSets.Normalize(value);
                skin.HandVariant = HandVariants.NormalizeFor(kind, skin.HandSet, skin.HandVariant);
            }),
            SettingsText.Hint("handSet"));

        // 手部变体（选项随手部美术变化）
        AddRow(column, SettingsText.Field("handVariant"),
            () => SettingsText.HandVariantOptions(kind, SettingsHost.Current.For(kind).HandSet),
            v => SettingsText.Option(v),
            () => HandVariants.NormalizeFor(kind,
                SettingsHost.Current.For(kind).HandSet,
                SettingsHost.Current.For(kind).HandVariant),
            value => Apply(() =>
            {
                MerchantSkinSettings skin = SettingsHost.Current.For(kind);
                skin.HandVariant = HandVariants.NormalizeFor(kind, skin.HandSet, value);
            }),
            SettingsText.Hint("handVariant"));

        // 商店界面的腿部贴图（仅商店商人）
        if (kind == MerchantKind.Real)
        {
            AddToggleRow(column, SettingsText.Field("showLeg"), SettingsText.Hint("leg"));
        }
    }

    private static void AddVoiceRows(VBoxContainer column)
    {
        AddRow(column, SettingsText.Field("language"),
            () => SettingsText.VoiceLanguageOptions,
            v => SettingsText.Option(v),
            () => SettingsHost.Current.Voice.Language,
            value => Apply(() => SettingsHost.Current.Voice.Language = VoiceLanguages.Normalize(value)),
            null);

        AddSliderRow(column, SettingsText.Field("volume"));
    }

    // ---------------------------------------------------------------- rows

    /// <summary>一行：左侧字段名 + 右侧下拉框 + 下方灰色说明 + 分隔线。</summary>
    private static void AddRow(
        VBoxContainer column,
        string label,
        Func<string[]> optionsProvider,
        Func<string, string> optionLabelProvider,
        Func<string> readCurrent,
        Action<string> onSelected,
        string? hint)
    {
        var row = new HBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        row.AddThemeConstantOverride("separation", 12);

        var text = new Label
        {
            Text = label,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            VerticalAlignment = VerticalAlignment.Center,
        };
        text.AddThemeFontSizeOverride("font_size", 18);

        var dropdown = new OptionButton
        {
            CustomMinimumSize = new Vector2(320, 40),
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

        row.AddChild(text);
        row.AddChild(dropdown);
        column.AddChild(row);

        if (!string.IsNullOrEmpty(hint))
        {
            column.AddChild(HintLabel(hint));
        }

        column.AddChild(new HSeparator());

        Refreshers.Add(() =>
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
    }

    /// <summary>只读一行（用于被锁定的项）。</summary>
    private static void AddStaticRow(VBoxContainer column, string label, string value, string? hint = null)
    {
        var row = new HBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        row.AddThemeConstantOverride("separation", 12);

        var text = new Label
        {
            Text = label,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            VerticalAlignment = VerticalAlignment.Center,
        };
        text.AddThemeFontSizeOverride("font_size", 18);

        var valueLabel = new Label
        {
            Text = value,
            CustomMinimumSize = new Vector2(320, 0),
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

    private static void AddToggleRow(VBoxContainer column, string label, string hint)
    {
        var row = new HBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        row.AddThemeConstantOverride("separation", 12);

        var text = new Label
        {
            Text = label,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            VerticalAlignment = VerticalAlignment.Center,
        };
        text.AddThemeFontSizeOverride("font_size", 18);

        MerchantSkinSettings realSkin = SettingsHost.Current.RealMerchant;
        bool usable = SettingsJson.CanToggleInventoryLeg(realSkin, MerchantKind.Real);

        var check = new CheckBox
        {
            ButtonPressed = realSkin.ShowInventoryLeg,
            Disabled = !usable,
            CustomMinimumSize = new Vector2(320, 40),
            SizeFlagsVertical = Control.SizeFlags.ShrinkCenter,
        };
        check.Toggled += pressed => Apply(() => SettingsHost.Current.RealMerchant.ShowInventoryLeg = pressed);

        row.AddChild(text);
        row.AddChild(check);
        column.AddChild(row);

        var hintLabel = HintLabel(hint);
        column.AddChild(hintLabel);
        column.AddChild(new HSeparator());

        Refreshers.Add(() =>
        {
            if (!GodotObject.IsInstanceValid(check))
            {
                return;
            }

            MerchantSkinSettings current = SettingsHost.Current.RealMerchant;
            bool available = SettingsJson.UsesOwnArt(current, MerchantKind.Real);
            check.Disabled = !available;
            check.ButtonPressed = current.ShowInventoryLeg && available;
            hintLabel.Text = available ? hint : SettingsText.Hint("legUnavailable");
        });
    }

    private static void AddSliderRow(VBoxContainer column, string label)
    {
        var row = new HBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        row.AddThemeConstantOverride("separation", 12);

        var text = new Label
        {
            Text = label,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            VerticalAlignment = VerticalAlignment.Center,
        };
        text.AddThemeFontSizeOverride("font_size", 18);

        var slider = new HSlider
        {
            MinValue = -12,
            MaxValue = 12,
            Step = 1,
            Value = SettingsHost.Current.Voice.VolumeDb,
            CustomMinimumSize = new Vector2(260, 28),
            SizeFlagsVertical = Control.SizeFlags.ShrinkCenter,
        };
        var valueLabel = new Label
        {
            Text = FormatDb(SettingsHost.Current.Voice.VolumeDb),
            CustomMinimumSize = new Vector2(130, 0),
            VerticalAlignment = VerticalAlignment.Center,
        };

        slider.ValueChanged += value =>
        {
            valueLabel.Text = FormatDb((float)value);
            Apply(() => SettingsHost.Current.Voice.VolumeDb = (float)value);
        };

        row.AddChild(text);
        row.AddChild(slider);
        row.AddChild(valueLabel);
        column.AddChild(row);
        column.AddChild(new HSeparator());

        Refreshers.Add(() =>
        {
            if (!GodotObject.IsInstanceValid(slider))
            {
                return;
            }

            float db = SettingsHost.Current.Voice.VolumeDb;
            slider.SetValueNoSignal(db);
            valueLabel.Text = FormatDb(db);
        });
    }

    private static Label HintLabel(string hint)
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

    // --------------------------------------------------------------- utils

    /// <summary>从当前场景里挑一个带 Theme 的 Control，继承游戏原生外观。</summary>
    private static Theme? FindVanillaTheme(Node host)
    {
        try
        {
            Node? current = host;
            while (current != null)
            {
                if (current is Control { Theme: not null } control)
                {
                    return control.Theme;
                }

                current = current.GetParent();
            }

            SceneTree? tree = Engine.GetMainLoop() as SceneTree;
            if (tree?.Root is Window { Theme: not null } rootWindow)
            {
                return rootWindow.Theme;
            }
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[Merchant2CuteII] theme lookup failed: {ex.Message}");
        }

        return null;
    }

    private static string FormatDb(float db)
    {
        float gain = (float)Math.Pow(10d, db / 20d);
        return $"{db:+0;-0;0} dB ({gain:0.00}x)";
    }

    private static void Apply(Action mutate)
    {
        try
        {
            SettingsHost.Mutate(_ => mutate());
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[Merchant2CuteII] Applying setting failed: {ex}");
        }
    }
}
