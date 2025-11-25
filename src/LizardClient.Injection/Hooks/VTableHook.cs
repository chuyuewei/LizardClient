using LizardClient.Core.Interfaces;
using LizardClient.Injection.Memory;
using LizardClient.Injection.Native;
using System.Runtime.InteropServices;

namespace LizardClient.Injection.Hooks;

/// <summary>
/// VTable Hook - 虚函数表 Hook
/// </summary>
public sealed class VTableHook
{
    private readonly ILogger _logger;
    private readonly MemoryManager _memoryManager;

    public VTableHook(ILogger logger, MemoryManager memoryManager)
    {
        _logger = logger;
        _memoryManager = memoryManager;
    }

    /// <summary>
    /// Hook 虚函数表中的函数
    /// </summary>
    /// <param name="objectAddress">对象实例地址</param>
    /// <param name="functionIndex">虚函数索引</param>
    /// <param name="hookAddress">Hook 函数地址</param>
    /// <returns>原始函数地址</returns>
    public IntPtr HookVirtualFunction(IntPtr objectAddress, int functionIndex, IntPtr hookAddress)
    {
        try
        {
            // 1. 读取对象的 VTable 指针 (对象的前 8 字节是 VTable 指针)
            var vtablePointer = _memoryManager.ReadInt64(objectAddress);
            if (vtablePointer == 0)
            {
                _logger.Error("无法读取 VTable 指针");
                return IntPtr.Zero;
            }

            var vtableAddress = new IntPtr(vtablePointer);
            _logger.Info($"VTable 地址: 0x{vtableAddress:X}");

            // 2. 计算目标虚函数地址的位置
            var functionPointerAddress = IntPtr.Add(vtableAddress, functionIndex * IntPtr.Size);

            // 3. 读取原始函数地址
            var originalFunctionAddress = new IntPtr(_memoryManager.ReadInt64(functionPointerAddress));
            _logger.Info($"原始函数地址: 0x{originalFunctionAddress:X}");

            // 4. 修改内存保护
            if (!_memoryManager.ChangeProtection(functionPointerAddress, (uint)IntPtr.Size,
                WinApiConstants.PAGE_READWRITE, out var oldProtection))
            {
                _logger.Error("修改 VTable 内存保护失败");
                return IntPtr.Zero;
            }

            // 5. 写入 Hook 函数地址
            if (!_memoryManager.WriteInt64(functionPointerAddress, hookAddress.ToInt64()))
            {
                _logger.Error("写入 Hook 地址失败");
                _memoryManager.ChangeProtection(functionPointerAddress, (uint)IntPtr.Size, oldProtection, out _);
                return IntPtr.Zero;
            }

            // 6. 恢复内存保护
            _memoryManager.ChangeProtection(functionPointerAddress, (uint)IntPtr.Size, oldProtection, out _);

            _logger.Info($"VTable Hook 成功 (索引: {functionIndex})");
            return originalFunctionAddress;
        }
        catch (Exception ex)
        {
            _logger.Error($"VTable Hook 失败: {ex.Message}", ex);
            return IntPtr.Zero;
        }
    }

    /// <summary>
    /// 恢复虚函数表中的函数
    /// </summary>
    /// <param name="objectAddress">对象实例地址</param>
    /// <param name="functionIndex">虚函数索引</param>
    /// <param name="originalAddress">原始函数地址</param>
    public bool UnhookVirtualFunction(IntPtr objectAddress, int functionIndex, IntPtr originalAddress)
    {
        try
        {
            // 1. 读取 VTable 指针
            var vtablePointer = _memoryManager.ReadInt64(objectAddress);
            if (vtablePointer == 0)
            {
                _logger.Error("无法读取 VTable 指针");
                return false;
            }

            var vtableAddress = new IntPtr(vtablePointer);

            // 2. 计算虚函数地址位置
            var functionPointerAddress = IntPtr.Add(vtableAddress, functionIndex * IntPtr.Size);

            // 3. 修改内存保护并恢复原始地址
            if (!_memoryManager.ChangeProtection(functionPointerAddress, (uint)IntPtr.Size,
                WinApiConstants.PAGE_READWRITE, out var oldProtection))
            {
                _logger.Error("修改 VTable 内存保护失败");
                return false;
            }

            var success = _memoryManager.WriteInt64(functionPointerAddress, originalAddress.ToInt64());
            _memoryManager.ChangeProtection(functionPointerAddress, (uint)IntPtr.Size, oldProtection, out _);

            if (success)
            {
                _logger.Info($"VTable Unhook 成功 (索引: {functionIndex})");
            }

            return success;
        }
        catch (Exception ex)
        {
            _logger.Error($"VTable Unhook 失败: {ex.Message}", ex);
            return false;
        }
    }

