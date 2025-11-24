using LizardClient.Core.Models;

namespace LizardClient.ModSystem.API;

/// <summary>
/// 模组接口定义，所有模组必须实现此接口
/// </summary>
public interface IMod
{
    /// <summary>
    /// 模组元数据信息
    /// </summary>
    ModInfo Info { get; }

    /// <summary>
    /// 模组是否已启用
    /// </summary>
    bool IsEnabled { get; set; }

    /// <summary>
    /// 模组初始化（仅调用一次）
    /// </summary>
    void OnLoad();

    /// <summary>
    /// 模组启用时调用
    /// </summary>
    void OnEnable();

    /// <summary>
    /// 模组禁用时调用
    /// </summary>
    void OnDisable();

    /// <summary>
    /// 模组卸载（仅调用一次）
    /// </summary>
    void OnUnload();

    /// <summary>
    /// 每帧更新（游戏Tick）
    /// </summary>
    void OnTick();

    /// <summary>
    /// 渲染时调用
    /// </summary>
    void OnRender();

    /// <summary>
    /// 输入事件处理
    /// </summary>
    /// <param name="key">按键代码</param>
    /// <param name="action">动作（按下/释放）</param>
    void OnInput(int key, InputAction action);

    /// <summary>
    /// 获取模组配置
    /// </summary>
    /// <returns>配置 JSON 字符串</returns>
    string GetConfig();

    /// <summary>
    /// 设置模组配置
    /// </summary>
    /// <param name="config">配置 JSON 字符串</param>
    void SetConfig(string config);
}

/// <summary>
/// 输入动作枚举
/// </summary>
public enum InputAction
{
    Press,
    Release,
    Repeat
}
