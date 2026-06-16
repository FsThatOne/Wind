using FengZhi.Foundation.Data;
using FengZhi.Foundation.Events;
using FengZhi.Foundation.Items;
using FengZhi.Foundation.SaveSystem;
using Xunit;

namespace FengZhi.Tests.integration.items;

public sealed class ItemSaveQueryContractsTest
{
    [Fact]
    public async Task SaveManagerRoundTrip_RestoresInventoryEquipmentOwnershipSilverAndKeyItems()
    {
        var store = new InMemorySavePayloadStore();
        var source = CreateServices(initialSilver: 180);
        source.Inventory.GrantItem("healing_pill", 3, ItemGrantSource.BattleReward);
        source.Inventory.GrantItem("letter_wind_stop", 1, ItemGrantSource.NarrativeReward);
        source.Inventory.GrantItem("fifth_sword", 1, ItemGrantSource.ExplorationPickup);
        var equipment = source.Inventory.Equipment.Single();
        Assert.True(source.Equipment.Equip("player", EquipmentSlotId.MainHand, equipment.Instance).Success);
        var saveManager = CreateSaveManager(store);
        saveManager.RegisterSerializer(source.ItemSystem);
        await saveManager.SaveGameAsync(SaveSlotId.Manual(1));
        var restored = CreateServices(initialSilver: 0);
        var loadManager = CreateSaveManager(store);
        loadManager.RegisterSerializer(restored.ItemSystem);

        var result = await loadManager.LoadGameAsync(SaveSlotId.Manual(1));

        Assert.True(result.Success);
        Assert.Equal(3, restored.Inventory.GetTotalQuantity("healing_pill"));
        Assert.Equal(1, restored.Inventory.GetTotalQuantity("letter_wind_stop"));
        Assert.Single(restored.Inventory.Equipment);
        Assert.Equal(equipment.InstanceId, restored.Inventory.Equipment[0].InstanceId);
        Assert.Equal("player", restored.Equipment.GetEquippedOwner(equipment.InstanceId)!.OwnerId);
        Assert.Equal(EquipmentSlotId.MainHand, restored.Equipment.GetEquippedOwner(equipment.InstanceId)!.Slot);
        Assert.Equal(180, restored.Economy.Silver);
        Assert.Empty(restored.ItemSystem.LastRestoreWarnings);
    }

    [Fact]
    public void RestoreSaveData_WithDuplicateEquipmentOwnershipKeepsOneOwnerAndReportsWarning()
    {
        var source = CreateServices(initialSilver: 100);
        source.Inventory.GrantItem("fifth_sword", 1, ItemGrantSource.ExplorationPickup);
        var equipment = source.Inventory.Equipment.Single();
        var saveData = source.ItemSystem.CreateSaveData() with
        {
            EquipmentOwnership = new ItemEquipmentOwnershipSaveData
            {
                EquippedItems = new List<EquippedItem>
                {
                    new("player", EquipmentSlotId.MainHand, equipment.InstanceId),
                    new("companion_lan", EquipmentSlotId.MainHand, equipment.InstanceId)
                }
            }
        };
        var restored = CreateServices(initialSilver: 0);

        var warnings = restored.ItemSystem.RestoreSaveData(saveData);

        Assert.Contains(warnings, warning => warning.Code == "duplicate_equipped_instance");
        Assert.Equal("player", restored.Equipment.GetEquippedOwner(equipment.InstanceId)!.OwnerId);
    }

    [Fact]
    public void ConsumedTaughtAndSoldItemsAreRemovedAndOperationLogIsSaved()
    {
        var services = CreateServices(initialSilver: 0);
        services.Inventory.GrantItem("fragment_wind_cut", 1, ItemGrantSource.NarrativeReward);
        services.Inventory.GrantItem("mountain_herb", 2, ItemGrantSource.ExplorationPickup);
        var teaching = new MartialFragmentTeachingService(
            services.Registry,
            services.Inventory,
            new EmptyKnowledgeProvider());

        var study = teaching.UseMartialFragment("fragment_wind_cut", "player");
        var sell = services.Economy.SellToShop("mountain_herb", 1);
        var saveData = services.ItemSystem.CreateSaveData();

        Assert.True(study.Success);
        Assert.True(sell.Success);
        Assert.Equal(0, services.Inventory.GetTotalQuantity("fragment_wind_cut"));
        Assert.Equal(1, services.Inventory.GetTotalQuantity("mountain_herb"));
        Assert.Contains(saveData.Inventory.OperationLog, entry => entry.Action == "consume_key_item" && entry.ItemId == "fragment_wind_cut");
        Assert.Contains(saveData.Inventory.OperationLog, entry => entry.Action == "sell" && entry.ItemId == "mountain_herb");
    }

