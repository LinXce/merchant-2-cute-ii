using System;
using Godot;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using System.Reflection;
using MegaCrit.Sts2.Core.Nodes.Screens.MainMenu;
using MegaCrit.Sts2.Core.Nodes.Screens.Settings;

namespace Merchant2CuteII.script.Settings;

/// <summary>
/// 在**游戏设置界面**（设置 → 游戏设置 分页）里，紧跟在原版「Modding」那一行之后
/// 插入一行「商人外观」，右侧是「打开设置」按钮，点开 <see cref="SettingsPanel"/>。
///
/// 做法：整行**复制**原版那一行（含分隔线、按钮的样式/贴图/字体），
/// 只改文案并重新接线，因此外观与原生设置行完全一致。
/// </summary>
public static class MerchantSettingsEntry
{
    public const string RowName = "Merchant2CuteIIRow";
    public const string DividerName = "Merchant2CuteIIDivider";
    public const string PageName = "Merchant2CuteIISettingsPage";

    private const string RowLabel = "「商人外观」";
    private const string ButtonLabel = "打开设置";

    /// <summary>在设置界面里注入入口行（重复调用安全）。</summary>
    public static void Inject(Node screen)
    {
        if (screen == null || !GodotObject.IsInstanceValid(screen))
        {
            return;
        }

        if (screen.FindChild(RowName, true, false) != null)
        {
            return;
        }

        // 原版锚点：%Modding 行 + 它上面的分隔线
        Control? moddingRow = screen.GetNodeOrNull<Control>("%Modding");
        Control? moddingDivider = screen.GetNodeOrNull<Control>("%ModdingDivider");
        if (moddingRow == null || moddingRow.GetParent() == null)
        {
            GD.PrintErr("[Merchant2CuteII] settings screen: %Modding row not found, skip entry injection");
            return;
        }

        try
        {
            Node parent = moddingRow.GetParent();
            int index = moddingRow.GetIndex();

            // 1) 分隔线（复制原版，保持一致）
            if (moddingDivider != null && moddingDivider.GetParent() == parent)
            {
                var dividerCopy = (Control)moddingDivider.Duplicate();
                dividerCopy.Name = DividerName;
                dividerCopy.UniqueNameInOwner = false;
                dividerCopy.Visible = true;
                parent.AddChild(dividerCopy);
                parent.MoveChild(dividerCopy, index + 1);
                index++;
            }

            // 2) 整行复制（含 Label 与 Button 的样式）
            var rowCopy = (Control)moddingRow.Duplicate();
            rowCopy.Name = RowName;
            rowCopy.UniqueNameInOwner = false;
            rowCopy.Visible = true;
            parent.AddChild(rowCopy);
            parent.MoveChild(rowCopy, index + 1);

            // 3) 改文案
            SetRowText(rowCopy, RowLabel);

            // 4) 重新接线：按钮点开我们的设置面板
            Node? button = rowCopy.GetNodeOrNull("ModdingButton");
            if (button == null)
            {
                GD.PrintErr("[Merchant2CuteII] settings row: copied button not found");
                return;
            }

            button.UniqueNameInOwner = false;
            WireButton(button, screen, rowCopy);

            GD.Print("[Merchant2CuteII] settings entry row injected");
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[Merchant2CuteII] settings entry injection failed: {ex}");
        }
    }

    /// <summary>
    /// 打开设置页：把子菜单页推进设置界面的子菜单栈（与快速SL、原版 Modding 页一致），
    /// 不是弹窗。拿不到栈时退回弹窗面板。
    /// </summary>
    private static void OpenSettingsPage(Node screen)
    {
        try
        {
            FieldInfo? stackField = typeof(NSubmenu).GetField("_stack",
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            var stack = stackField?.GetValue(screen) as NSubmenuStack;

            if (stack == null)
            {
                GD.PrintErr("[Merchant2CuteII] submenu stack not found, falling back to popup panel");
                SettingsPanel.Open(screen);
                return;
            }

            // 复用已有页面：反复开关设置不该不断新建节点（隐藏页会常驻并保留事件订阅）
            var page = stack.GetNodeOrNull<MerchantSettingsSubmenu>(PageName);
            if (page == null || !GodotObject.IsInstanceValid(page))
            {
                page = new MerchantSettingsSubmenu { Name = PageName };
                stack.AddChild(page);
            }

            if (ReferenceEquals(stack.Peek(), page))
            {
                return; // 已经在栈顶，别重复压入
            }

            stack.Push(page);
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[Merchant2CuteII] opening settings page failed: {ex}");
            SettingsPanel.Open(screen);
        }
    }

    private static void SetRowText(Node row, string text)
    {
        Node? label = row.GetNodeOrNull("Label");
        if (label == null)
        {
            return;
        }

        // 原版这一列是 MegaRichTextLabel（RichTextLabel 子类）
        if (label is RichTextLabel rich)
        {
            rich.Text = text;
            return;
        }

        label.Set("text", text);
    }

    private static void WireButton(Node button, Node screen, Node row)
    {
        // 复制品自带 NOpenModdingScreenButton 脚本，其 _Ready 会把按钮文字改成「模组设置」，
        // 因此在进入树后（下一帧）再改文字并接线。
        Callable.From(() =>
        {
            try
            {
                if (!GodotObject.IsInstanceValid(button))
                {
                    return;
                }

                // 复制出来的按钮没有任何 released 连接（原版只把 %ModdingButton 连到 OpenModdingScreen），
                // 所以这里不需要断开——早期版本盲目 Disconnect 会刷
                // "Attempt to disconnect a nonexistent connection" 报错。

                // 按钮内文字
                Node? innerLabel = button.GetNodeOrNull("Label");
                if (innerLabel != null)
                {
                    if (innerLabel is RichTextLabel richLabel)
                    {
                        richLabel.Text = ButtonLabel;
                    }
                    else
                    {
                        innerLabel.Set("text", ButtonLabel);
                    }
                }

                // 先接线（关键一步，不能被后面的可选项打断）
                button.Connect(NClickableControl.SignalName.Released,
                    Callable.From<NClickableControl>(_ => OpenSettingsPage(screen)));

                // 复制品可能继承了"禁用"状态，尽量恢复可点（失败不影响接线）
                try
                {
                    // 注意：Enable() 是 C# 方法，Godot 的 Call() 调不到，必须强转直接调用
                    if (button is NClickableControl clickable && !clickable.IsEnabled)
                    {
                        clickable.Enable();
                    }
                }
                catch (Exception ex)
                {
                    GD.Print($"[Merchant2CuteII] settings button enable skipped: {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                GD.PrintErr($"[Merchant2CuteII] settings row wiring failed: {ex.Message}");
            }
        }).CallDeferred();
    }
}
