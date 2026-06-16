using FengZhi.Foundation.Data;
using FengZhi.Foundation.Items;
using Xunit;

namespace FengZhi.Tests.unit.items;

public sealed class InventoryStackingTest
{
    [Fact]
    public void GrantItem_BattleRewardAddsStackableItemToInventory()
    {
        var inventory = CreateInventory();

        var result = inventory.GrantItem("healing_pill", 3, ItemGrantSource.BattleReward);

        Assert.True(result.Success);
        Assert.Equal(3, result.QuantityChanged);
        Assert.Equal(3, inventory.GetTotalQuantity("healing_pill"));
        Assert.Equal(new[] { 2, 1 }, inventory.GetStacks("healing_pill").Select(stack => stack.Quantity).ToArray());
    }

    [Fact]
    public void GrantItem_FullStackOverflowsIntoNewStacksWithoutCapacityLimit()
    {
        var inventory = CreateInventory();

        var result = inventory.GrantItem("healing_pill", 5, ItemGrantSource.ExplorationPickup);

        Assert.True(result.Success);
        var stacks = inventory.GetStacks("healing_pill");
        Assert.Equal(3, stacks.Count);
        Assert.Equal(new[] { 2, 2, 1 }, stacks.Select(stack => stack.Quantity).ToArray());
        Assert.Equal(5, inventory.GetTotalQuantity("healing_pill"));
    }

    [Fact]
    public void GrantItem_KeyItemUsesSeparateKeyItemList()
    {
        var inventory = CreateInventory();

        var result = inventory.GrantItem("letter_wind_stop", 1, ItemGrantSource.NarrativeReward);

        Assert.True(result.Success);
        Assert.Empty(inventory.Stacks);
        Assert.Empty(inventory.Equipment);
        Assert.Single(inventory.KeyItems);
        Assert.Equal("letter_wind_stop", inventory.KeyItems[0].ItemId);
    }

    [Fact]
    public void KeyItems_CannotBeDiscardedOrSoldButAuctionableKeyCanEnterAuction()
    {
        var inventory = CreateInventory();
        inventory.GrantItem("letter_wind_stop", 1, ItemGrantSource.NarrativeReward);
        inventory.GrantItem("auction_token", 1, ItemGrantSource.NarrativeReward);

        var discard = inventory.DiscardItem("letter_wind_stop", 1);
        var sell = inventory.SellItem("letter_wind_stop", 1);

        Assert.False(discard.Success);
        Assert.Equal("key_item_cannot_be_discarded", discard.ErrorCode);
        Assert.False(sell.Success);
        Assert.Equal("key_item_cannot_be_sold", sell.ErrorCode);
        Assert.Equal(1, inventory.GetTotalQuantity("letter_wind_stop"));
        Assert.False(inventory.CanEnterAuction("letter_wind_stop"));
        Assert.True(inventory.CanEnterAuction("auction_token"));
    }

    [Fact]
    public void GetSortedEntries_OrdersByCategoryAndNewestFirstWithinCategory()
    {
        var inventory = CreateInventory();
        inventory.GrantItem("dry_food", 1, ItemGrantSource.ExplorationPickup);
        inventory.GrantItem("old_cloth_boots", 1, ItemGrantSource.ExplorationPickup);
        inventory.GrantItem("healing_pill", 1, ItemGrantSource.BattleReward);
        inventory.GrantItem("letter_wind_stop", 1, ItemGrantSource.NarrativeReward);
        inventory.GrantItem("plain_iron_sword", 1, ItemGrantSource.ExplorationPickup);
        inventory.GrantItem("strong_pill", 1, ItemGrantSource.BattleReward);

        var entries = inventory.GetSortedEntries();

        Assert.Equal(new[]
        {
            InventoryEntryKind.KeyItem,
            InventoryEntryKind.Equipment,
            InventoryEntryKind.Equipment,
            InventoryEntryKind.CombatConsumable,
            InventoryEntryKind.CombatConsumable,
            InventoryEntryKind.ExplorationConsumable
        }, entries.Select(entry => entry.Kind).ToArray());
        Assert.Equal("plain_iron_sword", entries[1].ItemId);
        Assert.Equal("old_cloth_boots", entries[2].ItemId);
        Assert.Equal("strong_pill", entries[3].ItemId);
        Assert.Equal("healing_pill", entries[4].ItemId);
    }

    [Theory]
    [InlineData("", 1, "item_id_required")]
    [InlineData("healing_pill", 0, "quantity_must_be_positive")]
    [InlineData("missing_item", 1, "unknown_item_id")]
    public void GrantItem_InvalidInputsReturnStructuredFailure(string itemId, int quantity, string expectedError)
    {
        var inventory = CreateInventory();

        var result = inventory.GrantItem(itemId, quantity, ItemGrantSource.BattleReward);

        Assert.False(result.Success);
        Assert.Equal(expectedError, result.ErrorCode);
    }

    [Fact]
    public void DiscardItem_NotEnoughQuantityDoesNotPartiallyConsumeStacks()
    {
        var inventory = CreateInventory();
        inventory.GrantItem("healing_pill", 3, ItemGrantSource.BattleReward);

        var result = inventory.DiscardItem("healing_pill", 4);

        Assert.False(result.Success);
        Assert.Equal("not_enough_quantity", result.ErrorCode);
        Assert.Equal(3, inventory.GetTotalQuantity("healing_pill"));
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

    - id: strong_pill
      name: 壮气丸
      category: consumable
      rarity: uncommon
      max_stack: 5
      combat_usable: true
      exploration_usable: false
      base_price: 20
      effects:
        - type: restore_qi
          target: self
          value: 10

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

    private const string KeyItemsYaml = """
    - id: letter_wind_stop
      name: 风止旧信
      category: key_item
      rarity: rare
      auctionable: false
      description_key: item.key.letter_wind_stop.desc

    - id: auction_token
      name: 拍卖信物
      category: key_item
      rarity: epic
      auctionable: true
      description_key: item.key.auction_token.desc
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
