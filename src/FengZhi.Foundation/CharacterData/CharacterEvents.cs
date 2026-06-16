using FengZhi.Foundation.Events;

namespace FengZhi.Foundation.CharacterData;

/// <summary>角色创建事件</summary>
public sealed record CharacterCreatedEvent(string RuntimeId, CharacterType Type, string TemplateId) : GameEvent;

/// <summary>角色移除事件</summary>
public sealed record CharacterRemovedEvent(string RuntimeId, string Reason) : GameEvent;
