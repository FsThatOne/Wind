using FengZhi.Foundation.Data;

namespace FengZhi.Foundation.Items;

/// <summary>
/// Random source for equipment generation. Tests can inject deterministic implementations.
/// </summary>
public interface IItemRandomSource
{
    int NextInt(int maxExclusive);

    double NextDouble();
}

/// <summary>
/// Default random source backed by System.Random.
/// </summary>
public sealed class DefaultItemRandomSource : IItemRandomSource
{
    private readonly Random _random;

    public DefaultItemRandomSource(int? seed = null)
    {
        _random = seed.HasValue ? new Random(seed.Value) : new Random();
    }

    public int NextInt(int maxExclusive) => _random.Next(maxExclusive);

    public double NextDouble() => _random.NextDouble();
}

/// <summary>
/// Generated affix roll on an equipment instance.
/// </summary>
public sealed record EquipmentAffixRoll(
    string Id,
    string Stat,
    int Value,
    bool IsFixed);

/// <summary>
/// Runtime equipment instance generated from a static equipment template.
/// </summary>
public sealed record GeneratedEquipmentInstance(
    string InstanceId,
    string TemplateId,
    EquipmentGrade Grade,
    EquipmentSlot Slot,
    IReadOnlyDictionary<string, int> BaseAttributes,
    IReadOnlyList<EquipmentAffixRoll> Affixes,
    string DescriptionKey,
    int RefineCount = 0);

/// <summary>
/// Equipment presentation data that intentionally hides grade color and percentage rolls.
/// </summary>
public sealed record EquipmentDisplayInfo(
    string InstanceId,
    string TemplateId,
    string DescriptionKey,
    IReadOnlyList<string> Tags);

/// <summary>
/// Equipment ownership state.
/// </summary>
public sealed record EquippedItem(
    string OwnerId,
    EquipmentSlotId Slot,
    string InstanceId);

/// <summary>
/// Structured operation result for equipment commands.
/// </summary>
public sealed record EquipmentOperationResult(
    bool Success,
    string? ErrorCode,
    EquippedItem? EquippedItem)
{
    public static EquipmentOperationResult Ok(EquippedItem equippedItem)
    {
        return new EquipmentOperationResult(true, null, equippedItem);
    }

    public static EquipmentOperationResult Fail(string errorCode)
    {
        return new EquipmentOperationResult(false, errorCode, null);
    }
}

/// <summary>
/// Equipment generation and ownership service.
/// </summary>
public sealed class EquipmentService
{
    private static readonly IReadOnlyList<EquipmentSlotId> FiveSlotLayout =
        new[] { EquipmentSlotId.MainHand, EquipmentSlotId.Armor, EquipmentSlotId.Footwear, EquipmentSlotId.Accessory1, EquipmentSlotId.Accessory2 };

    private readonly IDataTable<EquipmentTemplateDefinition> _equipmentTemplates;
    private readonly IDataTable<AffixPoolDefinition> _affixPools;
    private readonly IItemRandomSource _randomSource;
    private readonly Dictionary<string, EquippedItem> _instanceToOwner = new(StringComparer.Ordinal);
    private readonly Dictionary<(string OwnerId, EquipmentSlotId Slot), string> _ownerSlotToInstance = new();

    public EquipmentService(DataRegistry registry, IItemRandomSource? randomSource = null)
    {
        ArgumentNullException.ThrowIfNull(registry);
        _equipmentTemplates = registry.GetTable<EquipmentTemplateDefinition>()
            ?? throw new ArgumentException("EquipmentTemplateDefinition table is not registered.", nameof(registry));
        _affixPools = registry.GetTable<AffixPoolDefinition>()
            ?? throw new ArgumentException("AffixPoolDefinition table is not registered.", nameof(registry));
        _randomSource = randomSource ?? new DefaultItemRandomSource();
    }

    public IReadOnlyDictionary<string, EquippedItem> InstanceToOwner => _instanceToOwner;

    public IReadOnlyList<EquipmentSlotId> GetFiveSlotLayout() => FiveSlotLayout;

