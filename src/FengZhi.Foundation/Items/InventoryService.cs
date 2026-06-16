using FengZhi.Foundation.Data;

namespace FengZhi.Foundation.Items;

/// <summary>
/// Source that granted an item. Runtime rules are identical, but results keep the source for audits.
/// </summary>
public enum ItemGrantSource
{
    BattleReward,
    ExplorationPickup,
    NarrativeReward
}

/// <summary>
/// Normalized inventory entry category used by sorted inventory queries.
/// </summary>
public enum InventoryEntryKind
{
    KeyItem,
    Equipment,
    CombatConsumable,
    ExplorationConsumable,
    Other
}

/// <summary>
/// Five equipment slots shared by the protagonist and companions.
/// </summary>
public enum EquipmentSlotId
{
    MainHand,
    Armor,
    Footwear,
    Accessory1,
    Accessory2
}

/// <summary>
/// Structured operation result for inventory commands.
/// </summary>
public sealed record InventoryOperationResult(
    bool Success,
    string? ErrorCode,
    string? ItemId,
    int QuantityChanged)
{
    public static InventoryOperationResult Ok(string itemId, int quantityChanged)
    {
        return new InventoryOperationResult(true, null, itemId, quantityChanged);
    }

    public static InventoryOperationResult Fail(string errorCode, string? itemId = null)
    {
        return new InventoryOperationResult(false, errorCode, itemId, 0);
    }
}

/// <summary>
/// Stackable inventory entry for consumables and materials.
/// </summary>
public sealed record InventoryStack(
    string StackId,
    string ItemId,
    int Quantity,
    int MaxStack,
    long AcquiredOrder);

/// <summary>
/// Runtime equipment inventory entry. The instance id is unique per granted equipment item.
/// </summary>
public sealed record InventoryEquipmentItem(
    string InstanceId,
    string TemplateId,
    GeneratedEquipmentInstance Instance,
    long AcquiredOrder);

/// <summary>
/// Runtime key item entry, stored separately from stackable and equipment inventories.
/// </summary>
public sealed record InventoryKeyItem(
    string ItemId,
    int Quantity,
    bool Auctionable,
    long AcquiredOrder);

/// <summary>
/// Read model for sorted inventory UI or queries.
/// </summary>
public sealed record InventoryDisplayEntry(
    InventoryEntryKind Kind,
    string ItemId,
    string? InstanceId,
    int Quantity,
    long AcquiredOrder);

/// <summary>
/// Battle action context owned by combat callers. Item code only reads and marks action consumption.
/// </summary>
public sealed class CombatItemActionContext
{
    public bool ActionConsumed { get; private set; }

    public string? ConsumedBy { get; private set; }

    public bool TryConsumeAction(string actionKey)
    {
        if (ActionConsumed)
            return false;

        ActionConsumed = true;
        ConsumedBy = actionKey;
        return true;
    }
}

/// <summary>
/// Read model for battle bag entries.
/// </summary>
public sealed record CombatUsableItem(
    string ItemId,
    string Name,
    int Quantity,
    IReadOnlyList<ItemEffectDefinition> Effects);

/// <summary>
/// Structured result for using a combat consumable.
/// </summary>
public sealed record CombatItemUseResult(
    bool Success,
    string? ErrorCode,
    string? ItemId,
    bool ActionConsumed,
    IReadOnlyList<ItemEffectDefinition> Effects)
{
    public static CombatItemUseResult Ok(string itemId, IReadOnlyList<ItemEffectDefinition> effects)
    {
        return new CombatItemUseResult(true, null, itemId, ActionConsumed: true, effects);
    }

    public static CombatItemUseResult Fail(string errorCode, string? itemId = null)
    {
        return new CombatItemUseResult(false, errorCode, itemId, ActionConsumed: false, Array.Empty<ItemEffectDefinition>());
    }
}

/// <summary>
/// Runtime inventory service. It enforces static item definitions rather than duplicating rules in callers.
/// </summary>
public sealed class InventoryService
{
    private readonly IDataTable<ConsumableDefinition> _consumables;
    private readonly IDataTable<EquipmentTemplateDefinition> _equipmentTemplates;
    private readonly IDataTable<KeyItemDefinition> _keyItems;
    private readonly EquipmentService _equipmentService;
    private readonly List<InventoryStack> _stacks = new();
    private readonly List<InventoryEquipmentItem> _equipment = new();
    private readonly List<InventoryKeyItem> _keyItemEntries = new();
    private readonly List<ItemOperationLogEntry> _operationLog = new();
    private long _nextOrder = 1;
    private long _nextStackId = 1;
    private long _nextEquipmentInstanceId = 1;

