using LizardClient.Core.Interfaces;
using LizardClient.Injection.Hooks;
using LizardClient.Injection.Memory;
using System.Runtime.InteropServices;

namespace LizardClient.Injection.Minecraft;

/// <summary>
/// Minecraft Hook 委托定义
/// </summary>
public static class MinecraftDelegates
{
    /// <summary>
    /// 游戏循环 Hook 回调
    /// </summary>
    public delegate void GameLoopDelegate();

    /// <summary>
    /// 渲染帧 Hook 回调
    /// </summary>
    public delegate void RenderFrameDelegate(float deltaTime);

    /// <summary>
    /// 输入处理 Hook 回调
    /// </summary>
    public delegate void ProcessInputDelegate(int key, int action);

    /// <summary>
    /// 发送网络包 Hook 回调
    /// </summary>
    public delegate void SendPacketDelegate(IntPtr packet);

    /// <summary>
    /// 接收网络包 Hook 回调
    /// </summary>
    public delegate void ReceivePacketDelegate(IntPtr packet);
}

/// <summary>
/// Minecraft 特定 Hook 管理器
/// </summary>
public sealed class MinecraftHooks : IDisposable
{
    private readonly ILogger _logger;
    private readonly HookEngine _hookEngine;
    private readonly GameOffsets _gameOffsets;
    private bool _disposed;
    private bool _hooksInstalled;

    // Hook 事件
    public event MinecraftDelegates.GameLoopDelegate? OnGameLoop;
    public event MinecraftDelegates.RenderFrameDelegate? OnRenderFrame;
    public event MinecraftDelegates.ProcessInputDelegate? OnProcessInput;
    public event MinecraftDelegates.SendPacketDelegate? OnSendPacket;
    public event MinecraftDelegates.ReceivePacketDelegate? OnReceivePacket;

    public MinecraftHooks(ILogger logger, HookEngine hookEngine, GameOffsets gameOffsets)
    {
        _logger = logger;
        _hookEngine = hookEngine;
        _gameOffsets = gameOffsets;

        _logger.Info($"Minecraft Hooks 管理器已初始化 (版本: {gameOffsets.Version})");
    }

    /// <summary>
    /// 安装所有 Hook
    /// </summary>
    public bool InstallHooks()
    {
        if (_hooksInstalled)
        {
            _logger.Warning("Hooks 已经安装");
            return true;
        }

        _logger.Info("开始安装 Minecraft Hooks...");

        int successCount = 0;
        int totalCount = 0;

        // 1. 游戏循环 Hook
        if (_gameOffsets.GameLoop != null)
        {
            totalCount++;
            if (InstallGameLoopHook())
            {
                successCount++;
                _logger.Info("✓ 游戏循环 Hook 安装成功");
            }
            else
            {
                _logger.Warning("✗ 游戏循环 Hook 安装失败");
            }
        }

        // 2. 渲染 Hook
        if (_gameOffsets.RenderFrame != null)
        {
            totalCount++;
            if (InstallRenderHook())
            {
                successCount++;
                _logger.Info("✓ 渲染 Hook 安装成功");
            }
            else
            {
                _logger.Warning("✗ 渲染 Hook 安装失败");
            }
        }

        // 3. 输入 Hook
        if (_gameOffsets.ProcessInput != null)
        {
            totalCount++;
            if (InstallInputHook())
            {
                successCount++;
                _logger.Info("✓ 输入 Hook 安装成功");
            }
            else
            {
                _logger.Warning("✗ 输入 Hook 安装失败");
            }
        }

        // 4. 网络 Hook
        if (_gameOffsets.SendPacket != null)
        {
            totalCount++;
            if (InstallSendPacketHook())
            {
                successCount++;
                _logger.Info("✓ 发送网络包 Hook 安装成功");
            }
            else
            {
                _logger.Warning("✗ 发送网络包 Hook 安装失败");
            }
        }

        if (_gameOffsets.ReceivePacket != null)
        {
            totalCount++;
            if (InstallReceivePacketHook())
            {
                successCount++;
                _logger.Info("✓ 接收网络包 Hook 安装成功");
            }
            else
            {
                _logger.Warning("✗ 接收网络包 Hook 安装失败");
            }
        }

        _logger.Info($"Hook 安装完成: {successCount}/{totalCount} 成功");
        _hooksInstalled = successCount > 0;
        return _hooksInstalled;
    }

