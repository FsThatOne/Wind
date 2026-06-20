using FengZhi.Foundation.CharacterData;
using FengZhi.Foundation.Combat;

namespace FengZhi.Foundation.CombatUi.DecisiveStrikeDirector;

/// <summary>
/// 动画命令上下文。Director 在 Tick 时把当前增量与依赖注入命令。
/// 命令不直接持有 director 句柄，避免循环引用。
/// </summary>
public sealed record AnimationCommandContext(
    BattleEventBus EventBus,
    TimeScaleController TimeScale,
    CameraRequestBus Camera,
    CombatCinematicLock CinematicLock);

/// <summary>
/// 动画命令接口。Director 队列中的最小执行单元。
/// 实现必须支持以下生命周期：
///   1. Start: 进入命令，acquire 资源
///   2. Tick: 按 deltaSeconds 推进；返回 true 表示完成
///   3. Stop: 中止命令（pause cancel / 战斗结束），反向释放尚未释放的资源
/// </summary>
public interface IAnimationCommand
{
    bool IsCompleted { get; }

    void Start(AnimationCommandContext context);

    bool Tick(double deltaSeconds);

    void Stop(bool cancelled);
}

/// <summary>
/// 决胜请求的入参。由战斗系统在 cu-005 玩家提交 IsDecisiveStrike=true
/// 后通过 director.RequestDecisiveStrike 传入。
/// PrecomputedDamage 来源于 Core Combat Phase 1 预结算。
/// </summary>
public sealed record DecisiveStrikeRequest(
    string SourceId,
    string TargetId,
    int PrecomputedDamage,
    MoveType MoveType);
