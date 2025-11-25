using LizardClient.Core.Interfaces;
using LizardClient.Injection.Native;
using LizardClient.ModSystem.API;
using LizardClient.ModSystem.Loader;
using System.Runtime.InteropServices;

namespace LizardClient.Injection.Hooks;

/// <summary>
/// 输入钩子 (WndProc Hook)
/// </summary>
public sealed class InputHook : IDisposable
{
    private readonly ILogger _logger;
    private readonly ModLoader _modLoader;
    private IntPtr _hWnd;
    private IntPtr _originalWndProc;
    private NativeMethods.WndProcDelegate? _hookDelegate;
    private bool _isHooked;

    // Windows 消息常量
    private const uint WM_KEYDOWN = 0x0100;
    private const uint WM_KEYUP = 0x0101;
    private const uint WM_SYSKEYDOWN = 0x0104;
    private const uint WM_SYSKEYUP = 0x0105;

    public InputHook(ILogger logger, ModLoader modLoader)
    {
        _logger = logger;
        _modLoader = modLoader;
    }

    public void Install()
    {
        if (_isHooked) return;

        try
        {
            // 1. 查找游戏窗口
            // 注意：这里假设窗口类名为 "LWJGL" (Minecraft 默认)，或者通过标题查找
            // 实际上应该通过进程 ID 获取主窗口，或者枚举窗口
            // 这里为了简单，尝试查找常见的 Minecraft 窗口类名
            _hWnd = NativeMethods.FindWindow("LWJGL", null);
            if (_hWnd == IntPtr.Zero)
            {
                // 尝试用 GLFW (高版本 MC)
                _hWnd = NativeMethods.FindWindow("GLFW30", null);
            }

            if (_hWnd == IntPtr.Zero)
            {
                _logger.Error("未找到 Minecraft 窗口");
                return;
            }

            _logger.Info($"找到窗口句柄: 0x{_hWnd:X}");

            // 2. 创建 Hook 委托
            _hookDelegate = new NativeMethods.WndProcDelegate(HookedWndProc);
            var hookPointer = Marshal.GetFunctionPointerForDelegate(_hookDelegate);

            // 3. 替换 WndProc
            _originalWndProc = NativeMethods.SetWindowLongPtr(_hWnd, NativeMethods.GWLP_WNDPROC, hookPointer);

            if (_originalWndProc == IntPtr.Zero)
            {
                var error = NativeMethods.GetLastError();
                _logger.Error($"SetWindowLongPtr 失败: {error}");
                return;
            }

            _isHooked = true;
            _logger.Info("InputHook 安装成功");
        }
        catch (Exception ex)
        {
            _logger.Error($"安装 InputHook 失败: {ex.Message}", ex);
        }
    }

    private IntPtr HookedWndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
    {
        try
        {
            if (msg == WM_KEYDOWN || msg == WM_SYSKEYDOWN)
            {
                _modLoader.TriggerInput((int)wParam, InputAction.Press);
            }
            else if (msg == WM_KEYUP || msg == WM_SYSKEYUP)
            {
                _modLoader.TriggerInput((int)wParam, InputAction.Release);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Input Error: {ex}");
        }

        // 调用原始 WndProc
        return NativeMethods.CallWindowProc(_originalWndProc, hWnd, msg, wParam, lParam);
    }

    public void Dispose()
    {
        if (_isHooked && _hWnd != IntPtr.Zero && _originalWndProc != IntPtr.Zero)
        {
            // 恢复原始 WndProc
            NativeMethods.SetWindowLongPtr(_hWnd, NativeMethods.GWLP_WNDPROC, _originalWndProc);
            _isHooked = false;
            _logger.Info("InputHook 已卸载");
        }
    }
}
