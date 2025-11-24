using LizardClient.Core.Models;
using Newtonsoft.Json;

namespace LizardClient.ModSystem.API;

/// <summary>
/// 模组基类，提供默认实现
/// </summary>
public abstract class ModBase : IMod
{
    private bool _isEnabled;

    /// <summary>
    /// 模组元数据信息
    /// </summary>
    public abstract ModInfo Info { get; }

    /// <summary>
    /// 模组是否已启用
    /// </summary>
    public bool IsEnabled
    {
        get => _isEnabled;
        set
        {
            if (_isEnabled == value) return;

            _isEnabled = value;
            if (_isEnabled)
            {
                OnEnable();
            }
            else
            {
                OnDisable();
            }
        }
    }

    /// <summary>
    /// 模组初始化（仅调用一次）
    /// </summary>
    public virtual void OnLoad()
    {
        // 默认不执行任何操作
    }

    /// <summary>
    /// 模组启用时调用
    /// </summary>
    public virtual void OnEnable()
    {
        // 默认不执行任何操作
    }

    /// <summary>
    /// 模组禁用时调用
    /// </summary>
    public virtual void OnDisable()
    {
        // 默认不执行任何操作
    }

    /// <summary>
    /// 模组卸载（仅调用一次）
    /// </summary>
    public virtual void OnUnload()
    {
        // 默认不执行任何操作
    }

    /// <summary>
    /// 每帧更新（游戏Tick）
    /// </summary>
    public virtual void OnTick()
    {
        // 默认不执行任何操作
    }

    /// <summary>
    /// 渲染时调用
    /// </summary>
    public virtual void OnRender()
    {
        // 默认不执行任何操作
    }

    /// <summary>
    /// 输入事件处理
    /// </summary>
    public virtual void OnInput(int key, InputAction action)
    {
        // 默认不执行任何操作
    }

    /// <summary>
    /// 获取模组配置
    /// </summary>
    public virtual string GetConfig()
    {
        return JsonConvert.SerializeObject(new { });
    }

    /// <summary>
    /// 设置模组配置
    /// </summary>
    public virtual void SetConfig(string config)
    {
        // 默认不执行任何操作
    }
}