    public InventoryService(DataRegistry registry, EquipmentService? equipmentService = null)
    {
        ArgumentNullException.ThrowIfNull(registry);
        _consumables = registry.GetTable<ConsumableDefinition>()
            ?? throw new ArgumentException("ConsumableDefinition table is not registered.", nameof(registry));
        _equipmentTemplates = registry.GetTable<EquipmentTemplateDefinition>()
            ?? throw new ArgumentException("EquipmentTemplateDefinition table is not registered.", nameof(registry));
        _keyItems = registry.GetTable<KeyItemDefinition>()
            ?? throw new ArgumentException("KeyItemDefinition table is not registered.", nameof(registry));
        _equipmentService = equipmentService ?? new EquipmentService(registry);
    }

    public IReadOnlyList<InventoryStack> Stacks => _stacks;

    public IReadOnlyList<InventoryEquipmentItem> Equipment => _equipment;

    public InventoryEquipmentItem? GetEquipment(string instanceId)
    {
        return _equipment.FirstOrDefault(item => item.InstanceId == instanceId);
    }

    public bool ReplaceEquipmentInstance(GeneratedEquipmentInstance instance)
    {
        var index = _equipment.FindIndex(item => item.InstanceId == instance.InstanceId);
        if (index < 0)
            return false;

        var current = _equipment[index];
        _equipment[index] = current with { Instance = instance };
        return true;
    }

    public IReadOnlyList<InventoryKeyItem> KeyItems => _keyItemEntries;

    public IReadOnlyList<ItemOperationLogEntry> OperationLog => _operationLog;

    /// <summary>
    /// Grants an item from battle, exploration, or narrative rewards.
    /// </summary>
    public InventoryOperationResult GrantItem(string itemId, int quantity, ItemGrantSource source)
    {
        if (string.IsNullOrWhiteSpace(itemId))
            return InventoryOperationResult.Fail("item_id_required");
        if (quantity <= 0)
            return InventoryOperationResult.Fail("quantity_must_be_positive", itemId);

        InventoryOperationResult result;
        var keyItem = _keyItems.Get(itemId);
        if (keyItem != null)
            result = GrantKeyItem(keyItem, quantity);
        else
        {
            var equipment = _equipmentTemplates.Get(itemId);
            if (equipment != null)
                result = GrantEquipment(equipment, quantity);
            else
            {
                var consumable = _consumables.Get(itemId);
                if (consumable != null)
                    result = GrantStackable(consumable, quantity);
                else
                    return InventoryOperationResult.Fail("unknown_item_id", itemId);
            }
        }

        if (result.Success)
            Log("grant", itemId, quantity, source.ToString());
        return result;
    }

    /// <summary>
    /// Returns the total quantity across all stacks or key item entries for a specific item id.
    /// </summary>
    public int GetTotalQuantity(string itemId)
    {
        var stackTotal = _stacks
            .Where(stack => stack.ItemId == itemId)
            .Sum(stack => stack.Quantity);
        var keyTotal = _keyItemEntries
            .Where(item => item.ItemId == itemId)
            .Sum(item => item.Quantity);
        return stackTotal + keyTotal;
    }

    public IReadOnlyList<InventoryStack> GetStacks(string itemId)
    {
        return _stacks.Where(stack => stack.ItemId == itemId).ToList();
    }

    public IReadOnlyList<CombatUsableItem> GetCombatUsableItems()
    {
        return _stacks
            .Where(stack => stack.Quantity > 0)
            .GroupBy(stack => stack.ItemId, StringComparer.Ordinal)
            .Select(group =>
            {
                var definition = _consumables.Get(group.Key);
                var quantity = group.Sum(stack => stack.Quantity);
                return definition == null || !definition.CombatUsable
                    ? null
                    : new CombatUsableItem(definition.Id, definition.Name, quantity, definition.Effects);
            })
            .Where(item => item != null)
            .Select(item => item!)
            .OrderBy(item => item.ItemId, StringComparer.Ordinal)
            .ToList();
    }

    public bool HasCombatUsableItem()
    {
        return GetCombatUsableItems().Count > 0;
    }

    public bool CanUseCombatItem(string itemId)
    {
        var definition = _consumables.Get(itemId);
        return definition?.CombatUsable == true && GetTotalQuantity(itemId) > 0;
    }

