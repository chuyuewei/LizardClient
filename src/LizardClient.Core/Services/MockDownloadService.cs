using System.Net.Http;
using LizardClient.Core.Interfaces;
using LizardClient.Core.Models;

namespace LizardClient.Core.Services;

/// <summary>
/// 模拟下载服务实现（实际应用中应连接真实API）
/// </summary>
public class MockDownloadService : IDownloadService
{
    private readonly ILogger _logger;
    private readonly HttpClient _httpClient;

    public MockDownloadService(ILogger logger)
    {
        _logger = logger;
        _httpClient = new HttpClient();
    }

    public async Task<List<DownloadItem>> GetMinecraftVersionsAsync()
    {
        await Task.Delay(500); // 模拟网络延迟

        return new List<DownloadItem>
        {
            new() { Id = "1.20.4", Name = "Minecraft 1.20.4", Version = "1.20.4", Type = DownloadItemType.MinecraftVersion, ReleaseDate = new DateTime(2023, 12, 7), Description = "最新版本", FileSize = 25 * 1024 * 1024, MinecraftVersion = "1.20.4" },
            new() { Id = "1.20.1", Name = "Minecraft 1.20.1", Version = "1.20.1", Type = DownloadItemType.MinecraftVersion, ReleaseDate = new DateTime(2023, 6, 12), Description = "稳定版本", FileSize = 24 * 1024 * 1024, MinecraftVersion = "1.20.1" },
            new() { Id = "1.19.4", Name = "Minecraft 1.19.4", Version = "1.19.4", Type = DownloadItemType.MinecraftVersion, ReleaseDate = new DateTime(2023, 3, 14), Description = "流行版本", FileSize = 23 * 1024 * 1024, MinecraftVersion = "1.19.4" },
            new() { Id = "1.18.2", Name = "Minecraft 1.18.2", Version = "1.18.2", Type = DownloadItemType.MinecraftVersion, ReleaseDate = new DateTime(2022, 2, 28), Description = "长期支持", FileSize = 22 * 1024 * 1024, MinecraftVersion = "1.18.2" },
            new() { Id = "1.16.5", Name = "Minecraft 1.16.5", Version = "1.16.5", Type = DownloadItemType.MinecraftVersion, ReleaseDate = new DateTime(2021, 1, 15), Description = "经典版本", FileSize = 20 * 1024 * 1024, MinecraftVersion = "1.16.5" },
            new() { Id = "1.12.2", Name = "Minecraft 1.12.2", Version = "1.12.2", Type = DownloadItemType.MinecraftVersion, ReleaseDate = new DateTime(2017, 9, 18), Description = "Mod 黄金版本", FileSize = 18 * 1024 * 1024, MinecraftVersion = "1.12.2" },
            new() { Id = "1.8.9", Name = "Minecraft 1.8.9", Version = "1.8.9", Type = DownloadItemType.MinecraftVersion, ReleaseDate = new DateTime(2015, 12, 9), Description = "PVP 经典", FileSize = 15 * 1024 * 1024, MinecraftVersion = "1.8.9" },
        };
    }

    public async Task<List<DownloadItem>> GetModLoadersAsync(string minecraftVersion)
    {
        await Task.Delay(300);

        var loaders = new List<DownloadItem>();

        // Forge
        loaders.Add(new DownloadItem
        {
            Id = $"forge-{minecraftVersion}",
            Name = $"Forge for {minecraftVersion}",
            Version = "Latest",
            Type = DownloadItemType.ModLoader,
            LoaderType = ModLoaderType.Forge,
            MinecraftVersion = minecraftVersion,
            Description = "最流行的 Mod 加载器",
            FileSize = 5 * 1024 * 1024,
            ReleaseDate = DateTime.Now.AddDays(-10),
            Downloads = 1000000
        });

        // Fabric
        loaders.Add(new DownloadItem
        {
            Id = $"fabric-{minecraftVersion}",
            Name = $"Fabric for {minecraftVersion}",
            Version = "Latest",
            Type = DownloadItemType.ModLoader,
            LoaderType = ModLoaderType.Fabric,
            MinecraftVersion = minecraftVersion,
            Description = "轻量级现代 Mod 加载器",
            FileSize = 3 * 1024 * 1024,
            ReleaseDate = DateTime.Now.AddDays(-5),
            Downloads = 500000
        });

        // Quilt (仅限较新版本)
        if (float.Parse(minecraftVersion.Split('.')[1]) >= 18)
        {
            loaders.Add(new DownloadItem
            {
                Id = $"quilt-{minecraftVersion}",
                Name = $"Quilt for {minecraftVersion}",
                Version = "Latest",
                Type = DownloadItemType.ModLoader,
                LoaderType = ModLoaderType.Quilt,
                MinecraftVersion = minecraftVersion,
                Description = "Fabric 的分支，带有额外功能",
                FileSize = 3 * 1024 * 1024,
                ReleaseDate = DateTime.Now.AddDays(-3),
                Downloads = 100000
            });
        }

        return loaders;
    }

