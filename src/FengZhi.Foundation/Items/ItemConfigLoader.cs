using FengZhi.Foundation.Data;
using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace FengZhi.Foundation.Items;

/// <summary>
/// Loads and validates all item YAML configuration tables.
/// </summary>
public sealed class ItemConfigLoader
{
    public const string ConsumablesPath = "assets/data/items/consumables.yaml";
    public const string EquipmentTemplatesPath = "assets/data/items/equipment-templates.yaml";
    public const string KeyItemsPath = "assets/data/items/key-items.yaml";
    public const string RecipesPath = "assets/data/items/recipes.yaml";
    public const string AffixPoolsPath = "assets/data/items/affix-pools.yaml";

    private static readonly IDeserializer _deserializer = new DeserializerBuilder()
        .WithNamingConvention(UnderscoredNamingConvention.Instance)
        .WithEnumNamingConvention(UnderscoredNamingConvention.Instance)
        .Build();

    /// <summary>
    /// Loads all item tables and registers them into the provided registry.
    /// </summary>
    public void LoadAll(
        string consumablesYaml,
        string equipmentTemplatesYaml,
        string keyItemsYaml,
        string recipesYaml,
        string affixPoolsYaml,
        DataRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(registry);

        var consumables = LoadList<ConsumableDefinition>(consumablesYaml, ConsumablesPath);
        var equipment = LoadList<EquipmentTemplateDefinition>(equipmentTemplatesYaml, EquipmentTemplatesPath);
        var keyItems = LoadList<KeyItemDefinition>(keyItemsYaml, KeyItemsPath);
        var recipes = LoadList<RecipeDefinition>(recipesYaml, RecipesPath);
        var affixPools = LoadList<AffixPoolDefinition>(affixPoolsYaml, AffixPoolsPath);

        ValidateConsumables(consumables, ConsumablesPath);
        ValidateKeyItems(keyItems, KeyItemsPath);
        ValidateAffixPools(affixPools, AffixPoolsPath);
        ValidateEquipment(equipment, affixPools, EquipmentTemplatesPath);
        var catalog = BuildCatalog(consumables, equipment, keyItems);
        ValidateRecipes(recipes, catalog, RecipesPath);

        registry.RegisterTable<ConsumableDefinition>(new ConsumableDefinitionTable(consumables));
        registry.RegisterTable<EquipmentTemplateDefinition>(new EquipmentTemplateDefinitionTable(equipment));
        registry.RegisterTable<KeyItemDefinition>(new KeyItemDefinitionTable(keyItems));
        registry.RegisterTable<RecipeDefinition>(new RecipeDefinitionTable(recipes));
        registry.RegisterTable<AffixPoolDefinition>(new AffixPoolDefinitionTable(affixPools));
        registry.RegisterTable<ItemCatalogEntry>(new ItemCatalogTable(catalog.GetAll()));
    }

    /// <summary>
    /// Deserializes a YAML list document and wraps parser errors with file and line information.
    /// </summary>
    public IReadOnlyList<T> LoadList<T>(string yamlContent, string filePath) where T : class
    {
        if (string.IsNullOrWhiteSpace(yamlContent))
            return Array.Empty<T>();

        try
        {
            return _deserializer.Deserialize<List<T>>(yamlContent) ?? new List<T>();
        }
        catch (YamlException ex)
        {
            throw new DataLoadException(filePath, (int)ex.Start.Line, $"YAML parse error: {ex.Message}", ex);
        }
    }

