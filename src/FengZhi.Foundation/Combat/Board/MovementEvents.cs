using FengZhi.Foundation.Animation;

namespace FengZhi.Foundation.Combat.Board;

/// <summary>
/// 可达范围已计算事件。UI 层据此高亮可达格。
/// </summary>
public readonly record struct MovementRangeCalculatedEvent(
    string ActorId,
    IReadOnlySet<GridPosition> ReachableCells,
    GridPosition Origin);

/// <summary>
/// 角色移动完成事件。UI 层据此播放移动动画。
/// </summary>
public readonly record struct ActorMovedEvent(
    string ActorId,
    GridPosition From,
    GridPosition To,
    IReadOnlyList<GridPosition> Path,
    Iso4Direction NewFacing);
