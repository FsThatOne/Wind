using FengZhi.Foundation.CombatUi.DecisiveStrikeDirector;

namespace FengZhi.Foundation.CombatUi;

/// <summary>
/// 决胜请求上下文提供者。Facade 调用方传入 actorId/targetId，由实现负责
/// 从战斗实例查表得到 PrecomputedDamage + MoveType。
///
/// cu-006 BUILD 仅交付 InMemoryDecisiveContextProvider；BattleInstance-aware
/// 实现在 cu-005 BUILD 时落地，避免污染 BattleFacade。
/// </summary>
public interface IDecisiveContextProvider
{
    /// <summary>
    /// 根据 actor / target 解析决胜请求。
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// actor 不在战斗 / target 不存在 / target 破绽未达阈值。
    /// </exception>
    DecisiveStrikeRequest GetContext(string actorId, string targetId);
}
