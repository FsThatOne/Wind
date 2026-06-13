using FengZhi.Foundation.Data;
using FengZhi.Foundation.Items;
using Xunit;

namespace FengZhi.Tests.unit.items;

public sealed class EquipmentGenerationTest
{
    [Fact]
    public void GenerateInstance_FifthGradeWeaponScalesBaseAttackByMultiplier()
    {
        var service = CreateEquipmentService();

        var instance = service.GenerateInstance("fifth_sword", "eq_test_1");

        Assert.Equal(EquipmentGrade.Fifth, instance.Grade);
        Assert.Equal(22, instance.BaseAttributes["attack"]);
        Assert.Equal("item.equipment.fifth_sword.desc", instance.DescriptionKey);
    }

    [Fact]
    public void GenerateInstance_SixthGradeEquipmentRollsTwoAffixesWithinFortyToHundredPercent()
    {
        var service = CreateEquipmentService(new SequenceItemRandomSource(new[] { 0, 0 }, new[] { 0.0, 0.999 }));

        var instance = service.GenerateInstance("sixth_armor", "eq_test_2");

        Assert.Equal(2, instance.Affixes.Count);
        Assert.All(instance.Affixes, affix =>
        {
            var maxValue = affix.Id == "guard" ? 10 : 5;
            Assert.InRange(affix.Value, (int)Math.Ceiling(maxValue * 0.4m), maxValue);
            Assert.False(affix.IsFixed);
        });
        Assert.Contains(instance.Affixes, affix => affix.Id == "guard" && affix.Value == 4);
        Assert.Contains(instance.Affixes, affix => affix.Id == "vitality" && affix.Value == 5);
    }

    [Fact]
    public void GenerateInstance_LegendaryEquipmentUsesFixedAttributesAndAffixes()
    {
        var service = CreateEquipmentService();

        var instance = service.GenerateInstance("nameless_blade", "eq_legend");

        Assert.Equal(EquipmentGrade.Legendary, instance.Grade);
        Assert.Equal(70, instance.BaseAttributes["attack"]);
        Assert.Equal(new[] { "silent_edge", "old_oath" }, instance.Affixes.Select(affix => affix.Id).ToArray());
        Assert.All(instance.Affixes, affix => Assert.True(affix.IsFixed));
    }

    [Fact]
    public void GetFiveSlotLayout_ReturnsSameSlotsForProtagonistAndCompanions()
    {
        var service = CreateEquipmentService();

        var slots = service.GetFiveSlotLayout();

        Assert.Equal(new[]
        {
            EquipmentSlotId.MainHand,
            EquipmentSlotId.Armor,
            EquipmentSlotId.Footwear,
            EquipmentSlotId.Accessory1,
            EquipmentSlotId.Accessory2
        }, slots);
    }

    [Fact]
    public void Equip_PreventsSameInstanceFromBeingEquippedByMultipleOwners()
    {
        var service = CreateEquipmentService();
        var instance = service.GenerateInstance("fifth_sword", "eq_unique");

        var first = service.Equip("player", EquipmentSlotId.MainHand, instance);
        var second = service.Equip("companion_a", EquipmentSlotId.MainHand, instance);

        Assert.True(first.Success);
        Assert.False(second.Success);
        Assert.Equal("equipment_instance_already_equipped", second.ErrorCode);
        Assert.Equal("player", service.GetEquippedOwner("eq_unique")!.OwnerId);
    }

    [Fact]
    public void Equip_PreventsSameAccessoryInstanceFromOccupyingTwoSlots()
    {
        var service = CreateEquipmentService();
        var accessory = service.GenerateInstance("jade_ring", "eq_ring");

        var first = service.Equip("player", EquipmentSlotId.Accessory1, accessory);
        var second = service.Equip("player", EquipmentSlotId.Accessory2, accessory);

        Assert.True(first.Success);
        Assert.False(second.Success);
        Assert.Equal("equipment_instance_already_equipped", second.ErrorCode);
    }

    [Fact]
    public void Equip_RejectsIncompatibleSlotAndOccupiedSlot()
    {
        var service = CreateEquipmentService();
        var sword = service.GenerateInstance("fifth_sword", "eq_sword");
        var secondSword = service.GenerateInstance("fifth_sword", "eq_sword_2");

        var incompatible = service.Equip("player", EquipmentSlotId.Accessory1, sword);
        var equipped = service.Equip("player", EquipmentSlotId.MainHand, sword);
        var occupied = service.Equip("player", EquipmentSlotId.MainHand, secondSword);

        Assert.False(incompatible.Success);
        Assert.Equal("slot_incompatible", incompatible.ErrorCode);
        Assert.True(equipped.Success);
        Assert.False(occupied.Success);
        Assert.Equal("slot_occupied", occupied.ErrorCode);
    }

