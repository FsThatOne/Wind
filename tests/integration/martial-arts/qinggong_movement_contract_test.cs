using FengZhi.Foundation.Data;
using FengZhi.Foundation.MartialArts;
using Xunit;

namespace Foundation.Tests.Integration.MartialArts;

#region Test Doubles

internal sealed class FakeQinggongProgressionProvider : IQinggongProgressionProvider
{
    private readonly HashSet<string> _learned = new();

    public void Learn(string characterId, string qinggongId) => _learned.Add($"{characterId}:{qinggongId}");
    public bool HasLearned(string characterId, string qinggongId) => _learned.Contains($"{characterId}:{qinggongId}");
}

internal sealed class FakeQinggongLockProvider : IQinggongLockProvider
{
    private readonly Dictionary<string, string> _locked = new();

    public void Lock(string characterId, string reason) => _locked[characterId] = reason;
    public void Unlock(string characterId) => _locked.Remove(characterId);
    public bool IsLocked(string characterId) => _locked.ContainsKey(characterId);
    public string? GetLockReason(string characterId) => _locked.TryGetValue(characterId, out var r) ? r : null;
}

internal sealed class QinggongDataTable : IDataTable<QinggongDefinition>
{
    private readonly Dictionary<string, QinggongDefinition> _items = new();

    public void Add(QinggongDefinition item) => _items[item.Id] = item;
    public QinggongDefinition? Get(string id) => _items.TryGetValue(id, out var v) ? v : null;
    public IReadOnlyList<QinggongDefinition> GetAll() => _items.Values.ToList();
    public int Count => _items.Count;
}

#endregion

public class QinggongMovementContractTest
{
    private const string PlayerId = "player";
    private const string CompanionId = "companion";

    private readonly QinggongDataTable _qinggongTable;
    private readonly FakeQinggongProgressionProvider _progression;
    private readonly FakeQinggongLockProvider _lockProvider;
    private readonly QinggongService _service;

    public QinggongMovementContractTest()
    {
        _qinggongTable = new QinggongDataTable();
        _progression = new FakeQinggongProgressionProvider();
        _lockProvider = new FakeQinggongLockProvider();

        // 注册轻功
        _qinggongTable.Add(new QinggongDefinition
        {
            Id = "xun_feng_bu",
            Name = "巡风步",
            MoveRangeBonus = 1,
            ActiveManeuvers = new List<QinggongActiveManeuver>
            {
                new() { Id = "step_after_skill", Description = "使用招式后可继续移动" }
            }
        });

        _qinggongTable.Add(new QinggongDefinition
        {
            Id = "ta_xue_wu_hen",
            Name = "踏雪无痕",
            MoveRangeBonus = 2,
            ActiveManeuvers = new List<QinggongActiveManeuver>
            {
                new() { Id = "flash_step", Description = "闪身穿过相邻敌人" },
                new() { Id = "retreat_after_hit", Description = "命中后退1格" }
            }
        });

        _service = new QinggongService(_qinggongTable, _progression, _lockProvider);
    }

    #region AC-1: 轻功槽存在

    [Fact]
    public void Loadout_BothPlayerAndCompanion_HaveQinggongSlot()
    {
        // Given: 主角和同伴初始化
        var player = new CharacterLoadout(PlayerId);
        var companion = new CharacterLoadout(CompanionId);

        // Then: 均有轻功槽（初始为 null）
        Assert.Null(player.QinggongSlot);
        Assert.Null(companion.QinggongSlot);

        // 装备后验证槽位存在且可用
        _progression.Learn(PlayerId, "xun_feng_bu");
        _progression.Learn(CompanionId, "ta_xue_wu_hen");

        Assert.Equal(QinggongEquipResult.Success, _service.EquipQinggong(player, "xun_feng_bu").Result);
        Assert.Equal(QinggongEquipResult.Success, _service.EquipQinggong(companion, "ta_xue_wu_hen").Result);

        Assert.Equal("xun_feng_bu", player.QinggongSlot);
        Assert.Equal("ta_xue_wu_hen", companion.QinggongSlot);
    }

    [Fact]
    public void Loadout_LockedCharacter_StillHasSlot()
    {
        // 边界：剧情锁定角色仍保留槽位
        var loadout = new CharacterLoadout(PlayerId);
        _lockProvider.Lock(PlayerId, "剧情中不可更换");

        // 槽位存在（属性可读），只是操作被拒
        Assert.Null(loadout.QinggongSlot);
    }

    #endregion

    #region AC-2: 移动修正查询

