using FengZhi.Foundation.CombatUi.DecisiveStrikeDirector;

namespace FengZhi.Foundation.CombatUi;

/// <summary>
/// <see cref="ICombatService"/> 默认实现。
/// 不持有 BattleInstance；通过注入的 <see cref="IDecisiveContextProvider"/>
/// 解析上下文，再委派给 <see cref="CombatAnimationDirector"/> 编排演出序列。
/// </summary>
public sealed class CombatService : ICombatService
{
    private readonly CombatAnimationDirector _director;
    private readonly IDecisiveContextProvider _contextProvider;

    public CombatService(CombatAnimationDirector director, IDecisiveContextProvider contextProvider)
    {
        _director = director ?? throw new ArgumentNullException(nameof(director));
        _contextProvider = contextProvider ?? throw new ArgumentNullException(nameof(contextProvider));
    }

    public void RequestDecisiveStrike(string actorId, string targetId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(actorId);
        ArgumentException.ThrowIfNullOrWhiteSpace(targetId);

        var request = _contextProvider.GetContext(actorId, targetId);
        _director.RequestDecisiveStrike(request);
    }
}
