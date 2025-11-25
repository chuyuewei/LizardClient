using LizardClient.Core.Interfaces;
using LizardClient.Injection.Minecraft;
using LizardClient.Injection.Memory;
using Newtonsoft.Json;

namespace LizardClient.Injection.AutoUpdate;

/// <summary>
/// 偏移自动更新器
/// </summary>
public sealed class OffsetUpdater
{
    private readonly ILogger _logger;
    private readonly SignatureScanner _scanner;
    private readonly string _databasePath;
    private Dictionary<string, Dictionary<string, string>> _offsetDatabase;

    public OffsetUpdater(ILogger logger, MemoryManager memoryManager, string? databasePath = null)
    {
        _logger = logger;
        _scanner = new SignatureScanner(logger, memoryManager);
        _databasePath = databasePath ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "OffsetDatabase.json");
        _offsetDatabase = new Dictionary<string, Dictionary<string, string>>();

        LoadDatabase();
    }

    /// <summary>
    /// 自动更新游戏偏移
    /// </summary>
    /// <param name="version">Minecraft 版本</param>
    /// <returns>更新后的 GameOffsets</returns>
    public GameOffsets AutoUpdateOffsets(MinecraftVersion version)
    {
        _logger.Info($"开始自动更新偏移 (版本: {version})");

        var gameOffsets = new GameOffsets(version);
        var versionKey = version.ToString();

        // 1. 尝试从数据库加载
        if (_offsetDatabase.TryGetValue(versionKey, out var savedOffsets))
        {
            _logger.Info("从数据库加载已保存的偏移...");
            if (TryLoadFromDatabase(savedOffsets, gameOffsets))
            {
                _logger.Info("已从数据库恢复偏移");
                return gameOffsets;
            }
        }

        // 2. 通过签名扫描查找偏移
        _logger.Info("开始签名扫描...");
        ScanAllSignatures(gameOffsets);

        // 3. 保存到数据库
        if (gameOffsets.IsValid())
        {
            SaveToDatabase(versionKey, gameOffsets);
            _logger.Info($"偏移已保存到数据库 ({gameOffsets.GetFoundOffsetsCount()} 个)");
        }
        else
        {
            _logger.Warning($"偏移不完整，仅找到 {gameOffsets.GetFoundOffsetsCount()} 个");
        }

        return gameOffsets;
    }

    /// <summary>
    /// 扫描所有签名
    /// </summary>
    private void ScanAllSignatures(GameOffsets gameOffsets)
    {
        // 游戏循环
        var gameLoopAddr = _scanner.ScanAndValidate(
            GameSignatures.GameLoopPattern.ModuleName,
            GameSignatures.GameLoopPattern.Signature,
            GameSignatures.GameLoopPattern.Offset
        );
        if (gameLoopAddr != IntPtr.Zero)
        {
            gameOffsets.GameLoop = new OffsetInfo(
                gameLoopAddr,
                GameSignatures.GameLoopPattern.Signature,
                null,
                GameSignatures.GameLoopPattern.Offset,
                GameSignatures.GameLoopPattern.ModuleName,
                autoDetected: true
            );
            _logger.Info($"✓ 游戏循环: 0x{gameLoopAddr:X}");
        }

        // 渲染帧
        var renderFrameAddr = _scanner.ScanAndValidate(
            GameSignatures.RenderFramePattern.ModuleName,
            GameSignatures.RenderFramePattern.Signature,
            GameSignatures.RenderFramePattern.Offset
        );
        if (renderFrameAddr != IntPtr.Zero)
        {
            gameOffsets.RenderFrame = new OffsetInfo(
                renderFrameAddr,
                GameSignatures.RenderFramePattern.Signature,
                null,
                GameSignatures.RenderFramePattern.Offset,
                GameSignatures.RenderFramePattern.ModuleName,
                autoDetected: true
            );
            _logger.Info($"✓ 渲染帧: 0x{renderFrameAddr:X}");
        }

        // 输入处理
        var processInputAddr = _scanner.ScanAndValidate(
            GameSignatures.ProcessInputPattern.ModuleName,
            GameSignatures.ProcessInputPattern.Signature,
            GameSignatures.ProcessInputPattern.Offset
        );
        if (processInputAddr != IntPtr.Zero)
        {
            gameOffsets.ProcessInput = new OffsetInfo(
                processInputAddr,
                GameSignatures.ProcessInputPattern.Signature,
                null,
                GameSignatures.ProcessInputPattern.Offset,
                GameSignatures.ProcessInputPattern.ModuleName,
                autoDetected: true
            );
            _logger.Info($"✓ 输入处理: 0x{processInputAddr:X}");
        }

        // 发送网络包
        var sendPacketAddr = _scanner.ScanAndValidate(
            GameSignatures.SendPacketPattern.ModuleName,
            GameSignatures.SendPacketPattern.Signature,
            GameSignatures.SendPacketPattern.Offset
        );
        if (sendPacketAddr != IntPtr.Zero)
        {
            gameOffsets.SendPacket = new OffsetInfo(
                sendPacketAddr,
                GameSignatures.SendPacketPattern.Signature,
                null,
                GameSignatures.SendPacketPattern.Offset,
                GameSignatures.SendPacketPattern.ModuleName,
                autoDetected: true
            );
            _logger.Info($"✓ 发送网络包: 0x{sendPacketAddr:X}");
        }
    }

    /// <summary>
    /// 从数据库加载偏移
    /// </summary>
    private bool TryLoadFromDatabase(Dictionary<string, string> savedOffsets, GameOffsets gameOffsets)
    {
        try
        {
            int loadedCount = 0;

            if (savedOffsets.TryGetValue("GameLoop", out var gameLoopStr) && long.TryParse(gameLoopStr, out var gameLoop))
            {
                gameOffsets.GameLoop = new OffsetInfo(new IntPtr(gameLoop));
                loadedCount++;
            }

            if (savedOffsets.TryGetValue("RenderFrame", out var renderFrameStr) && long.TryParse(renderFrameStr, out var renderFrame))
            {
                gameOffsets.RenderFrame = new OffsetInfo(new IntPtr(renderFrame));
                loadedCount++;
            }

            if (savedOffsets.TryGetValue("ProcessInput", out var processInputStr) && long.TryParse(processInputStr, out var processInput))
            {
                gameOffsets.ProcessInput = new OffsetInfo(new IntPtr(processInput));
                loadedCount++;
            }

            if (savedOffsets.TryGetValue("SendPacket", out var sendPacketStr) && long.TryParse(sendPacketStr, out var sendPacket))
            {
                gameOffsets.SendPacket = new OffsetInfo(new IntPtr(sendPacket));
                loadedCount++;
            }

            if (savedOffsets.TryGetValue("ReceivePacket", out var receivePacketStr) && long.TryParse(receivePacketStr, out var receivePacket))
            {
                gameOffsets.ReceivePacket = new OffsetInfo(new IntPtr(receivePacket));
                loadedCount++;
            }

            _logger.Info($"从数据库加载了 {loadedCount} 个偏移");
            return loadedCount >= 3; // 至少需要 3 个关键偏移
        }
        catch (Exception ex)
        {
            _logger.Error($"从数据库加载偏移失败: {ex.Message}", ex);
            return false;
        }
    }

    /// <summary>
    /// 保存到数据库
    /// </summary>
    private void SaveToDatabase(string versionKey, GameOffsets gameOffsets)
    {
        try
        {
            var offsetDict = new Dictionary<string, string>();

            if (gameOffsets.GameLoop != null)
                offsetDict["GameLoop"] = gameOffsets.GameLoop.Address.ToInt64().ToString();

            if (gameOffsets.RenderFrame != null)
                offsetDict["RenderFrame"] = gameOffsets.RenderFrame.Address.ToInt64().ToString();

            if (gameOffsets.ProcessInput != null)
                offsetDict["ProcessInput"] = gameOffsets.ProcessInput.Address.ToInt64().ToString();

            if (gameOffsets.SendPacket != null)
                offsetDict["SendPacket"] = gameOffsets.SendPacket.Address.ToInt64().ToString();

            if (gameOffsets.ReceivePacket != null)
                offsetDict["ReceivePacket"] = gameOffsets.ReceivePacket.Address.ToInt64().ToString();

            if (gameOffsets.PlayerEntity != null)
                offsetDict["PlayerEntity"] = gameOffsets.PlayerEntity.Address.ToInt64().ToString();

            if (gameOffsets.WorldObject != null)
                offsetDict["WorldObject"] = gameOffsets.WorldObject.Address.ToInt64().ToString();

            _offsetDatabase[versionKey] = offsetDict;
            SaveDatabase();
        }
        catch (Exception ex)
        {
            _logger.Error($"保存偏移到数据库失败: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// 加载数据库
    /// </summary>
    private void LoadDatabase()
    {
        try
        {
            if (File.Exists(_databasePath))
            {
                var json = File.ReadAllText(_databasePath);
                _offsetDatabase = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, string>>>(json)
                    ?? new Dictionary<string, Dictionary<string, string>>();

                _logger.Info($"偏移数据库已加载 ({_offsetDatabase.Count} 个版本)");
            }
            else
            {
                _logger.Info("偏移数据库不存在，将创建新数据库");
            }
        }
        catch (Exception ex)
        {
            _logger.Error($"加载偏移数据库失败: {ex.Message}", ex);
            _offsetDatabase = new Dictionary<string, Dictionary<string, string>>();
        }
    }

    /// <summary>
    /// 保存数据库
    /// </summary>
    private void SaveDatabase()
    {
        try
        {
            var json = JsonConvert.SerializeObject(_offsetDatabase, Formatting.Indented);
            File.WriteAllText(_databasePath, json);
            _logger.Info("偏移数据库已保存");
        }
        catch (Exception ex)
        {
            _logger.Error($"保存偏移数据库失败: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// 手动添加偏移
    /// </summary>
    public void AddManualOffset(string version, string name, IntPtr address)
    {
        if (!_offsetDatabase.ContainsKey(version))
        {
            _offsetDatabase[version] = new Dictionary<string, string>();
        }

        _offsetDatabase[version][name] = address.ToInt64().ToString();
        SaveDatabase();

        _logger.Info($"手动添加偏移: {version}/{name} = 0x{address:X}");
    }

    /// <summary>
    /// 获取所有已知版本
    /// </summary>
    public IEnumerable<string> GetKnownVersions()
    {
        return _offsetDatabase.Keys;
    }
}