    public CombatItemUseResult UseCombatItem(string itemId, CombatItemActionContext actionContext)
    {
        ArgumentNullException.ThrowIfNull(actionContext);

        var definition = _consumables.Get(itemId);
        if (definition == null)
            return CombatItemUseResult.Fail("unknown_item_id", itemId);
        if (!definition.CombatUsable)
            return CombatItemUseResult.Fail("item_not_combat_usable", itemId);
        if (GetTotalQuantity(itemId) <= 0)
            return CombatItemUseResult.Fail("item_not_available", itemId);
        if (!actionContext.TryConsumeAction("use_item"))
            return CombatItemUseResult.Fail("combat_action_already_consumed", itemId);

        RemoveOneStackable(itemId);
        Log("combat_use", itemId, -1, "use_item");
        return CombatItemUseResult.Ok(itemId, definition.Effects);
    }

    public IReadOnlyList<CombatUsableItem> GetExplorationConsumables()
    {
        return _stacks
            .Where(stack => stack.Quantity > 0)
            .GroupBy(stack => stack.ItemId, StringComparer.Ordinal)
            .Select(group =>
            {
                var definition = _consumables.Get(group.Key);
                var quantity = group.Sum(stack => stack.Quantity);
                return definition == null || !definition.ExplorationUsable
                    ? null
                    : new CombatUsableItem(definition.Id, definition.Name, quantity, definition.Effects);
            })
            .Where(item => item != null)
            .Select(item => item!)
            .OrderBy(item => item.ItemId, StringComparer.Ordinal)
            .ToList();
    }

    public InventoryOperationResult DiscardItem(string itemId, int quantity)
    {
        if (_keyItems.Get(itemId) != null)
            return InventoryOperationResult.Fail("key_item_cannot_be_discarded", itemId);
        if (quantity <= 0)
            return InventoryOperationResult.Fail("quantity_must_be_positive", itemId);
        var result = RemoveStackableQuantity(itemId, quantity);
        if (result.Success)
            Log("discard", itemId, -quantity);
        return result;
    }

    public InventoryOperationResult SellItem(string itemId, int quantity)
    {
        if (_keyItems.Get(itemId) != null)
            return InventoryOperationResult.Fail("key_item_cannot_be_sold", itemId);
        if (quantity <= 0)
            return InventoryOperationResult.Fail("quantity_must_be_positive", itemId);

        var result = RemoveStackableQuantity(itemId, quantity);
        if (result.Success)
            Log("sell", itemId, -quantity);
        return result;
    }

    public bool CanEnterAuction(string itemId)
    {
        var keyItem = _keyItems.Get(itemId);
        return keyItem != null && keyItem.Auctionable && _keyItemEntries.Any(item => item.ItemId == itemId);
    }

    /// <summary>
    /// Consumes key items through an explicit system action while preserving normal discard/sell restrictions.
    /// </summary>
    public InventoryOperationResult ConsumeKeyItem(string itemId, int quantity, string reason)
    {
        if (string.IsNullOrWhiteSpace(itemId))
            return InventoryOperationResult.Fail("item_id_required");
        if (quantity <= 0)
            return InventoryOperationResult.Fail("quantity_must_be_positive", itemId);
        if (string.IsNullOrWhiteSpace(reason))
            return InventoryOperationResult.Fail("key_item_consume_reason_required", itemId);
        if (_keyItems.Get(itemId) == null)
            return InventoryOperationResult.Fail("unknown_item_id", itemId);
        if (_keyItemEntries.Where(item => item.ItemId == itemId).Sum(item => item.Quantity) < quantity)
            return InventoryOperationResult.Fail("not_enough_quantity", itemId);

        var remaining = quantity;
        for (var i = _keyItemEntries.Count - 1; i >= 0 && remaining > 0; i--)
        {
            var item = _keyItemEntries[i];
            if (item.ItemId != itemId)
                continue;

            var removed = Math.Min(item.Quantity, remaining);
            remaining -= removed;
            if (removed == item.Quantity)
                _keyItemEntries.RemoveAt(i);
            else
                _keyItemEntries[i] = item with { Quantity = item.Quantity - removed };
        }

        Log("consume_key_item", itemId, -quantity, reason);
        return InventoryOperationResult.Ok(itemId, -quantity);
    }

