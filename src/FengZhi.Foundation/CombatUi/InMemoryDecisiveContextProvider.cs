using FengZhi.Foundation.CombatUi.DecisiveStrikeDirector;

namespace FengZhi.Foundation.CombatUi;

/// <summary>
/// 测试 / 集成场景使用的 in-memory provider。
/// 支持预注册 (actorId, targetId) → DecisiveStrikeRequest 映射；
/// cu-005 BUILD 阶段会有 BattleInstance-aware 实现替换它。
/// </summary>
public sealed class InMemoryDecisiveContextProvider : IDecisiveContextProvider
{
    private readonly Dictionary<(string actorId, string targetId), DecisiveStrikeRequest> _entries
        = new();

    public void Register(string actorId, string targetId, DecisiveStrikeRequest request)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(actorId);
        ArgumentException.ThrowIfNullOrWhiteSpace(targetId);
        ArgumentNullException.ThrowIfNull(request);
        _entries[(actorId, targetId)] = request;
    }

    public DecisiveStrikeRequest GetContext(string actorId, string targetId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(actorId);
        ArgumentException.ThrowIfNullOrWhiteSpace(targetId);
        if (!_entries.TryGetValue((actorId, targetId), out var request))
            throw new InvalidOperationException(
                $"No decisive context registered for actor='{actorId}' target='{targetId}'");
        return request;
    }
}
