using Xunit;
using FengZhi.Foundation.CharacterData;
using FengZhi.Foundation.Data;
using FengZhi.Foundation.Events;

namespace FengZhi.Tests.Foundation.CharacterData;

public class CharacterRegistryTests
{
    private readonly DataRegistry _dataRegistry;
    private readonly EventBus _eventBus;
    private readonly CharacterRegistry _registry;

    private const string PlayerYaml = @"
id: player
name: 风止·主角
type: player
base_hp: 100
base_neixi: 20
base_stagger_threshold: 5
strength: 8
agility: 9
inner_power: 10
insight: 7
constitution: 8
base_attack: 10
scaling_factor: 1.0
primary_style: gang
moves:
  - basic_attack
";

    private const string EnemyYaml = @"
id: bandit_swordsman
name: 匪徒·刀客
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

    private const string CompanionYaml = @"
id: companion_lin
name: 林姑娘
type: companion
base_hp: 90
base_neixi: 25
base_stagger_threshold: 5
strength: 6
agility: 10
inner_power: 12
insight: 9
constitution: 7
base_attack: 8
scaling_factor: 1.1
primary_style: rou
moves:
  - soft_palm
";

    public CharacterRegistryTests()
    {
        _dataRegistry = new DataRegistry();
        _eventBus = new EventBus();

        var loader = new CharacterConfigLoader();
        var sources = new Dictionary<string, string>
        {
            ["characters/player.yaml"] = PlayerYaml,
            ["characters/enemies/bandit_swordsman.yaml"] = EnemyYaml,
            ["characters/companions/companion_lin.yaml"] = CompanionYaml
        };
        loader.LoadAllTemplates(sources, _dataRegistry);

        _registry = new CharacterRegistry(_dataRegistry, _eventBus);
    }

    // ─── AC1: CreatePlayer 正确初始化 ────────────────────────

    [Fact]
    public void CreatePlayer_ReturnsCorrectlyInitializedInstance()
    {
        var player = _registry.CreatePlayer();

        Assert.NotNull(player);
        Assert.Equal(CharacterType.Player, player.Type);
        Assert.Equal("player", player.TemplateId);
        Assert.StartsWith("player_player_", player.RuntimeId);
        Assert.Equal(8, player.Attributes.Strength);
        Assert.Equal(9, player.Attributes.Agility);
        Assert.Equal(10, player.Attributes.InnerPower);
        Assert.Equal(100, player.Attributes.MaxHp);
        Assert.Equal(100, player.Attributes.CurrentHp);
        Assert.True(player.IsAlive);
    }

    // ─── AC2: CreateEnemy 五维与 YAML 一致 ───────────────────

    [Fact]
    public void CreateEnemy_AttributesMatchYamlTemplate()
    {
        var enemy = _registry.CreateEnemy("bandit_swordsman");

        Assert.Equal(CharacterType.Enemy, enemy.Type);
        Assert.Equal("bandit_swordsman", enemy.TemplateId);
        Assert.Equal(12, enemy.Attributes.Strength);
        Assert.Equal(6, enemy.Attributes.Agility);
        Assert.Equal(5, enemy.Attributes.InnerPower);
        Assert.Equal(5, enemy.Attributes.Insight);
        Assert.Equal(10, enemy.Attributes.Constitution);
        Assert.Equal(80, enemy.Attributes.MaxHp);
        Assert.Equal(15, enemy.Attributes.MaxNeiXi);
        Assert.Equal(4, enemy.Attributes.StaggerThreshold);
    }

    [Fact]
    public void GetSpeed_UsesAgilityPlusSpeedModifierWithLowerBound()
    {
        var player = _registry.CreatePlayer();

        Assert.Equal(9, player.GetSpeed());

        player.Modifiers.Add(new AttributeModifier
        {
            Source = "test:light_boots",
            Layer = ModifierLayer.SemiPermanent,
            Attribute = AttributeType.Speed,
            Value = 3
        });
        Assert.Equal(12, player.GetSpeed());

        player.Modifiers.Add(new AttributeModifier
        {
            Source = "test:heavy_wound",
            Layer = ModifierLayer.Temporary,
            Attribute = AttributeType.Speed,
            Value = -30
        });
        Assert.Equal(1, player.GetSpeed());
    }

    // ─── AC3: 创建角色时发布 CharacterCreatedEvent ──────────

    [Fact]
    public void CreatePlayer_PublishesCharacterCreatedEvent()
    {
        CharacterCreatedEvent? received = null;
        _eventBus.Subscribe<CharacterCreatedEvent>(e => received = e);

        var player = _registry.CreatePlayer();

        Assert.NotNull(received);
        Assert.Equal(player.RuntimeId, received!.RuntimeId);
        Assert.Equal(CharacterType.Player, received.Type);
        Assert.Equal("player", received.TemplateId);
    }