    private static void ValidateConsumables(IReadOnlyList<ConsumableDefinition> consumables, string filePath)
    {
        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var item in consumables)
        {
            RequireId(item.Id, filePath, "consumable.id");
            RequireText(item.Name, filePath, $"consumable[{item.Id}].name");
            RequireCategory(item.Category, filePath, item.Id, ItemCategory.Consumable, ItemCategory.Material);
            RequireDefined(item.Rarity, filePath, $"consumable[{item.Id}].rarity");
            RequireUniqueId(ids, item.Id, filePath);

            if (item.MaxStack < 1 || item.MaxStack > 999)
                throw new DataLoadException(filePath, null, $"字段 max_stack 非法: '{item.Id}' 必须在 1..999");

            if (item.BasePrice < 0)
                throw new DataLoadException(filePath, null, $"字段 base_price 非法: '{item.Id}' 必须 >= 0");

            foreach (var effect in item.Effects)
            {
                RequireText(effect.Type, filePath, $"consumable[{item.Id}].effects.type");
                if (effect.Value <= 0)
                    throw new DataLoadException(filePath, null, $"字段 effects.value 非法: '{item.Id}' 必须 > 0");
            }
        }
    }

    private static void ValidateEquipment(
        IReadOnlyList<EquipmentTemplateDefinition> equipment,
        IReadOnlyList<AffixPoolDefinition> affixPools,
        string filePath)
    {
        var ids = new HashSet<string>(StringComparer.Ordinal);
        var poolIds = affixPools.Select(pool => pool.Id).ToHashSet(StringComparer.Ordinal);

        foreach (var item in equipment)
        {
            RequireId(item.Id, filePath, "equipment.id");
            RequireText(item.Name, filePath, $"equipment[{item.Id}].name");
            RequireCategory(item.Category, filePath, item.Id, ItemCategory.Equipment);
            RequireDefined(item.Grade, filePath, $"equipment[{item.Id}].grade");
            RequireDefined(item.Slot, filePath, $"equipment[{item.Id}].slot");
            RequireUniqueId(ids, item.Id, filePath);

            if (item.BaseAttr.Count == 0)
                throw new DataLoadException(filePath, null, $"字段 base_attr 缺失: '{item.Id}'");

            foreach (var (stat, value) in item.BaseAttr)
            {
                RequireText(stat, filePath, $"equipment[{item.Id}].base_attr.key");
                if (value < 0)
                    throw new DataLoadException(filePath, null, $"字段 base_attr 非法: '{item.Id}' 属性 '{stat}' 必须 >= 0");
            }

            if (item.Grade == EquipmentGrade.Legendary)
            {
                if (item.FixedAffixes.Count == 0)
                    throw new DataLoadException(filePath, null, $"传说装备必须配置 fixed_affixes: '{item.Id}'");
            }
            else
            {
                RequireText(item.AffixPool, filePath, $"equipment[{item.Id}].affix_pool");
                if (!poolIds.Contains(item.AffixPool))
                    throw new DataLoadException(filePath, null, $"字段 affix_pool 引用不存在: 装备 '{item.Id}' -> 词条池 '{item.AffixPool}'");
            }

            if (item.BasePrice < 0)
                throw new DataLoadException(filePath, null, $"字段 base_price 非法: '{item.Id}' 必须 >= 0");
        }
    }

    private static void ValidateKeyItems(IReadOnlyList<KeyItemDefinition> keyItems, string filePath)
    {
        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var item in keyItems)
        {
            RequireId(item.Id, filePath, "key_item.id");
            RequireText(item.Name, filePath, $"key_item[{item.Id}].name");
            RequireCategory(item.Category, filePath, item.Id, ItemCategory.KeyItem);
            RequireDefined(item.Rarity, filePath, $"key_item[{item.Id}].rarity");
            RequireUniqueId(ids, item.Id, filePath);
        }
    }

    private static void ValidateRecipes(
        IReadOnlyList<RecipeDefinition> recipes,
        ItemDefinitionRegistry catalog,
        string filePath)
    {
        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var recipe in recipes)
        {
            RequireId(recipe.Id, filePath, "recipe.id");
            RequireDefined(recipe.Kind, filePath, $"recipe[{recipe.Id}].kind");
            RequireUniqueId(ids, recipe.Id, filePath);

            if (recipe.Ingredients.Count == 0)
                throw new DataLoadException(filePath, null, $"字段 ingredients 缺失: '{recipe.Id}'");

            foreach (var ingredient in recipe.Ingredients)
            {
                RequireText(ingredient.ItemId, filePath, $"recipe[{recipe.Id}].ingredients.item_id");
                if (!catalog.Has(ingredient.ItemId))
                    throw new DataLoadException(filePath, null, $"字段 ingredients 引用不存在: 配方 '{recipe.Id}' -> 物品 '{ingredient.ItemId}'");
                if (ingredient.Quantity <= 0)
                    throw new DataLoadException(filePath, null, $"字段 ingredients.qty 非法: 配方 '{recipe.Id}' 必须 > 0");
            }

            RequireText(recipe.OutputItemId, filePath, $"recipe[{recipe.Id}].output_item_id");
            if (!catalog.Has(recipe.OutputItemId))
                throw new DataLoadException(filePath, null, $"字段 output_item_id 引用不存在: 配方 '{recipe.Id}' -> 物品 '{recipe.OutputItemId}'");

            if (recipe.OutputQuantity <= 0)
                throw new DataLoadException(filePath, null, $"字段 output_qty 非法: '{recipe.Id}' 必须 > 0");

            if (recipe.SilverCost < 0)
                throw new DataLoadException(filePath, null, $"字段 silver_cost 非法: '{recipe.Id}' 必须 >= 0");
        }
    }

    private static void ValidateAffixPools(IReadOnlyList<AffixPoolDefinition> affixPools, string filePath)
    {
        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var pool in affixPools)
        {
            RequireId(pool.Id, filePath, "affix_pool.id");
            RequireDefined(pool.Grade, filePath, $"affix_pool[{pool.Id}].grade");
            RequireUniqueId(ids, pool.Id, filePath);

            if (pool.Affixes.Count == 0)
                throw new DataLoadException(filePath, null, $"字段 affixes 缺失: '{pool.Id}'");

            var affixIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (var affix in pool.Affixes)
            {
                RequireId(affix.Id, filePath, $"affix_pool[{pool.Id}].affixes.id");
                RequireText(affix.Stat, filePath, $"affix_pool[{pool.Id}].affixes.stat");
                RequireUniqueId(affixIds, affix.Id, filePath);
                if (affix.MaxValue <= 0)
                    throw new DataLoadException(filePath, null, $"字段 max_value 非法: 词条 '{affix.Id}' 必须 > 0");
            }
        }
    }

    private static ItemDefinitionRegistry BuildCatalog(
        IReadOnlyList<ConsumableDefinition> consumables,
        IReadOnlyList<EquipmentTemplateDefinition> equipment,
        IReadOnlyList<KeyItemDefinition> keyItems)
    {
        var ids = new HashSet<string>(StringComparer.Ordinal);
        var entries = new List<ItemCatalogEntry>();

        foreach (var item in consumables)
            AddCatalogEntry(ids, entries, new ItemCatalogEntry(item.Id, item.Name, item.Category, item.Rarity, item.MaxStack), ConsumablesPath);
        foreach (var item in equipment)
            AddCatalogEntry(ids, entries, new ItemCatalogEntry(item.Id, item.Name, item.Category, ItemRarity.Common, null), EquipmentTemplatesPath);
        foreach (var item in keyItems)
            AddCatalogEntry(ids, entries, new ItemCatalogEntry(item.Id, item.Name, item.Category, item.Rarity, 1), KeyItemsPath);

        return new ItemDefinitionRegistry(entries);
    }

    private static void AddCatalogEntry(
        HashSet<string> ids,
        List<ItemCatalogEntry> entries,
        ItemCatalogEntry entry,
        string filePath)
    {
        if (!ids.Add(entry.Id))
            throw new DataLoadException(filePath, null, $"跨物品表 id 重复: '{entry.Id}'");

        entries.Add(entry);
    }

    private static void RequireCategory(ItemCategory actual, string filePath, string id, params ItemCategory[] expected)
    {
        RequireDefined(actual, filePath, $"item[{id}].category");
        if (!expected.Contains(actual))
            throw new DataLoadException(filePath, null, $"字段 category 非法: '{id}' 必须为 {string.Join("/", expected)}");
    }

    private static void RequireDefined<TEnum>(TEnum value, string filePath, string fieldName)
        where TEnum : struct, Enum
    {
        if (!Enum.IsDefined(value))
            throw new DataLoadException(filePath, null, $"字段 {fieldName} 非法: '{value}'");
    }

    private static void RequireUniqueId(HashSet<string> ids, string id, string filePath)
    {
        if (!ids.Add(id))
            throw new DataLoadException(filePath, null, $"字段 id 重复: '{id}'");
    }

    private static void RequireId(string value, string filePath, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DataLoadException(filePath, null, $"缺少必填字段 {fieldName}");
    }

    private static void RequireText(string value, string filePath, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DataLoadException(filePath, null, $"缺少必填字段 {fieldName}");
    }
}

