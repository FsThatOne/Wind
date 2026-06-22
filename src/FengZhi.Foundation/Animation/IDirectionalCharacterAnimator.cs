using Godot;

namespace FengZhi.Foundation.Animation;

/// <summary>
/// ICharacterAnimator 兄弟接口（ADR-0021 §扩展点 1）。
/// 适用于 8 方向序列帧角色：上层只提供移动向量，方向分区 / 迟滞 /
/// 帧相位保持由 Adapter 内部承担；公共 ICharacterAnimator 契约保持简洁。
/// </summary>
public interface IDirectionalCharacterAnimator : ICharacterAnimator
{
    EightDirection? CurrentDirection { get; }

    /// <summary>
    /// 由上层每帧调用。零向量代表停止，Adapter 自行决定是否暂停，
    /// 但不会主动切到 Idle；非零向量驱动 Adapter 选择 walk_&lt;dir&gt; 动画。
    /// </summary>
    void SetMovementVector(Vector2 movement);
}
