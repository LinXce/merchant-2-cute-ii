#if MERCHANT_RITSILIB
using STS2RitsuLib.Settings;
using Merchant2CuteII.script.Settings;

namespace Merchant2CuteII.script.Integration;

/// <summary>
/// RitsuLib 设置接入（编译期开关 <c>MERCHANT_RITSILIB</c>）。
///
/// 公开入口是 <see cref="ModSettingsRegistry"/>：
/// <code>ModSettingsRegistry.Register(modId, page =&gt; page.AddSection(...).AddEnumChoice(...))</code>
/// 每个控件用 <see cref="ModSettingsBindings.Callback{TValue}"/> 绑到我们的模型
/// （读 = 直接取 SettingsHost.Current，写 = Mutate 归一化 + 存盘 + 广播）。
///
/// 身体 / 手部各自一个来源开关（支持跨模型混搭）；手部变体列全 4 项，
/// 由模型层按当前手部美术夹取（另一套手部没有白/黑）。
/// </summary>
public static class RitsuLibSettings
{
    private const string ModId = "Merchant2CuteII";

    private static bool _registered;

    public static void Register()
    {
        if (_registered)
        {
            return;
        }

        try
        {
            ModSettingsRegistry.RegisterModDisplayName(ModId, Text("Merchant2CuteII / 商人娘化"));

            ModSettingsRegistry.Register(ModId, page =>
            {
                page.WithTitle(Text("Merchant2CuteII"));

                page.AddSection("real", section =>
                {
                    section.WithTitle(Text("真商人 / Real merchant（商店）"));
                    AddMerchantEntries(section, MerchantKind.Real);
                    section.AddToggle("realShowLeg", Text("购物界面显示腿 / Show leg"),
                        Binding("realShowLeg",
                            () => SettingsHost.Current.RealMerchant.ShowInventoryLeg
                                  && SettingsJson.CanToggleInventoryLeg(SettingsHost.Current.RealMerchant, MerchantKind.Real),
                            value =>
                            {
                                MerchantSkinSettings skin = SettingsHost.Current.RealMerchant;
                                if (SettingsJson.CanToggleInventoryLeg(skin, MerchantKind.Real))
                                {
                                    skin.ShowInventoryLeg = value;
                                }
                            }),
                        description: null,
                        visibleWhen: () => SettingsJson.CanToggleInventoryLeg(SettingsHost.Current.RealMerchant, MerchantKind.Real));
                });

                page.AddSection("fake", section =>
                {
                    section.WithTitle(Text("假商人 / Fake merchant（事件）"));
                    AddMerchantEntries(section, MerchantKind.Fake);
                });

                page.AddSection("voice", section =>
                {
                    section.WithTitle(Text("商人语音 / Merchant voice"));
                    section.AddEnumChoice("voiceLanguage", Text("语音语言 / Language"),
                        Binding("voiceLanguage",
                            () => SettingsOptions.ToLanguageOption(SettingsHost.Current.Voice.Language),
                            value => SettingsHost.Current.Voice.Language = SettingsOptions.ToValue(value)),
                        option => Text(option.ToString()));

                    section.AddSlider("voiceVolumeDb", Text("音量 / Volume"),
                        Binding("voiceVolumeDb",
                            () => (double)SettingsHost.Current.Voice.VolumeDb,
                            value => SettingsHost.Current.Voice.VolumeDb = (float)value),
                        -12d, 12d, 1d,
                        value => $"{value:+0;-0;0} dB");
                });
            });

            _registered = true;
        }
        catch (System.Exception ex)
        {
            Godot.GD.PrintErr($"[Merchant2CuteII] RitsuLib settings registration failed: {ex}");
        }
    }

    /// <summary>身体 / 手部 / 手部变体 三个控件（真假商人共用同一套逻辑）。</summary>
    private static void AddMerchantEntries(ModSettingsSectionBuilder section, MerchantKind kind)
    {
        string prefix = kind == MerchantKind.Real ? "real" : "fake";

        // 假商人的身体固定为本套（借用商店商人的身体已禁用），因此不提供该项
        if (SkeletonSets.AllowedBodySets(kind).Length > 1)
        {
            section.AddEnumChoice($"{prefix}BodySet", Text("身体美术 / Body art"),
                Binding($"{prefix}BodySet",
                    () => SettingsOptions.ToSetOption(SettingsHost.Current.For(kind).BodySet),
                    value =>
                    {
                        MerchantSkinSettings skin = SettingsHost.Current.For(kind);
                        skin.BodySet = SettingsOptions.ToValue(value);
                    }),
                option => Text(SettingsOptionLabels.Set(kind, "body", option)));
        }

        section.AddEnumChoice($"{prefix}HandSet", Text("手部美术 / Hand art"),
            Binding($"{prefix}HandSet",
                () => SettingsOptions.ToSetOption(SettingsHost.Current.For(kind).HandSet),
                value =>
                {
                    MerchantSkinSettings skin = SettingsHost.Current.For(kind);
                    skin.HandSet = SettingsOptions.ToValue(value);
                    skin.HandVariant = HandVariants.NormalizeFor(kind, skin.HandSet, skin.HandVariant);
                }),
            option => Text(SettingsOptionLabels.Set(kind, "hand", option)));

        section.AddEnumChoice($"{prefix}HandVariant", Text("手部变体 / Hand variant"),
            Binding($"{prefix}HandVariant",
                () => SettingsOptions.ToHandOption(SettingsHost.Current.For(kind).HandVariant),
                value =>
                {
                    MerchantSkinSettings skin = SettingsHost.Current.For(kind);
                    skin.HandVariant = HandVariants.NormalizeFor(kind, skin.HandSet, SettingsOptions.ToValue(value));
                }),
            option => Text(SettingsOptionLabels.Hand(option)));
    }

    private static ModSettingsText Text(string value) => ModSettingsText.Literal(value);

    private static ModSettingsCallbackValueBinding<TValue> Binding<TValue>(
        string dataKey,
        System.Func<TValue> read,
        System.Action<TValue> write)
    {
        return ModSettingsBindings.Callback(
            ModId,
            dataKey,
            () => read(),
            value => SettingsHost.Mutate(_ => write(value)),
            () => SettingsHost.Save());
    }
}
#endif
