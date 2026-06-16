using YamlDotNet.Serialization;

namespace FengZhi.Foundation.Items;

/// <summary>
/// Runtime item category. The category decides which static table owns the definition.
/// </summary>
public enum ItemCategory
{
    Consumable,
    Equipment,
    KeyItem,
    Material
}

/// <summary>
/// Material and item rarity. Equipment grade is tracked separately and must not drive UI color.
/// </summary>
public enum ItemRarity
{
    Common,
    Uncommon,
    Rare,
    Epic,
    Legendary
}

/// <summary>
/// Equipment quality grade used by item templates and generation formulas.
/// </summary>
public enum EquipmentGrade
{
    Ninth,
    Eighth,
    Seventh,
    Sixth,
    Fifth,
    Fourth,
    Third,
    Second,
    First,
    Legendary
}

/// <summary>
/// Equipment slot shared by the protagonist and companions.
/// </summary>
public enum EquipmentSlot
{
    MainHand,
    Armor,
    Footwear,
    Accessory
}

/// <summary>
/// Recipe family. Later stories attach runtime execution rules to these values.
/// </summary>
public enum RecipeKind
{
    Alchemy,
    Forge,
    Refine
}

/// <summary>
/// Structured effect entry used by consumables and future item actions.
/// </summary>
public sealed class ItemEffectDefinition
{
    [YamlMember(Alias = "type")]
    public string Type { get; set; } = string.Empty;

    [YamlMember(Alias = "value")]
    public int Value { get; set; }

    [YamlMember(Alias = "target")]
    public string Target { get; set; } = string.Empty;
}

/// <summary>
/// Consumable item static definition.
/// </summary>
public sealed class ConsumableDefinition
{
    [YamlMember(Alias = "id")]
    public string Id { get; set; } = string.Empty;

    [YamlMember(Alias = "name")]
    public string Name { get; set; } = string.Empty;

    [YamlMember(Alias = "category")]
    public ItemCategory Category { get; set; } = ItemCategory.Consumable;

    [YamlMember(Alias = "rarity")]
    public ItemRarity Rarity { get; set; } = ItemRarity.Common;

    [YamlMember(Alias = "max_stack")]
    public int MaxStack { get; set; } = 99;

    [YamlMember(Alias = "combat_usable")]
    public bool CombatUsable { get; set; }

    [YamlMember(Alias = "exploration_usable")]
    public bool ExplorationUsable { get; set; }

    [YamlMember(Alias = "base_price")]
    public int BasePrice { get; set; }

    [YamlMember(Alias = "effects")]
    public List<ItemEffectDefinition> Effects { get; set; } = new();

    [YamlMember(Alias = "tags")]
    public List<string> Tags { get; set; } = new();
}

/// <summary>
/// Equipment template definition. Instances are generated from this immutable template.
/// </summary>
public sealed class EquipmentTemplateDefinition
{
    [YamlMember(Alias = "id")]
    public string Id { get; set; } = string.Empty;

    [YamlMember(Alias = "name")]
    public string Name { get; set; } = string.Empty;

    [YamlMember(Alias = "category")]
    public ItemCategory Category { get; set; } = ItemCategory.Equipment;

    [YamlMember(Alias = "grade")]
    public EquipmentGrade Grade { get; set; }

    [YamlMember(Alias = "slot")]
    public EquipmentSlot Slot { get; set; }

    [YamlMember(Alias = "base_attr")]
    public Dictionary<string, int> BaseAttr { get; set; } = new(StringComparer.Ordinal);

    [YamlMember(Alias = "affix_pool")]
    public string AffixPool { get; set; } = string.Empty;

    [YamlMember(Alias = "fixed_affixes")]
    public List<string> FixedAffixes { get; set; } = new();

    [YamlMember(Alias = "base_price")]
    public int BasePrice { get; set; }

    [YamlMember(Alias = "description_key")]
    public string DescriptionKey { get; set; } = string.Empty;
}

/// <summary>
/// Key item definition. Key items are separated from stackable inventory.
/// </summary>
public sealed class KeyItemDefinition
{
    [YamlMember(Alias = "id")]
    public string Id { get; set; } = string.Empty;

    [YamlMember(Alias = "name")]
    public string Name { get; set; } = string.Empty;

    [YamlMember(Alias = "category")]
    public ItemCategory Category { get; set; } = ItemCategory.KeyItem;

    [YamlMember(Alias = "rarity")]
    public ItemRarity Rarity { get; set; } = ItemRarity.Common;

    [YamlMember(Alias = "auctionable")]
    public bool Auctionable { get; set; }

    [YamlMember(Alias = "description_key")]
    public string DescriptionKey { get; set; } = string.Empty;

    [YamlMember(Alias = "linked_move_id")]
    public string? LinkedMoveId { get; set; }
}

/// <summary>
/// Recipe ingredient entry.
/// </summary>
public sealed class RecipeIngredientDefinition
{
    [YamlMember(Alias = "item_id")]
    public string ItemId { get; set; } = string.Empty;

    [YamlMember(Alias = "qty")]
    public int Quantity { get; set; }
}

/// <summary>
/// Crafting recipe static definition.
/// </summary>
public sealed class RecipeDefinition
{
    [YamlMember(Alias = "id")]
    public string Id { get; set; } = string.Empty;

    [YamlMember(Alias = "kind")]
    public RecipeKind Kind { get; set; }

    [YamlMember(Alias = "ingredients")]
    public List<RecipeIngredientDefinition> Ingredients { get; set; } = new();

    [YamlMember(Alias = "output_item_id")]
    public string OutputItemId { get; set; } = string.Empty;

    [YamlMember(Alias = "output_qty")]
    public int OutputQuantity { get; set; } = 1;

    [YamlMember(Alias = "silver_cost")]
    public int SilverCost { get; set; }
}

/// <summary>
/// Single affix candidate in an affix pool.
/// </summary>
public sealed class AffixDefinition
{
    [YamlMember(Alias = "id")]
    public string Id { get; set; } = string.Empty;

    [YamlMember(Alias = "stat")]
    public string Stat { get; set; } = string.Empty;

    [YamlMember(Alias = "max_value")]
    public int MaxValue { get; set; }
}

/// <summary>
/// Affix pool used by equipment generation.
/// </summary>
public sealed class AffixPoolDefinition
{
    [YamlMember(Alias = "id")]
    public string Id { get; set; } = string.Empty;

    [YamlMember(Alias = "grade")]
    public EquipmentGrade Grade { get; set; }

    [YamlMember(Alias = "affixes")]
    public List<AffixDefinition> Affixes { get; set; } = new();
}

/// <summary>
/// Aggregate item definition registry for cross-table id and rarity lookup.
/// </summary>
public sealed class ItemDefinitionRegistry
{
    private readonly Dictionary<string, ItemCatalogEntry> _entries;

    public ItemDefinitionRegistry(IEnumerable<ItemCatalogEntry> entries)
    {
        _entries = entries.ToDictionary(entry => entry.Id, StringComparer.Ordinal);
    }

    public int Count => _entries.Count;

    public bool Has(string id) => _entries.ContainsKey(id);

    public ItemCatalogEntry? Get(string id) => _entries.GetValueOrDefault(id);

    public ItemRarity? GetRarity(string id) => Get(id)?.Rarity;

    public IReadOnlyList<ItemCatalogEntry> GetAll() => _entries.Values.ToList();
}

/// <summary>
/// Normalized catalog entry shared by all item definition kinds.
/// </summary>
public sealed record ItemCatalogEntry(
    string Id,
    string Name,
    ItemCategory Category,
    ItemRarity Rarity,
    int? MaxStack);
