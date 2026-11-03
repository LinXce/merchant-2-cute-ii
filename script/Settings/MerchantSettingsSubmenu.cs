using System;
using System.Collections.Generic;
using Godot;
using MegaCrit.Sts2.Core.Nodes.Screens.MainMenu;

namespace Merchant2CuteII.script.Settings;

/// <summary>
/// 「商人外观」子菜单页 —— 推进设置界面的子菜单栈里的一整页（不是弹窗）。
///
/// 与快速SL、原版 Modding 页一致：占据设置内容区，自带原版返回按钮。
///
/// 同步策略：**只刷新不重建**。设置变更时对已存在的控件做静默刷新
/// （<c>Selected</c> / <c>ButtonPressed</c> / <c>SetValueNoSignal</c>，都不触发信号），
/// 避免"改动 → 整页重建 → 正在操作的控件被销毁 → 焦点跳走 / 行重复"。
/// </summary>
public partial class MerchantSettingsSubmenu : NSubmenu
{
    private const string BackButtonScene = "res://scenes/ui/back_button.tscn";

    private VBoxContainer? _content;
    private readonly List<Action> _refreshers = new();

    /// <summary>没有明确的首选焦点时不强抢焦点。</summary>
    protected override Control? InitialFocusedControl => null;

    public override void _Ready()
    {
        BuildBackButton();
        BuildLayout();
        BuildRows();

        // NSubmenu 约定：子类自己调用 ConnectSignals()（base._Ready() 会抛异常）
        ConnectSignals();

        // ConnectSignals 会先 Disable 返回按钮，等 VisibilityChanged 才 Enable；
        // Push() 只会把它设为可见 —— 先置 false，让 Push 触发那次变化，返回键才可点。
        Visible = false;
        MouseFilter = MouseFilterEnum.Ignore;

        SettingsHost.Changed += OnSettingsChanged;
    }

    public override void _ExitTree()
    {
        SettingsHost.Changed -= OnSettingsChanged;
    }

    private void OnSettingsChanged()
    {
        foreach (Action refresh in _refreshers)
        {
            try
            {
                refresh();
            }
            catch (Exception ex)
            {
                GD.PrintErr($"[Merchant2CuteII] settings page refresh failed: {ex.Message}");
            }
        }
    }

    // ---------------------------------------------------------------- build

    private void BuildBackButton()
    {
        var scene = ResourceLoader.Load<PackedScene>(BackButtonScene);
        if (scene == null)
        {
            GD.PrintErr("[Merchant2CuteII] back button scene missing: " + BackButtonScene);
            AddChild(new Button { Name = "BackButton" });
            return;
        }

        Node instance = scene.Instantiate();
        instance.Name = "BackButton";
        AddChild(instance);
    }

    private void BuildLayout()
    {
        AnchorRight = 1f;
        AnchorBottom = 1f;

        var margin = new MarginContainer { MouseFilter = MouseFilterEnum.Ignore };
        margin.SetAnchorsPreset(LayoutPreset.FullRect);
        // 四周留白：内容不贴屏幕边缘（原版设置页也是居中的窄列）
        margin.AddThemeConstantOverride("margin_left", 200);
        margin.AddThemeConstantOverride("margin_right", 200);
        margin.AddThemeConstantOverride("margin_top", 72);
        margin.AddThemeConstantOverride("margin_bottom", 56);

        var scroll = new ScrollContainer
        {
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            SizeFlagsVertical = Control.SizeFlags.ExpandFill,
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
        };

        _content = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        _content.AddThemeConstantOverride("separation", 6);

        scroll.AddChild(_content);
        margin.AddChild(scroll);
        AddChild(margin);
    }

    private void BuildRows()
    {
        if (_content == null)
        {
            return;
        }

        _refreshers.Clear();

        SettingsRows.AddTitle(_content, SettingsText.Title);

        SettingsRows.AddSection(_content, SettingsText.Section("real"), SettingsText.Hint("real"));
        SettingsRows.AddMerchantRows(_content, MerchantKind.Real, _refreshers);

        SettingsRows.AddSection(_content, SettingsText.Section("fake"), SettingsText.Hint("fake"));
        SettingsRows.AddMerchantRows(_content, MerchantKind.Fake, _refreshers);

        SettingsRows.AddSection(_content, SettingsText.Section("voice"), SettingsText.Hint("voice"));
        SettingsRows.AddVoiceRows(_content, _refreshers);

        _content.AddChild(new Control { CustomMinimumSize = new Vector2(0, 24) });
    }
}
