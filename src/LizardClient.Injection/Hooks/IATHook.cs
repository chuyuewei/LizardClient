using LizardClient.Core.Interfaces;
using LizardClient.Injection.Memory;
using LizardClient.Injection.Native;
using System.Runtime.InteropServices;

namespace LizardClient.Injection.Hooks;

/// <summary>
/// IAT Hook - 导入地址表 Hook
/// </summary>
public sealed class IATHook
{
    private readonly ILogger _logger;
    private readonly MemoryManager _memoryManager;

    public IATHook(ILogger logger, MemoryManager memoryManager)
    {
        _logger = logger;
        _memoryManager = memoryManager;
    }

    /// <summary>
    /// Hook 导入地址表中的函数
    /// </summary>
    /// <param name="moduleBase">模块基址</param>
    /// <param name="importModuleName">导入的模块名 (例如 "kernel32.dll")</param>
    /// <param name="functionName">函数名 (例如 "CreateFileW")</param>
    /// <param name="hookAddress">Hook 函数地址</param>
    /// <returns>原始函数地址</returns>
    public IntPtr HookImportFunction(IntPtr moduleBase, string importModuleName, string functionName, IntPtr hookAddress)
    {
        try
        {
            _logger.Info($"开始 IAT Hook: {importModuleName}!{functionName}");

            // 1. 解析 PE 头
            var dosHeader = ReadDosHeader(moduleBase);
            if (dosHeader.e_magic != 0x5A4D) // "MZ"
            {
                _logger.Error("无效的 DOS 头");
                return IntPtr.Zero;
            }

            var ntHeaderAddress = IntPtr.Add(moduleBase, dosHeader.e_lfanew);
            var ntHeaders = ReadNtHeaders(ntHeaderAddress);

            if (ntHeaders.Signature != 0x4550) // "PE"
            {
                _logger.Error("无效的 NT 头");
                return IntPtr.Zero;
            }

            // 2. 获取导入表地址
            var importDescriptorRVA = ntHeaders.OptionalHeader.ImportTable.VirtualAddress;
            if (importDescriptorRVA == 0)
            {
                _logger.Warning("模块没有导入表");
                return IntPtr.Zero;
            }

            var importDescriptorAddress = IntPtr.Add(moduleBase, (int)importDescriptorRVA);

            // 3. 遍历导入描述符
            var currentDescriptor = importDescriptorAddress;
            while (true)
            {
                var descriptor = ReadImportDescriptor(currentDescriptor);

                // 检查是否到达导入表末尾
                if (descriptor.Name == 0)
                    break;

                // 读取导入模块名称
                var moduleNameAddress = IntPtr.Add(moduleBase, (int)descriptor.Name);
                var currentModuleName = _memoryManager.ReadString(moduleNameAddress);

                if (currentModuleName.Equals(importModuleName, StringComparison.OrdinalIgnoreCase))
                {
                    _logger.Info($"找到导入模块: {currentModuleName}");

                    // 4. 遍历导入函数
                    var thunkAddress = IntPtr.Add(moduleBase, (int)descriptor.FirstThunk);
                    var originalThunkAddress = IntPtr.Add(moduleBase, (int)descriptor.OriginalFirstThunk);

                    for (int i = 0; ; i++)
                    {
                        var thunkData = _memoryManager.ReadUInt64(IntPtr.Add(originalThunkAddress, i * 8));

                        if (thunkData == 0)
                            break;

                        // 检查是否是按名称导入 (最高位为 0)
                        if ((thunkData & 0x8000000000000000) == 0)
                        {
                            // 读取函数名
                            var nameAddress = IntPtr.Add(moduleBase, (int)(thunkData + 2)); // +2 跳过 Hint
                            var currentFunctionName = _memoryManager.ReadString(nameAddress);

                            if (currentFunctionName.Equals(functionName, StringComparison.Ordinal))
                            {
                                _logger.Info($"找到导入函数: {currentFunctionName}");

                                // 5. 读取原始函数地址
                                var functionAddressPointer = IntPtr.Add(thunkAddress, i * 8);
                                var originalFunctionAddress = new IntPtr(_memoryManager.ReadInt64(functionAddressPointer));

                                // 6. 修改内存保护
                                if (!_memoryManager.ChangeProtection(functionAddressPointer, 8,
                                    WinApiConstants.PAGE_READWRITE, out var oldProtection))
                                {
                                    _logger.Error("修改 IAT 内存保护失败");
                                    return IntPtr.Zero;
                                }

                                // 7. 写入 Hook 地址
                                if (!_memoryManager.WriteInt64(functionAddressPointer, hookAddress.ToInt64()))
                                {
                                    _logger.Error("写入 Hook 地址失败");
                                    _memoryManager.ChangeProtection(functionAddressPointer, 8, oldProtection, out _);
                                    return IntPtr.Zero;
                                }

                                // 8. 恢复内存保护
                                _memoryManager.ChangeProtection(functionAddressPointer, 8, oldProtection, out _);

                                _logger.Info($"IAT Hook 成功: {importModuleName}!{functionName}");
                                return originalFunctionAddress;
                            }
                        }
                    }
                }

                currentDescriptor = IntPtr.Add(currentDescriptor, Marshal.SizeOf<IMAGE_IMPORT_DESCRIPTOR>());
            }

            _logger.Warning($"未找到导入函数: {importModuleName}!{functionName}");
            return IntPtr.Zero;
        }
        catch (Exception ex)
        {
            _logger.Error($"IAT Hook 失败: {ex.Message}", ex);
            return IntPtr.Zero;
        }
    }

