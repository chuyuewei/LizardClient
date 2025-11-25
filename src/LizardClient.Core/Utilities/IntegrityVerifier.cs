using System.Security.Cryptography;
using System.Text;

namespace LizardClient.Core.Utilities;

/// <summary>
/// 文件完整性验证工具
/// </summary>
public static class IntegrityVerifier
{
    /// <summary>
    /// 计算文件的 SHA256 哈希值
    /// </summary>
    /// <param name="filePath">文件路径</param>
    /// <returns>十六进制格式的哈希值</returns>
    public static async Task<string> ComputeSHA256Async(string filePath)
    {
        using var sha256 = SHA256.Create();
        using var stream = File.OpenRead(filePath);

        var hashBytes = await Task.Run(() => sha256.ComputeHash(stream));
        return Convert.ToHexString(hashBytes).ToLower();
    }

    /// <summary>
    /// 计算字节数组的 SHA256 哈希值
    /// </summary>
    public static string ComputeSHA256(byte[] data)
    {
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(data);
        return Convert.ToHexString(hashBytes).ToLower();
    }

    /// <summary>
    /// 计算流的 SHA256 哈希值（适用于大文件）
    /// </summary>
    public static async Task<string> ComputeSHA256Async(Stream stream)
    {
        using var sha256 = SHA256.Create();
        var hashBytes = await sha256.ComputeHashAsync(stream);
        return Convert.ToHexString(hashBytes).ToLower();
    }

    /// <summary>
    /// 验证文件哈希值
    /// </summary>
    /// <param name="filePath">文件路径</param>
    /// <param name="expectedHash">期望的哈希值（不区分大小写）</param>
    /// <returns>是否匹配</returns>
    public static async Task<bool> VerifySHA256Async(string filePath, string expectedHash)
    {
        if (string.IsNullOrEmpty(expectedHash))
            return false;

        var actualHash = await ComputeSHA256Async(filePath);
        return string.Equals(actualHash, expectedHash, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// 计算文件的 SHA512 哈希值（更安全但更慢）
    /// </summary>
    public static async Task<string> ComputeSHA512Async(string filePath)
    {
        using var sha512 = SHA512.Create();
        using var stream = File.OpenRead(filePath);

        var hashBytes = await Task.Run(() => sha512.ComputeHash(stream));
        return Convert.ToHexString(hashBytes).ToLower();
    }

    /// <summary>
    /// 验证文件 SHA512 哈希值
    /// </summary>
    public static async Task<bool> VerifySHA512Async(string filePath, string expectedHash)
    {
        if (string.IsNullOrEmpty(expectedHash))
            return false;

        var actualHash = await ComputeSHA512Async(filePath);
        return string.Equals(actualHash, expectedHash, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// 计算文件的 MD5 哈希值（不推荐用于安全目的）
    /// </summary>
    [Obsolete("MD5 is not secure, use SHA256 instead")]
    public static async Task<string> ComputeMD5Async(string filePath)
    {
        using var md5 = MD5.Create();
        using var stream = File.OpenRead(filePath);

        var hashBytes = await Task.Run(() => md5.ComputeHash(stream));
        return Convert.ToHexString(hashBytes).ToLower();
    }

    /// <summary>
    /// 使用指定算法验证文件
    /// </summary>
    /// <param name="filePath">文件路径</param>
    /// <param name="expectedHash">期望的哈希值</param>
    /// <param name="algorithm">哈希算法 (SHA256, SHA512, MD5)</param>
    public static async Task<bool> VerifyHashAsync(
        string filePath,
        string expectedHash,
        HashAlgorithmType algorithm = HashAlgorithmType.SHA256)
    {
        return algorithm switch
        {
            HashAlgorithmType.SHA256 => await VerifySHA256Async(filePath, expectedHash),
            HashAlgorithmType.SHA512 => await VerifySHA512Async(filePath, expectedHash),
#pragma warning disable CS0618 // Type or member is obsolete
            HashAlgorithmType.MD5 => string.Equals(await ComputeMD5Async(filePath), expectedHash, StringComparison.OrdinalIgnoreCase),
#pragma warning restore CS0618
            _ => throw new ArgumentException($"Unsupported algorithm: {algorithm}")
        };
    }
}

/// <summary>
/// 哈希算法类型
/// </summary>
public enum HashAlgorithmType
{
    /// <summary>
    /// SHA256 (推荐)
    /// </summary>
    SHA256,

    /// <summary>
    /// SHA512 (更安全)
    /// </summary>
    SHA512,

    /// <summary>
    /// MD5 (不推荐)
    /// </summary>
    [Obsolete("MD5 is not secure")]
    MD5
}
