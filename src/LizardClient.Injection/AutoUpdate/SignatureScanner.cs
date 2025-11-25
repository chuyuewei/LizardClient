using LizardClient.Core.Interfaces;
using LizardClient.Injection.Memory;
using System.Diagnostics;
using System.Text;

namespace LizardClient.Injection.AutoUpdate;

/// <summary>
/// 增强的签名扫描器 - 支持通配符模式
/// </summary>
public sealed class SignatureScanner
{
    private readonly ILogger _logger;
    private readonly MemoryManager _memoryManager;

    public SignatureScanner(ILogger logger, MemoryManager memoryManager)
    {
        _logger = logger;
        _memoryManager = memoryManager;
    }

    /// <summary>
    /// 在模块中扫描签名
    /// </summary>
    /// <param name="moduleName">模块名称 (例如 "javaw.exe")</param>
    /// <param name="signature">十六进制签名字符串 (例如 "48 8B 05 ?? ?? ?? ??")</param>
    /// <param name="offset">从匹配位置的偏移</param>
    /// <returns>找到的地址</returns>
    public IntPtr ScanModule(string moduleName, string signature, int offset = 0)
    {
        try
        {
            // 1. 获取目标进程的模块
            var process = Process.GetProcessById(_memoryManager.ProcessId);
            var module = process.Modules.Cast<ProcessModule>()
                .FirstOrDefault(m => m.ModuleName?.Equals(moduleName, StringComparison.OrdinalIgnoreCase) == true);

            if (module == null)
            {
                _logger.Warning($"未找到模块: {moduleName}");
                return IntPtr.Zero;
            }

            _logger.Info($"扫描模块: {moduleName} (基址: 0x{module.BaseAddress:X}, 大小: {module.ModuleMemorySize} 字节)");

            // 2. 解析签名
            var (pattern, mask) = ParseSignature(signature);

            // 3. 扫描内存
            var result = ScanMemoryRegion(module.BaseAddress, module.ModuleMemorySize, pattern, mask);

            if (result != IntPtr.Zero)
            {
                result = IntPtr.Add(result, offset);
                _logger.Info($"签名匹配成功: 0x{result:X}");
            }
            else
            {
                _logger.Warning($"签名匹配失败: {signature}");
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.Error($"签名扫描异常: {ex.Message}", ex);
            return IntPtr.Zero;
        }
    }

    /// <summary>
    /// 扫描内存区域
    /// </summary>
    private IntPtr ScanMemoryRegion(IntPtr baseAddress, int size, byte[] pattern, string mask)
    {
        const int chunkSize = 0x100000; // 1 MB chunks
        var totalScanned = 0;

        while (totalScanned < size)
        {
            var currentChunkSize = Math.Min(chunkSize, size - totalScanned);
            var currentAddress = IntPtr.Add(baseAddress, totalScanned);

            // 读取内存块
            var data = _memoryManager.ReadBytes(currentAddress, currentChunkSize);

            if (data.Length == 0)
            {
                totalScanned += currentChunkSize;
                continue;
            }

            // 在当前块中搜索模式
            for (int i = 0; i < data.Length - pattern.Length; i++)
            {
                if (MatchPattern(data, i, pattern, mask))
                {
                    var foundAddress = IntPtr.Add(currentAddress, i);
                    return foundAddress;
                }
            }

            totalScanned += currentChunkSize;
        }

        return IntPtr.Zero;
    }

    /// <summary>
    /// 匹配模式
    /// </summary>
    private bool MatchPattern(byte[] data, int offset, byte[] pattern, string mask)
    {
        for (int i = 0; i < pattern.Length; i++)
        {
            if (mask[i] == '?' || mask[i] == '*')
                continue;

            if (offset + i >= data.Length || data[offset + i] != pattern[i])
                return false;
        }

        return true;
    }

    /// <summary>
    /// 解析签名字符串
    /// 支持格式: "48 8B 05 ?? ?? ?? 00" 或 "48 8B 05 * * * 00"
    /// </summary>
    private (byte[] pattern, string mask) ParseSignature(string signature)
    {
        var parts = signature.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var pattern = new List<byte>();
        var mask = new StringBuilder();

        foreach (var part in parts)
        {
            if (part == "??" || part == "?" || part == "*")
            {
                pattern.Add(0x00); // 占位符
                mask.Append('?');
            }
            else
            {
                pattern.Add(Convert.ToByte(part, 16));
                mask.Append('x');
            }
        }

        return (pattern.ToArray(), mask.ToString());
    }

    /// <summary>
    /// 多模式扫描 (尝试多个签名)
    /// </summary>
    public IntPtr ScanMultiplePatterns(string moduleName, IEnumerable<(string signature, int offset)> patterns)
    {
        foreach (var (signature, offset) in patterns)
        {
            var result = ScanModule(moduleName, signature, offset);
            if (result != IntPtr.Zero)
            {
                _logger.Info($"使用备用签名找到匹配: {signature}");
                return result;
            }
        }

        _logger.Warning("所有签名模式均未找到匹配");
        return IntPtr.Zero;
    }

    /// <summary>
    /// 验证地址是否有效
    /// </summary>
    public bool ValidateAddress(IntPtr address, int expectedMinSize = 10)
    {
        if (address == IntPtr.Zero)
            return false;

        try
        {
            // 尝试读取一些字节来验证地址可访问
            var testBytes = _memoryManager.ReadBytes(address, expectedMinSize);
            return testBytes.Length >= expectedMinSize;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 扫描并验证
    /// </summary>
    public IntPtr ScanAndValidate(string moduleName, string signature, int offset = 0, int validationSize = 10)
    {
        var address = ScanModule(moduleName, signature, offset);

        if (address != IntPtr.Zero && ValidateAddress(address, validationSize))
        {
            _logger.Info($"地址验证成功: 0x{address:X}");
            return address;
        }

        if (address != IntPtr.Zero)
        {
            _logger.Warning($"地址验证失败: 0x{address:X}");
        }

        return IntPtr.Zero;
    }
}
