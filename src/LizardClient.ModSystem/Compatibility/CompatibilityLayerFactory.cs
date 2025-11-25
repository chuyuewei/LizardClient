using LizardClient.Core.Interfaces;

namespace LizardClient.ModSystem.Compatibility;

/// <summary>
/// 兼容层工厂
/// 根据模组类型创建合适的兼容层
/// </summary>
public sealed class CompatibilityLayerFactory
{
    private readonly List<IModCompatibilityLayer> _layers;
    private readonly ILogger _logger;

    public CompatibilityLayerFactory(ILogger logger)
    {
        _logger = logger;
        _layers = new List<IModCompatibilityLayer>
        {
            new ForgeCompatibilityLayer(logger),
            new FabricCompatibilityLayer(logger)
        };
    }

    /// <summary>
    /// 查找能够处理该模组的兼容层
    /// </summary>
    public IModCompatibilityLayer? FindCompatibleLayer(string modPath)
    {
        foreach (var layer in _layers)
        {
            if (layer.CanHandle(modPath))
            {
                _logger.Info($"Found compatible layer: {layer.Name} for {Path.GetFileName(modPath)}");
                return layer;
            }
        }

        return null;
    }

    /// <summary>
    /// 获取所有兼容层
    /// </summary>
    public IReadOnlyList<IModCompatibilityLayer> GetAllLayers()
    {
        return _layers.AsReadOnly();
    }

    /// <summary>
    /// 添加自定义兼容层
    /// </summary>
    public void AddLayer(IModCompatibilityLayer layer)
    {
        _layers.Add(layer);
        _logger.Info($"Added compatibility layer: {layer.Name}");
    }
}
