using LizardClient.Core.Models;
using LizardClient.ModSystem.API;

namespace LizardClient.ModSystem.Mods;

/// <summary>
/// FPS 显示模组
/// </summary>
public sealed class FPSDisplayMod : ModBase
{
    private int _currentFps;
    private DateTime _lastUpdate = DateTime.Now;
    private int _frameCount;

    public override ModInfo Info => new()
    {
        Id = "fps_display",
        Name = "FPS Display",
        Description = "实时显示帧率(FPS)",
        Version = "1.0.0",
        Author = "LizardClient Team",
        Category = ModCategory.Information,
        EnabledByDefault = true,
        SupportedVersions = new List<string> { "1.8.9", "1.12.2", "1.16.5", "1.20.4" }
    };

    public override void OnTick()
    {
        _frameCount++;

        var now = DateTime.Now;
        if ((now - _lastUpdate).TotalSeconds >= 1.0)
        {
            _currentFps = _frameCount;
            _frameCount = 0;
            _lastUpdate = now;
        }
    }

    public override void OnRender()
    {
        // 实际渲染代码需要 OpenGL/DirectX 调用
        // 这里仅作示例，显示在控制台
        Console.SetCursorPosition(0, 0);
        Console.Write($"FPS: {_currentFps}   ");
    }
}

/// <summary>
/// 坐标显示模组
/// </summary>
public sealed class CoordinatesMod : ModBase
{
    private double _x, _y, _z;

    public override ModInfo Info => new()
    {
        Id = "coordinates",
        Name = "Coordinates",
        Description = "显示玩家当前坐标",
        Version = "1.0.0",
        Author = "LizardClient Team",
        Category = ModCategory.Information,
        EnabledByDefault = true,
        SupportedVersions = new List<string> { "1.8.9", "1.12.2", "1.16.5", "1.20.4" }
    };

    public override void OnTick()
    {
        // 从游戏获取坐标（这里模拟）
        // 实际需要从 Minecraft 进程内存读取
        _x = Math.Round(Random.Shared.NextDouble() * 1000, 2);
        _y = Math.Round(Random.Shared.NextDouble() * 256, 2);
        _z = Math.Round(Random.Shared.NextDouble() * 1000, 2);
    }

    public override void OnRender()
    {
        // 渲染坐标到屏幕
        Console.SetCursorPosition(0, 1);
        Console.Write($"XYZ: {_x:F2}, {_y:F2}, {_z:F2}        ");
    }
}

/// <summary>
/// 缩放模组
/// </summary>
public sealed class ZoomMod : ModBase
{
    private bool _isZooming;
    private const int ZOOM_KEY = 67; // C 键

    public override ModInfo Info => new()
    {
        Id = "zoom",
        Name = "Zoom",
        Description = "按下 C 键进行缩放",
        Version = "1.0.0",
        Author = "LizardClient Team",
        Category = ModCategory.Visual,
        EnabledByDefault = true,
        SupportedVersions = new List<string> { "1.8.9", "1.12.2", "1.16.5", "1.20.4" }
    };

    public override void OnInput(int key, InputAction action)
    {
        if (key == ZOOM_KEY)
        {
            _isZooming = action == InputAction.Press;
        }
    }

    public override void OnTick()
    {
        if (_isZooming)
        {
            // 实际需要修改游戏的 FOV
            // 这里仅作示例
        }
    }
}

/// <summary>
/// 全亮模组
/// </summary>
public sealed class FullBrightMod : ModBase
{
    public override ModInfo Info => new()
    {
        Id = "fullbright",
        Name = "FullBright",
        Description = "使游戏全亮，无需火把",
        Version = "1.0.0",
        Author = "LizardClient Team",
        Category = ModCategory.Visual,
        EnabledByDefault = false,
        SupportedVersions = new List<string> { "1.8.9", "1.12.2", "1.16.5", "1.20.4" }
    };

    public override void OnEnable()
    {
        // 设置游戏亮度为最大
        Console.WriteLine("FullBright: 已启用");
    }

    public override void OnDisable()
    {
        // 恢复正常亮度
        Console.WriteLine("FullBright: 已禁用");
    }
}

/// <summary>
/// 按键显示模组
/// </summary>
public sealed class KeystrokesMod : ModBase
{
    private readonly HashSet<int> _pressedKeys = new();

    public override ModInfo Info => new()
    {
        Id = "keystrokes",
        Name = "Keystrokes",
        Description = "显示按键状态 (WASD, 鼠标等)",
        Version = "1.0.0",
        Author = "LizardClient Team",
        Category = ModCategory.PvP,
        EnabledByDefault = true,
        SupportedVersions = new List<string> { "1.8.9", "1.12.2", "1.16.5", "1.20.4" }
    };

    public override void OnInput(int key, InputAction action)
    {
        if (action == InputAction.Press)
        {
            _pressedKeys.Add(key);
        }
        else if (action == InputAction.Release)
        {
            _pressedKeys.Remove(key);
        }
    }

    public override void OnRender()
    {
        // 渲染按键状态
        Console.SetCursorPosition(0, 2);
        Console.Write($"Keys: {string.Join(", ", _pressedKeys)}           ");
    }
}
