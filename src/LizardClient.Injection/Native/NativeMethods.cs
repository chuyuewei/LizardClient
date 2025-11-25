using System.Runtime.InteropServices;

namespace LizardClient.Injection.Native;

/// <summary>
/// Windows API 常量
/// </summary>
public static class WinApiConstants
{
    // 进程访问权限
    public const uint PROCESS_ALL_ACCESS = 0x1F0FFF;
    public const uint PROCESS_CREATE_THREAD = 0x0002;
    public const uint PROCESS_QUERY_INFORMATION = 0x0400;
    public const uint PROCESS_VM_OPERATION = 0x0008;
    public const uint PROCESS_VM_WRITE = 0x0020;
    public const uint PROCESS_VM_READ = 0x0010;

    // 内存分配类型
    public const uint MEM_COMMIT = 0x1000;
    public const uint MEM_RESERVE = 0x2000;
    public const uint MEM_RELEASE = 0x8000;

    // 内存保护
    public const uint PAGE_EXECUTE_READWRITE = 0x40;
    public const uint PAGE_READWRITE = 0x04;
    public const uint PAGE_READONLY = 0x02;
    public const uint PAGE_EXECUTE_READ = 0x20;

    // 等待结果
    public const uint WAIT_ABANDONED = 0x00000080;
    public const uint WAIT_OBJECT_0 = 0x00000000;
    public const uint WAIT_TIMEOUT = 0x00000102;
    public const uint INFINITE = 0xFFFFFFFF;
}

/// <summary>
/// Windows API P/Invoke 声明
/// </summary>
public static class NativeMethods
{
    // === 进程操作 ===

    /// <summary>
    /// 打开现有进程对象
    /// </summary>
    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern IntPtr OpenProcess(
        uint dwDesiredAccess,
        bool bInheritHandle,
        int dwProcessId);

    /// <summary>
    /// 关闭对象句柄
    /// </summary>
    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool CloseHandle(IntPtr hObject);

    // === 内存操作 ===

    /// <summary>
    /// 在指定进程的虚拟地址空间中分配内存
    /// </summary>
    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern IntPtr VirtualAllocEx(
        IntPtr hProcess,
        IntPtr lpAddress,
        uint dwSize,
        uint flAllocationType,
        uint flProtect);

    /// <summary>
    /// 释放在指定进程中分配的内存
    /// </summary>
    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool VirtualFreeEx(
        IntPtr hProcess,
        IntPtr lpAddress,
        uint dwSize,
        uint dwFreeType);

    /// <summary>
    /// 改变内存页的保护属性
    /// </summary>
    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool VirtualProtectEx(
        IntPtr hProcess,
        IntPtr lpAddress,
        uint dwSize,
        uint flNewProtect,
        out uint lpflOldProtect);

    /// <summary>
    /// 从指定进程读取内存
    /// </summary>
    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool ReadProcessMemory(
        IntPtr hProcess,
        IntPtr lpBaseAddress,
        byte[] lpBuffer,
        uint nSize,
        out uint lpNumberOfBytesRead);

    /// <summary>
    /// 向指定进程写入内存
    /// </summary>
    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool WriteProcessMemory(
        IntPtr hProcess,
        IntPtr lpBaseAddress,
        byte[] lpBuffer,
        uint nSize,
        out uint lpNumberOfBytesWritten);

    // === 模块和函数 ===

    /// <summary>
    /// 获取模块句柄
    /// </summary>
    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    public static extern IntPtr GetModuleHandle(string lpModuleName);

    /// <summary>
    /// 获取导出函数地址
    /// </summary>
    [DllImport("kernel32.dll", CharSet = CharSet.Ansi, SetLastError = true)]
    public static extern IntPtr GetProcAddress(
        IntPtr hModule,
        string lpProcName);

    // === 线程操作 ===

    /// <summary>
    /// 在远程进程中创建线程
    /// </summary>
    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern IntPtr CreateRemoteThread(
        IntPtr hProcess,
        IntPtr lpThreadAttributes,
        uint dwStackSize,
        IntPtr lpStartAddress,
        IntPtr lpParameter,
        uint dwCreationFlags,
        out IntPtr lpThreadId);

    /// <summary>
    /// 等待对象信号
    /// </summary>
    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern uint WaitForSingleObject(
        IntPtr hHandle,
        uint dwMilliseconds);

    /// <summary>
    /// 获取线程退出代码
    /// </summary>
    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool GetExitCodeThread(
        IntPtr hThread,
        out uint lpExitCode);

    // === 调试和诊断 ===

    /// <summary>
    /// 检查远程调试器是否存在
    /// </summary>
    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool CheckRemoteDebuggerPresent(
        IntPtr hProcess,
        [MarshalAs(UnmanagedType.Bool)] out bool pbDebuggerPresent);

    /// <summary>
    /// 获取当前进程 ID
    /// </summary>
    [DllImport("kernel32.dll")]
    public static extern int GetCurrentProcessId();

    /// <summary>
    /// 获取最后的错误代码
    /// </summary>
    [DllImport("kernel32.dll")]
    public static extern uint GetLastError();

    // === 窗口和输入 ===

    public delegate IntPtr WndProcDelegate(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern IntPtr FindWindow(string? lpClassName, string? lpWindowName);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    public static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    public static extern IntPtr CallWindowProc(IntPtr lpPrevWndFunc, IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    public static extern IntPtr DefWindowProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    public const int GWLP_WNDPROC = -4;

    // === 本地内存操作 (用于内部 Hook) ===

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern bool VirtualProtect(IntPtr lpAddress, uint dwSize, uint flNewProtect, out uint lpflOldProtect);
}

/// <summary>
/// Windows API 错误码
/// </summary>
public static class WinError
{
    public const int ERROR_SUCCESS = 0;
    public const int ERROR_INVALID_HANDLE = 6;
    public const int ERROR_ACCESS_DENIED = 5;
    public const int ERROR_ALREADY_EXISTS = 183;
    public const int ERROR_NOT_FOUND = 1168;
}
