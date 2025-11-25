using LizardClient.Core.Services;
using LizardClient.Injection.Injectors;
using LizardClient.Injection.Memory;
using System.Diagnostics;

namespace LizardClient.Injection;

/// <summary>
/// 注入系统使用示例
/// </summary>
public static class InjectionExample
{
    /// <summary>
    /// 完整的 DLL 注入示例
    /// </summary>
    public static async Task<bool> InjectIntoMinecraftAsync(string dllPath)
    {
        var logger = new SerilogLogger();
        
        try
        {
            // 1. 查找 Minecraft 进程
            logger.Info("查找 Minecraft 进程...");
            var minecraftProcess = Process.GetProcessesByName("javaw")
                .FirstOrDefault(p => p.MainWindowTitle.Contains("Minecraft"));

            if (minecraftProcess == null)
            {
                logger.Error("未找到 Minecraft 进程");
                return false;
            }

            logger.Info($"找到 Minecraft 进程 (PID: {minecraftProcess.Id})");

            // 2. 创建进程注入器
            using var injector = new ProcessInjector(logger, minecraftProcess.Id);

            // 3. 检查是否被调试
            if (injector.IsTargetBeingDebugged())
            {
                logger.Warning("目标进程正在被调试，可能会被反作弊检测");
            }

            // 4. 注入 DLL
            logger.Info($"开始注入 DLL: {dllPath}");
            var success = injector.InjectDll(dllPath, InjectionMethod.StandardDll);

            if (success)
            {
                logger.Info("DLL 注入成功！");
                
                // 5. （可选）使用内存管理器进行额外操作
                var memMgr = injector.GetMemoryManager();
                
                // 示例：模式扫描查找特定代码
                // var pattern = new byte[] { 0x48, 0x8B, 0x05 };
                // var mask = "xxx";
                // var address = memMgr.PatternScan(baseAddress, scanSize, pattern, mask);
                
                return true;
            }
            else
            {
                logger.Error("DLL 注入失败");
                return false;
            }
        }
        catch (Exception ex)
        {
            logger.Error($"注入过程出错: {ex.Message}", ex);
            return false;
        }
    }

    /// <summary>
    /// 内存读写示例
    /// </summary>
    public static void MemoryOperationsExample(int processId)
    {
        var logger = new SerilogLogger();
        
        using var memMgr = new MemoryManager(logger, processId);

        // 示例地址（实际使用时需要通过模式扫描等方式获取）
        var exampleAddress = new IntPtr(0x12345678);

        // 读取整数
        var value = memMgr.ReadInt32(exampleAddress);
        logger.Info($"读取到的值: {value}");

        // 写入整数
        memMgr.WriteInt32(exampleAddress, 42);
        logger.Info("写入成功");

        // 读取字符串
        var text = memMgr.ReadString(exampleAddress);
        logger.Info($"读取到的字符串: {text}");

        // 模式扫描
        var pattern = new byte[] { 0x55, 0x8B, 0xEC };
        var mask = "xxx";
        var foundAddress = memMgr.PatternScan(exampleAddress, 0x100000, pattern, mask);
        
        if (foundAddress != IntPtr.Zero)
        {
            logger.Info($"找到模式匹配: 0x{foundAddress:X}");
        }
    }
}