    public GeneratedEquipmentInstance GenerateInstance(string templateId, string instanceId)
    {
        var template = _equipmentTemplates.Get(templateId)
            ?? throw new ArgumentException($"Unknown equipment template: {templateId}", nameof(templateId));

        var baseAttributes = template.BaseAttr.ToDictionary(
            pair => pair.Key,
            pair => ScaleAttribute(pair.Value, template.Grade),
            StringComparer.Ordinal);
        var affixes = template.Grade == EquipmentGrade.Legendary
            ? CreateFixedAffixes(template)
            : CreateRandomAffixes(template);

        return new GeneratedEquipmentInstance(
            instanceId,
            template.Id,
            template.Grade,
            template.Slot,
            baseAttributes,
            affixes,
            template.DescriptionKey);
    }

    public EquipmentDisplayInfo GetDisplayInfo(GeneratedEquipmentInstance instance)
    {
        return new EquipmentDisplayInfo(
            instance.InstanceId,
            instance.TemplateId,
            instance.DescriptionKey,
            new[] { "equipment", instance.Slot.ToString().ToLowerInvariant() });
    }

    public EquipmentOperationResult Equip(string ownerId, EquipmentSlotId slot, GeneratedEquipmentInstance instance)
    {
        if (string.IsNullOrWhiteSpace(ownerId))
            return EquipmentOperationResult.Fail("owner_id_required");
        if (!IsSlotCompatible(instance.Slot, slot))
            return EquipmentOperationResult.Fail("slot_incompatible");
        if (_instanceToOwner.ContainsKey(instance.InstanceId))
            return EquipmentOperationResult.Fail("equipment_instance_already_equipped");

        var key = (ownerId, slot);
        if (_ownerSlotToInstance.ContainsKey(key))
            return EquipmentOperationResult.Fail("slot_occupied");

        var equipped = new EquippedItem(ownerId, slot, instance.InstanceId);
        _instanceToOwner[instance.InstanceId] = equipped;
        _ownerSlotToInstance[key] = instance.InstanceId;
        return EquipmentOperationResult.Ok(equipped);
    }

    public bool Unequip(string instanceId)
    {
        if (!_instanceToOwner.TryGetValue(instanceId, out var equipped))
            return false;

        _instanceToOwner.Remove(instanceId);
        _ownerSlotToInstance.Remove((equipped.OwnerId, equipped.Slot));
        return true;
    }

    public EquippedItem? GetEquippedOwner(string instanceId)
    {
        return _instanceToOwner.GetValueOrDefault(instanceId);
    }

    public ItemEquipmentOwnershipSaveData CreateOwnershipSaveData()
    {
        return new ItemEquipmentOwnershipSaveData
        {
            EquippedItems = _instanceToOwner.Values.ToList()
        };
    }

    public IReadOnlyList<ItemRestoreWarning> RestoreOwnershipSaveData(
        ItemEquipmentOwnershipSaveData data,
        Func<string, GeneratedEquipmentInstance?> resolveInstance)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(resolveInstance);
        var warnings = new List<ItemRestoreWarning>();
        _instanceToOwner.Clear();
        _ownerSlotToInstance.Clear();

        foreach (var equipped in data.EquippedItems)
        {
            var instance = resolveInstance(equipped.InstanceId);
            if (instance == null)
            {
                warnings.Add(new ItemRestoreWarning("equipped_instance_missing", "Skipped equipped state for missing equipment instance.", equipped.InstanceId));
                continue;
            }

            if (!IsSlotCompatible(instance.Slot, equipped.Slot))
            {
                warnings.Add(new ItemRestoreWarning("equipped_slot_incompatible", "Skipped equipped state with incompatible slot.", equipped.InstanceId));
                continue;
            }

            if (_instanceToOwner.ContainsKey(equipped.InstanceId))
            {
                warnings.Add(new ItemRestoreWarning("duplicate_equipped_instance", "Skipped duplicate equipment ownership.", equipped.InstanceId));
                continue;
            }

            var slotKey = (equipped.OwnerId, equipped.Slot);
            if (_ownerSlotToInstance.ContainsKey(slotKey))
            {
                warnings.Add(new ItemRestoreWarning("duplicate_owner_slot", "Skipped equipped state for occupied owner slot.", equipped.InstanceId));
                continue;
            }

            _instanceToOwner[equipped.InstanceId] = equipped;
            _ownerSlotToInstance[slotKey] = equipped.InstanceId;
        }

