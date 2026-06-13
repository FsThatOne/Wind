using FengZhi.Foundation.Data;
using FengZhi.Foundation.Items;
using Xunit;

namespace FengZhi.Tests.unit.items;

public sealed class ItemYamlSchemaTest
{
    private readonly ItemConfigLoader _loader = new();

    [Fact]
    public void LoadAll_ValidItemYaml_RegistersQueryableTables()
    {
        var registry = new DataRegistry();

        _loader.LoadAll(ValidConsumablesYaml, ValidEquipmentYaml, ValidKeyItemsYaml, ValidRecipesYaml, ValidAffixPoolsYaml, registry);

        var consumables = registry.GetTable<ConsumableDefinition>();
        var equipment = registry.GetTable<EquipmentTemplateDefinition>();
        var keyItems = registry.GetTable<KeyItemDefinition>();
        var recipes = registry.GetTable<RecipeDefinition>();
        var affixPools = registry.GetTable<AffixPoolDefinition>();
        var catalog = Assert.IsType<ItemCatalogTable>(registry.GetTable<ItemCatalogEntry>());

        Assert.Equal(2, consumables!.Count);
        Assert.Equal(2, equipment!.Count);
        Assert.Single(keyItems!.GetAll());
        Assert.Single(recipes!.GetAll());
        Assert.Single(affixPools!.GetAll());
        Assert.Equal("止血丸", consumables.Get("zhi_xue_wan")!.Name);
        Assert.Equal(ItemRarity.Uncommon, consumables.Get("herb_green")!.Rarity);
        Assert.Equal(EquipmentGrade.Fifth, equipment.Get("plain_iron_sword")!.Grade);
        Assert.Equal(ItemCategory.KeyItem, keyItems.Get("letter_wind_stop")!.Category);
        Assert.True(catalog.GetRarity("herb_green").HasValue);
        Assert.Equal(ItemRarity.Uncommon, catalog.GetRarity("herb_green"));
    }

    [Fact]
    public void LoadAll_DuplicateIdInsideTable_FailsFastWithFileNameAndId()
    {
        var invalidConsumables = ValidConsumablesYaml + """

        - id: zhi_xue_wan
          name: 止血丸重录
          category: consumable
          rarity: common
          max_stack: 10
          effects:
            - type: heal_hp
              value: 20
        """;

        var ex = Assert.Throws<DataLoadException>(() =>
            _loader.LoadAll(invalidConsumables, ValidEquipmentYaml, ValidKeyItemsYaml, ValidRecipesYaml, ValidAffixPoolsYaml, new DataRegistry()));

        Assert.Contains(ItemConfigLoader.ConsumablesPath, ex.Message);
        Assert.Contains("id 重复", ex.Message);
        Assert.Contains("zhi_xue_wan", ex.Message);
    }

    [Fact]
    public void LoadAll_DuplicateIdAcrossTables_FailsFastWithFileNameAndId()
    {
        var invalidKeyItems = ValidKeyItemsYaml.Replace("letter_wind_stop", "zhi_xue_wan");

        var ex = Assert.Throws<DataLoadException>(() =>
            _loader.LoadAll(ValidConsumablesYaml, ValidEquipmentYaml, invalidKeyItems, ValidRecipesYaml, ValidAffixPoolsYaml, new DataRegistry()));

        Assert.Contains(ItemConfigLoader.KeyItemsPath, ex.Message);
        Assert.Contains("跨物品表 id 重复", ex.Message);
        Assert.Contains("zhi_xue_wan", ex.Message);
    }

    [Fact]
    public void LoadAll_InvalidEnumValue_FailsWithFileNameAndLineNumber()
    {
        var invalidConsumables = ValidConsumablesYaml.Replace("rarity: common", "rarity: mythic");

        var ex = Assert.Throws<DataLoadException>(() =>
            _loader.LoadAll(invalidConsumables, ValidEquipmentYaml, ValidKeyItemsYaml, ValidRecipesYaml, ValidAffixPoolsYaml, new DataRegistry()));

        Assert.Contains(ItemConfigLoader.ConsumablesPath, ex.Message);
        Assert.NotNull(ex.LineNumber);
    }

