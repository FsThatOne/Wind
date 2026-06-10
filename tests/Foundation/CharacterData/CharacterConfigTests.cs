using Xunit;
using FengZhi.Foundation.CharacterData;
using FengZhi.Foundation.Data;

namespace FengZhi.Tests.Foundation.CharacterData;

public class CharacterConfigTests
{
    private readonly CharacterConfigLoader _loader = new();

    private const string ValidPlayerYaml = @"
id: player
name: 风止·主角
type: player
base_hp: 100
base_neixi: 20
base_stagger_threshold: 5
strength: 8
agility: 8
inner_power: 8
insight: 8
constitution: 8
base_attack: 10
scaling_factor: 1.0
primary_style: gang
moves:
  - basic_attack
  - duan_shui_han_guang
";

    private const string ValidEnemyYaml = @"
id: bandit_leader
name: 匪首·刀疤
type: enemy
base_hp: 80
base_neixi: 15
base_stagger_threshold: 4
strength: 12
agility: 6
inner_power: 5
insight: 5
constitution: 10
base_attack: 12
scaling_factor: 0.9
primary_style: gang
moves:
  - heavy_slash
";

    private const string ValidTuningYaml = @"
base_hp: 100
base_neixi: 20
base_stagger_threshold: 5
attribute_min: 1
attribute_cap: 50
max_total_attributes: 250
realm_thresholds:
  - 25
  - 35
  - 50
  - 70
  - 90
  - 115
  - 140
  - 170
  - 200
crit_rate_cap: 0.30
neixi_recovery_floor: 2
stagger_decay_per_turn: 1
";

    // ─── AC1: YAML 正确反序列化为 CharacterTemplate ──────────

    [Fact]
    public void LoadTemplate_ValidYaml_DeserializesCorrectly()
    {
        var template = _loader.LoadTemplate(ValidPlayerYaml, "player.yaml");

        Assert.Equal("player", template.Id);
        Assert.Equal("风止·主角", template.Name);
        Assert.Equal("player", template.TypeName);
        Assert.Equal(100, template.BaseHp);
        Assert.Equal(20, template.BaseNeiXi);
        Assert.Equal(5, template.BaseStaggerThreshold);
        Assert.Equal(8, template.Strength);
        Assert.Equal(8, template.Agility);
        Assert.Equal(8, template.InnerPower);
        Assert.Equal(8, template.Insight);
        Assert.Equal(8, template.Constitution);
        Assert.Equal(10, template.BaseAttack);
        Assert.Equal(1.0f, template.ScalingFactor);
        Assert.Equal("gang", template.PrimaryStyle);
        Assert.Equal(2, template.Moves.Count);
        Assert.Contains("basic_attack", template.Moves);
    }

    [Fact]
    public void LoadTemplate_TypeResolution_ReturnsCorrectEnum()
    {
        var template = _loader.LoadTemplate(ValidPlayerYaml, "player.yaml");
        Assert.Equal(CharacterType.Player, template.GetCharacterType());
    }

    // ─── AC2: DataRegistry 查询有效对象 ─────────────────────

    [Fact]
    public void DataRegistry_GetTable_ReturnsValidTemplate()
    {
        var registry = new DataRegistry();
        var sources = new Dictionary<string, string>
        {
            ["characters/player.yaml"] = ValidPlayerYaml
        };

        _loader.LoadAllTemplates(sources, registry);

        var table = registry.GetTable<CharacterTemplate>();
        Assert.NotNull(table);
        var player = table!.Get("player");
        Assert.NotNull(player);
        Assert.Equal("风止·主角", player!.Name);
    }

    // ─── AC3: 格式错误 YAML → 异常含文件名和行号 ────────────