    [Fact]
    public void GetMovementContract_Equipped_ReturnsCorrectBonus()
    {
        // Given: 装备"巡风步"，移动范围修正 +1
        var loadout = new CharacterLoadout(PlayerId);
        _progression.Learn(PlayerId, "xun_feng_bu");
        _service.EquipQinggong(loadout, "xun_feng_bu");

        // When
        var contract = _service.GetMovementContract(loadout);

        // Then
        Assert.Equal("xun_feng_bu", contract.QinggongId);
        Assert.Equal(1, contract.MoveRangeBonus);
        Assert.Single(contract.ActiveManeuvers);
        Assert.Equal("step_after_skill", contract.ActiveManeuvers[0].Id);
    }

    [Fact]
    public void GetMovementContract_HigherBonus_ReturnsCorrectly()
    {
        var loadout = new CharacterLoadout(PlayerId);
        _progression.Learn(PlayerId, "ta_xue_wu_hen");
        _service.EquipQinggong(loadout, "ta_xue_wu_hen");

        var contract = _service.GetMovementContract(loadout);

        Assert.Equal("ta_xue_wu_hen", contract.QinggongId);
        Assert.Equal(2, contract.MoveRangeBonus);
        Assert.Equal(2, contract.ActiveManeuvers.Count);
    }

    [Fact]
    public void GetMovementContract_NotEquipped_ReturnsDefault()
    {
        // 未装备轻功时返回默认契约
        var loadout = new CharacterLoadout(PlayerId);

        var contract = _service.GetMovementContract(loadout);

        Assert.Null(contract.QinggongId);
        Assert.Equal(0, contract.MoveRangeBonus);
        Assert.Empty(contract.ActiveManeuvers);
    }

    [Fact]
    public void GetMovementContract_AfterUnequip_ReturnsDefault()
    {
        var loadout = new CharacterLoadout(PlayerId);
        _progression.Learn(PlayerId, "xun_feng_bu");
        _service.EquipQinggong(loadout, "xun_feng_bu");
        _service.UnequipQinggong(loadout);

        var contract = _service.GetMovementContract(loadout);

        Assert.Null(contract.QinggongId);
        Assert.Equal(0, contract.MoveRangeBonus);
    }

    #endregion

    #region AC-3: 非法装备

    [Fact]
    public void EquipQinggong_NotLearned_Fails()
    {
        // Given: 角色未习得
        var loadout = new CharacterLoadout(PlayerId);

        // When
        var result = _service.EquipQinggong(loadout, "xun_feng_bu");

        // Then
        Assert.Equal(QinggongEquipResult.QinggongNotLearned, result.Result);
        Assert.Null(loadout.QinggongSlot);
    }

    [Fact]
    public void EquipQinggong_NotFound_Fails()
    {
        // 轻功 id 不存在
        var loadout = new CharacterLoadout(PlayerId);

        var result = _service.EquipQinggong(loadout, "nonexistent_qinggong");

        Assert.Equal(QinggongEquipResult.QinggongNotFound, result.Result);
    }

    [Fact]
    public void EquipQinggong_SlotLocked_Fails()
    {
        // 轻功槽被剧情锁定
        var loadout = new CharacterLoadout(PlayerId);
        _progression.Learn(PlayerId, "xun_feng_bu");
        _lockProvider.Lock(PlayerId, "剧情中不可更换轻功");

        var result = _service.EquipQinggong(loadout, "xun_feng_bu");

        Assert.Equal(QinggongEquipResult.SlotLocked, result.Result);
        Assert.Equal("剧情中不可更换轻功", result.LockReason);
        Assert.Null(loadout.QinggongSlot);
    }

    [Fact]
    public void UnequipQinggong_SlotLocked_Fails()
    {
        // 锁定时也不能卸下
        var loadout = new CharacterLoadout(PlayerId);
        _progression.Learn(PlayerId, "xun_feng_bu");
        _service.EquipQinggong(loadout, "xun_feng_bu");
        _lockProvider.Lock(PlayerId, "剧情中不可更换轻功");

        var result = _service.UnequipQinggong(loadout);

        Assert.Equal(QinggongEquipResult.SlotLocked, result.Result);
        Assert.Equal("xun_feng_bu", loadout.QinggongSlot);
    }

    [Fact]
    public void EquipQinggong_ReplacesExisting()
    {
        // 装备新轻功替换旧轻功
        var loadout = new CharacterLoadout(PlayerId);
        _progression.Learn(PlayerId, "xun_feng_bu");
        _progression.Learn(PlayerId, "ta_xue_wu_hen");

        _service.EquipQinggong(loadout, "xun_feng_bu");
        Assert.Equal("xun_feng_bu", loadout.QinggongSlot);

        _service.EquipQinggong(loadout, "ta_xue_wu_hen");
        Assert.Equal("ta_xue_wu_hen", loadout.QinggongSlot);
    }

    #endregion
}
