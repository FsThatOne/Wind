using FengZhi.Foundation.Data;
using FengZhi.Foundation.Events;

namespace FengZhi.Foundation.Items;

/// <summary>
/// Query contract implemented by the martial arts system to report character move ownership.
/// </summary>
public interface IMartialMoveKnowledgeProvider
{
    bool HasLearnedMove(string characterId, string moveId);
}

/// <summary>
/// Distinguishes whether a martial key item is used by the protagonist or taught to a companion.
/// </summary>
public enum MartialFragmentUseMode
{
    SelfStudy,
    TeachCompanion
}

/// <summary>
/// Structured request emitted by item usage and consumed by the martial arts system.
/// </summary>
public sealed record MartialFragmentLearnRequest(
    string ItemId,
    string MoveId,
    string LearnerCharacterId,
    string? TeacherCharacterId,
    MartialFragmentUseMode Mode);

/// <summary>
/// EventBus payload for systems that prefer event-driven martial learning integration.
/// </summary>
public sealed record MartialFragmentLearnRequestedEvent(MartialFragmentLearnRequest Request) : GameEvent;

/// <summary>
/// Structured result for martial fragment/manual use.
/// </summary>
public sealed record MartialFragmentTeachingResult(
    bool Success,
    string? ErrorCode,
    string? ItemId,
    string? MoveId,
    int QuantityChanged,
    MartialFragmentLearnRequest? Request)
{
    public static MartialFragmentTeachingResult Ok(
        string itemId,
        string moveId,
        int quantityChanged,
        MartialFragmentLearnRequest request)
    {
        return new MartialFragmentTeachingResult(true, null, itemId, moveId, quantityChanged, request);
    }

    public static MartialFragmentTeachingResult Fail(string errorCode, string? itemId = null, string? moveId = null)
    {
        return new MartialFragmentTeachingResult(false, errorCode, itemId, moveId, 0, null);
    }
}

/// <summary>
/// Bridges martial fragment key items to martial learning requests without mutating martial arts state directly.
/// </summary>
public sealed class MartialFragmentTeachingService
{
    private const string ConsumeReason = "martial_fragment_learning";

    private readonly IDataTable<KeyItemDefinition> _keyItems;
    private readonly InventoryService _inventory;
    private readonly IMartialMoveKnowledgeProvider _knowledgeProvider;
    private readonly IEventBus? _eventBus;

    public MartialFragmentTeachingService(
        DataRegistry registry,
        InventoryService inventory,
        IMartialMoveKnowledgeProvider knowledgeProvider,
        IEventBus? eventBus = null)
    {
        ArgumentNullException.ThrowIfNull(registry);
        _inventory = inventory ?? throw new ArgumentNullException(nameof(inventory));
        _knowledgeProvider = knowledgeProvider ?? throw new ArgumentNullException(nameof(knowledgeProvider));
        _eventBus = eventBus;
        _keyItems = registry.GetTable<KeyItemDefinition>()
            ?? throw new ArgumentException("KeyItemDefinition table is not registered.", nameof(registry));
    }

    /// <summary>
    /// Uses a linked martial key item for protagonist self-study or companion teaching.
    /// </summary>
    public MartialFragmentTeachingResult UseMartialFragment(
        string itemId,
        string protagonistCharacterId,
        string? targetCharacterId = null)
    {
        if (string.IsNullOrWhiteSpace(itemId))
            return MartialFragmentTeachingResult.Fail("item_id_required");
        if (string.IsNullOrWhiteSpace(protagonistCharacterId))
            return MartialFragmentTeachingResult.Fail("protagonist_character_required", itemId);

        var definition = _keyItems.Get(itemId);
        if (definition == null)
            return MartialFragmentTeachingResult.Fail("unknown_item_id", itemId);
        if (string.IsNullOrWhiteSpace(definition.LinkedMoveId))
            return MartialFragmentTeachingResult.Fail("key_item_not_martial_fragment", itemId);
        if (_inventory.GetTotalQuantity(itemId) <= 0)
            return MartialFragmentTeachingResult.Fail("item_not_available", itemId, definition.LinkedMoveId);

        var moveId = definition.LinkedMoveId;
        var protagonistKnowsMove = _knowledgeProvider.HasLearnedMove(protagonistCharacterId, moveId);
        var hasTeachingTarget = !string.IsNullOrWhiteSpace(targetCharacterId)
            && !string.Equals(targetCharacterId, protagonistCharacterId, StringComparison.Ordinal);

        if (!protagonistKnowsMove)
        {
            if (hasTeachingTarget)
                return MartialFragmentTeachingResult.Fail("protagonist_must_self_study_first", itemId, moveId);

            return ConsumeAndCreateRequest(
                itemId,
                moveId,
                protagonistCharacterId,
                teacherCharacterId: null,
                MartialFragmentUseMode.SelfStudy);
        }

        if (!hasTeachingTarget)
            return MartialFragmentTeachingResult.Fail("protagonist_already_learned", itemId, moveId);

        var learnerCharacterId = targetCharacterId!;
        if (_knowledgeProvider.HasLearnedMove(learnerCharacterId, moveId))
            return MartialFragmentTeachingResult.Fail("target_already_learned", itemId, moveId);

        return ConsumeAndCreateRequest(
            itemId,
            moveId,
            learnerCharacterId,
            protagonistCharacterId,
            MartialFragmentUseMode.TeachCompanion);
    }

    private MartialFragmentTeachingResult ConsumeAndCreateRequest(
        string itemId,
        string moveId,
        string learnerCharacterId,
        string? teacherCharacterId,
        MartialFragmentUseMode mode)
    {
        var consume = _inventory.ConsumeKeyItem(itemId, 1, ConsumeReason);
        if (!consume.Success)
            return MartialFragmentTeachingResult.Fail(consume.ErrorCode ?? "key_item_consume_failed", itemId, moveId);

        var request = new MartialFragmentLearnRequest(itemId, moveId, learnerCharacterId, teacherCharacterId, mode);
        _eventBus?.Publish(new MartialFragmentLearnRequestedEvent(request));
        return MartialFragmentTeachingResult.Ok(itemId, moveId, consume.QuantityChanged, request);
    }
}
