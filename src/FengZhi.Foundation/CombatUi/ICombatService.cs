namespace FengZhi.Foundation.CombatUi;

/// <summary>
/// cu-001 / cu-005 调用方使用的战斗服务 Facade。
/// 封装 DecisiveContext 解析 + CombatAnimationDirector 调度细节。
///
/// 调用方契约：在调用本方法前，玩家已通过 cu-004 选定 IsDecisiveStrike=true 的行动。
/// 本 Facade 不重复判定决胜资格——资格检查由 IDecisiveContextProvider 实现负责。
/// </summary>
public interface ICombatService
{
    /// <summary>
    /// 请求 actor 对 target 发动一击决胜。
    /// 转译为 DecisiveStrikeRequest 后交给 director；同时只允许一条决胜在跑。
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// 上下文解析失败，或 director 已忙（IsBusy=true）。
    /// </exception>
    void RequestDecisiveStrike(string actorId, string targetId);
}
