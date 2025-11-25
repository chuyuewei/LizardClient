using LizardClient.Core.Interfaces;
using LizardClient.Injection.Memory;
using LizardClient.Injection.Native;
using System.Runtime.InteropServices;

namespace LizardClient.Injection.Hooks;

/// <summary>
/// Hook 引擎 - 提供函数拦截和修改功能
/// </summary>
public sealed class HookEngine : IDisposable
{
    private readonly ILogger _logger;
    private readonly MemoryManager _memoryManager;
    private readonly Dictionary<string, HookInfo> _hooks = new();
    private readonly object _lockObject = new();
    private bool _disposed;

    public HookEngine(ILogger logger, MemoryManager memoryManager)
    {
        _logger = logger;
        _memoryManager = memoryManager;
        _logger.Info("Hook 引擎已初始化");
    }

    /// <summary>
    /// 获取所有 Hook 信息
    /// </summary>
    public IReadOnlyDictionary<string, HookInfo> GetAllHooks()
    {
        lock (_lockObject)
        {
            return new Dictionary<string, HookInfo>(_hooks);
        }
    }

    /// <summary>
    /// 安装 Detour Hook
    /// </summary>
    /// <param name="name">Hook 名称</param>
    /// <param name="targetAddress">目标函数地址</param>
    /// <param name="hookAddress">Hook 函数地址</param>
    /// <returns>成功返回 true</returns>
    public bool InstallDetourHook(string name, IntPtr targetAddress, IntPtr hookAddress)
    {
        lock (_lockObject)
        {
            if (_hooks.ContainsKey(name))
            {
                _logger.Warning($"Hook '{name}' 已存在");
                return false;
            }

            try
            {
                // 1. 读取原始字节
                const int hookSize = 14; // JMP [RIP+0]; Address (14 bytes for x64)
                var originalBytes = _memoryManager.ReadBytes(targetAddress, hookSize);

                if (originalBytes.Length < hookSize)
                {
                    _logger.Error($"无法读取目标函数字节: {name}");
                    return false;
                }

                // 2. 创建 Trampoline (存放原始指令 + 跳回)
                var trampolineSize = (uint)(hookSize + 14); // 原始指令 + 跳回的 JMP
                var trampolineAddress = _memoryManager.AllocateMemory(trampolineSize);

                if (trampolineAddress == IntPtr.Zero)
                {
                    _logger.Error($"分配 Trampoline 内存失败: {name}");
                    return false;
                }

                // 3. 写入 Trampoline: 原始指令 + JMP 回到目标函数后续代码
                var trampolineBytes = new List<byte>();
                trampolineBytes.AddRange(originalBytes);

                // JMP 回目标函数 (跳过被 hook 的部分)
                var returnAddress = IntPtr.Add(targetAddress, hookSize);
                trampolineBytes.AddRange(CreateAbsoluteJump(returnAddress));

                if (!_memoryManager.WriteBytes(trampolineAddress, trampolineBytes.ToArray()))
                {
                    _logger.Error($"写入 Trampoline 失败: {name}");
                    _memoryManager.FreeMemory(trampolineAddress);
                    return false;
                }

                // 4. 修改目标函数内存保护
                if (!_memoryManager.ChangeProtection(targetAddress, hookSize,
                    WinApiConstants.PAGE_EXECUTE_READWRITE, out var oldProtection))
                {
                    _logger.Error($"修改内存保护失败: {name}");
                    _memoryManager.FreeMemory(trampolineAddress);
                    return false;
                }

                // 5. 写入 Hook 跳转
                var hookJump = CreateAbsoluteJump(hookAddress);
                if (!_memoryManager.WriteBytes(targetAddress, hookJump))
                {
                    _logger.Error($"写入 Hook 跳转失败: {name}");
                    _memoryManager.ChangeProtection(targetAddress, hookSize, oldProtection, out _);
                    _memoryManager.FreeMemory(trampolineAddress);
                    return false;
                }

                // 6. 恢复内存保护
                _memoryManager.ChangeProtection(targetAddress, hookSize, oldProtection, out _);

                // 7. 创建 Hook 信息
                var hookInfo = new HookInfo
                {
                    Name = name,
                    Type = HookType.Detour,
                    TargetAddress = targetAddress,
                    HookAddress = hookAddress,
                    TrampolineAddress = trampolineAddress,
                    OriginalBytes = originalBytes,
                    IsEnabled = true
                };

                _hooks[name] = hookInfo;
                _logger.Info($"Detour Hook 安装成功: {hookInfo}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.Error($"安装 Detour Hook 失败: {name}", ex);
                return false;
            }
        }
    }

    /// <summary>
    /// 卸载 Hook
    /// </summary>
    public bool UninstallHook(string name)
    {
        lock (_lockObject)
        {
            if (!_hooks.TryGetValue(name, out var hookInfo))
            {
                _logger.Warning($"Hook '{name}' 不存在");
                return false;
            }

            try
            {
                // 1. 恢复原始字节
                if (!_memoryManager.ChangeProtection(hookInfo.TargetAddress,
                    (uint)hookInfo.OriginalBytes.Length, WinApiConstants.PAGE_EXECUTE_READWRITE, out var oldProtection))
                {
                    _logger.Error($"修改内存保护失败: {name}");
                    return false;
                }

                if (!_memoryManager.WriteBytes(hookInfo.TargetAddress, hookInfo.OriginalBytes))
                {
                    _logger.Error($"恢复原始字节失败: {name}");
                    return false;
                }

                _memoryManager.ChangeProtection(hookInfo.TargetAddress,
                    (uint)hookInfo.OriginalBytes.Length, oldProtection, out _);

                // 2. 释放 Trampoline 内存
                if (hookInfo.TrampolineAddress != IntPtr.Zero)
                {
                    _memoryManager.FreeMemory(hookInfo.TrampolineAddress);
                }

                // 3. 移除 Hook 记录
                _hooks.Remove(name);
                _logger.Info($"Hook 已卸载: {name}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.Error($"卸载 Hook 失败: {name}", ex);
                return false;
            }
        }
    }

    /// <summary>
    /// 启用 Hook
    /// </summary>
    public bool EnableHook(string name)
    {
        lock (_lockObject)
        {
            if (!_hooks.TryGetValue(name, out var hookInfo))
            {
                _logger.Warning($"Hook '{name}' 不存在");
                return false;
            }

            if (hookInfo.IsEnabled)
            {
                return true; // 已经启用
            }

            // 重新写入 Hook 跳转
            var hookJump = CreateAbsoluteJump(hookInfo.HookAddress);
            if (_memoryManager.WriteBytes(hookInfo.TargetAddress, hookJump))
            {
                hookInfo.IsEnabled = true;
                _logger.Info($"Hook 已启用: {name}");
                return true;
            }

            return false;
        }
    }

    /// <summary>
    /// 禁用 Hook
    /// </summary>
    public bool DisableHook(string name)
    {
        lock (_lockObject)
        {
            if (!_hooks.TryGetValue(name, out var hookInfo))
            {
                _logger.Warning($"Hook '{name}' 不存在");
                return false;
            }

            if (!hookInfo.IsEnabled)
            {
                return true; // 已经禁用
            }

            // 恢复原始字节
            if (_memoryManager.WriteBytes(hookInfo.TargetAddress, hookInfo.OriginalBytes))
            {
                hookInfo.IsEnabled = false;
                _logger.Info($"Hook 已禁用: {name}");
                return true;
            }

            return false;
        }
    }

    /// <summary>
    /// 创建绝对跳转指令 (x64)
    /// JMP [RIP+0]; dq address
    /// FF 25 00 00 00 00 | address (8 bytes)
    /// </summary>
    private byte[] CreateAbsoluteJump(IntPtr destination)
    {
        var jumpBytes = new byte[14];

        // JMP [RIP+0]
        jumpBytes[0] = 0xFF;
        jumpBytes[1] = 0x25;
        jumpBytes[2] = 0x00;
        jumpBytes[3] = 0x00;
        jumpBytes[4] = 0x00;
        jumpBytes[5] = 0x00;

        // 目标地址 (8 bytes for x64)
        var addressBytes = BitConverter.GetBytes(destination.ToInt64());
        Array.Copy(addressBytes, 0, jumpBytes, 6, 8);

        return jumpBytes;
    }

    /// <summary>
    /// 卸载所有 Hook
    /// </summary>
    public void UninstallAllHooks()
    {
        lock (_lockObject)
        {
            var hookNames = _hooks.Keys.ToList();
            foreach (var name in hookNames)
            {
                UninstallHook(name);
            }
        }
    }

    public void Dispose()
    {
        if (_disposed) return;

        UninstallAllHooks();
        _logger.Info("Hook 引擎已释放");

        _disposed = true;
    }
}