    public ItemInventorySaveData CreateSaveData()
    {
        return new ItemInventorySaveData
        {
            Stacks = _stacks
                .Select(stack => new ItemStackSaveData(stack.StackId, stack.ItemId, stack.Quantity, stack.MaxStack, stack.AcquiredOrder))
                .ToList(),
            Equipment = _equipment
                .Select(item => new ItemEquipmentSaveData(item.InstanceId, item.TemplateId, item.Instance, item.AcquiredOrder))
                .ToList(),
            KeyItems = _keyItemEntries
                .Select(item => new ItemKeyItemSaveData(item.ItemId, item.Quantity, item.Auctionable, item.AcquiredOrder))
                .ToList(),
            OperationLog = _operationLog.ToList(),
            NextOrder = _nextOrder,
            NextStackId = _nextStackId,
            NextEquipmentInstanceId = _nextEquipmentInstanceId
        };
    }

    public IReadOnlyList<ItemRestoreWarning> RestoreSaveData(ItemInventorySaveData data)
    {
        ArgumentNullException.ThrowIfNull(data);
        var warnings = new List<ItemRestoreWarning>();
        _stacks.Clear();
        _equipment.Clear();
        _keyItemEntries.Clear();
        _operationLog.Clear();

        foreach (var stack in data.Stacks)
        {
            var definition = _consumables.Get(stack.ItemId);
            if (definition == null)
            {
                warnings.Add(new ItemRestoreWarning("unknown_stack_item", "Skipped stack with unknown item id.", stack.ItemId));
                continue;
            }

            if (stack.Quantity <= 0)
            {
                warnings.Add(new ItemRestoreWarning("invalid_stack_quantity", "Skipped stack with non-positive quantity.", stack.StackId));
                continue;
            }

            _stacks.Add(new InventoryStack(
                stack.StackId,
                stack.ItemId,
                stack.Quantity,
                Math.Max(1, stack.MaxStack),
                Math.Max(0, stack.AcquiredOrder)));
        }

        foreach (var item in data.Equipment)
        {
            if (_equipmentTemplates.Get(item.TemplateId) == null)
            {
                warnings.Add(new ItemRestoreWarning("unknown_equipment_template", "Skipped equipment with unknown template id.", item.TemplateId));
                continue;
            }

            if (string.IsNullOrWhiteSpace(item.InstanceId) || item.Instance.InstanceId != item.InstanceId)
            {
                warnings.Add(new ItemRestoreWarning("invalid_equipment_instance", "Skipped equipment with mismatched instance id.", item.InstanceId));
                continue;
            }

            _equipment.Add(new InventoryEquipmentItem(
                item.InstanceId,
                item.TemplateId,
                item.Instance,
                Math.Max(0, item.AcquiredOrder)));
        }

        foreach (var item in data.KeyItems)
        {
            var definition = _keyItems.Get(item.ItemId);
            if (definition == null)
            {
                warnings.Add(new ItemRestoreWarning("unknown_key_item", "Skipped key item with unknown id.", item.ItemId));
                continue;
            }

            if (item.Quantity <= 0)
            {
                warnings.Add(new ItemRestoreWarning("invalid_key_item_quantity", "Skipped key item with non-positive quantity.", item.ItemId));
                continue;
            }

            _keyItemEntries.Add(new InventoryKeyItem(
                item.ItemId,
                item.Quantity,
                definition.Auctionable,
                Math.Max(0, item.AcquiredOrder)));
        }

        _operationLog.AddRange(data.OperationLog);
        _nextOrder = Math.Max(data.NextOrder, GetNextOrderFloor());
        _nextStackId = Math.Max(1, data.NextStackId);
        _nextEquipmentInstanceId = Math.Max(1, data.NextEquipmentInstanceId);
        return warnings;
    }

    /// <summary>
    /// Returns a combined sorted read model without mixing runtime storage lists.
    /// </summary>
    public IReadOnlyList<InventoryDisplayEntry> GetSortedEntries()
    {
        return _keyItemEntries
            .Select(item => new InventoryDisplayEntry(InventoryEntryKind.KeyItem, item.ItemId, null, item.Quantity, item.AcquiredOrder))
            .Concat(_equipment.Select(item => new InventoryDisplayEntry(InventoryEntryKind.Equipment, item.TemplateId, item.InstanceId, 1, item.AcquiredOrder)))
            .Concat(_stacks.Select(stack =>
            {
                var definition = _consumables.Get(stack.ItemId);
                return new InventoryDisplayEntry(ToStackKind(definition), stack.ItemId, null, stack.Quantity, stack.AcquiredOrder);
            }))
            .OrderBy(entry => SortRank(entry.Kind))
            .ThenByDescending(entry => entry.AcquiredOrder)
            .ToList();
    }

