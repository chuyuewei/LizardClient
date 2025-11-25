using LizardClient.Core.Interfaces;
using LizardClient.Injection.Memory;
using System.Runtime.InteropServices;

namespace LizardClient.Injection.Hooks;

/// <summary>
/// Hook 示例 - 演示如何 Hook 函数
/// </summary>
/// <remarks>
/// 这是一个简单的 Hook 框架示例，展示如何拦截和修改函数调用。
/// 实际使用时需要根据目标程序的具体情况调整。
/// </remarks>
public sealed class HookExample : IDisposable
{
    private readonly ILogger _logger;
    private readonly MemoryManager _memoryManager;
    private IntPtr _originalFunctionAddress;
    private IntPtr _hookFunctionAddress;
    private IntPtr _trampolineAddress;
    private byte[]? _originalBytes;
    private bool _isHooked;
    private bool _disposed;

    public HookExample(ILogger logger, int targetProcessId)
    {
        _logger = logger;
        _memoryManager = new MemoryManager(logger, targetProcessId);
        _logger.Info("Hook 示例已初始化");
    }

    /// <summary>
    /// 安装 Hook
    /// </summary>
    /// <param name="targetFunctionAddress">要 Hook 的函数地址</param>
    /// <param name="hookFunctionAddress">Hook 函数的地址</param>
    public bool InstallHook(IntPtr targetFunctionAddress, IntPtr hookFunctionAddress)
    {
        try
        {
            if (_isHooked)
            {
                _logger.Warning("Hook 已经安装");
                return false;
            }

            _originalFunctionAddress = targetFunctionAddress;
            _hookFunctionAddress = hookFunctionAddress;

            _logger.Info($"开始安装 Hook: 目标地址 0x{targetFunctionAddress:X}, Hook 地址 0x{hookFunctionAddress:X}");

            // 1. 读取原始字节（前 5 个字节用于 JMP 指令）
            _originalBytes = _memoryManager.ReadBytes(targetFunctionAddress, 5);
            if (_originalBytes == null || _originalBytes.Length < 5)
            {
                _logger.Error("读取原始字节失败");
                return false;
            }

            _logger.Info($"原始字节: {BitConverter.ToString(_originalBytes)}");

            // 2. 创建跳转指令 (JMP)
            // x86/x64 JMP 指令格式: E9 <相对偏移>
            var jumpBytes = CreateJumpInstruction(targetFunctionAddress, hookFunctionAddress);

            // 3. 修改目标函数的内存保护（改为可执行可写）
            if (!_memoryManager.ChangeProtection(targetFunctionAddress, 5, 0x40, out var oldProtection)) // PAGE_EXECUTE_READWRITE
            {
                _logger.Error("修改内存保护失败");
                return false;
            }

            // 4. 写入跳转指令
            if (!_memoryManager.WriteBytes(targetFunctionAddress, jumpBytes))
            {
                _logger.Error("写入跳转指令失败");
                // 恢复内存保护
                _memoryManager.ChangeProtection(targetFunctionAddress, 5, oldProtection, out _);
                return false;
            }

            // 5. 恢复原来的内存保护
            _memoryManager.ChangeProtection(targetFunctionAddress, 5, oldProtection, out _);

            _isHooked = true;
            _logger.Info("Hook 安装成功！");
            return true;
        }
        catch (Exception ex)
        {
            _logger.Error($"安装 Hook 失败: {ex.Message}", ex);
            return false;
        }
    }

    /// <summary>
    /// 卸载 Hook
    /// </summary>
    public bool UninstallHook()
    {
        try
        {
            if (!_isHooked || _originalBytes == null)
            {
                _logger.Warning("Hook 未安装或原始字节丢失");
                return false;
            }

            _logger.Info("开始卸载 Hook...");

            // 1. 修改内存保护
            if (!_memoryManager.ChangeProtection(_originalFunctionAddress, 5, 0x40, out var oldProtection))
            {
                _logger.Error("修改内存保护失败");
                return false;
            }

            // 2. 恢复原始字节
            if (!_memoryManager.WriteBytes(_originalFunctionAddress, _originalBytes))
            {
                _logger.Error("恢复原始字节失败");
                _memoryManager.ChangeProtection(_originalFunctionAddress, 5, oldProtection, out _);
                return false;
            }

            // 3. 恢复内存保护
            _memoryManager.ChangeProtection(_originalFunctionAddress, 5, oldProtection, out _);

            _isHooked = false;
            _logger.Info("Hook 卸载成功！");
            return true;
        }
        catch (Exception ex)
        {
            _logger.Error($"卸载 Hook 失败: {ex.Message}", ex);
            return false;
        }
    }