    [Fact]
    public void InventoryGrantEquipment_CreatesUniqueGeneratedInstances()
    {
        var registry = CreateRegistry();
        var inventory = new InventoryService(registry, CreateEquipmentService(registry: registry));

        var result = inventory.GrantItem("fifth_sword", 2, ItemGrantSource.ExplorationPickup);

        Assert.True(result.Success);
        Assert.Equal(2, inventory.Equipment.Count);
        Assert.NotEqual(inventory.Equipment[0].InstanceId, inventory.Equipment[1].InstanceId);
        Assert.Equal(22, inventory.Equipment[0].Instance.BaseAttributes["attack"]);
    }

    [Fact]
    public void GetDisplayInfo_HidesGradeColorAndRollPercentages()
    {
        var service = CreateEquipmentService();
        var instance = service.GenerateInstance("sixth_armor", "eq_display");

        var display = service.GetDisplayInfo(instance);

        Assert.Equal("item.equipment.sixth_armor.desc", display.DescriptionKey);
        Assert.Contains("equipment", display.Tags);
        Assert.DoesNotContain(display.Tags, tag => tag.Contains("percent", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(display.Tags, tag => tag.Contains("color", StringComparison.OrdinalIgnoreCase));
    }

    private static EquipmentService CreateEquipmentService(
        IItemRandomSource? randomSource = null,
        DataRegistry? registry = null)
    {
        return new EquipmentService(registry ?? CreateRegistry(), randomSource);
    }

    private static DataRegistry CreateRegistry()
    {
        var registry = new DataRegistry();
        new ItemConfigLoader().LoadAll(
            ConsumablesYaml,
            EquipmentYaml,
            KeyItemsYaml,
            RecipesYaml,
            AffixPoolsYaml,
            registry);
        return registry;
    }

    private sealed class SequenceItemRandomSource : IItemRandomSource
    {
        private readonly Queue<int> _ints;
        private readonly Queue<double> _doubles;

        public SequenceItemRandomSource(IEnumerable<int> ints, IEnumerable<double> doubles)
        {
            _ints = new Queue<int>(ints);
            _doubles = new Queue<double>(doubles);
        }

        public int NextInt(int maxExclusive)
        {
            var value = _ints.Count == 0 ? 0 : _ints.Dequeue();
            return Math.Clamp(value, 0, maxExclusive - 1);
        }

        public double NextDouble()
        {
            return _doubles.Count == 0 ? 0 : _doubles.Dequeue();
        }
    }

    private const string ConsumablesYaml = """
    - id: dry_food
      name: 干粮
      category: consumable
      rarity: common
      max_stack: 99
      combat_usable: false
      exploration_usable: true
      base_price: 5
      effects:
        - type: restore_stamina
          target: self
          value: 1
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

    - id: sixth_armor
      name: 六品甲
      category: equipment
      grade: sixth
      slot: armor
      base_attr:
        defense: 10
      affix_pool: sixth_armor_pool
      base_price: 90
      description_key: item.equipment.sixth_armor.desc

    - id: jade_ring
      name: 青玉戒
      category: equipment
      grade: first
      slot: accessory
      base_attr:
        insight: 3
      affix_pool: accessory_pool
      base_price: 500
      description_key: item.equipment.jade_ring.desc

    - id: nameless_blade
      name: 无名刃
      category: equipment
      grade: legendary
      slot: main_hand
      base_attr:
        attack: 10
      fixed_affixes: [silent_edge, old_oath]
      base_price: 0
      description_key: item.equipment.nameless_blade.desc
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
    - id: forge_fifth_sword
      kind: forge
      ingredients:
        - item_id: dry_food
          qty: 1
      output_item_id: fifth_sword
      output_qty: 1
      silver_cost: 10
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

    - id: sixth_armor_pool
      grade: sixth
      affixes:
        - id: guard
          stat: defense
          max_value: 10
        - id: vitality
          stat: constitution
          max_value: 5
        - id: calm
          stat: insight
          max_value: 4

    - id: accessory_pool
      grade: first
      affixes:
        - id: spirit
          stat: insight
          max_value: 10
        - id: flow
          stat: agility
          max_value: 10
        - id: inner_breath
          stat: inner_force
          max_value: 10
        - id: resolve
          stat: constitution
          max_value: 10
    """;
}
