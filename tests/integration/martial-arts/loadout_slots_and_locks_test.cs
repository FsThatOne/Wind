using FengZhi.Foundation.CharacterData;
using FengZhi.Foundation.Data;
using FengZhi.Foundation.MartialArts;
using Xunit;

namespace FengZhi.Tests.Integration.MartialArts;

public sealed class LoadoutSlotsAndLocksTest
{
    private readonly MoveDefinitionTable _moveTable;
    private readonly MoveProgressionService _progressionService;

    public LoadoutSlotsAndLocksTest()
    {
        var loader = new MartialArtsConfigLoader();
        var moves = loader.LoadList<MoveDefinition>(TestMovesYaml, "test");
        _moveTable = new MoveDefinitionTable(moves);
        _progressionService = new MoveProgressionService(_moveTable);

        // 习得所有测试招式
        _progressionService.LearnBasicMove("move_gang_1");
        _progressionService.LearnBasicMove("move_gang_2");
        _progressionService.LearnBasicMove("move_gang_3");
        _progressionService.LearnBasicMove("move_gang_4");
        _progressionService.LearnBasicMove("move_gang_5");
        _progressionService.LearnBasicMove("move_rou_1");
        _progressionService.LearnBasicMove("move_qiao_1");
    }

    // === AC-1：同伴 loadout 可配置 ===

    [Fact]
    public void EquipMove_AllSlots_LoadoutUpdated()
    {
        var service = CreateService();
        var loadout = new CharacterLoadout("companion_a");

        service.EquipMove(loadout, 0, "move_gang_1");
        service.EquipMove(loadout, 1, "move_gang_2");
        service.EquipMove(loadout, 2, "move_gang_3");
        service.EquipMove(loadout, 3, "move_gang_4");
        service.EquipMove(loadout, 4, "move_gang_5");
        service.EquipMove(loadout, 5, "move_rou_1");

        var equipped = loadout.GetEquippedMoves();
        Assert.Equal(6, equipped.Count);
        Assert.Equal("move_gang_1", equipped[0]);
        Assert.Equal("move_rou_1", equipped[5]);
    }

    [Fact]
    public void EquipMove_NotLearned_ReturnsMoveNotLearned()
    {
        var service = CreateService();
        var loadout = new CharacterLoadout("hero");

        var result = service.EquipMove(loadout, 0, "move_not_learned");

        Assert.Equal(LoadoutResult.MoveNotFound, result.Result);
    }

    [Fact]
    public void EquipMove_DuplicateInDifferentSlot_ReturnsDuplicateMove()
    {
        var service = CreateService();
        var loadout = new CharacterLoadout("hero");

        service.EquipMove(loadout, 0, "move_gang_1");
        var result = service.EquipMove(loadout, 1, "move_gang_1");

        Assert.Equal(LoadoutResult.DuplicateMove, result.Result);
    }

    [Fact]
    public void EquipMove_ReplaceInSameSlot_Success()
    {
        var service = CreateService();
        var loadout = new CharacterLoadout("hero");

        service.EquipMove(loadout, 0, "move_gang_1");
        var result = service.EquipMove(loadout, 0, "move_gang_2");

        Assert.Equal(LoadoutResult.Success, result.Result);
        Assert.Equal("move_gang_2", loadout.GetMoveSlot(0));
    }

    [Fact]
    public void GetEquippedMoves_StableOrder_SkipsNulls()
    {
        var service = CreateService();
        var loadout = new CharacterLoadout("hero");

        service.EquipMove(loadout, 1, "move_gang_1");
        service.EquipMove(loadout, 4, "move_rou_1");

        var equipped = loadout.GetEquippedMoves();
        Assert.Equal(2, equipped.Count);
        Assert.Equal("move_gang_1", equipped[0]);
        Assert.Equal("move_rou_1", equipped[1]);
    }

    // === AC-2：体系覆盖警告 ===

    [Fact]
    public void CheckTypeCoverage_AllThreeTypes_NoWarning()
    {
        var service = CreateService();
        var loadout = new CharacterLoadout("hero");

        service.EquipMove(loadout, 0, "move_gang_1");
        service.EquipMove(loadout, 1, "move_rou_1");
        service.EquipMove(loadout, 2, "move_qiao_1");

        var warning = service.CheckTypeCoverage(loadout);

        Assert.False(warning.HasWarning);
        Assert.Empty(warning.MissingTypes);
    }

    [Fact]
    public void CheckTypeCoverage_MissingQiao_ReturnsWarning()
    {
        var service = CreateService();
        var loadout = new CharacterLoadout("hero");

        service.EquipMove(loadout, 0, "move_gang_1");
        service.EquipMove(loadout, 1, "move_gang_2");
        service.EquipMove(loadout, 2, "move_gang_3");
        service.EquipMove(loadout, 3, "move_gang_4");
        service.EquipMove(loadout, 4, "move_gang_5");
        service.EquipMove(loadout, 5, "move_rou_1");

        var warning = service.CheckTypeCoverage(loadout);

        Assert.True(warning.HasWarning);
        Assert.Contains(MoveType.Qiao, warning.MissingTypes);
    }

    [Fact]
    public void CheckTypeCoverage_MissingTwoTypes_ReturnsBoth()
    {
        var service = CreateService();
        var loadout = new CharacterLoadout("hero");

        service.EquipMove(loadout, 0, "move_gang_1");

        var warning = service.CheckTypeCoverage(loadout);

        Assert.True(warning.HasWarning);
        Assert.Contains(MoveType.Rou, warning.MissingTypes);
        Assert.Contains(MoveType.Qiao, warning.MissingTypes);
    }

