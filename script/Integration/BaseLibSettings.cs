#if MERCHANT_BASELIB
using BaseLib.Config;
using Merchant2CuteII.script.Settings;

namespace Merchant2CuteII.script.Integration;

/// <summary>
/// BaseLib 设置接入（编译期开关 <c>MERCHANT_BASELIB</c>）。
///
/// BaseLib 的 <see cref="ModConfig"/> 只接受 <b>static</b> 属性（见 BaseLib 源码
/// <c>CheckConfigProperties</c>），enum 属性会自动生成下拉框、带 <c>[ConfigSlider]</c> 的数值生成滑条。
///
/// 每个属性都直接代理到 <see cref="SettingsHost"/>.Current，于是四个入口
/// （BaseLib 界面 / 自带面板 / RitsuLib 页面 / 控制台）读写同一份内存模型。
///
/// 身体和手部是两个独立开关（支持跨模型混搭）；手部变体的可用集合取决于手部美术，
/// 这里列全部 4 项，由 <see cref="HandVariants.NormalizeFor"/> 兜底夹取。
/// </summary>
public sealed class MerchantBaseLibConfig : SimpleModConfig
{
    private static MerchantBaseLibConfig? instance;

    public static bool Registered { get; private set; }

    /// <summary>由 MainFile 调用一次。重复调用无副作用。</summary>
    public static void Register()
    {
        if (Registered)
        {
            return;
        }

        // 注意 1：BaseLib 3.4.7 的 ModConfig 没有静态 SaveDebounced<T>()，所以自持实例。
        // 注意 2：ctor 里的 Init() 会读 BaseLib 自己的 cfg 并写回静态属性，
        //         若该文件是旧值会覆盖玩家刚在别处改的配置 → 构造后必须用我们的 json 重新载入。
        instance = new MerchantBaseLibConfig();
        SettingsHost.Load();
        instance.Save();

        // 任何入口改了设置都同步 BaseLib 的 cfg，避免下次启动被旧 cfg 反向覆盖。
        SettingsHost.Changed += () => instance?.Save();

        ModConfigRegistry.Register("Merchant2CuteII", instance);
        Registered = true;
    }

    // ---------------------------------------------------------- real merchant

    [ConfigSection("RealMerchant")]
    public static SkeletonSetOption RealBodySet
    {
        get => SettingsOptions.ToSetOption(SettingsHost.Current.RealMerchant.BodySet);
        set => Set(() => SettingsHost.Current.RealMerchant.BodySet = SettingsOptions.ToValue(value));
    }

    public static SkeletonSetOption RealHandSet
    {
        get => SettingsOptions.ToSetOption(SettingsHost.Current.RealMerchant.HandSet);
        set => Set(() =>
        {
            MerchantSkinSettings skin = SettingsHost.Current.RealMerchant;
            skin.HandSet = SettingsOptions.ToValue(value);
            skin.HandVariant = HandVariants.NormalizeFor(MerchantKind.Real, skin.HandSet, skin.HandVariant);
        });
    }

    public static HandVariantOption RealHandVariant
    {
        get => SettingsOptions.ToHandOption(SettingsHost.Current.RealMerchant.HandVariant);
        set => Set(() =>
        {
            MerchantSkinSettings skin = SettingsHost.Current.RealMerchant;
            skin.HandVariant = HandVariants.NormalizeFor(MerchantKind.Real, skin.HandSet, SettingsOptions.ToValue(value));
        });
    }

    /// <summary>购物界面腿：借用另一套美术时不可用（读取恒为 false，写入被忽略）。</summary>
    public static bool RealShowInventoryLeg
    {
        get
        {
            MerchantSkinSettings skin = SettingsHost.Current.RealMerchant;
            return skin.ShowInventoryLeg && SettingsJson.CanToggleInventoryLeg(skin, MerchantKind.Real);
        }
        set => Set(() =>
        {
            MerchantSkinSettings skin = SettingsHost.Current.RealMerchant;
            if (SettingsJson.CanToggleInventoryLeg(skin, MerchantKind.Real))
            {
                skin.ShowInventoryLeg = value;
            }
        });
    }

    // ---------------------------------------------------------- fake merchant

    // 假商人的身体美术固定为本套，故不提供该项（借用商店商人的身体已禁用）。

    [ConfigSection("FakeMerchant")]
    public static SkeletonSetOption FakeHandSet
    {
        get => SettingsOptions.ToSetOption(SettingsHost.Current.FakeMerchant.HandSet);
        set => Set(() =>
        {
            MerchantSkinSettings skin = SettingsHost.Current.FakeMerchant;
            skin.HandSet = SettingsOptions.ToValue(value);
            skin.HandVariant = HandVariants.NormalizeFor(MerchantKind.Fake, skin.HandSet, skin.HandVariant);
        });
    }

    public static HandVariantOption FakeHandVariant
    {
        get => SettingsOptions.ToHandOption(SettingsHost.Current.FakeMerchant.HandVariant);
        set => Set(() =>
        {
            MerchantSkinSettings skin = SettingsHost.Current.FakeMerchant;
            skin.HandVariant = HandVariants.NormalizeFor(MerchantKind.Fake, skin.HandSet, SettingsOptions.ToValue(value));
        });
    }

    // ---------------------------------------------------------------- voice

    [ConfigSection("MerchantVoice")]
    public static VoiceLanguageOption VoiceLanguage
    {
        get => SettingsOptions.ToLanguageOption(SettingsHost.Current.Voice.Language);
        set => Set(() => SettingsHost.Current.Voice.Language = SettingsOptions.ToValue(value));
    }

    [ConfigSlider(min: -12, max: 12, step: 1, Format = "{0:+0;-0;0} dB")]
    public static float VoiceVolumeDb
    {
        get => SettingsHost.Current.Voice.VolumeDb;
        set => Set(() => SettingsHost.Current.Voice.VolumeDb = System.Math.Clamp(value, -12f, 12f));
    }

    // 动画（投毒 / idle 之类）属于实现细节，不作为可配置项暴露给玩家。

    private static void Set(System.Action mutate)
    {
        SettingsHost.Mutate(_ => mutate());
    }
}
#endif
