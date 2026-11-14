using System.Collections.Generic;

namespace Merchant2CuteII.script.Settings;

/// <summary>设置界面支持的语言。</summary>
public enum UiLanguage
{
    English = 0,
    SimplifiedChinese = 1,
    TraditionalChinese = 2,
}

/// <summary>
/// 设置界面的文案，**跟随游戏语言**（由 <see cref="SettingsLocale"/> 从
/// <c>LocManager.Instance.Language</c> 读出并写入 <see cref="Language"/>）。
///
/// 命名约定：不使用"本套/另一套/另一个"这种相对说法，一律用美术归属来称呼
/// （商店商人美术 / 假商人美术）。
///
/// 本文件**不依赖 Godot**，可被单测直接编译（语言通过 <see cref="Language"/> 注入）。
/// </summary>
public static class SettingsText
{
    /// <summary>当前界面语言。默认英文（游戏默认语言也是 eng）。</summary>
    public static UiLanguage Language { get; set; } = UiLanguage.English;

    private static bool IsChinese => Language != UiLanguage.English;

    private static string T(string en, string zh)
    {
        return IsChinese ? zh : en;
    }

    public static string Title => T("Merchant Appearance", "商人外观设置");

    public static string Section(string key) => key switch
    {
        "real" => T("Shop merchant", "商店商人"),
        "fake" => T("Reverse merchant", "假商人"),
        "voice" => T("Merchant voice", "商人语音"),
        _ => key,
    };

    public static string Field(string key) => key switch
    {
        "bodySet" => T("Body art", "身体美术"),
        "handSet" => T("Hand art", "手部美术"),
        "handVariant" => T("Hand variant", "手部变体"),
        "showLeg" => T("Show leg texture in shop UI", "商店界面显示腿部贴图"),
        "language" => T("Voice language", "语音语言"),
        "volume" => T("Voice volume", "语音音量"),
        _ => key,
    };

    public static string Option(string key) => key switch
    {
        "hand" => T("Hand", "手"),
        "foot" => T("Barefoot", "裸足"),
        "white" => T("White stockings", "白丝"),
        "black" => T("Black stockings", "黑丝"),
        "default" => T("Vanilla", "原版"),
        "jp" => T("Japanese", "日语"),
        "zh" => T("Chinese", "中文"),
        _ => key,
    };

    /// <summary>美术来源显示名：直接说这套美术属于谁。</summary>
    public static string ArtOwner(bool fakeArt)
    {
        return fakeArt
            ? T("Reverse merchant art", "假商人美术")
            : T("Shop merchant art", "商店商人美术");
    }

    public static string Hint(string key) => key switch
    {
        "real" => T(
            "The merchant you meet in shops. Body art and hand art can be chosen independently.",
            "商店里出现的商人。身体与手部可以分别选择使用哪一套美术。"),
        "fake" => T(
            "The merchant from the reverse-merchant event. Only the hand art can be changed.",
            "事件里出现的商人。手部可以选择使用哪一套美术。"),
        "bodySet" => T(
            "Art used for the body (including the shop UI).",
            "身体（含商店界面）使用的美术。"),
        "handSet" => T(
            "Art used for the hands. White / black stockings are only available with the shop merchant's hand art.",
            "手部使用的美术；白丝、黑丝只有在使用商店商人的手部美术时才可选。"),
        "handVariant" => T(
            "Options follow the hand art selected above.",
            "可选项会跟随上面选择的手部美术变化。"),
        "leg" => T(
            "Available when the hand variant is Hand; hidden for barefoot / white / black.",
            "手部变体为“手”时可开启；裸足 / 白丝 / 黑丝 时固定不显示。"),
        "legNeedsHand" => T(
            "Only available when the hand variant is Hand.",
            "只有在手部变体为“手”时才可以开关这张腿部贴图。"),
        "legUnavailable" => T(
            "This leg texture belongs to the shop merchant art; unavailable while using the reverse merchant art.",
            "这张腿部贴图属于商店商人美术，选用假商人美术时不可用。"),
        "bodyLocked" => T(
            "The reverse merchant's body always uses its own art.",
            "假商人的身体固定使用假商人美术。"),
        "voice" => T(
            "Replaces the merchant's voice lines.",
            "替换商人的语音。"),
        "whiteBlack" => T(
            "White and black stockings are only provided by the shop merchant's hand art.",
            "白丝、黑丝两种变体只有商店商人的手部美术提供。"),
        _ => string.Empty,
    };

    /// <summary>当前手部来源下真正可用的变体。</summary>
    public static string[] HandVariantOptions(MerchantKind kind, string handSet) => HandVariants.Allowed(kind, handSet);

    public static string[] BodySetOptions(MerchantKind kind) => SkeletonSets.AllowedBodySets(kind);

    public static string[] HandSetOptions(MerchantKind kind) => SkeletonSets.All;

    public static string[] VoiceLanguageOptions => VoiceLanguages.All;

    public static IReadOnlyList<string> All => new[] { Title };
}
