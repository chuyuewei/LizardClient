namespace LizardClient.Core.Models;

/// <summary>
/// 语言信息
/// </summary>
public sealed class Language
{
    /// <summary>
    /// 语言代码 (例如: "en-US", "zh-CN")
    /// </summary>
    public string Code { get; init; } = string.Empty;

    /// <summary>
    /// 语言名称 (本地化显示，例如: "English", "中文")
    /// </summary>
    public string NativeName { get; init; } = string.Empty;

    /// <summary>
    /// 语言的英文名称
    /// </summary>
    public string EnglishName { get; init; } = string.Empty;

    /// <summary>
    /// 是否为从右至左的语言
    /// </summary>
    public bool IsRightToLeft { get; init; }

    /// <summary>
    /// 图标/旗帜 emoji (可选)
    /// </summary>
    public string? Icon { get; init; }

    public override string ToString() => $"{NativeName} ({Code})";

    public override bool Equals(object? obj)
    {
        return obj is Language other && Code.Equals(other.Code, StringComparison.OrdinalIgnoreCase);
    }

    public override int GetHashCode()
    {
        return Code.ToLowerInvariant().GetHashCode();
    }

    /// <summary>
    /// 预定义语言
    /// </summary>
    public static class Predefined
    {
        public static readonly Language English = new()
        {
            Code = "en-US",
            NativeName = "English",
            EnglishName = "English",
            IsRightToLeft = false,
            Icon = "🇺🇸"
        };

        public static readonly Language Chinese = new()
        {
            Code = "zh-CN",
            NativeName = "简体中文",
            EnglishName = "Simplified Chinese",
            IsRightToLeft = false,
            Icon = "🇨🇳"
        };

        public static readonly Language ChineseTraditional = new()
        {
            Code = "zh-TW",
            NativeName = "繁體中文",
            EnglishName = "Traditional Chinese",
            IsRightToLeft = false,
            Icon = "🇹🇼"
        };

        public static readonly Language Japanese = new()
        {
            Code = "ja-JP",
            NativeName = "日本語",
            EnglishName = "Japanese",
            IsRightToLeft = false,
            Icon = "🇯🇵"
        };

        public static readonly Language Korean = new()
        {
            Code = "ko-KR",
            NativeName = "한국어",
            EnglishName = "Korean",
            IsRightToLeft = false,
            Icon = "🇰🇷"
        };

        public static IEnumerable<Language> GetAll()
        {
            yield return English;
            yield return Chinese;
            yield return ChineseTraditional;
            yield return Japanese;
            yield return Korean;
        }
    }
}