/// <summary>Consumable table with O(1) id lookup.</summary>
public sealed class ConsumableDefinitionTable : ItemDataTable<ConsumableDefinition>
{
    public ConsumableDefinitionTable(IEnumerable<ConsumableDefinition> items)
        : base(items, item => item.Id)
    {
    }
}

/// <summary>Equipment template table with O(1) id lookup.</summary>
public sealed class EquipmentTemplateDefinitionTable : ItemDataTable<EquipmentTemplateDefinition>
{
    public EquipmentTemplateDefinitionTable(IEnumerable<EquipmentTemplateDefinition> items)
        : base(items, item => item.Id)
    {
    }
}

/// <summary>Key item table with O(1) id lookup.</summary>
public sealed class KeyItemDefinitionTable : ItemDataTable<KeyItemDefinition>
{
    public KeyItemDefinitionTable(IEnumerable<KeyItemDefinition> items)
        : base(items, item => item.Id)
    {
    }
}

/// <summary>Recipe table with O(1) id lookup.</summary>
public sealed class RecipeDefinitionTable : ItemDataTable<RecipeDefinition>
{
    public RecipeDefinitionTable(IEnumerable<RecipeDefinition> items)
        : base(items, item => item.Id)
    {
    }
}

/// <summary>Affix pool table with O(1) id lookup.</summary>
public sealed class AffixPoolDefinitionTable : ItemDataTable<AffixPoolDefinition>
{
    public AffixPoolDefinitionTable(IEnumerable<AffixPoolDefinition> items)
        : base(items, item => item.Id)
    {
    }
}

/// <summary>Normalized item catalog table with O(1) id lookup.</summary>
public sealed class ItemCatalogTable : ItemDataTable<ItemCatalogEntry>
{
    public ItemCatalogTable(IEnumerable<ItemCatalogEntry> items)
        : base(items, item => item.Id)
    {
    }

    public ItemRarity? GetRarity(string id) => Get(id)?.Rarity;
}

/// <summary>
/// Shared immutable data table implementation for item definitions.
/// </summary>
public abstract class ItemDataTable<T> : IDataTable<T> where T : class
{
    private readonly Dictionary<string, T> _map;
    private readonly List<T> _list;

    protected ItemDataTable(IEnumerable<T> items, Func<T, string> idSelector)
    {
        _list = items.ToList();
        _map = _list.ToDictionary(idSelector, StringComparer.Ordinal);
    }

    public int Count => _list.Count;

    public T? Get(string id) => _map.TryGetValue(id, out var item) ? item : null;

    public IReadOnlyList<T> GetAll() => _list;
}
