using FengZhi.Foundation.CharacterData;
using FengZhi.Foundation.Data;
using FengZhi.Foundation.MartialArts;
using Xunit;

namespace FengZhi.Tests.unit.martial_arts;

public sealed class MartialArtsYamlSchemaTest
{
    private readonly MartialArtsConfigLoader _loader = new();

    [Fact]
    public void LoadAll_ValidMartialArtsYaml_RegistersQueryableTables()
    {
        var registry = new DataRegistry();

        _loader.LoadAll(ValidMovesYaml, ValidXinfaYaml, ValidQinggongYaml, ValidCounterMatrixYaml, registry);

        var moves = registry.GetTable<MoveDefinition>();
        var xinfa = registry.GetTable<XinfaDefinition>();
        var qinggong = registry.GetTable<QinggongDefinition>();
        var matrix = registry.GetTable<CounterMatrixEntry>();

        Assert.NotNull(moves);
        Assert.NotNull(xinfa);
        Assert.NotNull(qinggong);
        Assert.NotNull(matrix);
        Assert.Equal(3, moves!.Count);
        Assert.Equal("敛虚息", moves.Get("lian_xu_xi")!.Name);
        Assert.Equal(MoveType.Rou, moves.Get("lian_xu_xi")!.Type);
        Assert.Equal(1, qinggong!.Get("xun_feng_bu")!.MoveRangeBonus);
        Assert.Contains("lian_xu_xi", xinfa!.Get("feng_zhi_xinfa")!.ExclusiveMoves);
    }

    [Fact]
    public void LoadAll_MoveMissingRequiredFields_FailsFastWithFieldName()
    {
        var invalidMoves = """
        - id: broken_move
          name: 断章
          type: rou
          category: basic
          neixi_cost: 1
          base_multiplier: 1.0
          special_effects: [guard_up]
          tags: [defensive]
        """;

        var ex = Assert.Throws<DataLoadException>(() =>
            _loader.LoadAll(invalidMoves, ValidXinfaYaml, ValidQinggongYaml, ValidCounterMatrixYaml, new DataRegistry()));

        Assert.Contains("assets/data/martial-arts/moves.yaml", ex.Message);
        Assert.Contains("trigger_conditions", ex.Message);
    }

    [Fact]
    public void CounterMatrix_QueryAnyAttackerDefenderRelation_ReturnsExpectedRelation()
    {
        var registry = new DataRegistry();
        _loader.LoadAll(ValidMovesYaml, ValidXinfaYaml, ValidQinggongYaml, ValidCounterMatrixYaml, registry);

        var matrix = Assert.IsType<CounterMatrixTable>(registry.GetTable<CounterMatrixEntry>());

        Assert.Equal(CounterRelation.Advantage, matrix.Get(MoveType.Rou, MoveType.Gang)!.Relation);
        Assert.Equal(CounterRelation.Disadvantage, matrix.Get(MoveType.Gang, MoveType.Rou)!.Relation);
        Assert.Equal(CounterRelation.Neutral, matrix.Get(MoveType.Qiao, MoveType.Qiao)!.Relation);
    }

    [Theory]
    [InlineData("duplicate_id", "id 重复")]
    [InlineData("negative_neixi", "neixi_cost")]
    [InlineData("zero_multiplier", "base_multiplier")]
    [InlineData("duplicate_counter_key", "克制矩阵 key 重复")]
    public void LoadAll_InvalidConfig_FailsFastWithFileAndField(string scenario, string expectedKeyword)
    {
        var (moves, matrix) = scenario switch
        {
            "duplicate_id" => (DuplicateMoveIdYaml, ValidCounterMatrixYaml),
            "negative_neixi" => (ValidMovesYaml.Replace("neixi_cost: 2", "neixi_cost: -1"), ValidCounterMatrixYaml),
            "zero_multiplier" => (ValidMovesYaml.Replace("base_multiplier: 0.85", "base_multiplier: 0"), ValidCounterMatrixYaml),
            "duplicate_counter_key" => (ValidMovesYaml, ValidCounterMatrixYaml + """

                - attacker: gang
                  defender: gang
                  relation: neutral
                  multiplier: 1.0
                """),
            _ => throw new ArgumentOutOfRangeException(nameof(scenario))
        };

        var ex = Assert.Throws<DataLoadException>(() =>
            _loader.LoadAll(moves, ValidXinfaYaml, ValidQinggongYaml, matrix, new DataRegistry()));

        Assert.Contains("assets/data/martial-arts/", ex.Message);
        Assert.Contains(expectedKeyword, ex.Message);
    }

