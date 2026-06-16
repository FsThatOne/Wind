using FengZhi.Foundation.Data;
using FengZhi.Foundation.Items;
using Xunit;

namespace FengZhi.Tests.unit.items;

public sealed class CraftingAlchemyRefineTest
{
    [Fact]
    public void CraftAlchemy_ConsumesHerbsAndCreatesPillWithQualityScaledEffect()
    {
        var (inventory, crafting) = CreateServices(initialSilver: 30);
        inventory.GrantItem("mountain_herb", 2, ItemGrantSource.ExplorationPickup);

        var result = crafting.CraftAlchemy("brew_healing_pill", AlchemyQuality.Extreme);

        Assert.True(result.Success);
        Assert.Equal("healing_pill", result.ItemId);
        Assert.Equal(1, inventory.GetTotalQuantity("healing_pill"));
        Assert.Equal(0, inventory.GetTotalQuantity("mountain_herb"));
        Assert.Equal(20, crafting.Silver);
        Assert.Single(result.Effects);
        Assert.Equal(120, result.Effects[0].Value);
    }

    [Theory]
    [InlineData(AlchemyQuality.Extreme, 120)]
    [InlineData(AlchemyQuality.High, 100)]
    [InlineData(AlchemyQuality.Middle, 80)]
    [InlineData(AlchemyQuality.Low, 60)]
    public void ScaleAlchemyEffect_UsesConfiguredQualityCoefficients(AlchemyQuality quality, int expected)
    {
        var value = CraftingService.ScaleAlchemyEffect(100, quality);

        Assert.Equal(expected, value);
    }

    [Fact]
    public void CraftAlchemy_UnknownQualityFailsBeforeSpending()
    {
        var (inventory, crafting) = CreateServices(initialSilver: 30);
        inventory.GrantItem("mountain_herb", 2, ItemGrantSource.ExplorationPickup);

        var result = crafting.CraftAlchemy("brew_healing_pill", (AlchemyQuality)999);

        Assert.False(result.Success);
        Assert.Equal("unknown_alchemy_quality", result.ErrorCode);
        Assert.Equal(2, inventory.GetTotalQuantity("mountain_herb"));
        Assert.Equal(30, crafting.Silver);
    }

    [Fact]
    public void Forge_ConsumesMaterialsAndSilverThenCreatesFixedGradeEquipment()
    {
        var (inventory, crafting) = CreateServices(initialSilver: 100);
        inventory.GrantItem("iron_ore", 3, ItemGrantSource.ExplorationPickup);

        var result = crafting.Forge("forge_fifth_sword");

        Assert.True(result.Success);
        Assert.Equal("fifth_sword", result.ItemId);
        Assert.Equal(0, inventory.GetTotalQuantity("iron_ore"));
        Assert.Equal(50, crafting.Silver);
        Assert.NotNull(result.Equipment);
        Assert.Equal(EquipmentGrade.Fifth, result.Equipment!.Instance.Grade);
        Assert.Equal(22, result.Equipment.Instance.BaseAttributes["attack"]);
    }

    [Theory]
    [InlineData(2, 100, "not_enough_material")]
    [InlineData(3, 49, "not_enough_silver")]
    public void Forge_WhenMaterialsOrSilverInsufficientFailsAtomically(int oreCount, int silver, string expectedError)
    {
        var (inventory, crafting) = CreateServices(initialSilver: silver);
        inventory.GrantItem("iron_ore", oreCount, ItemGrantSource.ExplorationPickup);

        var result = crafting.Forge("forge_fifth_sword");

        Assert.False(result.Success);
        Assert.Equal(expectedError, result.ErrorCode);
        Assert.Equal(oreCount, inventory.GetTotalQuantity("iron_ore"));
        Assert.Equal(silver, crafting.Silver);
        Assert.Empty(inventory.Equipment);
    }

    [Fact]
    public void RefineEquipment_IncreasesAttributeWithoutExceedingGradeCapTimesPointNineFive()
    {
        var (inventory, crafting) = CreateServices(initialSilver: 100);
        inventory.GrantItem("iron_ore", 3, ItemGrantSource.ExplorationPickup);
        inventory.GrantItem("refine_stone", 1, ItemGrantSource.ExplorationPickup);
        var forged = crafting.Forge("forge_fifth_sword").Equipment!;
        var cap = (int)Math.Floor(EquipmentService.GetGradeAttributeCap(EquipmentGrade.Fifth) * 0.95m);

        var result = crafting.RefineEquipment(
            forged.InstanceId,
            "refine_weapon",
            new RefinePlan(new Dictionary<string, int> { ["attack"] = 999 }));

        Assert.True(result.Success);
        var refined = inventory.GetEquipment(forged.InstanceId)!.Instance;
        Assert.Equal(cap, refined.BaseAttributes["attack"]);
        Assert.Equal(1, refined.RefineCount);
        Assert.Equal(0, inventory.GetTotalQuantity("refine_stone"));
    }

