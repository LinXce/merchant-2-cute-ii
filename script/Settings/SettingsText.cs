using System.Collections.Generic;
using Godot;

namespace Merchant2CuteII.script.Settings;

/// <summary>
/// 设置界面的文案。双语并排，且**不使用"另一套/另一个"这种相对说法**，
/// 一律用美术归属来称呼：商店商人美术 / 假商人美术。
/// </summary>
public static class SettingsText
{
    public const string Title = "商人外观设置";

    public static string Section(string key) => key switch
    {
        "real" => "商店商人",
        "fake" => "假商人",
        "voice" => "商人语音",
        _ => key,
    };

    public static string Field(string key) => key switch
    {
        "bodySet" => "身体美术",
        "handSet" => "手部美术",
        "handVariant" => "手部变体",
        "showLeg" => "商店界面显示腿部贴图",
        "language" => "语音语言",
        "volume" => "语音音量",
        _ => key,
    };

    public static string Option(string key) => key switch
    {
        "hand" => "手",
        "foot" => "裸足",
        "white" => "白丝",
        "black" => "黑丝",
        "default" => "原版",
        "jp" => "日语",
        "zh" => "中文",
        _ => key,
    };

    /// <summary>美术来源显示名：直接说这套美术属于谁，而不是"本套 / 另一套"。</summary>
    public static string ArtOwner(bool fakeArt)
    {
        return fakeArt ? "假商人美术" : "商店商人美术";
    }

    public static string Hint(string key) => key switch
    {
        "real" => "商店里出现的商人。身体与手部可以分别选择使用哪一套美术。",
        "fake" => "事件里出现的商人。手部可以选择使用哪一套美术。",
        "bodySet" => "身体（含商店界面）使用的美术。",
        "handSet" => "手部使用的美术；白丝、黑丝只有在使用商店商人的手部美术时才可选。",
        "handVariant" => "可选项会跟随上面选择的手部美术变化。",
        "leg" => "手部变体为“手”时可开启；裸足 / 白丝 / 黑丝 时固定不显示。",
        "legNeedsHand" => "只有在手部变体为“手”时才可以开关这张腿部贴图。",
        "legUnavailable" => "这张腿部贴图属于商店商人美术，选用假商人美术时不可用。",
        "bodyLocked" => "假商人的身体固定使用假商人美术。",
        "voice" => "替换商人的语音。",
        "whiteBlack" => "白丝、黑丝两种变体只有商店商人的手部美术提供。",
        _ => string.Empty,
    };

    /// <summary>当前手部来源下真正可用的变体。</summary>
    public static string[] HandVariantOptions(MerchantKind kind, string handSet) => HandVariants.Allowed(kind, handSet);

    public static string[] BodySetOptions(MerchantKind kind) => SkeletonSets.AllowedBodySets(kind);

    public static string[] HandSetOptions(MerchantKind kind) => SkeletonSets.All;

    public static string[] VoiceLanguageOptions => VoiceLanguages.All;

    public static IReadOnlyList<string> All => new[] { Title };
}