    public async Task<List<DownloadItem>> SearchModsAsync(string query, string minecraftVersion, ModLoaderType? loaderType = null)
    {
        await Task.Delay(400);

        var mods = new List<DownloadItem>
        {
            new() { Id = "jei", Name = "Just Enough Items (JEI)", Version = "15.2.0.27", Type = DownloadItemType.Mod, MinecraftVersion = minecraftVersion, Author = "mezz", Description = "物品和配方查看器", FileSize = 2 * 1024 * 1024, Downloads = 500000, IconUrl = "" },
            new() { Id = "optifine", Name = "OptiFine", Version = "HD U I5", Type = DownloadItemType.Mod, MinecraftVersion = minecraftVersion, Author = "sp614x", Description = "优化和光影支持", FileSize = 3 * 1024 * 1024, Downloads = 1000000, IconUrl = "" },
            new() { Id = "journeymap", Name = "JourneyMap", Version = "5.9.7", Type = DownloadItemType.Mod, MinecraftVersion = minecraftVersion, Author = "techbrew", Description = "实时小地图", FileSize = 2 * 1024 * 1024, Downloads = 300000, IconUrl = "" },
            new() { Id = "biomes", Name = "Biomes O' Plenty", Version = "18.0.0.590", Type = DownloadItemType.Mod, MinecraftVersion = minecraftVersion, Author = "Forstride", Description = "添加80+新生物群系", FileSize = 15 * 1024 * 1024, Downloads = 250000, IconUrl = "" },
            new() { Id = "tinkers", Name = "Tinkers' Construct", Version = "3.7.1.126", Type = DownloadItemType.Mod, MinecraftVersion = minecraftVersion, Author = "mDiyo", Description = "工具制作系统", FileSize = 8 * 1024 * 1024, Downloads = 400000, IconUrl = "" },
            new() { Id = "waila", Name = "WTHIT (What The Hell Is That)", Version = "7.1.0", Type = DownloadItemType.Mod, MinecraftVersion = minecraftVersion, Author = "deirn", Description = "方块信息显示", FileSize = 1 * 1024 * 1024, Downloads = 200000, IconUrl = "" },
            new() { Id = "appleskin", Name = "AppleSkin", Version = "2.5.1", Type = DownloadItemType.Mod, MinecraftVersion = minecraftVersion, Author = "squeek502", Description = "显示食物信息", FileSize = 512 * 1024, Downloads = 180000, IconUrl = "" },
            new() { Id = "inventory", Name = "Inventory Tweaks Renewed", Version = "1.4.6", Type = DownloadItemType.Mod, MinecraftVersion = minecraftVersion, Author = "David1544", Description = "物品栏管理", FileSize = 800 * 1024, Downloads = 150000, IconUrl = "" },
        };

        if (!string.IsNullOrWhiteSpace(query))
        {
            mods = mods.Where(m => m.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                                   m.Description.Contains(query, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        return mods;
    }

    public async Task<bool> DownloadItemAsync(DownloadItem item, IProgress<DownloadProgressInfo> progress, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.Info($"开始下载: {item.Name}");

            // 模拟下载过程
            var totalBytes = item.FileSize;
            var bytesDownloaded = 0L;
            var random = new Random();

            while (bytesDownloaded < totalBytes && !cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(100, cancellationToken);

                // 随机增加下载量
                var chunk = random.Next(50000, 200000);
                bytesDownloaded = Math.Min(bytesDownloaded + chunk, totalBytes);

                var progressPercentage = (double)bytesDownloaded / totalBytes * 100;
                var speed = chunk / 100.0; // KB/s
                var remaining = (totalBytes - bytesDownloaded) / (speed * 1024);

                progress.Report(new DownloadProgressInfo
                {
                    ItemName = item.Name,
                    BytesDownloaded = bytesDownloaded,
                    TotalBytes = totalBytes,
                    ProgressPercentage = progressPercentage,
                    DownloadSpeed = speed,
                    EstimatedTimeRemaining = TimeSpan.FromSeconds(remaining),
                    Status = "下载中"
                });
            }

            if (cancellationToken.IsCancellationRequested)
            {
                _logger.Info($"下载已取消: {item.Name}");
                return false;
            }

            _logger.Info($"下载完成: {item.Name}");
            return true;
        }
        catch (Exception ex)
        {
            _logger.Error($"下载失败: {item.Name} - {ex.Message}", ex);
            return false;
        }
    }

    public async Task<bool> InstallItemAsync(DownloadItem item)
    {
        await Task.Delay(500); // 模拟安装过程
        _logger.Info($"安装完成: {item.Name}");
        item.IsInstalled = true;
        return true;
    }

    public async Task<List<DownloadItem>> GetInstalledItemsAsync()
    {
        await Task.Delay(200);
        return new List<DownloadItem>(); // 返回空列表，实际应用中应读取已安装列表
    }

    public async Task<bool> UninstallItemAsync(DownloadItem item)
    {
        await Task.Delay(300);
        _logger.Info($"卸载完成: {item.Name}");
        item.IsInstalled = false;
        return true;
    }
}