    /// <summary>
    /// 安装游戏循环 Hook
    /// </summary>
    private bool InstallGameLoopHook()
    {
        if (_gameOffsets.GameLoop == null)
            return false;

        try
        {
            // 创建 Hook 函数
            var hookDelegate = new Action(() =>
            {
                try
                {
                    OnGameLoop?.Invoke();
                }
                catch (Exception ex)
                {
                    _logger.Error($"游戏循环 Hook 回调异常: {ex.Message}", ex);
                }
            });

            var hookPointer = Marshal.GetFunctionPointerForDelegate(hookDelegate);

            // 安装 Hook
            return _hookEngine.InstallDetourHook(
                "MinecraftGameLoop",
                _gameOffsets.GameLoop.Address,
                hookPointer
            );
        }
        catch (Exception ex)
        {
            _logger.Error($"安装游戏循环 Hook 失败: {ex.Message}", ex);
            return false;
        }
    }

    /// <summary>
    /// 安装渲染 Hook
    /// </summary>
    private bool InstallRenderHook()
    {
        if (_gameOffsets.RenderFrame == null)
            return false;

        try
        {
            // 创建 Hook 函数 (接受 deltaTime 参数)
            var hookDelegate = new Action<float>((deltaTime) =>
            {
                try
                {
                    OnRenderFrame?.Invoke(deltaTime);
                }
                catch (Exception ex)
                {
                    _logger.Error($"渲染 Hook 回调异常: {ex.Message}", ex);
                }
            });

            var hookPointer = Marshal.GetFunctionPointerForDelegate(hookDelegate);

            return _hookEngine.InstallDetourHook(
                "MinecraftRenderFrame",
                _gameOffsets.RenderFrame.Address,
                hookPointer
            );
        }
        catch (Exception ex)
        {
            _logger.Error($"安装渲染 Hook 失败: {ex.Message}", ex);
            return false;
        }
    }

    /// <summary>
    /// 安装输入 Hook
    /// </summary>
    private bool InstallInputHook()
    {
        if (_gameOffsets.ProcessInput == null)
            return false;

        try
        {
            var hookDelegate = new Action<int, int>((key, action) =>
            {
                try
                {
                    OnProcessInput?.Invoke(key, action);
                }
                catch (Exception ex)
                {
                    _logger.Error($"输入 Hook 回调异常: {ex.Message}", ex);
                }
            });

            var hookPointer = Marshal.GetFunctionPointerForDelegate(hookDelegate);

            return _hookEngine.InstallDetourHook(
                "MinecraftProcessInput",
                _gameOffsets.ProcessInput.Address,
                hookPointer
            );
        }
        catch (Exception ex)
        {
            _logger.Error($"安装输入 Hook 失败: {ex.Message}", ex);
            return false;
        }
    }

    /// <summary>
    /// 安装发送网络包 Hook
    /// </summary>
    private bool InstallSendPacketHook()
    {
        if (_gameOffsets.SendPacket == null)
            return false;

        try
        {
            var hookDelegate = new Action<IntPtr>((packet) =>
            {
                try
                {
                    OnSendPacket?.Invoke(packet);
                }
                catch (Exception ex)
                {
                    _logger.Error($"发送网络包 Hook 回调异常: {ex.Message}", ex);
                }
            });

            var hookPointer = Marshal.GetFunctionPointerForDelegate(hookDelegate);

            return _hookEngine.InstallDetourHook(
                "MinecraftSendPacket",
                _gameOffsets.SendPacket.Address,
                hookPointer
            );
        }
        catch (Exception ex)
        {
            _logger.Error($"安装发送网络包 Hook 失败: {ex.Message}", ex);
            return false;
        }
    }

    /// <summary>
    /// 安装接收网络包 Hook
    /// </summary>
    private bool InstallReceivePacketHook()
    {
        if (_gameOffsets.ReceivePacket == null)
            return false;

        try
        {
            var hookDelegate = new Action<IntPtr>((packet) =>
            {
                try
                {
                    OnReceivePacket?.Invoke(packet);
                }
                catch (Exception ex)
                {
                    _logger.Error($"接收网络包 Hook 回调异常: {ex.Message}", ex);
                }
            });

            var hookPointer = Marshal.GetFunctionPointerForDelegate(hookDelegate);

            return _hookEngine.InstallDetourHook(
                "MinecraftReceivePacket",
                _gameOffsets.ReceivePacket.Address,
                hookPointer
            );
        }
        catch (Exception ex)
        {
            _logger.Error($"安装接收网络包 Hook 失败: {ex.Message}", ex);
            return false;
        }
    }

    /// <summary>
    /// 卸载所有 Hook
    /// </summary>
    public void UninstallHooks()
    {
        if (!_hooksInstalled)
            return;

        _logger.Info("开始卸载 Minecraft Hooks...");
        _hookEngine.UninstallAllHooks();
        _hooksInstalled = false;
        _logger.Info("Minecraft Hooks 已卸载");
    }

    public void Dispose()
    {
        if (_disposed) return;

        UninstallHooks();
        _logger.Info("Minecraft Hooks 管理器已释放");

        _disposed = true;
    }
}
