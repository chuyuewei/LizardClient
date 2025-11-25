namespace LizardClient.Injection.Minecraft;

/// <summary>
/// Minecraft 版本信息
/// </summary>
public sealed class MinecraftVersion : IComparable<MinecraftVersion>
{
    public int Major { get; init; }
    public int Minor { get; init; }
    public int Patch { get; init; }
    public string? PreRelease { get; init; }

    public MinecraftVersion(int major, int minor, int patch, string? preRelease = null)
    {
        Major = major;
        Minor = minor;
        Patch = patch;
        PreRelease = preRelease;
    }

    /// <summary>
    /// 解析版本字符串 (例如 "1.20.1" 或 "1.21-pre1")
    /// </summary>
    public static MinecraftVersion? Parse(string versionString)
    {
        if (string.IsNullOrWhiteSpace(versionString))
            return null;

        try
        {
            // 分离预发布标识
            var parts = versionString.Split('-', 2);
            var versionPart = parts[0];
            var preRelease = parts.Length > 1 ? parts[1] : null;

            // 解析版本号
            var numbers = versionPart.Split('.');
            if (numbers.Length < 2)
                return null;

            var major = int.Parse(numbers[0]);
            var minor = int.Parse(numbers[1]);
            var patch = numbers.Length > 2 ? int.Parse(numbers[2]) : 0;

            return new MinecraftVersion(major, minor, patch, preRelease);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// 比较版本
    /// </summary>
    public int CompareTo(MinecraftVersion? other)
    {
        if (other == null) return 1;

        var majorCompare = Major.CompareTo(other.Major);
        if (majorCompare != 0) return majorCompare;

        var minorCompare = Minor.CompareTo(other.Minor);
        if (minorCompare != 0) return minorCompare;

        var patchCompare = Patch.CompareTo(other.Patch);
        if (patchCompare != 0) return patchCompare;

        // 预发布版本小于正式版本
        if (PreRelease != null && other.PreRelease == null) return -1;
        if (PreRelease == null && other.PreRelease != null) return 1;

        return string.Compare(PreRelease, other.PreRelease, StringComparison.Ordinal);
    }

    public override string ToString()
    {
        var version = $"{Major}.{Minor}.{Patch}";
        return PreRelease != null ? $"{version}-{PreRelease}" : version;
    }

    public override bool Equals(object? obj)
    {
        return obj is MinecraftVersion other && CompareTo(other) == 0;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Major, Minor, Patch, PreRelease);
    }

    public static bool operator ==(MinecraftVersion? left, MinecraftVersion? right)
    {
        if (left is null) return right is null;
        return left.Equals(right);
    }

    public static bool operator !=(MinecraftVersion? left, MinecraftVersion? right)
    {
        return !(left == right);
    }

    public static bool operator <(MinecraftVersion left, MinecraftVersion right)
    {
        return left.CompareTo(right) < 0;
    }

    public static bool operator >(MinecraftVersion left, MinecraftVersion right)
    {
        return left.CompareTo(right) > 0;
    }

    public static bool operator <=(MinecraftVersion left, MinecraftVersion right)
    {
        return left.CompareTo(right) <= 0;
    }

    public static bool operator >=(MinecraftVersion left, MinecraftVersion right)
    {
        return left.CompareTo(right) >= 0;
    }
}