    [Fact]
    public void LoadTemplate_MalformedYaml_ThrowsDataLoadExceptionWithLineInfo()
    {
        var badYaml = @"
id: broken
name: [invalid yaml structure
  this is not valid:
";
        var ex = Assert.Throws<DataLoadException>(() =>
            _loader.LoadTemplate(badYaml, "characters/broken.yaml"));

        Assert.Contains("characters/broken.yaml", ex.Message);
        Assert.NotNull(ex.LineNumber);
        Assert.True(ex.LineNumber > 0);
    }

    [Fact]
    public void LoadTemplate_EmptyYaml_ThrowsDataLoadException()
    {
        var ex = Assert.Throws<DataLoadException>(() =>
            _loader.LoadTemplate("", "characters/empty.yaml"));

        Assert.Contains("characters/empty.yaml", ex.Message);
    }

    // ─── AC4: GetAll() 返回全部已注册模板 ───────────────────

    [Fact]
    public void DataRegistry_GetAll_ReturnsAllRegisteredTemplates()
    {
        var registry = new DataRegistry();
        var sources = new Dictionary<string, string>
        {
            ["characters/player.yaml"] = ValidPlayerYaml,
            ["characters/enemies/bandit_leader.yaml"] = ValidEnemyYaml
        };

        _loader.LoadAllTemplates(sources, registry);

        var table = registry.GetTable<CharacterTemplate>()!;
        var all = table.GetAll();
        Assert.Equal(2, all.Count);
        Assert.Equal(2, table.Count);
    }

    // ─── AC5: attribute-tuning.yaml 正确加载 ────────────────

    [Fact]
    public void LoadTuning_ValidYaml_LoadsCorrectValues()
    {
        var config = _loader.LoadTuning(ValidTuningYaml, "balance/attribute-tuning.yaml");

        Assert.Equal(100, config.BaseHp);
        Assert.Equal(20, config.BaseNeiXi);
        Assert.Equal(50, config.AttributeCap);
        Assert.Equal(250, config.MaxTotalAttributes);
        Assert.Equal(9, config.RealmThresholds.Count);
        Assert.Equal(25, config.RealmThresholds[0]);
        Assert.Equal(200, config.RealmThresholds[8]);
        Assert.Equal(0.30f, config.CritRateCap);
        Assert.Equal(2, config.NeiXiRecoveryFloor);
    }

    [Fact]
    public void LoadTuning_BaseHp_CanBeUsedByFormulaEngine()
    {
        var config = _loader.LoadTuning(ValidTuningYaml, "balance/attribute-tuning.yaml");
        // 验证 tuning 中的 base_hp=100 可以传入 FormulaEngine
        int computedMaxHp = FormulaEngine.MaxHp(config.BaseHp, 10, 0);
        Assert.Equal(140, computedMaxHp); // 100 + 10*4 = 140
    }

    // ─── AC6: 同 ID 模板 → 启动时报错 ──────────────────────

    [Fact]
    public void LoadAllTemplates_DuplicateId_ThrowsDataLoadException()
    {
        var registry = new DataRegistry();
        var sources = new Dictionary<string, string>
        {
            ["characters/player.yaml"] = ValidPlayerYaml,
            ["characters/player_dupe.yaml"] = ValidPlayerYaml // 同 ID "player"
        };

        var ex = Assert.Throws<DataLoadException>(() =>
            _loader.LoadAllTemplates(sources, registry));

        Assert.Contains("player", ex.Message);
        Assert.Contains("重复", ex.Message);
    }

    // ─── 边界: 缺少 id 字段 ─────────────────────────────────

    [Fact]
    public void LoadAllTemplates_MissingId_ThrowsDataLoadException()
    {
        var yamlNoId = @"
name: 无名
type: enemy
strength: 10
";
        var registry = new DataRegistry();
        var sources = new Dictionary<string, string>
        {
            ["characters/no_id.yaml"] = yamlNoId
        };

        var ex = Assert.Throws<DataLoadException>(() =>
            _loader.LoadAllTemplates(sources, registry));

        Assert.Contains("id", ex.Message);
    }

    // ─── 边界: Get 不存在的 ID 返回 null ────────────────────

    [Fact]
    public void DataRegistry_GetNonExistentId_ReturnsNull()
    {
        var registry = new DataRegistry();
        var sources = new Dictionary<string, string>
        {
            ["characters/player.yaml"] = ValidPlayerYaml
        };

        _loader.LoadAllTemplates(sources, registry);

        var table = registry.GetTable<CharacterTemplate>()!;
        Assert.Null(table.Get("nonexistent"));
    }

    // ─── 边界: 未注册的表类型返回 null ──────────────────────

    [Fact]
    public void DataRegistry_UnregisteredTable_ReturnsNull()
    {
        var registry = new DataRegistry();
        Assert.Null(registry.GetTable<CharacterTemplate>());
    }
}
