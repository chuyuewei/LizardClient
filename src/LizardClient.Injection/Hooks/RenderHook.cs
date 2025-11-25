using LizardClient.Core.Interfaces;
using LizardClient.Injection.Native;
using LizardClient.ModSystem.Loader;
using System.Runtime.InteropServices;

namespace LizardClient.Injection.Hooks;

/// <summary>
/// OpenGL 渲染钩子
/// </summary>
public sealed class RenderHook(ILogger logger, ModLoader modLoader) : IDisposable
{
    private readonly ILogger _logger = logger;
    private readonly ModLoader _modLoader = modLoader;
    private IntPtr _originalWglSwapBuffers;
    private WglSwapBuffersDelegate? _originalDelegate;
    private WglSwapBuffersDelegate? _hookDelegate;
    private bool _isHooked;
    private byte[]? _originalBytes;
    private IntPtr _trampolineAddress;

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate bool WglSwapBuffersDelegate(IntPtr hDc);

    public void Install()
    {
        if (_isHooked) return;

        try
        {
            // 1. 获取 opengl32.dll 模块句柄
            var hModule = NativeMethods.GetModuleHandle("opengl32.dll");
            if (hModule == IntPtr.Zero)
            {
                _logger.Error("未找到 opengl32.dll");
                return;
            }

            // 2. 获取 wglSwapBuffers 地址
            _originalWglSwapBuffers = NativeMethods.GetProcAddress(hModule, "wglSwapBuffers");
            if (_originalWglSwapBuffers == IntPtr.Zero)
            {
                _logger.Error("未找到 wglSwapBuffers 函数");
                return;
            }

            _logger.Info($"找到 wglSwapBuffers 地址: 0x{_originalWglSwapBuffers:X}");

            // 3. 保存原始字节 (5字节用于 JMP)
            _originalBytes = new byte[5];
            Marshal.Copy(_originalWglSwapBuffers, _originalBytes, 0, 5);

            // 4. 创建 Hook 委托
            _hookDelegate = new WglSwapBuffersDelegate(HookedWglSwapBuffers);
            var hookPointer = Marshal.GetFunctionPointerForDelegate(_hookDelegate);

            // 5. 写入 JMP 指令
            InstallTrampolineHook(hookPointer);

            _isHooked = true;
            _logger.Info("RenderHook 安装成功");
        }
        catch (Exception ex)
        {
            _logger.Error($"安装 RenderHook 失败: {ex.Message}", ex);
        }
    }

    private void InstallTrampolineHook(IntPtr hookAddress)
    {
        // 分配 Trampoline 内存
        _trampolineAddress = Marshal.AllocHGlobal(128); // 分配足够空间

        // 1. 写入内存保护 (Trampoline 可执行)
        NativeMethods.VirtualProtect(_trampolineAddress, 128, WinApiConstants.PAGE_EXECUTE_READWRITE, out _);

        // 2. 写入原始字节到 Trampoline
        Marshal.Copy(_originalBytes!, 0, _trampolineAddress, 5);

        // 3. 在 Trampoline 中写入跳回原函数的 JMP
        var returnAddress = IntPtr.Add(_originalWglSwapBuffers, 5);
        WriteJmp(IntPtr.Add(_trampolineAddress, 5), returnAddress);

        // 4. 创建调用原始函数的委托
        _originalDelegate = Marshal.GetDelegateForFunctionPointer<WglSwapBuffersDelegate>(_trampolineAddress);

        // 5. 修改目标函数入口为 JMP 到我们的 Hook
        NativeMethods.VirtualProtect(_originalWglSwapBuffers, 5, WinApiConstants.PAGE_EXECUTE_READWRITE, out var oldProtect);
        WriteJmp(_originalWglSwapBuffers, hookAddress);
        NativeMethods.VirtualProtect(_originalWglSwapBuffers, 5, oldProtect, out _);
    }

    private static void WriteJmp(IntPtr from, IntPtr to)
    {
        var offset = (int)(to.ToInt64() - from.ToInt64() - 5);
        var instruction = new byte[5];
        instruction[0] = 0xE9; // JMP
        BitConverter.GetBytes(offset).CopyTo(instruction, 1);
        Marshal.Copy(instruction, 0, from, 5);
    }

    private bool HookedWglSwapBuffers(IntPtr hDc)
    {
        try
        {
            // 触发模组渲染
            _modLoader.RenderMods();
        }
        catch (Exception ex)
        {
            // 防止渲染循环崩溃
            Console.WriteLine($"Render Error: {ex}");
        }

        // 调用原始函数
        if (_originalDelegate != null)
        {
            return _originalDelegate(hDc);
        }
        return false;
    }

    public void Dispose()
    {
        if (_isHooked)
        {
            // 恢复原始字节
            NativeMethods.VirtualProtect(_originalWglSwapBuffers, 5, WinApiConstants.PAGE_EXECUTE_READWRITE, out var oldProtect);
            Marshal.Copy(_originalBytes!, 0, _originalWglSwapBuffers, 5);
            NativeMethods.VirtualProtect(_originalWglSwapBuffers, 5, oldProtect, out _);

            _isHooked = false;
        }

        if (_trampolineAddress != IntPtr.Zero)
        {
            Marshal.FreeHGlobal(_trampolineAddress);
            _trampolineAddress = IntPtr.Zero;
        }
    }
}
