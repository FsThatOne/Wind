using FengZhi.Foundation.Data;
using FengZhi.Foundation.Items;
using Xunit;

namespace FengZhi.Tests.unit.items;

public sealed class EconomyShopAuctionTest
{
    [Fact]
    public void Silver_UsesNonNegativeIntegerAndInsufficientShopPurchaseDoesNotChangeState()
    {
        var (inventory, economy) = CreateServices(initialSilver: -10);

        var result = economy.BuyFromShop("healing_pill", 1);

        Assert.Equal(0, economy.Silver);
        Assert.False(result.Success);
        Assert.Equal("not_enough_silver", result.ErrorCode);
        Assert.Equal(0, inventory.GetTotalQuantity("healing_pill"));
    }

    [Fact]
    public void ShopPrices_UseFixedBuyPriceAndDefaultSellRatioWithFloor()
    {
        var (_, economy) = CreateServices(initialSilver: 0);

        var buyPrice = economy.GetShopBuyPrice("fifth_sword");
        var sellPrice = economy.GetShopSellPrice("fifth_sword");
        var lowSellPrice = economy.GetShopSellPrice("mountain_herb");

        Assert.Equal(100, buyPrice);
        Assert.Equal(30, sellPrice);
        Assert.Equal(0, lowSellPrice);
    }

    [Fact]
    public void SellToShop_ConsumesStackableItemsAndAddsSilverBySellRatio()
    {
        var (inventory, economy) = CreateServices(initialSilver: 5);
        inventory.GrantItem("healing_pill", 2, ItemGrantSource.ExplorationPickup);

        var result = economy.SellToShop("healing_pill", 2);

        Assert.True(result.Success);
        Assert.Equal(-2, result.QuantityChanged);
        Assert.Equal(6, result.SilverChanged);
        Assert.Equal(11, economy.Silver);
        Assert.Equal(0, inventory.GetTotalQuantity("healing_pill"));
    }

    [Fact]
    public void KeyItems_CannotBeSoldAndOnlyAuctionableKeyItemsCanEnterAuctionFlow()
    {
        var (inventory, economy) = CreateServices(initialSilver: 0);
        inventory.GrantItem("letter_wind_stop", 1, ItemGrantSource.NarrativeReward);
        inventory.GrantItem("auction_token", 1, ItemGrantSource.NarrativeReward);

        var sell = economy.SellToShop("letter_wind_stop", 1);

        Assert.False(sell.Success);
        Assert.Equal("key_item_cannot_be_sold", sell.ErrorCode);
        Assert.Equal(1, inventory.GetTotalQuantity("letter_wind_stop"));
        Assert.False(economy.CanEnterAuction("letter_wind_stop"));
        Assert.True(economy.CanEnterAuction("auction_token"));
    }

    [Fact]
    public void BlackMarketBarter_VerifiesSpecifiedItemCombinationAndGrantsOutput()
    {
        var (inventory, economy) = CreateServices(initialSilver: 0);
        inventory.GrantItem("mountain_herb", 3, ItemGrantSource.ExplorationPickup);
        var offer = new BlackMarketBarterOffer(
            "barter_healing",
            new[] { new BarterRequirement("mountain_herb", 2) },
            "healing_pill",
            1);

        var result = economy.ExecuteBlackMarketBarter(offer);

        Assert.True(result.Success);
        Assert.Equal("healing_pill", result.ItemId);
        Assert.Equal(1, inventory.GetTotalQuantity("mountain_herb"));
        Assert.Equal(1, inventory.GetTotalQuantity("healing_pill"));
        Assert.Equal(0, economy.Silver);
    }

    [Fact]
    public void BlackMarketBarter_WhenRequirementMissingFailsAtomically()
    {
        var (inventory, economy) = CreateServices(initialSilver: 0);
        inventory.GrantItem("mountain_herb", 1, ItemGrantSource.ExplorationPickup);
        var offer = new BlackMarketBarterOffer(
            "barter_healing",
            new[] { new BarterRequirement("mountain_herb", 2) },
            "healing_pill",
            1);

        var result = economy.ExecuteBlackMarketBarter(offer);

        Assert.False(result.Success);
        Assert.Equal("not_enough_barter_items", result.ErrorCode);
        Assert.Equal(1, inventory.GetTotalQuantity("mountain_herb"));
        Assert.Equal(0, inventory.GetTotalQuantity("healing_pill"));
    }

    [Fact]
    public void BlackMarketBarter_WhenOutputItemUnknownFailsAtomicallyWithoutDiscardingPayment()
    {
        var (inventory, economy) = CreateServices(initialSilver: 0);
        inventory.GrantItem("mountain_herb", 3, ItemGrantSource.ExplorationPickup);
        var offer = new BlackMarketBarterOffer(
            "barter_unknown_output",
            new[] { new BarterRequirement("mountain_herb", 2) },
            "no_such_item",
            1);

        var result = economy.ExecuteBlackMarketBarter(offer);

        Assert.False(result.Success);
        Assert.Equal("unknown_item_id", result.ErrorCode);
        // 原子性：支付物未被扣除，产出物未被授予
        Assert.Equal(3, inventory.GetTotalQuantity("mountain_herb"));
        Assert.Equal(0, inventory.GetTotalQuantity("no_such_item"));
    }

