using Godot;

namespace FengZhi.Foundation.Presentation.Shared;

/// <summary>
/// Presentation UI 面板基类。提供统一的脏标记刷新入口。
/// </summary>
public abstract partial class BaseUiPanel : Control
{
    private bool _isDirty;
    private int _refreshCount;

    /// <summary>
    /// 当前面板是否等待合批刷新。
    /// </summary>
    public bool IsDirty => _isDirty;

    /// <summary>
    /// 已执行的刷新次数，供测试和性能留证使用。
    /// </summary>
    public int RefreshCount => _refreshCount;

    public override void _Process(double delta)
    {
        if (!_isDirty)
            return;

        _refreshCount++;
        _isDirty = false;
        Refresh();
    }

    /// <summary>
    /// 标记面板需要在下一次 Process 中刷新。
    /// </summary>
    protected void MarkDirty()
    {
        _isDirty = true;
    }

    /// <summary>
    /// 子类在这里执行实际 UI 绑定。
    /// </summary>
    protected virtual void Refresh()
    {
    }
}