    [Fact]
    public void RefineEquipment_AtCapFailsAtomicallyWithoutSpending()
    {
        var (inventory, crafting) = CreateServices(initialSilver: 200);
        inventory.GrantItem("iron_ore", 3, ItemGrantSource.ExplorationPickup);
        inventory.GrantItem("refine_stone", 2, ItemGrantSource.ExplorationPickup);
        var forged = crafting.Forge("forge_fifth_sword").Equipment!;
        crafting.RefineEquipment(forged.InstanceId, "refine_weapon", new RefinePlan(new Dictionary<string, int> { ["attack"] = 999 }));
        var silverBefore = crafting.Silver;

        var result = crafting.RefineEquipment(
            forged.InstanceId,
            "refine_weapon",
            new RefinePlan(new Dictionary<string, int> { ["attack"] = 1 }));

        Assert.False(result.Success);
        Assert.Equal("refine_cap_reached", result.ErrorCode);
        Assert.Equal(1, inventory.GetTotalQuantity("refine_stone"));
        Assert.Equal(silverBefore, crafting.Silver);
        Assert.Equal(1, inventory.GetEquipment(forged.InstanceId)!.Instance.RefineCount);
    }

    [Fact]
    public void RefineEquipment_DefaultMaxCountIsThreeAndFourthAttemptFailsAtomically()
    {
        var (inventory, crafting) = CreateServices(initialSilver: 200);
        inventory.GrantItem("iron_ore", 3, ItemGrantSource.ExplorationPickup);
        inventory.GrantItem("refine_stone", 4, ItemGrantSource.ExplorationPickup);
        var forged = crafting.Forge("forge_fifth_sword").Equipment!;
        var plan = new RefinePlan(new Dictionary<string, int> { ["attack"] = 1 });

        Assert.True(crafting.RefineEquipment(forged.InstanceId, "refine_weapon", plan).Success);
        Assert.True(crafting.RefineEquipment(forged.InstanceId, "refine_weapon", plan).Success);
        Assert.True(crafting.RefineEquipment(forged.InstanceId, "refine_weapon", plan).Success);
        var silverBefore = crafting.Silver;
        var fourth = crafting.RefineEquipment(forged.InstanceId, "refine_weapon", plan);

        Assert.False(fourth.Success);
        Assert.Equal("refine_max_count_reached", fourth.ErrorCode);
        Assert.Equal(1, inventory.GetTotalQuantity("refine_stone"));
        Assert.Equal(silverBefore, crafting.Silver);
        Assert.Equal(3, inventory.GetEquipment(forged.InstanceId)!.Instance.RefineCount);
    }

    private static (InventoryService Inventory, CraftingService Crafting) CreateServices(int initialSilver)
    {
        var registry = new DataRegistry();
        new ItemConfigLoader().LoadAll(
            ConsumablesYaml,
            EquipmentYaml,
            KeyItemsYaml,
            RecipesYaml,
            AffixPoolsYaml,
            registry);
        var equipmentService = new EquipmentService(registry, new SequenceItemRandomSource());
        var inventory = new InventoryService(registry, equipmentService);
        var crafting = new CraftingService(registry, inventory, initialSilver);
        return (inventory, crafting);
    }

    private sealed class SequenceItemRandomSource : IItemRandomSource
    {
        public int NextInt(int maxExclusive) => 0;

        public double NextDouble() => 0;
    }

    private const string ConsumablesYaml = """
    - id: healing_pill
      name: 伤药
      category: consumable
      rarity: common
      max_stack: 99
      combat_usable: true
      exploration_usable: false
      base_price: 10
      effects:
        - type: heal_hp
          target: self
          value: 100

    - id: mountain_herb
      name: 山草
      category: material
      rarity: common
      max_stack: 99
      combat_usable: false
      exploration_usable: false
      base_price: 1
      effects: []

    - id: iron_ore
      name: 铁矿
      category: material
      rarity: common
      max_stack: 99
      combat_usable: false
      exploration_usable: false
      base_price: 3
      effects: []

    - id: refine_stone
      name: 精炼石
      category: material
      rarity: uncommon
      max_stack: 99
      combat_usable: false
      exploration_usable: false
      base_price: 8
      effects: []
    """;

    private const string EquipmentYaml = """
    - id: fifth_sword
      name: 五品剑
      category: equipment
      grade: fifth
      slot: main_hand
      base_attr:
        attack: 10
      affix_pool: fifth_weapon_pool
      base_price: 100
      description_key: item.equipment.fifth_sword.desc
    """;

    private const string KeyItemsYaml = """
    - id: letter_wind_stop
      name: 风止旧信
      category: key_item
      rarity: rare
      auctionable: false
      description_key: item.key.letter_wind_stop.desc
    """;

    private const string RecipesYaml = """
    - id: brew_healing_pill
      kind: alchemy
      ingredients:
        - item_id: mountain_herb
          qty: 2
      output_item_id: healing_pill
      output_qty: 1
      silver_cost: 10

    - id: forge_fifth_sword
      kind: forge
      ingredients:
        - item_id: iron_ore
          qty: 3
      output_item_id: fifth_sword
      output_qty: 1
      silver_cost: 50

    - id: refine_weapon
      kind: refine
      ingredients:
        - item_id: refine_stone
          qty: 1
      output_item_id: fifth_sword
      output_qty: 1
      silver_cost: 20
    """;

    private const string AffixPoolsYaml = """
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
