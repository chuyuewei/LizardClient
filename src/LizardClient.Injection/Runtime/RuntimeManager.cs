using LizardClient.Core.Interfaces;
using LizardClient.Injection.Hooks;
using LizardClient.ModSystem.Loader;

namespace LizardClient.Injection.Runtime;

/// <summary>
/// 运行时管理器 - 负责在注入后初始化模组系统和钩子
/// </summary>
public sealed class RuntimeManager : IDisposable
{
    private readonly ILogger _logger;
    private readonly ModLoader _modLoader;
    private RenderHook? _renderHook;
    private InputHook? _inputHook;
    private bool _initialized;

    public RuntimeManager(ILogger logger)
    {
        _logger = logger;
        _modLoader = new ModLoader(logger);
    }

    /// <summary>
    /// 初始化运行时环境
    /// </summary>
    /// <param name="modsDirectory">模组目录路径</param>
    public void Initialize(string modsDirectory)
    {
        if (_initialized) return;

        try
        {
            _logger.Info("正在初始化运行时环境...");

            // 1. 加载模组
            _modLoader.LoadModsFromDirectory(modsDirectory);
            _logger.Info($"已加载 {_modLoader.LoadedMods.Count} 个模组");

            // 2. 初始化钩子
            _renderHook = new RenderHook(_logger, _modLoader);
            _inputHook = new InputHook(_logger, _modLoader);

            // 3. 安装钩子
            _renderHook.Install();
            _inputHook.Install();

            _initialized = true;
            _logger.Info("运行时环境初始化完成");
        }
        catch (Exception ex)
        {
            _logger.Error($"运行时初始化失败: {ex.Message}", ex);
        }
    }

    public void Dispose()
    {
        if (!_initialized) return;

        try
        {
            _logger.Info("正在关闭运行时环境...");

            // 卸载钩子
            _renderHook?.Dispose();
            _inputHook?.Dispose();

            // 卸载模组
            _modLoader.UnloadAllMods();

            _initialized = false;
            _logger.Info("运行时环境已关闭");
        }
        catch (Exception ex)
        {
            _logger.Error($"关闭运行时环境失败: {ex.Message}", ex);
        }
    }
}
