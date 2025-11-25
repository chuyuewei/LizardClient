namespace LizardClient.ModSystem.Events;

/// <summary>
/// Tick事件 - 每个游戏帧触发
/// </summary>
public sealed class TickEvent : ModEvent
{
    /// <summary>
    /// 增量时间（秒）
    /// </summary>
    public float DeltaTime { get; set; }

    /// <summary>
    /// 总游戏时间（秒）
    /// </summary>
    public double TotalTime { get; set; }

    /// <summary>
    /// 当前帧数
    /// </summary>
    public long FrameCount { get; set; }

    /// <summary>
    /// Tick事件不可取消
    /// </summary>
    public override bool IsCancellable => false;
}

/// <summary>
/// 渲染事件 - 每帧渲染前触发
/// </summary>
public sealed class RenderEvent : ModEvent
{
    /// <summary>
    /// 增量时间（秒）
    /// </summary>
    public float DeltaTime { get; set; }

    /// <summary>
    /// 渲染分辨率宽度
    /// </summary>
    public int Width { get; set; }

    /// <summary>
    /// 渲染分辨率高度
    /// </summary>
    public int Height { get; set; }

    /// <summary>
    /// 渲染事件不可取消
    /// </summary>
    public override bool IsCancellable => false;
}

/// <summary>
/// 输入事件 - 键盘/鼠标输入时触发
/// </summary>
public sealed class InputEvent : ModEvent
{
    /// <summary>
    /// 输入动作类型
    /// </summary>
    public InputActionType ActionType { get; set; }

    /// <summary>
    /// 键码或按钮
    /// </summary>
    public int KeyCode { get; set; }

    /// <summary>
    /// 是否按下
    /// </summary>
    public bool IsPressed { get; set; }

    /// <summary>
    /// 鼠标X坐标
    /// </summary>
    public float MouseX { get; set; }

    /// <summary>
    /// 鼠标Y坐标
    /// </summary>
    public float MouseY { get; set; }

    /// <summary>
    /// 输入事件可取消（阻止后续处理）
    /// </summary>
    public override bool IsCancellable => true;
}

/// <summary>
/// 输入动作类型
/// </summary>
public enum InputActionType
{
    /// <summary>
    /// 键盘按键
    /// </summary>
    KeyPress,

    /// <summary>
    /// 鼠标点击
    /// </summary>
    MouseClick,

    /// <summary>
    /// 鼠标移动
    /// </summary>
    MouseMove,

    /// <summary>
    /// 鼠标滚轮
    /// </summary>
    MouseWheel
}

/// <summary>
/// 模组加载事件
/// </summary>
public sealed class ModLoadEvent : ModEvent
{
    /// <summary>
    /// 被加载的模组ID
    /// </summary>
    public string ModId { get; set; } = string.Empty;

    /// <summary>
    /// 模组名称
    /// </summary>
    public string ModName { get; set; } = string.Empty;

    /// <summary>
    /// 模组版本
    /// </summary>
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// 模组加载事件不可取消
    /// </summary>
    public override bool IsCancellable => false;
}

/// <summary>
/// 模组卸载事件
/// </summary>
public sealed class ModUnloadEvent : ModEvent
{
    /// <summary>
    /// 被卸载的模组ID
    /// </summary>
    public string ModId { get; set; } = string.Empty;

    /// <summary>
    /// 模组名称
    /// </summary>
    public string ModName { get; set; } = string.Empty;

    /// <summary>
    /// 模组卸载事件不可取消
    /// </summary>
    public override bool IsCancellable => false;
}

/// <summary>
/// 实体生成事件
/// </summary>
public sealed class EntitySpawnEvent : ModEvent
{
    /// <summary>
    /// 实体类型
    /// </summary>
    public string EntityType { get; set; } = string.Empty;

    /// <summary>
    /// X坐标
    /// </summary>
    public float X { get; set; }

    /// <summary>
    /// Y坐标
    /// </summary>
    public float Y { get; set; }

    /// <summary>
    /// Z坐标
    /// </summary>
    public float Z { get; set; }

    /// <summary>
    /// 实体生成事件可取消
    /// </summary>
    public override bool IsCancellable => true;
}

/// <summary>
/// 方块放置事件
/// </summary>
public sealed class BlockPlaceEvent : ModEvent
{
    /// <summary>
    /// 方块类型
    /// </summary>
    public string BlockType { get; set; } = string.Empty;

    /// <summary>
    /// 方块X坐标
    /// </summary>
    public int X { get; set; }

    /// <summary>
    /// 方块Y坐标
    /// </summary>
    public int Y { get; set; }

    /// <summary>
    /// 方块Z坐标
    /// </summary>
    public int Z { get; set; }

    /// <summary>
    /// 玩家ID
    /// </summary>
    public string? PlayerId { get; set; }

    /// <summary>
    /// 方块放置事件可取消
    /// </summary>
    public override bool IsCancellable => true;
}
