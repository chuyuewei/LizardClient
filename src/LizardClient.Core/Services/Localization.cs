using LizardClient.Core.Interfaces;

namespace LizardClient.Core.Services;

/// <summary>
/// 本地化辅助类 - 提供便捷的字符串本地化访问
/// </summary>
public static class Localization
{
    private static ILocalizationService? _service;

    /// <summary>
    /// 初始化本地化服务
    /// </summary>
    public static void Initialize(ILocalizationService localizationService)
    {
        _service = localizationService;
    }

    /// <summary>
    /// 获取本地化字符串
    ///</summary>
    public static string Get(string key, params object[] args)
    {
        if (_service == null)
        {
            // 如果服务未初始化，返回键本身
            return $"[{key}]";
        }

        return _service.GetString(key, args);
    }

    /// <summary>
    /// 尝试获取本地化字符串
    /// </summary>
    public static bool TryGet(string key, out string value)
    {
        if (_service == null)
        {
            value = $"[{key}]";
            return false;
        }

        return _service.TryGetString(key, out value);
    }

    /// <summary>
    /// 当前语言
    /// </summary>
    public static string CurrentLanguage => _service?.CurrentLanguage ?? "en-US";

    /// <summary>
    /// 设置语言
    /// </summary>
    public static void SetLanguage(string languageCode)
    {
        _service?.SetLanguage(languageCode);
    }

    /// <summary>
    /// 获取可用语言
    /// </summary>
    public static IEnumerable<string> GetAvailableLanguages()
    {
        return _service?.GetAvailableLanguages() ?? new[] { "en-US" };
    }

    // === 常用字符串快捷访问 ===

    public static class Common
    {
        public static string AppName => Get("Common.AppName");
        public static string OK => Get("Common.OK");
        public static string Cancel => Get("Common.Cancel");
        public static string Yes => Get("Common.Yes");
        public static string No => Get("Common.No");
        public static string Save => Get("Common.Save");
        public static string Load => Get("Common.Load");
        public static string Delete => Get("Common.Delete");
        public static string Error => Get("Common.Error");
        public static string Warning => Get("Common.Warning");
        public static string Info => Get("Common.Info");
        public static string Success => Get("Common.Success");
        public static string Failed => Get("Common.Failed");
        public static string Loading => Get("Common.Loading");
        public static string PleaseWait => Get("Common.PleaseWait");
    }

    // === 注入系统字符串 ===

    public static class Injection
    {
        public static string ProcessInjectorInitialized(int processId)
            => Get("Injection.ProcessInjectorInitialized", processId);

        public static string StartingDllInjection(string dllPath)
            => Get("Injection.StartingDllInjection", dllPath);

        public static string InjectionMethod(string method)
            => Get("Injection.InjectionMethod", method);

        public static string InjectionSuccess => Get("Injection.InjectionSuccess");
        public static string InjectionFailed => Get("Injection.InjectionFailed");
    }

    // === Hook 引擎字符串 ===

    public static class Hook
    {
        public static string EngineInitialized => Get("Hook.EngineInitialized");

        public static string DetourInstallSuccess(string hookInfo)
            => Get("Hook.DetourInstallSuccess", hookInfo);

        public static string HookUninstalled(string name)
            => Get("Hook.HookUninstalled", name);

        public static string HookEnabled(string name)
            => Get("Hook.HookEnabled", name);

        public static string HookDisabled(string name)
            => Get("Hook.HookDisabled", name);

        public static string EngineDisposed => Get("Hook.EngineDisposed");

        public static string AlreadyExists(string name)
            => Get("Hook.AlreadyExists", name);

        public static string NotFound(string name)
            => Get("Hook.NotFound", name);
    }

    // === Minecraft 集成字符串 ===

    public static class Minecraft
    {
        public static string HooksManagerInitialized(string version)
            => Get("Minecraft.HooksManagerInitialized", version);

        public static string StartInstallingHooks => Get("Minecraft.StartInstallingHooks");
        public static string GameLoopHookSuccess => Get("Minecraft.GameLoopHookSuccess");
        public static string RenderHookSuccess => Get("Minecraft.RenderHookSuccess");
        public static string InputHookSuccess => Get("Minecraft.InputHookSuccess");

        public static string HookInstallComplete(int success, int total)
            => Get("Minecraft.HookInstallComplete", success, total);

        public static string HooksUninstalled => Get("Minecraft.HooksUninstalled");
        public static string HooksManagerDisposed => Get("Minecraft.HooksManagerDisposed");
    }

    // === 内存管理字符串 ===

    public static class Memory
    {
        public static string ManagerInitialized(int processId)
            => Get("Memory.ManagerInitialized", processId);

        public static string AllocatedMemory(IntPtr address, int size)
            => Get("Memory.AllocatedMemory", address, size);

        public static string FreedMemory(IntPtr address)
            => Get("Memory.FreedMemory", address);

        public static string PatternScanSuccess(IntPtr address)
            => Get("Memory.PatternScanSuccess", address);

        public static string PatternScanFailed => Get("Memory.PatternScanFailed");
        public static string ManagerDisposed => Get("Memory.ManagerDisposed");
    }

    // === 自动更新系统字符串 ===

    public static class AutoUpdate
    {
        public static string StartAutoUpdate(string version)
            => Get("AutoUpdate.StartAutoUpdate", version);

        public static string StartingSignatureScan => Get("AutoUpdate.StartingSignatureScan");

        public static string OffsetsSavedToDatabase(int count)
            => Get("AutoUpdate.OffsetsSavedToDatabase", count);

        public static string DatabaseLoaded(int versions)
            => Get("AutoUpdate.DatabaseLoaded", versions);

        public static string DatabaseSaved => Get("AutoUpdate.DatabaseSaved");
    }
}