    /// <summary>
    /// 创建 JMP 跳转指令
    /// </summary>
    private byte[] CreateJumpInstruction(IntPtr fromAddress, IntPtr toAddress)
    {
        // 计算相对偏移
        // 相对偏移 = 目标地址 - (当前地址 + 5)
        // 5 是因为 JMP 指令本身占 5 字节 (E9 + 4字节偏移)
        var offset = toAddress.ToInt64() - (fromAddress.ToInt64() + 5);

        var jumpBytes = new byte[5];
        jumpBytes[0] = 0xE9; // JMP 指令的操作码

        // 写入 4 字节偏移（小端序）
        var offsetBytes = BitConverter.GetBytes((int)offset);
        Array.Copy(offsetBytes, 0, jumpBytes, 1, 4);

        return jumpBytes;
    }

    /// <summary>
    /// 创建 Trampoline（可选，用于调用原始函数）
    /// </summary>
    /// <remarks>
    /// Trampoline 是一小段代码，包含原始函数的前几条指令和跳转到原始函数剩余部分的指令。
    /// 这样 Hook 函数可以调用原始函数的功能。
    /// </remarks>
    private IntPtr CreateTrampoline()
    {
        try
        {
            if (_originalBytes == null)
            {
                _logger.Error("原始字节未设置");
                return IntPtr.Zero;
            }

            // 分配内存用于 Trampoline
            var trampolineSize = (uint)(_originalBytes.Length + 5); // 原始指令 + JMP
            _trampolineAddress = _memoryManager.AllocateMemory(trampolineSize);

            if (_trampolineAddress == IntPtr.Zero)
            {
                _logger.Error("分配 Trampoline 内存失败");
                return IntPtr.Zero;
            }

            // 写入原始指令
            _memoryManager.WriteBytes(_trampolineAddress, _originalBytes);

            // 写入跳转回原始函数的指令
            var returnAddress = IntPtr.Add(_originalFunctionAddress, _originalBytes.Length);
            var jumpBackBytes = CreateJumpInstruction(
                IntPtr.Add(_trampolineAddress, _originalBytes.Length),
                returnAddress);

            _memoryManager.WriteBytes(IntPtr.Add(_trampolineAddress, _originalBytes.Length), jumpBackBytes);

            _logger.Info($"Trampoline 创建成功: 0x{_trampolineAddress:X}");
            return _trampolineAddress;
        }
        catch (Exception ex)
        {
            _logger.Error($"创建 Trampoline 失败: {ex.Message}", ex);
            return IntPtr.Zero;
        }
    }

    /// <summary>
    /// Hook 是否已安装
    /// </summary>
    public bool IsHooked => _isHooked;

    /// <summary>
    /// Trampoline 地址（用于调用原始函数）
    /// </summary>
    public IntPtr TrampolineAddress => _trampolineAddress;

    public void Dispose()
    {
        if (_disposed) return;

        // 卸载 Hook
        if (_isHooked)
        {
            UninstallHook();
        }

        // 释放 Trampoline 内存
        if (_trampolineAddress != IntPtr.Zero)
        {
            _memoryManager.FreeMemory(_trampolineAddress);
        }

        _memoryManager?.Dispose();
        _logger.Info("Hook 示例已释放");

        _disposed = true;
    }
}

/// <summary>
/// 使用示例
/// </summary>
public static class HookUsageExample
{
    /// <summary>
    /// 演示如何使用 Hook
    /// </summary>
    public static void DemonstrateHook(ILogger logger, int targetProcessId)
    {
        // 创建 Hook 实例
        using var hook = new HookExample(logger, targetProcessId);

        // 假设我们要 Hook 的函数地址（实际使用时需要通过模式扫描或其他方式获取）
        var targetFunctionAddress = new IntPtr(0x12345678);

        // 我们的 Hook 函数地址（在注入的 DLL 中）
        var hookFunctionAddress = new IntPtr(0x87654321);

        // 安装 Hook
        if (hook.InstallHook(targetFunctionAddress, hookFunctionAddress))
        {
            logger.Info("Hook 已激活！");

            // 在这里，所有对目标函数的调用都会被重定向到我们的 Hook 函数

            // ... 执行其他操作 ...

            // 完成后卸载 Hook
            hook.UninstallHook();
        }
        else
        {
            logger.Error("Hook 安装失败");
        }
    }
}
