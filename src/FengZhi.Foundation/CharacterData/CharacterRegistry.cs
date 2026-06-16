using FengZhi.Foundation.Data;
using FengZhi.Foundation.Events;

namespace FengZhi.Foundation.CharacterData;

/// <summary>
/// 角色注册表接口。运行时角色管理的唯一入口。
/// </summary>
public interface ICharacterRegistry
{
    CharacterInstance CreatePlayer();
    CharacterInstance CreateCompanion(string templateId);
    CharacterInstance CreateEnemy(string templateId);
    CharacterInstance? GetCharacter(string runtimeId);
    IReadOnlyList<CharacterInstance> GetAllAlive();
    void RemoveCharacter(string runtimeId, string reason = "defeated");
}

/// <summary>
/// CharacterRegistry 实现。
/// 从 DataRegistry 读取模板 → 实例化 CharacterInstance → EventBus 通知。
/// </summary>
public sealed class CharacterRegistry : ICharacterRegistry
{
    private readonly DataRegistry _dataRegistry;
    private readonly IEventBus _eventBus;
    private readonly Dictionary<string, CharacterInstance> _characters = new();

    public CharacterRegistry(DataRegistry dataRegistry, IEventBus eventBus)
    {
        _dataRegistry = dataRegistry;
        _eventBus = eventBus;
    }

    public CharacterInstance CreatePlayer()
    {
        return CreateFromTemplate("player", CharacterType.Player);
    }

    public CharacterInstance CreateCompanion(string templateId)
    {
        return CreateFromTemplate(templateId, CharacterType.Companion);
    }

    public CharacterInstance CreateEnemy(string templateId)
    {
        return CreateFromTemplate(templateId, CharacterType.Enemy);
    }

    public CharacterInstance? GetCharacter(string runtimeId)
    {
        return _characters.TryGetValue(runtimeId, out var c) ? c : null;
    }

    public IReadOnlyList<CharacterInstance> GetAllAlive()
    {
        return _characters.Values.Where(c => c.IsAlive).ToList();
    }

    public void RemoveCharacter(string runtimeId, string reason = "defeated")
    {
        if (_characters.Remove(runtimeId))
        {
            _eventBus.Publish(new CharacterRemovedEvent(runtimeId, reason));
        }
    }

    // ─── 私有 ─────────────────────────────────────────────────

    private CharacterInstance CreateFromTemplate(string templateId, CharacterType type)
    {
        var table = _dataRegistry.GetTable<CharacterTemplate>()
            ?? throw new InvalidOperationException("CharacterTemplate 数据表尚未注册到 DataRegistry");

        var template = table.Get(templateId)
            ?? throw new ArgumentException($"角色模板 '{templateId}' 不存在");

        var attrs = new CharacterAttributes
        {
            MaxHp = template.BaseHp,
            MaxNeiXi = template.BaseNeiXi,
            StaggerThreshold = template.BaseStaggerThreshold,
            Strength = template.Strength,
            Agility = template.Agility,
            InnerPower = template.InnerPower,
            Insight = template.Insight,
            Constitution = template.Constitution,
            BaseAttack = template.BaseAttack,
            ScalingFactor = template.ScalingFactor,
            Type = type,
            TemplateId = templateId
        };
        attrs.ResetForCombat();

        var modifiers = new ModifierStack();
        var runtimeId = GenerateRuntimeId(type, templateId);
        var instance = new CharacterInstance(runtimeId, attrs, modifiers);

        _characters[runtimeId] = instance;
        _eventBus.Publish(new CharacterCreatedEvent(runtimeId, type, templateId));

        return instance;
    }

    private static string GenerateRuntimeId(CharacterType type, string templateId)
    {
        var shortGuid = Guid.NewGuid().ToString("N")[..8];
        return $"{type.ToString().ToLowerInvariant()}_{templateId}_{shortGuid}";
    }
}
