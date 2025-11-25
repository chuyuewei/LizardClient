using LizardClient.Core.Interfaces;
using LizardClient.ModSystem.Models;
using System.Text.RegularExpressions;

namespace LizardClient.ModSystem.Core;

/// <summary>
/// 依赖解析结果
/// </summary>
public class ResolutionResult
{
    public bool IsSuccess { get; set; }
    public List<ModMetadata> SortedMods { get; set; } = new();
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
}

/// <summary>
/// 模组依赖解析器
/// </summary>
public sealed class DependencyResolver
{
    private readonly ILogger _logger;

    public DependencyResolver(ILogger logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// 解析依赖并确定加载顺序
    /// </summary>
    public ResolutionResult Resolve(IEnumerable<ModMetadata> mods)
    {
        var result = new ResolutionResult();
        var modMap = mods.ToDictionary(m => m.Id, m => m);
        var sorted = new List<ModMetadata>();
        var visited = new HashSet<string>();
        var processing = new HashSet<string>();

        // 1. 检查缺失的依赖和不兼容性
        foreach (var mod in mods)
        {
            // 检查依赖
            foreach (var dep in mod.Dependencies)
            {
                if (!modMap.ContainsKey(dep.ModId))
                {
                    if (!dep.IsOptional)
                    {
                        result.Errors.Add($"模组 '{mod.Name}' ({mod.Id}) 缺少必需依赖: {dep.ModId} ({dep.VersionRange})");
                        result.IsSuccess = false;
                    }
                    else
                    {
                        result.Warnings.Add($"模组 '{mod.Name}' ({mod.Id}) 缺少可选依赖: {dep.ModId}");
                    }
                }
                else
                {
                    var depMod = modMap[dep.ModId];
                    if (!CheckVersion(depMod.Version, dep.VersionRange))
                    {
                        var msg = $"模组 '{mod.Name}' 需要 {dep.ModId} 版本 {dep.VersionRange}，但找到的是 {depMod.Version}";
                        if (!dep.IsOptional)
                        {
                            result.Errors.Add(msg);
                            result.IsSuccess = false;
                        }
                        else
                        {
                            result.Warnings.Add(msg);
                        }
                    }
                }
            }

            // 检查不兼容性
            foreach (var incompatibleId in mod.Incompatibilities)
            {
                if (modMap.ContainsKey(incompatibleId))
                {
                    result.Errors.Add($"模组 '{mod.Name}' 与 '{modMap[incompatibleId].Name}' ({incompatibleId}) 不兼容");
                    result.IsSuccess = false;
                }
            }
        }

        if (!result.IsSuccess)
        {
            return result;
        }

        // 2. 拓扑排序
        try
        {
            foreach (var mod in mods)
            {
                Visit(mod, modMap, visited, processing, sorted);
            }

            result.SortedMods = sorted;
            result.IsSuccess = true;
        }
        catch (InvalidOperationException ex) // 循环依赖
        {
            result.Errors.Add(ex.Message);
            result.IsSuccess = false;
        }

        return result;
    }

    private void Visit(
        ModMetadata mod,
        Dictionary<string, ModMetadata> modMap,
        HashSet<string> visited,
        HashSet<string> processing,
        List<ModMetadata> sorted)
    {
        if (visited.Contains(mod.Id)) return;

        if (processing.Contains(mod.Id))
        {
            throw new InvalidOperationException($"检测到循环依赖: {mod.Id}");
        }

        processing.Add(mod.Id);

        // 处理显式依赖
        foreach (var dep in mod.Dependencies)
        {
            if (modMap.ContainsKey(dep.ModId))
            {
                Visit(modMap[dep.ModId], modMap, visited, processing, sorted);
            }
        }

        // 处理 LoadAfter (当前模组应该在这些模组之后加载 -> 相当于依赖它们)
        foreach (var afterId in mod.LoadAfter)
        {
            if (modMap.ContainsKey(afterId))
            {
                Visit(modMap[afterId], modMap, visited, processing, sorted);
            }
        }

        // 注意: LoadBefore 处理比较复杂，通常在构建图时反向处理
        // 这里简化处理，仅支持基本的 LoadAfter 和 Dependencies

        processing.Remove(mod.Id);
        visited.Add(mod.Id);
        sorted.Add(mod);
    }

    /// <summary>
    /// 增强的版本检查 (使用 SemVer)
    /// </summary>
    private bool CheckVersion(string version, string range)
    {
        try
        {
            return LizardClient.Core.Utilities.VersionComparer.IsInRange(version, range);
        }
        catch (Exception ex)
        {
            _logger.Warning($"Version check failed for {version} against {range}: {ex.Message}");
            return false;
        }
    }
}