    [Theory]
    [InlineData(40, "bid_must_exceed_current_highest")]
    [InlineData(50, "bid_must_exceed_current_highest")]
    public void SubmitAuctionBid_RejectsBidNotAboveCurrentHighestWithoutChangingState(int bid, string expectedError)
    {
        var (inventory, economy) = CreateServices(initialSilver: 200, new NeverFollowAuctionStrategy());
        economy.OpenAuctionLot("lot_1", "healing_pill", 1, 50);

        var result = economy.SubmitAuctionBid("lot_1", bid);

        Assert.False(result.Success);
        Assert.Equal(expectedError, result.ErrorCode);
        Assert.Equal(200, economy.Silver);
        Assert.Equal(0, inventory.GetTotalQuantity("healing_pill"));
        Assert.Equal(50, economy.GetAuctionLot("lot_1")!.CurrentHighestBid);
        Assert.False(economy.GetAuctionLot("lot_1")!.IsClosed);
    }

    [Fact]
    public void SubmitAuctionBid_WhenSilverEnoughAndNpcDoesNotFollowAwardsLotAndDeductsSilver()
    {
        var (inventory, economy) = CreateServices(initialSilver: 200, new NeverFollowAuctionStrategy());
        economy.OpenAuctionLot("lot_1", "healing_pill", 2, 50);

        var result = economy.SubmitAuctionBid("lot_1", 80);

        Assert.True(result.Success);
        Assert.Equal(-80, result.SilverChanged);
        Assert.Equal(2, result.QuantityChanged);
        Assert.Equal(120, economy.Silver);
        Assert.Equal(2, inventory.GetTotalQuantity("healing_pill"));
        Assert.True(result.AuctionLot!.IsClosed);
        Assert.Equal("player", result.AuctionLot.HighestBidderId);
        Assert.Equal(80, result.AuctionLot.CurrentHighestBid);
    }

    [Fact]
    public void SubmitAuctionBid_WhenSilverInsufficientFailsWithoutChangingAuctionOrInventory()
    {
        var (inventory, economy) = CreateServices(initialSilver: 70, new NeverFollowAuctionStrategy());
        economy.OpenAuctionLot("lot_1", "healing_pill", 1, 50);

        var result = economy.SubmitAuctionBid("lot_1", 80);

        Assert.False(result.Success);
        Assert.Equal("not_enough_silver", result.ErrorCode);
        Assert.Equal(70, economy.Silver);
        Assert.Equal(0, inventory.GetTotalQuantity("healing_pill"));
        var lot = economy.GetAuctionLot("lot_1")!;
        Assert.Equal(50, lot.CurrentHighestBid);
        Assert.Null(lot.HighestBidderId);
        Assert.False(lot.IsClosed);
    }

    [Fact]
    public void SubmitAuctionBid_WhenNpcFollowsUpdatesHighestBidWithoutDeductingPlayerSilver()
    {
        var (inventory, economy) = CreateServices(initialSilver: 200, new AlwaysFollowAuctionStrategy());
        economy.OpenAuctionLot("lot_1", "healing_pill", 1, 50);

        var result = economy.SubmitAuctionBid("lot_1", 80);

        Assert.True(result.Success);
        Assert.Equal(0, result.SilverChanged);
        Assert.Equal(200, economy.Silver);
        Assert.Equal(0, inventory.GetTotalQuantity("healing_pill"));
        Assert.False(result.AuctionLot!.IsClosed);
        Assert.Equal("npc", result.AuctionLot.HighestBidderId);
        Assert.Equal(90, result.AuctionLot.CurrentHighestBid);
    }

    private static (InventoryService Inventory, EconomyService Economy) CreateServices(
        int initialSilver,
        IAuctionNpcBidStrategy? auctionNpcBidStrategy = null)
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
        var economy = new EconomyService(registry, inventory, initialSilver, auctionNpcBidStrategy);
        return (inventory, economy);
    }

    private sealed class NeverFollowAuctionStrategy : IAuctionNpcBidStrategy
    {
        public AuctionNpcBidDecision Decide(AuctionLot lot, int playerBid)
        {
            _ = lot;
            _ = playerBid;
            return new AuctionNpcBidDecision(false, 0);
        }
    }

    private sealed class AlwaysFollowAuctionStrategy : IAuctionNpcBidStrategy
    {
        public AuctionNpcBidDecision Decide(AuctionLot lot, int playerBid)
        {
            _ = lot;
            return new AuctionNpcBidDecision(true, playerBid + 10);
        }
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
        - item_id: mountain_herb
          qty: 2
      output_item_id: healing_pill
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
    """;
}