    private InventoryOperationResult GrantStackable(ConsumableDefinition definition, int quantity)
    {
        var remaining = quantity;
        for (var i = 0; i < _stacks.Count && remaining > 0; i++)
        {
            var stack = _stacks[i];
            if (stack.ItemId != definition.Id || stack.Quantity >= stack.MaxStack)
                continue;

            var added = Math.Min(stack.MaxStack - stack.Quantity, remaining);
            _stacks[i] = stack with { Quantity = stack.Quantity + added };
            remaining -= added;
        }

        while (remaining > 0)
        {
            var added = Math.Min(definition.MaxStack, remaining);
            _stacks.Add(new InventoryStack(
                $"stack_{_nextStackId++}",
                definition.Id,
                added,
                definition.MaxStack,
                _nextOrder++));
            remaining -= added;
        }

        return InventoryOperationResult.Ok(definition.Id, quantity);
    }

    private void RemoveOneStackable(string itemId)
    {
        for (var i = _stacks.Count - 1; i >= 0; i--)
        {
            var stack = _stacks[i];
            if (stack.ItemId != itemId)
                continue;

            if (stack.Quantity <= 1)
                _stacks.RemoveAt(i);
            else
                _stacks[i] = stack with { Quantity = stack.Quantity - 1 };
            return;
        }
    }

    private InventoryOperationResult RemoveStackableQuantity(string itemId, int quantity)
    {
        if (_stacks.Where(stack => stack.ItemId == itemId).Sum(stack => stack.Quantity) < quantity)
            return InventoryOperationResult.Fail("not_enough_quantity", itemId);

        var remaining = quantity;
        for (var i = _stacks.Count - 1; i >= 0 && remaining > 0; i--)
        {
            var stack = _stacks[i];
            if (stack.ItemId != itemId)
                continue;

            var removed = Math.Min(stack.Quantity, remaining);
            remaining -= removed;
            if (removed == stack.Quantity)
                _stacks.RemoveAt(i);
            else
                _stacks[i] = stack with { Quantity = stack.Quantity - removed };
        }

        return InventoryOperationResult.Ok(itemId, -quantity);
    }

    private InventoryOperationResult GrantEquipment(EquipmentTemplateDefinition definition, int quantity)
    {
        for (var i = 0; i < quantity; i++)
        {
            var instanceId = $"eq_{_nextEquipmentInstanceId++}";
            var instance = _equipmentService.GenerateInstance(definition.Id, instanceId);
            _equipment.Add(new InventoryEquipmentItem(
                instanceId,
                definition.Id,
                instance,
                _nextOrder++));
        }

        return InventoryOperationResult.Ok(definition.Id, quantity);
    }

    private InventoryOperationResult GrantKeyItem(KeyItemDefinition definition, int quantity)
    {
        var existingIndex = _keyItemEntries.FindIndex(item => item.ItemId == definition.Id);
        if (existingIndex >= 0)
        {
            var existing = _keyItemEntries[existingIndex];
            _keyItemEntries[existingIndex] = existing with { Quantity = existing.Quantity + quantity };
        }
        else
        {
            _keyItemEntries.Add(new InventoryKeyItem(
                definition.Id,
                quantity,
                definition.Auctionable,
                _nextOrder++));
        }

        return InventoryOperationResult.Ok(definition.Id, quantity);
    }

    private static InventoryEntryKind ToStackKind(ConsumableDefinition? definition)
    {
        if (definition?.CombatUsable == true)
            return InventoryEntryKind.CombatConsumable;
        if (definition?.ExplorationUsable == true)
            return InventoryEntryKind.ExplorationConsumable;
        return InventoryEntryKind.Other;
    }

    private static int SortRank(InventoryEntryKind kind)
    {
        return kind switch
        {
            InventoryEntryKind.KeyItem => 0,
            InventoryEntryKind.Equipment => 1,
            InventoryEntryKind.CombatConsumable => 2,
            InventoryEntryKind.ExplorationConsumable => 3,
            InventoryEntryKind.Other => 4,
            _ => 99
        };
    }

    private void Log(string action, string itemId, int quantity, string? context = null)
    {
        _operationLog.Add(new ItemOperationLogEntry(action, itemId, quantity, context));
    }

    private long GetNextOrderFloor()
    {
        var maxOrder = _stacks.Select(stack => stack.AcquiredOrder)
            .Concat(_equipment.Select(item => item.AcquiredOrder))
            .Concat(_keyItemEntries.Select(item => item.AcquiredOrder))
            .DefaultIfEmpty(0)
            .Max();
        return Math.Max(1, maxOrder + 1);
    }
}
