using FengZhi.Foundation.Data;
using FengZhi.Foundation.Items;
using Xunit;

namespace FengZhi.Tests.integration.items;

public sealed class CombatConsumablesTest
{
    [Fact]
    public void GetCombatUsableItems_ReturnsOnlyCombatUsableItemsWithPositiveQuantity()
    {
        var inventory = CreateInventory();
        inventory.GrantItem("healing_pill", 2, ItemGrantSource.BattleReward);
        inventory.GrantItem("dry_food", 1, ItemGrantSource.ExplorationPickup);
        inventory.GrantItem("letter_wind_stop", 1, ItemGrantSource.NarrativeReward);

        var items = inventory.GetCombatUsableItems();

        Assert.Single(items);
        Assert.Equal("healing_pill", items[0].ItemId);
        Assert.Equal(2, items[0].Quantity);
        Assert.Single(items[0].Effects);
        Assert.Equal("heal_hp", items[0].Effects[0].Type);
    }

    [Fact]
    public void CanUseCombatItem_ReturnsFalseWhenNoCombatUsableItemsRemain()
    {
        var inventory = CreateInventory();

        Assert.False(inventory.HasCombatUsableItem());
        Assert.False(inventory.CanUseCombatItem("healing_pill"));

        inventory.GrantItem("dry_food", 1, ItemGrantSource.ExplorationPickup);
        Assert.False(inventory.HasCombatUsableItem());
        Assert.False(inventory.CanUseCombatItem("dry_food"));
    }

    [Fact]
    public void UseCombatItem_ConsumesOneStackAndReturnsStructuredEffects()
    {
        var inventory = CreateInventory();
        inventory.GrantItem("healing_pill", 2, ItemGrantSource.BattleReward);
        var context = new CombatItemActionContext();

        var result = inventory.UseCombatItem("healing_pill", context);

        Assert.True(result.Success);
        Assert.True(result.ActionConsumed);
        Assert.True(context.ActionConsumed);
        Assert.Equal("use_item", context.ConsumedBy);
        Assert.Equal(1, inventory.GetTotalQuantity("healing_pill"));
        Assert.Single(result.Effects);
        Assert.Equal("heal_hp", result.Effects[0].Type);
        Assert.Equal(20, result.Effects[0].Value);
    }

    [Fact]
    public void UseCombatItem_FailsWhenActionAlreadyConsumedAndDoesNotSpendItem()
    {
        var inventory = CreateInventory();
        inventory.GrantItem("healing_pill", 1, ItemGrantSource.BattleReward);
        var context = new CombatItemActionContext();
        Assert.True(context.TryConsumeAction("move"));

        var result = inventory.UseCombatItem("healing_pill", context);

        Assert.False(result.Success);
        Assert.Equal("combat_action_already_consumed", result.ErrorCode);
        Assert.False(result.ActionConsumed);
        Assert.Equal(1, inventory.GetTotalQuantity("healing_pill"));
        Assert.Equal("move", context.ConsumedBy);
    }

    [Fact]
    public void UseCombatItem_LastConsumableImmediatelyDisablesBattleBag()
    {
        var inventory = CreateInventory();
        inventory.GrantItem("healing_pill", 1, ItemGrantSource.BattleReward);

        var result = inventory.UseCombatItem("healing_pill", new CombatItemActionContext());

        Assert.True(result.Success);
        Assert.Equal(0, inventory.GetTotalQuantity("healing_pill"));
        Assert.Empty(inventory.GetCombatUsableItems());
        Assert.False(inventory.HasCombatUsableItem());
        Assert.False(inventory.CanUseCombatItem("healing_pill"));
    }

    [Theory]
    [InlineData("missing_item", "unknown_item_id")]
    [InlineData("dry_food", "item_not_combat_usable")]
    public void UseCombatItem_InvalidItemReturnsStructuredFailure(string itemId, string expectedError)
    {
        var inventory = CreateInventory();
        inventory.GrantItem("dry_food", 1, ItemGrantSource.ExplorationPickup);

        var result = inventory.UseCombatItem(itemId, new CombatItemActionContext());

        Assert.False(result.Success);
        Assert.Equal(expectedError, result.ErrorCode);
        Assert.False(result.ActionConsumed);
    }

    private static InventoryService CreateInventory()
    {
        var registry = new DataRegistry();
        new ItemConfigLoader().LoadAll(
            ConsumablesYaml,
            EquipmentYaml,
            KeyItemsYaml,
            RecipesYaml,
            AffixPoolsYaml,
            registry);
        return new InventoryService(registry);
    }

    private const string ConsumablesYaml = """
    - id: healing_pill
      name: 伤药
      category: consumable
      rarity: common
      max_stack: 2
      combat_usable: true
      exploration_usable: false
      base_price: 10
      effects:
        - type: heal_hp
          target: self
          value: 20

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
        - item_id: dry_food
          qty: 1
      output_item_id: healing_pill
      output_qty: 1
      silver_cost: 1
    """;

    private const string AffixPoolsYaml = """
    - id: fifth_weapon_pool
      grade: fifth
      affixes:
        - id: sharp_edge
          stat: attack
          max_value: 6
    """;
}