    /// <summary>
    /// 复制整个 VTable 并 Hook (更安全，不影响其他对象)
    /// </summary>
    /// <param name="objectAddress">对象实例地址</param>
    /// <param name="vtableSize">VTable 大小 (虚函数数量)</param>
    /// <param name="hooksToApply">要 Hook 的函数 (索引 -> Hook 地址)</param>
    /// <returns>原始 VTable 地址</returns>
    public IntPtr CopyAndHookVTable(IntPtr objectAddress, int vtableSize, Dictionary<int, IntPtr> hooksToApply)
    {
        try
        {
            // 1. 读取原始 VTable 地址
            var originalVTableAddress = new IntPtr(_memoryManager.ReadInt64(objectAddress));
            if (originalVTableAddress == IntPtr.Zero)
            {
                _logger.Error("无法读取 VTable 指针");
                return IntPtr.Zero;
            }

            // 2. 分配新的 VTable 内存
            var newVTableSize = (uint)(vtableSize * IntPtr.Size);
            var newVTableAddress = _memoryManager.AllocateMemory(newVTableSize, WinApiConstants.PAGE_READWRITE);

            if (newVTableAddress == IntPtr.Zero)
            {
                _logger.Error("分配新 VTable 内存失败");
                return IntPtr.Zero;
            }

            // 3. 复制原始 VTable
            var originalVTable = _memoryManager.ReadBytes(originalVTableAddress, (int)newVTableSize);
            if (!_memoryManager.WriteBytes(newVTableAddress, originalVTable))
            {
                _logger.Error("复制 VTable 失败");
                _memoryManager.FreeMemory(newVTableAddress);
                return IntPtr.Zero;
            }

            // 4. 应用 Hooks
            foreach (var (index, hookAddress) in hooksToApply)
            {
                var functionPointerAddress = IntPtr.Add(newVTableAddress, index * IntPtr.Size);
                _memoryManager.WriteInt64(functionPointerAddress, hookAddress.ToInt64());
            }

            // 5. 替换对象的 VTable 指针
            if (!_memoryManager.ChangeProtection(objectAddress, (uint)IntPtr.Size,
                WinApiConstants.PAGE_READWRITE, out var oldProtection))
            {
                _logger.Error("修改对象内存保护失败");
                _memoryManager.FreeMemory(newVTableAddress);
                return IntPtr.Zero;
            }

            if (!_memoryManager.WriteInt64(objectAddress, newVTableAddress.ToInt64()))
            {
                _logger.Error("替换 VTable 指针失败");
                _memoryManager.ChangeProtection(objectAddress, (uint)IntPtr.Size, oldProtection, out _);
                _memoryManager.FreeMemory(newVTableAddress);
                return IntPtr.Zero;
            }

            _memoryManager.ChangeProtection(objectAddress, (uint)IntPtr.Size, oldProtection, out _);

            _logger.Info($"VTable 复制并 Hook 成功 (应用了 {hooksToApply.Count} 个 Hook)");
            return originalVTableAddress;
        }
        catch (Exception ex)
        {
            _logger.Error($"复制并 Hook VTable 失败: {ex.Message}", ex);
            return IntPtr.Zero;
        }
    }
}
