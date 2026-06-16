using FengZhi.Foundation.CharacterData;
using FengZhi.Foundation.Data;
using FengZhi.Foundation.MartialArts;
using Xunit;

namespace Foundation.Tests.Integration.MartialArts;

#region Test Doubles

/// <summary>简易角色属性提供者（测试用）。</summary>
internal sealed class FakeStatProvider : ICharacterStatProvider
{
    private readonly Dictionary<string, Dictionary<string, int>> _stats = new();

    public void Set(string characterId, string stat, int value)
    {
        if (!_stats.ContainsKey(characterId))
            _stats[characterId] = new Dictionary<string, int>();
        _stats[characterId][stat] = value;
    }

    public int GetStat(string characterId, string statName)
    {
        if (_stats.TryGetValue(characterId, out var charStats) &&
            charStats.TryGetValue(statName, out var val))
            return val;
        return 0;
    }
}

/// <summary>可控封印提供者（测试用）。</summary>
internal sealed class FakeSealProvider : IXinfaSealProvider
{
    private readonly HashSet<string> _sealed = new();
    public void Seal(string characterId) => _sealed.Add(characterId);
    public void Unseal(string characterId) => _sealed.Remove(characterId);
    public bool IsSealed(string characterId) => _sealed.Contains(characterId);
}

/// <summary>简易 DataTable 实现（测试用）。</summary>
internal sealed class SimpleDataTable<T> : IDataTable<T> where T : class
{
    private readonly Dictionary<string, T> _items = new();
    private readonly Func<T, string> _idSelector;

    public SimpleDataTable(Func<T, string> idSelector) => _idSelector = idSelector;

    public void Add(T item) => _items[_idSelector(item)] = item;
    public T? Get(string id) => _items.TryGetValue(id, out var v) ? v : null;
    public IReadOnlyList<T> GetAll() => _items.Values.ToList();
    public int Count => _items.Count;
}

#endregion

public class XinfaEquipmentEffectsTest
{
    private const string CharId = "player";
    private const string XinfaId = "gang_xinfa";

    private readonly SimpleDataTable<XinfaDefinition> _xinfaTable;
    private readonly SimpleDataTable<MoveDefinition> _moveTable;
    private readonly FakeStatProvider _statProvider;
    private readonly FakeSealProvider _sealProvider;
    private readonly MoveProgressionService _progressionService;
    private readonly XinfaService _service;
    private readonly CharacterLoadout _loadout;

    public XinfaEquipmentEffectsTest()
    {
        _xinfaTable = new SimpleDataTable<XinfaDefinition>(x => x.Id);
        _moveTable = new SimpleDataTable<MoveDefinition>(m => m.Id);
        _statProvider = new FakeStatProvider();
        _sealProvider = new FakeSealProvider();
        _progressionService = new MoveProgressionService(_moveTable);
        _loadout = new CharacterLoadout(CharId);

        // 注册心法：需要力量 >= 20
        _xinfaTable.Add(new XinfaDefinition
        {
            Id = XinfaId,
            Name = "刚猛心法",
            Requirements = new List<XinfaRequirement>
            {
                new() { Stat = "strength", Min = 20 }
            },
            PassiveBonuses = new List<XinfaPassiveBonus>
            {
                new() { Stat = "attack", Value = 10f }
            },
            ExclusiveMoves = new List<string> { "gang_xinfa_move_1", "gang_xinfa_move_2" }
        });

        // 注册一个无门槛心法
        _xinfaTable.Add(new XinfaDefinition
        {
            Id = "simple_xinfa",
            Name = "入门心法",
            Requirements = new List<XinfaRequirement>(),
            PassiveBonuses = new List<XinfaPassiveBonus>
            {
                new() { Stat = "defense", Value = 5f }
            },
            ExclusiveMoves = new List<string> { "simple_exclusive" }
        });

        // 注册多门槛心法
        _xinfaTable.Add(new XinfaDefinition
        {
            Id = "multi_req_xinfa",
            Name = "双门槛心法",
            Requirements = new List<XinfaRequirement>
            {
                new() { Stat = "strength", Min = 20 },
                new() { Stat = "inner_force", Min = 15 }
            },
            PassiveBonuses = new List<XinfaPassiveBonus>
            {
                new() { Stat = "attack", Value = 8f }
            },
            ExclusiveMoves = new List<string> { "multi_exclusive" }
        });

        // 注册一些招式供装备
        RegisterMove("move_a", "招式A", MoveType.Gang);
        RegisterMove("move_b", "招式B", MoveType.Rou);
        RegisterMove("move_c", "招式C", MoveType.Qiao);
        RegisterMove("gang_xinfa_move_1", "心法招式1", MoveType.Gang);
        RegisterMove("gang_xinfa_move_2", "心法招式2", MoveType.Gang);

        _service = new XinfaService(_xinfaTable, _moveTable, _statProvider, _sealProvider);
    }