    [Fact]
    public void GrantItem_NarrativeRewardInterfaceAddsInventoryQuantity()
    {
        var services = CreateServices(initialSilver: 0);

        var result = services.ItemSystem.GrantItem(new ItemRewardGrantRequest("healing_pill", 2));

        Assert.True(result.Success);
        Assert.Equal(2, services.Inventory.GetTotalQuantity("healing_pill"));
    }

    [Fact]
    public void GetExplorationStaminaRecoveryEffects_ReturnsNaturalDayConsumableEffects()
    {
        var services = CreateServices(initialSilver: 0);
        services.Inventory.GrantItem("dry_food", 2, ItemGrantSource.ExplorationPickup);

        var effects = services.ItemSystem.GetExplorationStaminaRecoveryEffects();

        var effect = Assert.Single(effects);
        Assert.Equal("dry_food", effect.ItemId);
        Assert.Equal("restore_stamina", effect.Type);
        Assert.Equal(2, effect.Value);
    }

    [Fact]
    public void GetEquipmentDisplayInfo_HidesAffixPercentAndExposesDescriptionKeyAndTags()
    {
        var services = CreateServices(initialSilver: 0);
        services.Inventory.GrantItem("fifth_sword", 1, ItemGrantSource.ExplorationPickup);
        var instanceId = services.Inventory.Equipment.Single().InstanceId;

        var display = services.ItemSystem.GetEquipmentDisplayInfo(instanceId);

        Assert.NotNull(display);
        Assert.Equal("item.equipment.fifth_sword.desc", display!.DescriptionKey);
        Assert.Contains("equipment", display.Tags);
        Assert.DoesNotContain("%", string.Join(",", display.Tags));
    }

    private static SaveManager CreateSaveManager(InMemorySavePayloadStore store)
    {
        return new SaveManager(
            store,
            new EventBus(),
            slot => new SlotMetadata
            {
                SlotId = slot,
                ChapterName = "第一章",
                SceneName = "竹林道",
                GameDay = 12,
                Playtime = TimeSpan.FromMinutes(42)
            });
    }

    private static ItemTestServices CreateServices(int initialSilver)
    {
        var registry = new DataRegistry();
        new ItemConfigLoader().LoadAll(
            ConsumablesYaml,
            EquipmentYaml,
            KeyItemsYaml,
            RecipesYaml,
            AffixPoolsYaml,
            registry);
        var equipment = new EquipmentService(registry, new SequenceItemRandomSource());
        var inventory = new InventoryService(registry, equipment);
        var economy = new EconomyService(registry, inventory, initialSilver, new NeverFollowAuctionStrategy());
        var itemSystem = new ItemSystemService(inventory, equipment, economy);
        return new ItemTestServices(registry, inventory, equipment, economy, itemSystem);
    }

    private sealed record ItemTestServices(
        DataRegistry Registry,
        InventoryService Inventory,
        EquipmentService Equipment,
        EconomyService Economy,
        ItemSystemService ItemSystem);

    private sealed class EmptyKnowledgeProvider : IMartialMoveKnowledgeProvider
    {
        public bool HasLearnedMove(string characterId, string moveId)
        {
            _ = characterId;
            _ = moveId;
            return false;
        }
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

    - id: dry_food
      name: 干粮
      category: consumable
      rarity: common
      max_stack: 99
      combat_usable: false
      exploration_usable: true
      base_price: 3
      effects:
        - type: restore_stamina
          target: self
          value: 2

    - id: mountain_herb
      name: 山草
      category: material
      rarity: common
      max_stack: 99
      combat_usable: false
      exploration_usable: false
      base_price: 2
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

    - id: fragment_wind_cut
      name: 听风斩残卷
      category: key_item
      rarity: rare
      auctionable: false
      description_key: item.key.fragment_wind_cut.desc
      linked_move_id: wind_cut
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
        - id: steady_hand
          stat: insight
          max_value: 4
    """;
}
