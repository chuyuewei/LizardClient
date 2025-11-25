using LizardClient.Core.Interfaces;
using LizardClient.Injection.Native;
using System.Runtime.InteropServices;
using System.Text;

namespace LizardClient.Injection.Memory;

/// <summary>
/// 内存管理器 - 提供进程内存读写功能
/// </summary>
public sealed class MemoryManager : IDisposable
{
    private readonly ILogger _logger;
    private readonly IntPtr _processHandle;
    private readonly int _processId;
    private bool _disposed;

    public MemoryManager(ILogger logger, int processId)
    {
        _logger = logger;
        _processId = processId;

        // 打开进程
        _processHandle = NativeMethods.OpenProcess(
            WinApiConstants.PROCESS_VM_READ | WinApiConstants.PROCESS_VM_WRITE | WinApiConstants.PROCESS_VM_OPERATION,
            false,
            processId);

        if (_processHandle == IntPtr.Zero)
        {
            var error = Marshal.GetLastWin32Error();
            throw new InvalidOperationException($"无法打开进程 {processId}，错误代码: {error}");
        }

        _logger.Info($"内存管理器已初始化 (进程 ID: {processId})");
    }

    /// <summary>
    /// 进程句柄
    /// </summary>
    public IntPtr ProcessHandle => _processHandle;

    /// <summary>
    /// 进程 ID
    /// </summary>
    public int ProcessId => _processId;

    // === 读取内存 ===

    /// <summary>
    /// 读取字节数组
    /// </summary>
    public byte[] ReadBytes(IntPtr address, int size)
    {
        var buffer = new byte[size];

        if (!NativeMethods.ReadProcessMemory(_processHandle, address, buffer, (uint)size, out var bytesRead))
        {
            var error = Marshal.GetLastWin32Error();
            _logger.Warning($"读取内存失败 (地址: 0x{address:X}, 大小: {size}), 错误: {error}");
            return Array.Empty<byte>();
        }

        return buffer;
    }

    /// <summary>
    /// 读取 Int32
    /// </summary>
    public int ReadInt32(IntPtr address)
    {
        var bytes = ReadBytes(address, sizeof(int));
        return bytes.Length >= sizeof(int) ? BitConverter.ToInt32(bytes, 0) : 0;
    }

    /// <summary>
    /// 读取 Int64
    /// </summary>
    public long ReadInt64(IntPtr address)
    {
        var bytes = ReadBytes(address, sizeof(long));
        return bytes.Length >= sizeof(long) ? BitConverter.ToInt64(bytes, 0) : 0;
    }

    /// <summary>
    /// 读取 UInt64
    /// </summary>
    public ulong ReadUInt64(IntPtr address)
    {
        var bytes = ReadBytes(address, sizeof(ulong));
        return bytes.Length >= sizeof(ulong) ? BitConverter.ToUInt64(bytes, 0) : 0;
    }

    /// <summary>
    /// 读取 Float
    /// </summary>
    public float ReadFloat(IntPtr address)
    {
        var bytes = ReadBytes(address, sizeof(float));
        return bytes.Length >= sizeof(float) ? BitConverter.ToSingle(bytes, 0) : 0f;
    }

    /// <summary>
    /// 读取 Double
    /// </summary>
    public double ReadDouble(IntPtr address)
    {
        var bytes = ReadBytes(address, sizeof(double));
        return bytes.Length >= sizeof(double) ? BitConverter.ToDouble(bytes, 0) : 0.0;
    }

    /// <summary>
    /// 读取 Boolean
    /// </summary>
    public bool ReadBoolean(IntPtr address)
    {
        var bytes = ReadBytes(address, 1);
        return bytes.Length > 0 && bytes[0] != 0;
    }

    /// <summary>
    /// 读取字符串 (UTF-8)
    /// </summary>
    public string ReadString(IntPtr address, int maxLength = 256)
    {
        var bytes = ReadBytes(address, maxLength);
        var nullIndex = Array.IndexOf(bytes, (byte)0);

        if (nullIndex >= 0)
        {
            bytes = bytes.Take(nullIndex).ToArray();
        }

        return Encoding.UTF8.GetString(bytes);
    }

    // === 写入内存 ===