    private void RegisterMove(string id, string name, MoveType type)
    {
        _moveTable.Add(new MoveDefinition
        {
            Id = id,
            Name = name,
            Type = type,
            Category = MoveCategory.Basic,
            BaseMultiplier = 1.0f
        });
    }

    #region AC-1: 门槛检查

    [Fact]
    public void EquipXinfa_StrengthInsufficient_ReturnsDeficit()
    {
        // Given: 心法要求力量 >= 20, 玩家力量为 15
        _statProvider.Set(CharId, "strength", 15);

        // When
        var result = _service.EquipXinfa(_loadout, XinfaId);

        // Then: 装备失败，差值 = 5
        Assert.Equal(XinfaEquipResult.RequirementsNotMet, result.Result);
        Assert.Single(result.Deficits);
        Assert.Equal("strength", result.Deficits[0].Stat);
        Assert.Equal(20, result.Deficits[0].Required);
        Assert.Equal(15, result.Deficits[0].Current);
        Assert.Equal(5, result.Deficits[0].Deficit);
        Assert.Null(_loadout.XinfaSlot);
    }

    [Fact]
    public void EquipXinfa_StrengthExactlyMeetsRequirement_Succeeds()
    {
        // 边界：力量正好 20 可装备
        _statProvider.Set(CharId, "strength", 20);

        var result = _service.EquipXinfa(_loadout, XinfaId);

        Assert.Equal(XinfaEquipResult.Success, result.Result);
        Assert.Equal(XinfaId, _loadout.XinfaSlot);
    }

    [Fact]
    public void EquipXinfa_MultipleRequirementsNotMet_ReturnsAllDeficits()
    {
        // 边界：多个属性门槛同时不足
        _statProvider.Set(CharId, "strength", 10);
        _statProvider.Set(CharId, "inner_force", 5);

        var result = _service.EquipXinfa(_loadout, "multi_req_xinfa");

        Assert.Equal(XinfaEquipResult.RequirementsNotMet, result.Result);
        Assert.Equal(2, result.Deficits.Count);
        Assert.Contains(result.Deficits, d => d.Stat == "strength" && d.Deficit == 10);
        Assert.Contains(result.Deficits, d => d.Stat == "inner_force" && d.Deficit == 10);
    }

    [Fact]
    public void EquipXinfa_NoRequirements_AlwaysSucceeds()
    {
        var result = _service.EquipXinfa(_loadout, "simple_xinfa");

        Assert.Equal(XinfaEquipResult.Success, result.Result);
        Assert.Equal("simple_xinfa", _loadout.XinfaSlot);
    }

    [Fact]
    public void EquipXinfa_NotFound_ReturnsError()
    {
        var result = _service.EquipXinfa(_loadout, "nonexistent");

        Assert.Equal(XinfaEquipResult.XinfaNotFound, result.Result);
    }

    #endregion

    #region AC-2: 被动加成

    [Fact]
    public void GetPassiveBonuses_WithRealmScaling_ReturnsCorrectModifier()
    {
        // Given: 心法 base_bonus=10, 境界缩放=1.3 (炉火纯青)
        _statProvider.Set(CharId, "strength", 25);
        _service.EquipXinfa(_loadout, XinfaId);

        // When
        var bonuses = _service.GetPassiveBonuses(_loadout, RealmTier.LuHuoChunQing);

        // Then: modifier = 10 * 1.3 = 13
        Assert.Single(bonuses);
        Assert.Equal("attack", bonuses[0].Stat);
        Assert.Equal(13f, bonuses[0].Value, precision: 2);
    }

    [Fact]
    public void GetPassiveBonuses_LowestRealm_ReturnsScaledDown()
    {
        // 边界：初学乍练 0.7
        _statProvider.Set(CharId, "strength", 25);
        _service.EquipXinfa(_loadout, XinfaId);

        var bonuses = _service.GetPassiveBonuses(_loadout, RealmTier.ChuXueZhaLian);

        Assert.Single(bonuses);
        Assert.Equal(7f, bonuses[0].Value, precision: 2);
    }