    [Fact]
    public void CreateEnemy_PublishesCharacterCreatedEvent()
    {
        CharacterCreatedEvent? received = null;
        _eventBus.Subscribe<CharacterCreatedEvent>(e => received = e);

        var enemy = _registry.CreateEnemy("bandit_swordsman");

        Assert.NotNull(received);
        Assert.Equal(enemy.RuntimeId, received!.RuntimeId);
        Assert.Equal(CharacterType.Enemy, received.Type);
    }

    // ─── AC4: GetCharacter 查询 ──────────────────────────────

    [Fact]
    public void GetCharacter_ExistingId_ReturnsInstance()
    {
        var player = _registry.CreatePlayer();
        var found = _registry.GetCharacter(player.RuntimeId);

        Assert.NotNull(found);
        Assert.Same(player, found);
    }

    [Fact]
    public void GetCharacter_NonExistentId_ReturnsNull()
    {
        Assert.Null(_registry.GetCharacter("nonexistent_id"));
    }

    // ─── AC5: RemoveCharacter 后 GetAllAlive 不再包含 ────────

    [Fact]
    public void RemoveCharacter_NoLongerInGetAllAlive()
    {
        var enemy = _registry.CreateEnemy("bandit_swordsman");
        _registry.RemoveCharacter(enemy.RuntimeId);

        var alive = _registry.GetAllAlive();
        Assert.DoesNotContain(alive, c => c.RuntimeId == enemy.RuntimeId);
    }

    [Fact]
    public void RemoveCharacter_PublishesCharacterRemovedEvent()
    {
        CharacterRemovedEvent? received = null;
        _eventBus.Subscribe<CharacterRemovedEvent>(e => received = e);

        var enemy = _registry.CreateEnemy("bandit_swordsman");
        _registry.RemoveCharacter(enemy.RuntimeId, "defeated");

        Assert.NotNull(received);
        Assert.Equal(enemy.RuntimeId, received!.RuntimeId);
        Assert.Equal("defeated", received.Reason);
    }

    // ─── AC6: GetAttackForType 正确调用 FormulaEngine.F1 ─────

    [Fact]
    public void GetAttackForType_Gang_UsesStrength()
    {
        var enemy = _registry.CreateEnemy("bandit_swordsman");
        // F1: base_attack + primary_attr * scaling_factor + modifier_sum
        // = 12 + 12 * 0.9 + 0 = 12 + 10.8 = 22.8 → 23
        int attack = enemy.GetAttackForType(MoveType.Gang);
        Assert.Equal(23, attack);
    }

    [Fact]
    public void GetAttackForType_WithModifier_IncludesModifierSum()
    {
        var player = _registry.CreatePlayer();
        player.Modifiers.Add(new AttributeModifier
        {
            Source = "weapon_buff",
            Layer = ModifierLayer.Temporary,
            Attribute = AttributeType.Attack,
            Value = 5
        });

        // F1: 10 + 8 * 1.0 + 5 = 23
        int attack = player.GetAttackForType(MoveType.Gang);
        Assert.Equal(23, attack);
    }

    // ─── 边界: 不存在的模板 ID → 异常 ──────────────────────

    [Fact]
    public void CreateEnemy_NonExistentTemplate_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => _registry.CreateEnemy("nonexistent"));
    }

    // ─── 边界: GetAllAlive 过滤死亡角色 ─────────────────────

    [Fact]
    public void GetAllAlive_ExcludesDeadCharacters()
    {
        var enemy = _registry.CreateEnemy("bandit_swordsman");
        enemy.Attributes.ApplyDamage(9999); // 击杀

        var alive = _registry.GetAllAlive();
        Assert.DoesNotContain(alive, c => c.RuntimeId == enemy.RuntimeId);
    }

    // ─── 边界: 多角色管理 ───────────────────────────────────

    [Fact]
    public void CreateMultipleCharacters_AllTracked()
    {
        var player = _registry.CreatePlayer();
        var companion = _registry.CreateCompanion("companion_lin");
        var enemy = _registry.CreateEnemy("bandit_swordsman");

        var alive = _registry.GetAllAlive();
        Assert.Equal(3, alive.Count);
    }

    // ─── EventBus 基础验证 ──────────────────────────────────

    [Fact]
    public void EventBus_Unsubscribe_StopsReceivingEvents()
    {
        int callCount = 0;
        var unsub = _eventBus.Subscribe<CharacterCreatedEvent>(_ => callCount++);

        _registry.CreatePlayer();
        Assert.Equal(1, callCount);

        unsub();
        _registry.CreateEnemy("bandit_swordsman");
        Assert.Equal(1, callCount); // 不再接收
    }
}