    /// <summary>
    /// 写入字节数组
    /// </summary>
    public bool WriteBytes(IntPtr address, byte[] data)
    {
        if (!NativeMethods.WriteProcessMemory(_processHandle, address, data, (uint)data.Length, out var bytesWritten))
        {
            var error = Marshal.GetLastWin32Error();
            _logger.Warning($"写入内存失败 (地址: 0x{address:X}, 大小: {data.Length}), 错误: {error}");
            return false;
        }

        return bytesWritten == data.Length;
    }

    /// <summary>
    /// 写入 Int32
    /// </summary>
    public bool WriteInt32(IntPtr address, int value)
    {
        return WriteBytes(address, BitConverter.GetBytes(value));
    }

    /// <summary>
    /// 写入 Int64
    /// </summary>
    public bool WriteInt64(IntPtr address, long value)
    {
        return WriteBytes(address, BitConverter.GetBytes(value));
    }

    /// <summary>
    /// 写入 Float
    /// </summary>
    public bool WriteFloat(IntPtr address, float value)
    {
        return WriteBytes(address, BitConverter.GetBytes(value));
    }

    /// <summary>
    /// 写入 Double
    /// </summary>
    public bool WriteDouble(IntPtr address, double value)
    {
        return WriteBytes(address, BitConverter.GetBytes(value));
    }

    /// <summary>
    /// 写入 Boolean
    /// </summary>
    public bool WriteBoolean(IntPtr address, bool value)
    {
        return WriteBytes(address, new[] { (byte)(value ? 1 : 0) });
    }

    /// <summary>
    /// 写入字符串 (UTF-8)
    /// </summary>
    public bool WriteString(IntPtr address, string value)
    {
        var bytes = Encoding.UTF8.GetBytes(value + '\0');
        return WriteBytes(address, bytes);
    }

    // === 内存分配 ===

    /// <summary>
    /// 在目标进程中分配内存
    /// </summary>
    public IntPtr AllocateMemory(uint size, uint protection = WinApiConstants.PAGE_EXECUTE_READWRITE)
    {
        var address = NativeMethods.VirtualAllocEx(
            _processHandle,
            IntPtr.Zero,
            size,
            WinApiConstants.MEM_COMMIT | WinApiConstants.MEM_RESERVE,
            protection);

        if (address == IntPtr.Zero)
        {
            var error = Marshal.GetLastWin32Error();
            _logger.Error($"分配内存失败 (大小: {size}), 错误: {error}");
        }
        else
        {
            _logger.Info($"已分配内存: 0x{address:X} (大小: {size} 字节)");
        }

        return address;
    }

    /// <summary>
    /// 释放内存
    /// </summary>
    public bool FreeMemory(IntPtr address)
    {
        if (NativeMethods.VirtualFreeEx(_processHandle, address, 0, WinApiConstants.MEM_RELEASE))
        {
            _logger.Info($"已释放内存: 0x{address:X}");
            return true;
        }

        var error = Marshal.GetLastWin32Error();
        _logger.Warning($"释放内存失败 (地址: 0x{address:X}), 错误: {error}");
        return false;
    }

    /// <summary>
    /// 修改内存保护
    /// </summary>
    public bool ChangeProtection(IntPtr address, uint size, uint newProtection, out uint oldProtection)
    {
        return NativeMethods.VirtualProtectEx(_processHandle, address, size, newProtection, out oldProtection);
    }

    /// <summary>
    /// 模式扫描 (查找特征码)
    /// </summary>
    public IntPtr PatternScan(IntPtr startAddress, int scanSize, byte[] pattern, string mask)
    {
        var data = ReadBytes(startAddress, scanSize);

        for (int i = 0; i < data.Length - pattern.Length; i++)
        {
            bool found = true;

            for (int j = 0; j < pattern.Length; j++)
            {
                if (mask[j] != '?' && data[i + j] != pattern[j])
                {
                    found = false;
                    break;
                }
            }

            if (found)
            {
                var foundAddress = IntPtr.Add(startAddress, i);
                _logger.Info($"模式扫描成功: 0x{foundAddress:X}");
                return foundAddress;
            }
        }

        _logger.Warning("模式扫描失败: 未找到匹配");
        return IntPtr.Zero;
    }

    public void Dispose()
    {
        if (_disposed) return;

        if (_processHandle != IntPtr.Zero)
        {
            NativeMethods.CloseHandle(_processHandle);
            _logger.Info("内存管理器已释放");
        }

        _disposed = true;
    }
}
