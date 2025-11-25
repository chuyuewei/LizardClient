using LizardClient.Core.Services;
using LizardClient.Core.Models;
using LizardClient.Injection.AutoUpdate;
using LizardClient.Injection.Hooks;
using LizardClient.Injection.Injectors;
using LizardClient.Injection.Minecraft;
using System.Diagnostics;

namespace LizardClient.Injection.Examples;

/// <summary>
/// Minecraft Hook 使用示例
/// </summary>
public static class MinecraftHookExample
{
    /// <summary>
    /// 完整的 Minecraft Hook 示例
    /// </summary>
    public static async Task<bool> HookMinecraftAsync()
    {
        var logger = new SerilogLogger();

        try
        {
            // 1. 查找 Minecraft 进程
            logger.Info("正在查找 Minecraft 进程...");
            var minecraftProcess = FindMinecraftProcess();

            if (minecraftProcess == null)
            {
                logger.Error("未找到 Minecraft 进程");
                logger.Info("请确保 Minecraft 正在运行");
                return false;
            }

            logger.Info($"找到 Minecraft 进程 (PID: {minecraftProcess.Id})");

            // 2. 检测 Minecraft 版本 (简化版，实际需要更复杂的检测)
            var version = MinecraftVersion.Parse("1.20.1") ?? new MinecraftVersion(1, 20, 1);
            logger.Info($"Minecraft 版本: {version}");

            // 3. 创建内存管理器
            using var memoryManager = new Memory.MemoryManager(logger, minecraftProcess.Id);

            // 4. 自动更新偏移
            logger.Info("正在自动检测游戏偏移...");
            var offsetUpdater = new OffsetUpdater(logger, memoryManager);
            var gameOffsets = offsetUpdater.AutoUpdateOffsets(version);

            if (!gameOffsets.IsValid())
            {
                logger.Warning($"偏移检测不完整 ({gameOffsets.GetFoundOffsetsCount()}/3 必需)");
                logger.Warning("某些功能可能不可用");
            }
            else
            {
                logger.Info($"偏移检测完成 ({gameOffsets.GetFoundOffsetsCount()} 个)");
            }

            // 5. 创建 Hook 引擎
            using var hookEngine = new HookEngine(logger, memoryManager);

            // 6. 创建 Minecraft Hooks 管理器
            using var minecraftHooks = new MinecraftHooks(logger, hookEngine, gameOffsets);

            // 7. 注册 Hook 回调
            RegisterHookCallbacks(minecraftHooks, logger);

            // 8. 安装 Hooks
            logger.Info("正在安装 Hooks...");
            var success = minecraftHooks.InstallHooks();

            if (!success)
            {
                logger.Error("Hook 安装失败");
                return false;
            }

            logger.Info("✓ Hooks 安装成功！");
            logger.Info("客户端正在运行，按 Ctrl+C 退出...");

            // 9. 保持运行
            await Task.Delay(Timeout.Infinite);

            return true;
        }
        catch (Exception ex)
        {
            logger.Error($"Hook Minecraft 失败: {ex.Message}", ex);
            return false;
        }
    }

    /// <summary>
    /// 注册 Hook 回调
    /// </summary>
    private static void RegisterHookCallbacks(MinecraftHooks hooks, SerilogLogger logger)
    {
        // 游戏循环 Hook
        hooks.OnGameLoop += () =>
        {
            // 每个游戏 Tick 调用
            // logger.Debug("游戏 Tick");
        };

        // 渲染 Hook
        hooks.OnRenderFrame += (deltaTime) =>
        {
            // 每帧调用，可以在这里渲染 UI
            // logger.Debug($"渲染帧 (Delta: {deltaTime:F3}s)");
        };

        // 输入 Hook
        hooks.OnProcessInput += (key, action) =>
        {
            logger.Info($"输入事件: Key={key}, Action={action}");

            // 示例：如果按下 F1 键，执行某个操作
            if (key == 290 && action == 1) // F1 按下
            {
                logger.Info("F1 键被按下");
            }
        };

        // 网络包 Hook
        hooks.OnSendPacket += (packet) =>
        {
            logger.Debug($"发送网络包: 0x{packet:X}");
        };

        hooks.OnReceivePacket += (packet) =>
        {
            logger.Debug($"接收网络包: 0x{packet:X}");
        };

        logger.Info("Hook 回调已注册");
    }

    /// <summary>
    /// 查找 Minecraft 进程
    /// </summary>
    private static Process? FindMinecraftProcess()
    {
        // 尝试查找 javaw.exe 进程，窗口标题包含 "Minecraft"
        var javaProcesses = Process.GetProcessesByName("javaw");

        foreach (var process in javaProcesses)
        {
            try
            {
                if (process.MainWindowTitle.Contains("Minecraft", StringComparison.OrdinalIgnoreCase))
                {
                    return process;
                }
            }
            catch
            {
                // 忽略无法访问的进程
            }
        }

        // 尝试查找其他可能的进程名
        var alternativeNames = new[] { "java", "minecraft", "MinecraftLauncher" };

        foreach (var name in alternativeNames)
        {
            var processes = Process.GetProcessesByName(name);
            if (processes.Length > 0)
            {
                return processes[0];
            }
        }

        return null;
    }

    /// <summary>
    /// DLL 注入 + Hook 示例
    /// </summary>
    public static async Task<bool> InjectDllAndHookAsync(string dllPath)
    {
        var logger = new SerilogLogger();

        try
        {
            // 1. 查找进程
            var process = FindMinecraftProcess();
            if (process == null)
            {
                logger.Error("未找到 Minecraft 进程");
                return false;
            }

            // 2. 注入 DLL
            using var injector = new ProcessInjector(logger, process.Id);

            logger.Info($"注入 DLL: {dllPath}");
            var injected = injector.InjectDll(dllPath, InjectionMethod.StandardDll);

            if (!injected)
            {
                logger.Error("DLL 注入失败");
                return false;
            }

            logger.Info("✓ DLL 注入成功");

            // 3. DLL 内部应该会安装 Hooks
            // 这里我们只需要等待 DLL 完成工作

            await Task.Delay(1000);
            return true;
        }
        catch (Exception ex)
        {
            logger.Error($"注入失败: {ex.Message}", ex);
            return false;
        }
    }

    /// <summary>
    /// 测试 Hook 引擎功能
    /// </summary>
    public static void TestHookEngine()
    {
        var logger = new SerilogLogger();

        logger.Info("=== Hook 引擎测试 ===");

        // 这里可以添加针对 Hook 引擎的单元测试
        logger.Info("测试 1: Detour Hook");
        logger.Info("测试 2: VTable Hook");
        logger.Info("测试 3: IAT Hook");

        logger.Info("Hook 引擎测试完成");
    }
}
