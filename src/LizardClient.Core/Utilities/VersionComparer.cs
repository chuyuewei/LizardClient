using System.Text.RegularExpressions;

namespace LizardClient.Core.Utilities;

/// <summary>
/// 语义化版本比较工具
/// 支持 SemVer 格式: MAJOR.MINOR.PATCH[-PRERELEASE][+BUILD]
/// </summary>
public static class VersionComparer
{
    private static readonly Regex VersionRegex = new Regex(
        @"^(?<major>\d+)\.(?<minor>\d+)\.(?<patch>\d+)(?:-(?<prerelease>[0-9A-Za-z\-\.]+))?(?:\+(?<build>[0-9A-Za-z\-\.]+))?$",
        RegexOptions.Compiled
    );

    /// <summary>
    /// 比较两个版本号
    /// </summary>
    /// <param name="version1">版本1</param>
    /// <param name="version2">版本2</param>
    /// <returns>
    /// 负数: version1 < version2
    /// 0: version1 == version2
    /// 正数: version1 > version2
    /// </returns>
    public static int Compare(string version1, string version2)
    {
        var v1 = ParseVersion(version1);
        var v2 = ParseVersion(version2);

        if (v1 == null && v2 == null) return 0;
        if (v1 == null) return -1;
        if (v2 == null) return 1;

        // 比较 Major
        if (v1.Major != v2.Major)
            return v1.Major.CompareTo(v2.Major);

        // 比较 Minor
        if (v1.Minor != v2.Minor)
            return v1.Minor.CompareTo(v2.Minor);

        // 比较 Patch
        if (v1.Patch != v2.Patch)
            return v1.Patch.CompareTo(v2.Patch);

        // 比较 Prerelease (有 prerelease 的版本 < 没有 prerelease 的版本)
        if (string.IsNullOrEmpty(v1.Prerelease) && !string.IsNullOrEmpty(v2.Prerelease))
            return 1;
        if (!string.IsNullOrEmpty(v1.Prerelease) && string.IsNullOrEmpty(v2.Prerelease))
            return -1;
        if (!string.IsNullOrEmpty(v1.Prerelease) && !string.IsNullOrEmpty(v2.Prerelease))
            return string.Compare(v1.Prerelease, v2.Prerelease, StringComparison.Ordinal);

        return 0;
    }

    /// <summary>
    /// 检查 version1 是否大于 version2
    /// </summary>
    public static bool IsGreaterThan(string version1, string version2)
    {
        return Compare(version1, version2) > 0;
    }

    /// <summary>
    /// 检查 version1 是否小于 version2
    /// </summary>
    public static bool IsLessThan(string version1, string version2)
    {
        return Compare(version1, version2) < 0;
    }

    /// <summary>
    /// 检查 version1 是否等于 version2
    /// </summary>
    public static bool IsEqual(string version1, string version2)
    {
        return Compare(version1, version2) == 0;
    }

    /// <summary>
    /// 检查版本是否在指定范围内
    /// </summary>
    /// <param name="version">要检查的版本</param>
    /// <param name="range">版本范围 (例如: ">=1.0.0", "1.0.0-2.0.0", "*")</param>
    public static bool IsInRange(string version, string range)
    {
        if (range == "*" || string.IsNullOrEmpty(range))
            return true;

        // 处理 >= 格式
        if (range.StartsWith(">="))
        {
            var minVersion = range.Substring(2).Trim();
            return Compare(version, minVersion) >= 0;
        }

        // 处理 > 格式
        if (range.StartsWith(">"))
        {
            var minVersion = range.Substring(1).Trim();
            return Compare(version, minVersion) > 0;
        }

        // 处理 <= 格式
        if (range.StartsWith("<="))
        {
            var maxVersion = range.Substring(2).Trim();
            return Compare(version, maxVersion) <= 0;
        }

        // 处理 < 格式
        if (range.StartsWith("<"))
        {
            var maxVersion = range.Substring(1).Trim();
            return Compare(version, maxVersion) < 0;
        }

        // 处理范围格式 "1.0.0-2.0.0"
        if (range.Contains("-") && !range.Contains(">=") && !range.Contains("<="))
        {
            var parts = range.Split('-');
            if (parts.Length == 2)
            {
                return Compare(version, parts[0].Trim()) >= 0 &&
                       Compare(version, parts[1].Trim()) <= 0;
            }
        }

        // 精确匹配
        return IsEqual(version, range);
    }

    /// <summary>
    /// 解析版本字符串
    /// </summary>
    private static SemanticVersion? ParseVersion(string versionString)
    {
        if (string.IsNullOrWhiteSpace(versionString))
            return null;

        var match = VersionRegex.Match(versionString.Trim());
        if (!match.Success)
        {
            // 尝试简单格式 (仅包含数字和点)
            var simpleParts = versionString.Split('.');
            if (simpleParts.Length >= 3 &&
                int.TryParse(simpleParts[0], out int major) &&
                int.TryParse(simpleParts[1], out int minor) &&
                int.TryParse(simpleParts[2], out int patch))
            {
                return new SemanticVersion
                {
                    Major = major,
                    Minor = minor,
                    Patch = patch
                };
            }

            return null;
        }

        return new SemanticVersion
        {
            Major = int.Parse(match.Groups["major"].Value),
            Minor = int.Parse(match.Groups["minor"].Value),
            Patch = int.Parse(match.Groups["patch"].Value),
            Prerelease = match.Groups["prerelease"].Value,
            Build = match.Groups["build"].Value
        };
    }

    /// <summary>
    /// 验证版本字符串格式是否有效
    /// </summary>
    public static bool IsValidVersion(string versionString)
    {
        return ParseVersion(versionString) != null;
    }

    /// <summary>
    /// 获取版本的主版本号
    /// </summary>
    public static int? GetMajorVersion(string versionString)
    {
        return ParseVersion(versionString)?.Major;
    }

    /// <summary>
    /// 获取版本的次版本号
    /// </summary>
    public static int? GetMinorVersion(string versionString)
    {
        return ParseVersion(versionString)?.Minor;
    }

    /// <summary>
    /// 获取版本的补丁号
    /// </summary>
    public static int? GetPatchVersion(string versionString)
    {
        return ParseVersion(versionString)?.Patch;
    }

    /// <summary>
    /// 语义化版本结构
    /// </summary>
    private class SemanticVersion
    {
        public int Major { get; set; }
        public int Minor { get; set; }
        public int Patch { get; set; }
        public string Prerelease { get; set; } = string.Empty;
        public string Build { get; set; } = string.Empty;
    }
}
