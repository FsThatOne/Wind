using FengZhi.Foundation.Data;
using FengZhi.Foundation.Events;
using FengZhi.Foundation.Items;
using Xunit;

namespace FengZhi.Tests.integration.items;

public sealed class MartialFragmentsTeachingTest
{
    private const string ProtagonistId = "player";
    private const string CompanionId = "companion_lan";
    private const string MoveId = "wind_cut";
    private const string FragmentItemId = "fragment_wind_cut";

    [Fact]
    public void MartialFragment_KeyItemCannotBeDiscardedOrSoldButCanSelfStudy()
    {
        var (inventory, service, _) = CreateServices();
        inventory.GrantItem(FragmentItemId, 1, ItemGrantSource.NarrativeReward);

        var discard = inventory.DiscardItem(FragmentItemId, 1);
        var sell = inventory.SellItem(FragmentItemId, 1);
        var result = service.UseMartialFragment(FragmentItemId, ProtagonistId);

        Assert.False(discard.Success);
        Assert.Equal("key_item_cannot_be_discarded", discard.ErrorCode);
        Assert.False(sell.Success);
        Assert.Equal("key_item_cannot_be_sold", sell.ErrorCode);
        Assert.True(result.Success);
        Assert.Equal(-1, result.QuantityChanged);
        Assert.Equal(0, inventory.GetTotalQuantity(FragmentItemId));
        Assert.Equal(MartialFragmentUseMode.SelfStudy, result.Request!.Mode);
        Assert.Equal(ProtagonistId, result.Request.LearnerCharacterId);
        Assert.Null(result.Request.TeacherCharacterId);
    }

    [Fact]
    public void UseMartialFragment_ProtagonistNotLearnedCannotTeachCompanionAndDoesNotConsume()
    {
        var (inventory, service, _) = CreateServices();
        inventory.GrantItem(FragmentItemId, 1, ItemGrantSource.NarrativeReward);

        var result = service.UseMartialFragment(FragmentItemId, ProtagonistId, CompanionId);

        Assert.False(result.Success);
        Assert.Equal("protagonist_must_self_study_first", result.ErrorCode);
        Assert.Null(result.Request);
        Assert.Equal(1, inventory.GetTotalQuantity(FragmentItemId));
    }

    [Fact]
    public void UseMartialFragment_TargetEmptyWhenProtagonistNotLearnedCreatesSelfStudyRequest()
    {
        var (inventory, service, _) = CreateServices();
        inventory.GrantItem(FragmentItemId, 1, ItemGrantSource.NarrativeReward);

        var result = service.UseMartialFragment(FragmentItemId, ProtagonistId, targetCharacterId: "");

        Assert.True(result.Success);
        Assert.Equal(FragmentItemId, result.Request!.ItemId);
        Assert.Equal(MoveId, result.Request.MoveId);
        Assert.Equal(ProtagonistId, result.Request.LearnerCharacterId);
        Assert.Equal(MartialFragmentUseMode.SelfStudy, result.Request.Mode);
        Assert.Equal(0, inventory.GetTotalQuantity(FragmentItemId));
    }

    [Fact]
    public void UseMartialFragment_ProtagonistLearnedTeachesCompanionConsumesAndPublishesRequest()
    {
        var eventBus = new EventBus();
        var (inventory, service, knowledge) = CreateServices(eventBus);
        knowledge.Learn(ProtagonistId, MoveId);
        inventory.GrantItem(FragmentItemId, 2, ItemGrantSource.NarrativeReward);
        MartialFragmentLearnRequestedEvent? published = null;
        eventBus.Subscribe<MartialFragmentLearnRequestedEvent>(evt => published = evt);

        var result = service.UseMartialFragment(FragmentItemId, ProtagonistId, CompanionId);

        Assert.True(result.Success);
        Assert.Equal(-1, result.QuantityChanged);
        Assert.Equal(1, inventory.GetTotalQuantity(FragmentItemId));
        Assert.Equal(MartialFragmentUseMode.TeachCompanion, result.Request!.Mode);
        Assert.Equal(CompanionId, result.Request.LearnerCharacterId);
        Assert.Equal(ProtagonistId, result.Request.TeacherCharacterId);
        Assert.NotNull(published);
        Assert.Equal(result.Request, published!.Request);
    }

    [Fact]
    public void UseMartialFragment_TargetAlreadyLearnedFailsAndDoesNotConsume()
    {
        var (inventory, service, knowledge) = CreateServices();
        knowledge.Learn(ProtagonistId, MoveId);
        knowledge.Learn(CompanionId, MoveId);
        inventory.GrantItem(FragmentItemId, 1, ItemGrantSource.NarrativeReward);

        var result = service.UseMartialFragment(FragmentItemId, ProtagonistId, CompanionId);

        Assert.False(result.Success);
        Assert.Equal("target_already_learned", result.ErrorCode);
        Assert.Null(result.Request);
        Assert.Equal(1, inventory.GetTotalQuantity(FragmentItemId));
    }

    [Fact]
    public void UseMartialFragment_KeyItemWithoutLinkedMoveFailsAndDoesNotConsume()
    {
        var (inventory, service, _) = CreateServices();
        inventory.GrantItem("letter_wind_stop", 1, ItemGrantSource.NarrativeReward);

        var result = service.UseMartialFragment("letter_wind_stop", ProtagonistId);

        Assert.False(result.Success);
        Assert.Equal("key_item_not_martial_fragment", result.ErrorCode);
        Assert.Equal(1, inventory.GetTotalQuantity("letter_wind_stop"));
    }

    private static (InventoryService Inventory, MartialFragmentTeachingService Service, TestKnowledgeProvider Knowledge)
        CreateServices(IEventBus? eventBus = null)
    {
        var registry = new DataRegistry();
        new ItemConfigLoader().LoadAll(
            ConsumablesYaml,
            EquipmentYaml,
            KeyItemsYaml,
            RecipesYaml,
            AffixPoolsYaml,
            registry);
        var inventory = new InventoryService(registry);
        var knowledge = new TestKnowledgeProvider();
        var service = new MartialFragmentTeachingService(registry, inventory, knowledge, eventBus);
        return (inventory, service, knowledge);
    }

    private sealed class TestKnowledgeProvider : IMartialMoveKnowledgeProvider
    {
        private readonly HashSet<(string CharacterId, string MoveId)> _learned = new();

        public bool HasLearnedMove(string characterId, string moveId)
        {
            return _learned.Contains((characterId, moveId));
        }

        public void Learn(string characterId, string moveId)
        {
            _learned.Add((characterId, moveId));
        }
    }

    private const string ConsumablesYaml = """
    - id: dry_food
      name: 干粮
      category: material
      rarity: common
      max_stack: 99
      combat_usable: false
      exploration_usable: false
      base_price: 5
      effects: []
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
    - id: fragment_wind_cut
      name: 听风斩残卷
      category: key_item
      rarity: rare
      auctionable: false
      description_key: item.key.fragment_wind_cut.desc
      linked_move_id: wind_cut

    - id: letter_wind_stop
      name: 风止旧信
      category: key_item
      rarity: rare
      auctionable: false
      description_key: item.key.letter_wind_stop.desc
    """;

    private const string RecipesYaml = """
    - id: brew_dry_food
      kind: alchemy
      ingredients:
        - item_id: dry_food
          qty: 1
      output_item_id: dry_food
      output_qty: 1
      silver_cost: 0
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
