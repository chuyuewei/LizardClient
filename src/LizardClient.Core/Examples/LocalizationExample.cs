using LizardClient.Core.Interfaces;
using LizardClient.Core.Models;
using LizardClient.Core.Services;

namespace LizardClient.Core.Examples;

/// <summary>
/// 本地化服务使用示例
/// </summary>
public static class LocalizationExample
{
    /// <summary>
    /// 基本使用示例
    /// </summary>
    public static async Task BasicUsageAsync()
    {
        // 1. 创建依赖服务
        var logger = new SerilogLogger();
        var configService = new JsonConfigurationService(logger);

        // 2. 创建本地化服务
        var localizationService = new LocalizationService(logger, configService);

        // 3. 初始化静态辅助类
        Localization.Initialize(localizationService);

        // 4. 使用本地化字符串
        Console.WriteLine(Localization.Common.AppName);
        Console.WriteLine(Localization.Common.Loading);

        // 5. 带参数的本地化
        Console.WriteLine(Localization.Injection.ProcessInjectorInitialized(12345));
        Console.WriteLine(Localization.Memory.ManagerInitialized(12345));

        // 6. 切换语言
        Console.WriteLine($"当前语言: {Localization.CurrentLanguage}");

        Console.WriteLine("\n切换到英语:");
        Localization.SetLanguage("en-US");
        Console.WriteLine(Localization.Common.AppName);
        Console.WriteLine(Localization.Injection.InjectionSuccess);

        Console.WriteLine("\n切换到中文:");
        Localization.SetLanguage("zh-CN");
        Console.WriteLine(Localization.Common.AppName);
        Console.WriteLine(Localization.Injection.InjectionSuccess);

        // 7. 获取可用语言
        Console.WriteLine("\n可用语言:");
        foreach (var lang in Localization.GetAvailableLanguages())
        {
            Console.WriteLine($"  - {lang}");
        }

        // 8. 直接使用服务
        var customString = localizationService.GetString("Hook.EngineInitialized");
        Console.WriteLine($"\n直接访问: {customString}");

        // 9. 尝试获取字符串
        if (Localization.TryGet("Hook.HookEnabled", out var hookEnabled))
        {
            Console.WriteLine($"找到字符串: {hookEnabled}");
        }
    }

    /// <summary>
    /// 在实际应用中的使用示例
    /// </summary>
    public static void RealWorldUsage(ILogger logger)
    {
        // 替换硬编码的字符串
        // 之前: logger.Info("Hook 引擎已初始化");
        // 现在:
        logger.Info(Localization.Hook.EngineInitialized);

        // 替换带参数的字符串
        int processId = 12345;
        // 之前: logger.Info($"内存管理器已初始化 (进程 ID: {processId})");
        // 现在:
        logger.Info(Localization.Memory.ManagerInitialized(processId));

        // 替换成功/失败消息
        bool success = true;
        logger.Info(success ? Localization.Common.Success : Localization.Common.Failed);
    }

    /// <summary>
    /// 监听语言更改事件
    /// </summary>
    public static void LanguageChangedEventExample(ILocalizationService localizationService)
    {
        localizationService.LanguageChanged += (sender, e) =>
        {
            Console.WriteLine($"语言已更改: {e.OldLanguage} -> {e.NewLanguage}");

            // 可以在这里更新 UI
            // UpdateUI();
        };

        // 切换语言会触发事件
        localizationService.SetLanguage("en-US");
        localizationService.SetLanguage("zh-CN");
    }

    /// <summary>
    /// 自定义资源目录示例
    /// </summary>
    public static void CustomResourceDirectoryExample()
    {
        var logger = new SerilogLogger();
        var configService = new JsonConfigurationService(logger);

        // 使用自定义资源目录
        var customResourcePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "LizardClient", "Localization");

        var localizationService = new LocalizationService(logger, configService, customResourcePath);

        Console.WriteLine($"资源目录: {customResourcePath}");
        Console.WriteLine($"当前语言: {localizationService.CurrentLanguage}");
    }
}
