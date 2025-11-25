namespace LizardClient.Core.Interfaces;

/// <summary>
/// 本地化服务接口
/// </summary>
public interface ILocalizationService
{
    /// <summary>
    /// 获取本地化字符串
    /// </summary>
    /// <param name="key">资源键</param>
    /// <param name="args">格式化参数</param>
    /// <returns>本地化后的字符串</returns>
    string GetString(string key, params object[] args);

    /// <summary>
    /// 尝试获取本地化字符串
    /// </summary>
    /// <param name="key">资源键</param>
    /// <param name="value">输出的本地化字符串</param>
    /// <returns>是否找到对应的键</returns>
    bool TryGetString(string key, out string value);

    /// <summary>
    /// 当前语言代码
    /// </summary>
    string CurrentLanguage { get; }

    /// <summary>
    /// 设置语言
    /// </summary>
    /// <param name="languageCode">语言代码 (例如: "en-US", "zh-CN")</param>
    void SetLanguage(string languageCode);

    /// <summary>
    /// 获取可用的语言列表
    /// </summary>
    IEnumerable<string> GetAvailableLanguages();

    /// <summary>
    /// 重新加载资源文件
    /// </summary>
    void ReloadResources();

    /// <summary>
    /// 语言更改事件
    /// </summary>
    event EventHandler<LanguageChangedEventArgs>? LanguageChanged;
}

/// <summary>
/// 语言更改事件参数
/// </summary>
public sealed class LanguageChangedEventArgs : EventArgs
{
    public string OldLanguage { get; init; } = string.Empty;
    public string NewLanguage { get; init; } = string.Empty;
}