    [Theory]
    [InlineData("max_stack: 20", "max_stack: 0", "max_stack")]
    [InlineData("base_price: 30", "base_price: -1", "base_price")]
    [InlineData("value: 45", "value: 0", "effects.value")]
    public void LoadAll_InvalidConsumableFields_FailFastWithFieldName(
        string oldValue,
        string newValue,
        string expectedKeyword)
    {
        var invalidConsumables = ValidConsumablesYaml.Replace(oldValue, newValue);

        var ex = Assert.Throws<DataLoadException>(() =>
            _loader.LoadAll(invalidConsumables, ValidEquipmentYaml, ValidKeyItemsYaml, ValidRecipesYaml, ValidAffixPoolsYaml, new DataRegistry()));

        Assert.Contains(ItemConfigLoader.ConsumablesPath, ex.Message);
        Assert.Contains(expectedKeyword, ex.Message);
    }

    [Fact]
    public void LoadAll_EquipmentReferencesMissingAffixPool_FailsWithTemplateAndPoolId()
    {
        var invalidEquipment = ValidEquipmentYaml.Replace("fifth_weapon_pool", "missing_pool");

        var ex = Assert.Throws<DataLoadException>(() =>
            _loader.LoadAll(ValidConsumablesYaml, invalidEquipment, ValidKeyItemsYaml, ValidRecipesYaml, ValidAffixPoolsYaml, new DataRegistry()));

        Assert.Contains(ItemConfigLoader.EquipmentTemplatesPath, ex.Message);
        Assert.Contains("plain_iron_sword", ex.Message);
        Assert.Contains("missing_pool", ex.Message);
    }

    [Fact]
    public void LoadAll_RecipeReferencesMissingIngredient_FailsWithRecipeAndItemId()
    {
        var invalidRecipes = ValidRecipesYaml.Replace("herb_green", "missing_herb");

        var ex = Assert.Throws<DataLoadException>(() =>
            _loader.LoadAll(ValidConsumablesYaml, ValidEquipmentYaml, ValidKeyItemsYaml, invalidRecipes, ValidAffixPoolsYaml, new DataRegistry()));

        Assert.Contains(ItemConfigLoader.RecipesPath, ex.Message);
        Assert.Contains("brew_zhi_xue_wan", ex.Message);
        Assert.Contains("missing_herb", ex.Message);
    }

    [Fact]
    public void LoadAll_LegendaryEquipmentWithoutFixedAffixes_FailsFast()
    {
        var invalidEquipment = """
        - id: nameless_blade
          name: 无名刃
          category: equipment
          grade: legendary
          slot: main_hand
          base_attr:
            attack: 99
        """;

        var ex = Assert.Throws<DataLoadException>(() =>
            _loader.LoadAll(ValidConsumablesYaml, invalidEquipment, ValidKeyItemsYaml, ValidRecipesYaml, ValidAffixPoolsYaml, new DataRegistry()));

        Assert.Contains(ItemConfigLoader.EquipmentTemplatesPath, ex.Message);
        Assert.Contains("fixed_affixes", ex.Message);
    }

    private const string ValidConsumablesYaml = """
    - id: zhi_xue_wan
      name: 止血丸
      category: consumable
      rarity: common
      max_stack: 20
      combat_usable: true
      exploration_usable: false
      base_price: 30
      effects:
        - type: heal_hp
          target: self
          value: 45
      tags: [medicine, combat]

    - id: herb_green
      name: 青蘅草
      category: material
      rarity: uncommon
      max_stack: 99
      combat_usable: false
      exploration_usable: false
      base_price: 5
      effects: []
      tags: [herb, material]
    """;

    private const string ValidEquipmentYaml = """
    - id: plain_iron_sword
      name: 素铁剑
      category: equipment
      grade: fifth
      slot: main_hand
      base_attr:
        attack: 12
      affix_pool: fifth_weapon_pool
      base_price: 120
      description_key: item.equipment.plain_iron_sword.desc

    - id: old_cloth_boots
      name: 旧布靴
      category: equipment
      grade: ninth
      slot: footwear
      base_attr:
        agility: 2
      affix_pool: fifth_weapon_pool
      base_price: 20
      description_key: item.equipment.old_cloth_boots.desc
    """;

    private const string ValidKeyItemsYaml = """
    - id: letter_wind_stop
      name: 风止旧信
      category: key_item
      rarity: rare
      auctionable: false
      description_key: item.key.letter_wind_stop.desc
    """;

    private const string ValidRecipesYaml = """
    - id: brew_zhi_xue_wan
      kind: alchemy
      ingredients:
        - item_id: herb_green
          qty: 2
      output_item_id: zhi_xue_wan
      output_qty: 1
      silver_cost: 3
    """;

    private const string ValidAffixPoolsYaml = """
    - id: fifth_weapon_pool
      grade: fifth
      affixes:
        - id: sharp_edge
          stat: attack
          max_value: 6
        - id: steady_hand
          stat: insight
          max_value: 4
    """;
}