    // === AC-3：剧情锁定槽位 ===

    [Fact]
    public void EquipMove_SlotLocked_ReturnsLockedWithReason()
    {
        var lockProvider = new TestLockProvider("companion_a", 2, "疗伤中不可换招");
        var service = CreateService(lockProvider);
        var loadout = new CharacterLoadout("companion_a");

        var result = service.EquipMove(loadout, 2, "move_gang_1");

        Assert.Equal(LoadoutResult.SlotLocked, result.Result);
        Assert.Equal("疗伤中不可换招", result.LockReason);
    }

    [Fact]
    public void UnequipMove_SlotLocked_ReturnsLocked()
    {
        var lockProvider = new TestLockProvider("companion_a", 0, "剧情需要");
        var service = CreateService(lockProvider);
        var loadout = new CharacterLoadout("companion_a");

        var result = service.UnequipMove(loadout, 0);

        Assert.Equal(LoadoutResult.SlotLocked, result.Result);
    }

    [Fact]
    public void EquipMove_UnlockedSlot_Success()
    {
        var lockProvider = new TestLockProvider("companion_a", 2, "疗伤中");
        var service = CreateService(lockProvider);
        var loadout = new CharacterLoadout("companion_a");

        // 槽位 0 未锁定
        var result = service.EquipMove(loadout, 0, "move_gang_1");

        Assert.Equal(LoadoutResult.Success, result.Result);
    }

    // === AC-4：同伴偏好提醒 ===

    [Fact]
    public void EquipMove_PreferenceConflict_SuccessWithReminder()
    {
        var service = CreateService();
        service.RegisterPreference(new CharacterPreference
        {
            CharacterId = "companion_b",
            DislikedTags = new List<string> { "direct" }
        });
        var loadout = new CharacterLoadout("companion_b");

        var result = service.EquipMove(loadout, 0, "move_gang_1"); // gang_1 has "direct" tag

        Assert.Equal(LoadoutResult.Success, result.Result);
        Assert.NotNull(result.PreferenceReminder);
        Assert.Contains("direct", result.PreferenceReminder);
    }

    [Fact]
    public void EquipMove_NoPreferenceConflict_NoReminder()
    {
        var service = CreateService();
        service.RegisterPreference(new CharacterPreference
        {
            CharacterId = "companion_b",
            DislikedTags = new List<string> { "finishing" }
        });
        var loadout = new CharacterLoadout("companion_b");

        var result = service.EquipMove(loadout, 0, "move_rou_1"); // rou_1 has "defensive" tag

        Assert.Equal(LoadoutResult.Success, result.Result);
        Assert.Null(result.PreferenceReminder);
    }

    // === Helpers ===

    private LoadoutService CreateService(ISlotLockProvider? lockProvider = null) =>
        new(_moveTable, _progressionService, lockProvider);

    private sealed class TestLockProvider : ISlotLockProvider
    {
        private readonly string _characterId;
        private readonly int _lockedSlot;
        private readonly string _reason;

        public TestLockProvider(string characterId, int lockedSlot, string reason)
        {
            _characterId = characterId;
            _lockedSlot = lockedSlot;
            _reason = reason;
        }

        public bool IsLocked(string characterId, int slotIndex) =>
            characterId == _characterId && slotIndex == _lockedSlot;

        public string? GetLockReason(string characterId, int slotIndex) =>
            IsLocked(characterId, slotIndex) ? _reason : null;
    }

    // === Test Data ===

    private const string TestMovesYaml = """
    - id: move_gang_1
      name: 刚系测试1
      type: gang
      category: basic
      neixi_cost: 2
      base_multiplier: 1.0
      trigger_conditions: [always]
      special_effects: [stagger_bonus]
      tags: [direct]

    - id: move_gang_2
      name: 刚系测试2
      type: gang
      category: basic
      neixi_cost: 2
      base_multiplier: 1.0
      trigger_conditions: [always]
      special_effects: [stagger_bonus]
      tags: [direct]

    - id: move_gang_3
      name: 刚系测试3
      type: gang
      category: basic
      neixi_cost: 2
      base_multiplier: 1.0
      trigger_conditions: [always]
      special_effects: [stagger_bonus]
      tags: [direct]

    - id: move_gang_4
      name: 刚系测试4
      type: gang
      category: basic
      neixi_cost: 2
      base_multiplier: 1.0
      trigger_conditions: [always]
      special_effects: [stagger_bonus]
      tags: [direct]

    - id: move_gang_5
      name: 刚系测试5
      type: gang
      category: basic
      neixi_cost: 2
      base_multiplier: 1.0
      trigger_conditions: [always]
      special_effects: [stagger_bonus]
      tags: [direct]

    - id: move_rou_1
      name: 柔系测试1
      type: rou
      category: basic
      neixi_cost: 2
      base_multiplier: 0.8
      trigger_conditions: [always]
      special_effects: [guard_up]
      tags: [defensive]

    - id: move_qiao_1
      name: 巧系测试1
      type: qiao
      category: basic
      neixi_cost: 3
      base_multiplier: 1.0
      trigger_conditions: [always]
      special_effects: [immobilize_chance]
      tags: [control]
    """;
}
