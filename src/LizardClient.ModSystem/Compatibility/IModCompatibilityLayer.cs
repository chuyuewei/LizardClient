namespace LizardClient.ModSystem.Compatibility;

/// <summary>
/// 模组兼容层接口
/// 用于适配不同mod加载器的mod到LizardClient
/// </summary>
public interface IModCompatibilityLayer
{
    /// <summary>
    /// 兼容层名称
    /// </summary>
    string Name { get; }

    /// <summary>
    /// 检查是否可以处理该模组
    /// </summary>
    bool CanHandle(string modPath);

    /// <summary>
    /// 解析模组元数据
    /// </summary>
    Models.ModMetadata? ParseMetadata(string modPath);

    /// <summary>
    /// 加载模组
    /// </summary>
    API.IMod? LoadMod(string modPath, Models.ModMetadata metadata);
}