    [Fact]
    public void LoadAll_MoveWithUnknownEnumValue_FailsWithFileNameAndLineNumber()
    {
        var invalidMoves = ValidMovesYaml.Replace("type: rou", "type: fire");

        var ex = Assert.Throws<DataLoadException>(() =>
            _loader.LoadAll(invalidMoves, ValidXinfaYaml, ValidQinggongYaml, ValidCounterMatrixYaml, new DataRegistry()));

        Assert.Contains("assets/data/martial-arts/moves.yaml", ex.Message);
        Assert.NotNull(ex.LineNumber);
    }

    [Fact]
    public void LoadAll_XinfaReferencesMissingExclusiveMove_FailsWithXinfaAndMoveId()
    {
        var invalidXinfa = ValidXinfaYaml.Replace("lian_xu_xi", "missing_move");

        var ex = Assert.Throws<DataLoadException>(() =>
            _loader.LoadAll(ValidMovesYaml, invalidXinfa, ValidQinggongYaml, ValidCounterMatrixYaml, new DataRegistry()));

        Assert.Contains("assets/data/martial-arts/xinfa.yaml", ex.Message);
        Assert.Contains("feng_zhi_xinfa", ex.Message);
        Assert.Contains("missing_move", ex.Message);
    }

    private const string ValidMovesYaml = """
    - id: lian_xu_xi
      name: 敛虚息
      type: rou
      category: basic
      neixi_cost: 2
      base_multiplier: 0.85
      trigger_conditions: [always]
      special_effects: [guard_up]
      tags: [defensive, neixi]

    - id: qing_dian_feng
      name: 轻点封
      type: qiao
      category: basic
      neixi_cost: 3
      base_multiplier: 1.0
      trigger_conditions: [target_not_immobilized]
      special_effects: [immobilize_chance]
      tags: [control, precise]

    - id: tie_bi_heng_lan
      name: 铁臂横拦
      type: gang
      category: basic
      neixi_cost: 4
      base_multiplier: 1.15
      trigger_conditions: [adjacent_enemy]
      special_effects: [stagger_bonus]
      tags: [direct, stagger]
    """;

    private const string DuplicateMoveIdYaml = """
    - id: lian_xu_xi
      name: 敛虚息
      type: rou
      category: basic
      neixi_cost: 2
      base_multiplier: 0.85
      trigger_conditions: [always]
      special_effects: [guard_up]
      tags: [defensive, neixi]

    - id: lian_xu_xi
      name: 敛虚息重录
      type: rou
      category: basic
      neixi_cost: 2
      base_multiplier: 0.85
      trigger_conditions: [always]
      special_effects: [guard_up]
      tags: [defensive, neixi]
    """;

    private const string ValidXinfaYaml = """
    - id: feng_zhi_xinfa
      name: 风止心法
      requirements:
        - stat: inner_force
          min: 8
      passive_bonuses:
        - stat: rou_multiplier
          value: 0.10
      exclusive_moves:
        - lian_xu_xi
        - qing_dian_feng
    """;

    private const string ValidQinggongYaml = """
    - id: xun_feng_bu
      name: 巡风步
      move_range_bonus: 1
      active_maneuvers:
        - id: step_after_skill
          description: 使用部分招式后若仍有行动力，可继续移动。
    """;

    private const string ValidCounterMatrixYaml = """
    - attacker: gang
      defender: gang
      relation: neutral
      multiplier: 1.0
    - attacker: gang
      defender: rou
      relation: disadvantage
      multiplier: 0.7
    - attacker: gang
      defender: qiao
      relation: advantage
      multiplier: 1.3
    - attacker: rou
      defender: gang
      relation: advantage
      multiplier: 1.3
    - attacker: rou
      defender: rou
      relation: neutral
      multiplier: 1.0
    - attacker: rou
      defender: qiao
      relation: disadvantage
      multiplier: 0.7
    - attacker: qiao
      defender: gang
      relation: disadvantage
      multiplier: 0.7
    - attacker: qiao
      defender: rou
      relation: advantage
      multiplier: 1.3
    - attacker: qiao
      defender: qiao
      relation: neutral
      multiplier: 1.0
    """;
}