    /// <summary>
    /// 恢复 IAT 中的函数
    /// </summary>
    public bool UnhookImportFunction(IntPtr moduleBase, string importModuleName, string functionName, IntPtr originalAddress)
    {
        // 实现与 HookImportFunction 类似，但写入原始地址
        // 为简洁起见，这里省略实现细节
        return HookImportFunction(moduleBase, importModuleName, functionName, originalAddress) != IntPtr.Zero;
    }

    // === PE 结构定义 ===

    [StructLayout(LayoutKind.Sequential)]
    private struct IMAGE_DOS_HEADER
    {
        public ushort e_magic;
        public ushort e_cblp;
        public ushort e_cp;
        public ushort e_crlc;
        public ushort e_cparhdr;
        public ushort e_minalloc;
        public ushort e_maxalloc;
        public ushort e_ss;
        public ushort e_sp;
        public ushort e_csum;
        public ushort e_ip;
        public ushort e_cs;
        public ushort e_lfarlc;
        public ushort e_ovno;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public ushort[] e_res1;
        public ushort e_oemid;
        public ushort e_oeminfo;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 10)]
        public ushort[] e_res2;
        public int e_lfanew;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct IMAGE_NT_HEADERS64
    {
        public uint Signature;
        public IMAGE_FILE_HEADER FileHeader;
        public IMAGE_OPTIONAL_HEADER64 OptionalHeader;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct IMAGE_FILE_HEADER
    {
        public ushort Machine;
        public ushort NumberOfSections;
        public uint TimeDateStamp;
        public uint PointerToSymbolTable;
        public uint NumberOfSymbols;
        public ushort SizeOfOptionalHeader;
        public ushort Characteristics;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct IMAGE_OPTIONAL_HEADER64
    {
        public ushort Magic;
        public byte MajorLinkerVersion;
        public byte MinorLinkerVersion;
        public uint SizeOfCode;
        public uint SizeOfInitializedData;
        public uint SizeOfUninitializedData;
        public uint AddressOfEntryPoint;
        public uint BaseOfCode;
        public ulong ImageBase;
        public uint SectionAlignment;
        public uint FileAlignment;
        public ushort MajorOperatingSystemVersion;
        public ushort MinorOperatingSystemVersion;
        public ushort MajorImageVersion;
        public ushort MinorImageVersion;
        public ushort MajorSubsystemVersion;
        public ushort MinorSubsystemVersion;
        public uint Win32VersionValue;
        public uint SizeOfImage;
        public uint SizeOfHeaders;
        public uint CheckSum;
        public ushort Subsystem;
        public ushort DllCharacteristics;
        public ulong SizeOfStackReserve;
        public ulong SizeOfStackCommit;
        public ulong SizeOfHeapReserve;
        public ulong SizeOfHeapCommit;
        public uint LoaderFlags;
        public uint NumberOfRvaAndSizes;
        public IMAGE_DATA_DIRECTORY ExportTable;
        public IMAGE_DATA_DIRECTORY ImportTable;
        // ... 其他数据目录省略
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct IMAGE_DATA_DIRECTORY
    {
        public uint VirtualAddress;
        public uint Size;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct IMAGE_IMPORT_DESCRIPTOR
    {
        public uint OriginalFirstThunk;
        public uint TimeDateStamp;
        public uint ForwarderChain;
        public uint Name;
        public uint FirstThunk;
    }

    // === 辅助方法 ===

    private IMAGE_DOS_HEADER ReadDosHeader(IntPtr address)
    {
        var bytes = _memoryManager.ReadBytes(address, Marshal.SizeOf<IMAGE_DOS_HEADER>());
        var handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
        try
        {
            return Marshal.PtrToStructure<IMAGE_DOS_HEADER>(handle.AddrOfPinnedObject());
        }
        finally
        {
            handle.Free();
        }
    }

    private IMAGE_NT_HEADERS64 ReadNtHeaders(IntPtr address)
    {
        var bytes = _memoryManager.ReadBytes(address, Marshal.SizeOf<IMAGE_NT_HEADERS64>());
        var handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
        try
        {
            return Marshal.PtrToStructure<IMAGE_NT_HEADERS64>(handle.AddrOfPinnedObject());
        }
        finally
        {
            handle.Free();
        }
    }

    private IMAGE_IMPORT_DESCRIPTOR ReadImportDescriptor(IntPtr address)
    {
        var bytes = _memoryManager.ReadBytes(address, Marshal.SizeOf<IMAGE_IMPORT_DESCRIPTOR>());
        var handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
        try
        {
            return Marshal.PtrToStructure<IMAGE_IMPORT_DESCRIPTOR>(handle.AddrOfPinnedObject());
        }
        finally
        {
            handle.Free();
        }
    }
}