    [Fact]
    public void GetPassiveBonuses_HighestRealm_ReturnsScaledUp()
    {
        // 边界：返璞归真 2.0
        _statProvider.Set(CharId, "strength", 25);
        _service.EquipXinfa(_loadout, XinfaId);

        var bonuses = _service.GetPassiveBonuses(_loadout, RealmTier.FanPuGuiZhen);

        Assert.Single(bonuses);
        Assert.Equal(20f, bonuses[0].Value, precision: 2);
    }

    [Fact]
    public void GetPassiveBonuses_NoXinfaEquipped_ReturnsEmpty()
    {
        var bonuses = _service.GetPassiveBonuses(_loadout, RealmTier.RongHuiGuanTong);

        Assert.Empty(bonuses);
    }

    #endregion

    #region AC-3: 专属招式与封印

    [Fact]
    public void GetCombatMoveSet_WithXinfa_ReturnsBasePlusExclusive()
    {
        // Given: 装备 3 个基础招式 + 心法含 2 个专属招式
        _statProvider.Set(CharId, "strength", 25);
        _progressionService.LearnBasicMove("move_a");
        _progressionService.LearnBasicMove("move_b");
        _progressionService.LearnBasicMove("move_c");

        var loadoutSvc = new LoadoutService(_moveTable, _progressionService);
        loadoutSvc.EquipMove(_loadout, 0, "move_a");
        loadoutSvc.EquipMove(_loadout, 1, "move_b");
        loadoutSvc.EquipMove(_loadout, 2, "move_c");

        _service.EquipXinfa(_loadout, XinfaId);

        // When
        var moveSet = _service.GetCombatMoveSet(_loadout);

        // Then: 3 基础 + 2 心法专属
        Assert.Equal(3, moveSet.BaseMoves.Count);
        Assert.Equal(2, moveSet.XinfaExclusiveMoves.Count);
        Assert.Equal(5, moveSet.TotalCount);
        Assert.Contains("gang_xinfa_move_1", moveSet.XinfaExclusiveMoves);
        Assert.Contains("gang_xinfa_move_2", moveSet.XinfaExclusiveMoves);
    }

    [Fact]
    public void GetCombatMoveSet_XinfaSealed_ExclusiveMovesUnavailable()
    {
        // Given: 心法被封印
        _statProvider.Set(CharId, "strength", 25);
        _service.EquipXinfa(_loadout, XinfaId);
        _sealProvider.Seal(CharId);

        // When
        var moveSet = _service.GetCombatMoveSet(_loadout);

        // Then: 心法专属招式不可用
        Assert.Empty(moveSet.XinfaExclusiveMoves);
    }

    [Fact]
    public void GetPassiveBonuses_XinfaSealed_ReturnsEmpty()
    {
        // Given: 心法被封印
        _statProvider.Set(CharId, "strength", 25);
        _service.EquipXinfa(_loadout, XinfaId);
        _sealProvider.Seal(CharId);

        // When
        var bonuses = _service.GetPassiveBonuses(_loadout, RealmTier.LuHuoChunQing);

        // Then: 被动加成暂时失效
        Assert.Empty(bonuses);
    }

    [Fact]
    public void GetCombatMoveSet_SealLifted_ExclusiveMovesAvailableAgain()
    {
        // 封印解除后恢复
        _statProvider.Set(CharId, "strength", 25);
        _service.EquipXinfa(_loadout, XinfaId);
        _sealProvider.Seal(CharId);
        Assert.Empty(_service.GetCombatMoveSet(_loadout).XinfaExclusiveMoves);

        _sealProvider.Unseal(CharId);
        var moveSet = _service.GetCombatMoveSet(_loadout);
        Assert.Equal(2, moveSet.XinfaExclusiveMoves.Count);
    }

    #endregion

    #region AC-5: 卸下心法

    [Fact]
    public void UnequipXinfa_BonusesAndExclusiveMovesGone()
    {
        // Given: 装备心法后卸下
        _statProvider.Set(CharId, "strength", 25);
        _service.EquipXinfa(_loadout, XinfaId);
        Assert.NotNull(_loadout.XinfaSlot);

        // When
        _service.UnequipXinfa(_loadout);

        // Then: 被动加成和专属招式不再出现
        Assert.Null(_loadout.XinfaSlot);
        var bonuses = _service.GetPassiveBonuses(_loadout, RealmTier.LuHuoChunQing);
        Assert.Empty(bonuses);
        var moveSet = _service.GetCombatMoveSet(_loadout);
        Assert.Empty(moveSet.XinfaExclusiveMoves);
    }

    #endregion
}
