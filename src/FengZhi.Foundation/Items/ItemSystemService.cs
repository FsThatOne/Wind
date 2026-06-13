using System.Text.Json;
using FengZhi.Foundation.SaveSystem;

namespace FengZhi.Foundation.Items;

public sealed record ItemRestoreWarning(string Code, string Message, string? RefId = null);

public sealed record ItemOperationLogEntry(string Action, string ItemId, int Quantity, string? Context = null);

public sealed record ItemRewardGrantRequest(string ItemId, int Quantity);

public sealed record ExplorationConsumableEffect(string ItemId, string Type, string Target, int Value);

public sealed record ItemStackSaveData(
    string StackId,
    string ItemId,
    int Quantity,
    int MaxStack,
    long AcquiredOrder);

public sealed record ItemEquipmentSaveData(
    string InstanceId,
    string TemplateId,
    GeneratedEquipmentInstance Instance,
    long AcquiredOrder);

public sealed record ItemKeyItemSaveData(
    string ItemId,
    int Quantity,
    bool Auctionable,
    long AcquiredOrder);

public sealed record ItemInventorySaveData
{
    public List<ItemStackSaveData> Stacks { get; init; } = new();

    public List<ItemEquipmentSaveData> Equipment { get; init; } = new();

    public List<ItemKeyItemSaveData> KeyItems { get; init; } = new();

    public List<ItemOperationLogEntry> OperationLog { get; init; } = new();

    public long NextOrder { get; init; } = 1;

    public long NextStackId { get; init; } = 1;

    public long NextEquipmentInstanceId { get; init; } = 1;
}

public sealed record ItemEconomySaveData
{
    public int Silver { get; init; }
}

public sealed record ItemEquipmentOwnershipSaveData
{
    public List<EquippedItem> EquippedItems { get; init; } = new();
}

public sealed record ItemSystemSaveData
{
    public ItemInventorySaveData Inventory { get; init; } = new();

    public ItemEconomySaveData Economy { get; init; } = new();

    public ItemEquipmentOwnershipSaveData EquipmentOwnership { get; init; } = new();
}

/// <summary>
/// Saveable facade and cross-system query boundary for the item system.
/// </summary>
public sealed class ItemSystemService : ISaveable
{
    public const string ItemSaveKey = "items";

    private readonly InventoryService _inventory;
    private readonly EquipmentService _equipment;
    private readonly EconomyService _economy;

    public ItemSystemService(InventoryService inventory, EquipmentService equipment, EconomyService economy)
    {
        _inventory = inventory ?? throw new ArgumentNullException(nameof(inventory));
        _equipment = equipment ?? throw new ArgumentNullException(nameof(equipment));
        _economy = economy ?? throw new ArgumentNullException(nameof(economy));
    }

    public string SaveKey => ItemSaveKey;

    public IReadOnlyList<ItemRestoreWarning> LastRestoreWarnings { get; private set; } = Array.Empty<ItemRestoreWarning>();

    public SaveSnapshot Serialize()
    {
        return new SaveSnapshot
        {
            Values = new Dictionary<string, JsonElement>(StringComparer.Ordinal)
            {
                ["runtime"] = JsonSerializer.SerializeToElement(CreateSaveData())
            }
        };
    }

    public void Deserialize(SaveSnapshot snapshot, int version)
    {
        _ = version;
        if (!snapshot.Values.TryGetValue("runtime", out var runtimeElement))
            return;

        var data = runtimeElement.Deserialize<ItemSystemSaveData>();
        if (data == null)
            return;

        LastRestoreWarnings = RestoreSaveData(data);
    }

    public ItemSystemSaveData CreateSaveData()
    {
        return new ItemSystemSaveData
        {
            Inventory = _inventory.CreateSaveData(),
            Economy = _economy.CreateSaveData(),
            EquipmentOwnership = _equipment.CreateOwnershipSaveData()
        };
    }

    public IReadOnlyList<ItemRestoreWarning> RestoreSaveData(ItemSystemSaveData data)
    {
        ArgumentNullException.ThrowIfNull(data);
        var warnings = new List<ItemRestoreWarning>();
        warnings.AddRange(_inventory.RestoreSaveData(data.Inventory));
        warnings.AddRange(_economy.RestoreSaveData(data.Economy));
        warnings.AddRange(_equipment.RestoreOwnershipSaveData(
            data.EquipmentOwnership,
            instanceId => _inventory.GetEquipment(instanceId)?.Instance));
        return warnings;
    }

    public InventoryOperationResult GrantItem(string itemId, int quantity)
    {
        return _inventory.GrantItem(itemId, quantity, ItemGrantSource.NarrativeReward);
    }

    public InventoryOperationResult GrantItem(ItemRewardGrantRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return GrantItem(request.ItemId, request.Quantity);
    }

    public IReadOnlyList<ExplorationConsumableEffect> GetExplorationStaminaRecoveryEffects()
    {
        return _inventory.GetExplorationConsumables()
            .SelectMany(item => item.Effects
                .Where(effect => string.Equals(effect.Type, "restore_stamina", StringComparison.Ordinal))
                .Select(effect => new ExplorationConsumableEffect(item.ItemId, effect.Type, effect.Target, effect.Value)))
            .ToList();
    }

    public EquipmentDisplayInfo? GetEquipmentDisplayInfo(string instanceId)
    {
        var equipment = _inventory.GetEquipment(instanceId);
        return equipment == null ? null : _equipment.GetDisplayInfo(equipment.Instance);
    }

    public IReadOnlyList<CombatUsableItem> GetCombatUsableItems()
    {
        return _inventory.GetCombatUsableItems();
    }

    public bool CanUseMartialFragment(string itemId)
    {
        return _inventory.GetTotalQuantity(itemId) > 0;
    }
}
