using Godot;

namespace FengZhi.Foundation.Animation;

/// <summary>
/// <see cref="ICharacterAnimator"/> 兄弟接口（ADR-0022 §2，取代 ADR-0021 §扩展点 1）。
///
/// 适用于 iso 视角下 4 斜向序列帧角色：上层只提供 cart 空间移动向量，
/// 方向选区 / ±15° 迟滞 / 帧相位保持由 Adapter 内部承担；
/// 公共 <see cref="ICharacterAnimator"/> 契约（Play/Stop/Facing/Finished/FrameEvent）保持简洁。
/// </summary>
public interface IIso4CharacterAnimator : ICharacterAnimator
{
    /// <summary>
    /// 最近一次非零移动向量解算出的 4 斜向；未启动 / Stop 后为 <c>null</c>。
    /// </summary>
    Iso4Direction? CurrentDirection { get; }

    /// <summary>
    /// 由上层每帧调用。零向量代表停止移动（Adapter 应暂停 sprite，但不会主动切到 Idle、
    /// 不会清空 <see cref="CurrentDirection"/>）；非零向量驱动 Adapter 选择 walk_&lt;dir&gt; 动画。
    ///
    /// 入参为 **cart 空间**向量（逻辑层），不是屏幕空间；屏幕投影由 Adapter 之外的
    /// <see cref="Geometry.IsoProjection"/> 完成。
    /// </summary>
    void SetMovementVector(Vector2 movement);
}