        return warnings;
    }

    public static int GetAffixCount(EquipmentGrade grade)
    {
        return grade switch
        {
            EquipmentGrade.Ninth or EquipmentGrade.Eighth or EquipmentGrade.Seventh => 1,
            EquipmentGrade.Sixth or EquipmentGrade.Fifth or EquipmentGrade.Fourth => 2,
            EquipmentGrade.Third or EquipmentGrade.Second => 3,
            EquipmentGrade.First => 4,
            EquipmentGrade.Legendary => 0,
            _ => 0
        };
    }

    public static decimal GetGradeMultiplier(EquipmentGrade grade)
    {
        return grade switch
        {
            EquipmentGrade.Ninth => 1.0m,
            EquipmentGrade.Eighth => 1.2m,
            EquipmentGrade.Seventh => 1.4m,
            EquipmentGrade.Sixth => 1.7m,
            EquipmentGrade.Fifth => 2.2m,
            EquipmentGrade.Fourth => 2.8m,
            EquipmentGrade.Third => 3.5m,
            EquipmentGrade.Second => 4.5m,
            EquipmentGrade.First => 5.5m,
            EquipmentGrade.Legendary => 7.0m,
            _ => 1.0m
        };
    }

    public static int GetGradeAttributeCap(EquipmentGrade grade)
    {
        return grade switch
        {
            EquipmentGrade.Ninth => 20,
            EquipmentGrade.Eighth => 28,
            EquipmentGrade.Seventh => 36,
            EquipmentGrade.Sixth => 50,
            EquipmentGrade.Fifth => 70,
            EquipmentGrade.Fourth => 95,
            EquipmentGrade.Third => 125,
            EquipmentGrade.Second => 160,
            EquipmentGrade.First => 200,
            EquipmentGrade.Legendary => 260,
            _ => 20
        };
    }

    private static int ScaleAttribute(int value, EquipmentGrade grade)
    {
        return (int)Math.Round(value * GetGradeMultiplier(grade), MidpointRounding.AwayFromZero);
    }

    private IReadOnlyList<EquipmentAffixRoll> CreateRandomAffixes(EquipmentTemplateDefinition template)
    {
        var pool = _affixPools.Get(template.AffixPool)
            ?? throw new InvalidOperationException($"Missing affix pool: {template.AffixPool}");
        var count = Math.Min(GetAffixCount(template.Grade), pool.Affixes.Count);
        var available = pool.Affixes.ToList();
        var rolls = new List<EquipmentAffixRoll>(count);

        for (var i = 0; i < count; i++)
        {
            var index = _randomSource.NextInt(available.Count);
            var affix = available[index];
            available.RemoveAt(index);

            var minValue = Math.Max(1, (int)Math.Ceiling(affix.MaxValue * 0.4m));
            var range = affix.MaxValue - minValue + 1;
            var value = minValue + (range <= 1 ? 0 : (int)Math.Floor(_randomSource.NextDouble() * range));
            value = Math.Clamp(value, minValue, affix.MaxValue);
            rolls.Add(new EquipmentAffixRoll(affix.Id, affix.Stat, value, IsFixed: false));
        }

        return rolls;
    }

    private static IReadOnlyList<EquipmentAffixRoll> CreateFixedAffixes(EquipmentTemplateDefinition template)
    {
        return template.FixedAffixes
            .Select(id => new EquipmentAffixRoll(id, id, 0, IsFixed: true))
            .ToList();
    }

    private static bool IsSlotCompatible(EquipmentSlot templateSlot, EquipmentSlotId targetSlot)
    {
        return templateSlot switch
        {
            EquipmentSlot.MainHand => targetSlot == EquipmentSlotId.MainHand,
            EquipmentSlot.Armor => targetSlot == EquipmentSlotId.Armor,
            EquipmentSlot.Footwear => targetSlot == EquipmentSlotId.Footwear,
            EquipmentSlot.Accessory => targetSlot is EquipmentSlotId.Accessory1 or EquipmentSlotId.Accessory2,
            _ => false
        };
    }
}
