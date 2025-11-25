using LizardClient.Core.Interfaces;
using LizardClient.Injection.Native;
using LizardClient.Injection.Memory;
using System.Runtime.InteropServices;
using System.Text;

namespace LizardClient.Injection.Injectors;

/// <summary>
/// 注入方法类型
/// </summary>
public enum InjectionMethod
{
    /// <summary>
    /// 标准 DLL 注入 (CreateRemoteThread + LoadLibrary)
    /// </summary>
    StandardDll,

    /// <summary>
    /// Manual Mapping (更隐蔽)
    /// </summary>
    ManualMap,

    /// <summary>
    /// Shellcode 注入
    /// </summary>
    Shellcode
}

/// <summary>
/// 进程注入器
/// </summary>
public sealed class ProcessInjector : IDisposable
{
    private readonly ILogger _logger;
    private readonly MemoryManager _memoryManager;
    private readonly int _targetProcessId;
    private bool _disposed;

    public ProcessInjector(ILogger logger, int targetProcessId)
    {
        _logger = logger;
        _targetProcessId = targetProcessId;
        _memoryManager = new MemoryManager(logger, targetProcessId);

        _logger.Info($"进程注入器已初始化 (目标进程: {targetProcessId})");
    }

    /// <summary>
    /// 注入 DLL 到目标进程
    /// </summary>
    public bool InjectDll(string dllPath, InjectionMethod method = InjectionMethod.StandardDll)
    {
        if (!File.Exists(dllPath))
        {
            _logger.Error($"DLL 文件不存在: {dllPath}");
            return false;
        }

        _logger.Info($"开始注入 DLL: {dllPath}");
        _logger.Info($"注入方法: {method}");

        return method switch
        {
            InjectionMethod.StandardDll => InjectDllStandard(dllPath),
            InjectionMethod.ManualMap => InjectDllManualMap(dllPath),
            InjectionMethod.Shellcode => throw new NotImplementedException("Shellcode 注入尚未实现"),
            _ => false
        };
    }

    /// <summary>
    /// 标准 DLL 注入 (CreateRemoteThread + LoadLibrary)
    /// </summary>
    private bool InjectDllStandard(string dllPath)
    {
        try
        {
            // 1. 获取 LoadLibraryA 地址
            var kernel32 = NativeMethods.GetModuleHandle("kernel32.dll");
            if (kernel32 == IntPtr.Zero)
            {
                _logger.Error("无法获取 kernel32.dll 句柄");
                return false;
            }

            var loadLibraryAddr = NativeMethods.GetProcAddress(kernel32, "LoadLibraryA");
            if (loadLibraryAddr == IntPtr.Zero)
            {
                _logger.Error("无法获取 LoadLibraryA 地址");
                return false;
            }

            _logger.Info($"LoadLibraryA 地址: 0x{loadLibraryAddr:X}");

            // 2. 在目标进程中分配内存
            var pathBytes = Encoding.ASCII.GetBytes(dllPath);
            var allocatedMemory = _memoryManager.AllocateMemory((uint)(pathBytes.Length + 1));
            
            if (allocatedMemory == IntPtr.Zero)
            {
                _logger.Error("内存分配失败");
                return false;
            }

            // 3. 将 DLL 路径写入目标进程
            if (!_memoryManager.WriteBytes(allocatedMemory, pathBytes))
            {
                _logger.Error("写入 DLL 路径失败");
                _memoryManager.FreeMemory(allocatedMemory);
                return false;
            }

            // 4. 创建远程线程执行 LoadLibrary
            var threadHandle = NativeMethods.CreateRemoteThread(
                _memoryManager.ProcessHandle,
                IntPtr.Zero,
                0,
                loadLibraryAddr,
                allocatedMemory,
                0,
                out var threadId);

            if (threadHandle == IntPtr.Zero)
            {
                var error = Marshal.GetLastWin32Error();
                _logger.Error($"创建远程线程失败，错误代码: {error}");
                _memoryManager.FreeMemory(allocatedMemory);
                return false;
            }

            _logger.Info($"远程线程已创建 (ID: {threadId})");

            // 5. 等待线程完成
            var waitResult = NativeMethods.WaitForSingleObject(threadHandle, 5000);
            
            if (waitResult == WinApiConstants.WAIT_TIMEOUT)
            {
                _logger.Warning("等待线程超时");
            }

            // 6. 获取线程退出代码（即 LoadLibrary 的返回值，是模块句柄）
            NativeMethods.GetExitCodeThread(threadHandle, out var exitCode);
            
            if (exitCode == 0)
            {
                _logger.Error("LoadLibrary 返回 NULL，DLL 加载失败");
                NativeMethods.CloseHandle(threadHandle);
                _memoryManager.FreeMemory(allocatedMemory);
                return false;
            }

            _logger.Info($"DLL 已加载到目标进程 (模块句柄: 0x{exitCode:X})");

            // 7. 清理
            NativeMethods.CloseHandle(threadHandle);
            _memoryManager.FreeMemory(allocatedMemory);

            _logger.Info("DLL 注入成功");
            return true;
        }
        catch (Exception ex)
        {
            _logger.Error($"DLL 注入失败: {ex.Message}", ex);
            return false;
        }
    }

    /// <summary>
    /// Manual Map 注入 (更复杂但更隐蔽)
    /// </summary>
    private bool InjectDllManualMap(string dllPath)
    {
        _logger.Warning("Manual Map 注入尚未完全实现");
        
        try
        {
            // 1. 读取 DLL 文件
            var dllBytes = File.ReadAllBytes(dllPath);
            _logger.Info($"DLL 文件大小: {dllBytes.Length} 字节");

            // 2. 解析 PE 头
            // TODO: 实现 PE 解析
            // TODO: 重定位处理
            // TODO: 导入表处理
            // TODO: 调用 DllMain

            _logger.Info("Manual Map 注入需要进一步实现");
            return false;
        }
        catch (Exception ex)
        {
            _logger.Error($"Manual Map 注入失败: {ex.Message}", ex);
            return false;
        }
    }

    /// <summary>
    /// 检查目标进程是否被调试
    /// </summary>
    public bool IsTargetBeingDebugged()
    {
        NativeMethods.CheckRemoteDebuggerPresent(_memoryManager.ProcessHandle, out var isDebugged);
        
        if (isDebugged)
        {
            _logger.Warning("目标进程正在被调试");
        }

        return isDebugged;
    }

    /// <summary>
    /// 获取内存管理器
    /// </summary>
    public MemoryManager GetMemoryManager() => _memoryManager;

    public void Dispose()
    {
        if (_disposed) return;

        _memoryManager?.Dispose();
        _logger.Info("进程注入器已释放");
        
        _disposed = true;
    }
}
