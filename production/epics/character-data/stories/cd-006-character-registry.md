# Story: cd-006 — Character Registry

> **Epic**: character-data
> **Type**: Integration
> **Priority**: P1 — 运行时角色管理的唯一入口
> **Depends On**: cd-001, cd-002, cd-003, cd-005
> **Blocked By**: cd-005 (需要 CharacterTemplate 加载)
> **ADR Guidance**: ADR-0001 (EventBus 通知), ADR-0003 (DataRegistry 查询)
> **GDD Source**: design/gdd/character-attributes.md §Interactions with Other Systems
> **Control Manifest Version**: 2026-06-10
> **Status**: Done

## Goal

实现 `ICharacterRegistry` 接口，提供角色工厂方法和运行时实例管理，作为下游系统查询角色属性的统一入口。

## Scope

### In Scope
- `ICharacterRegistry` 接口:
  - `CreatePlayer()` → CharacterInstance
  - `CreateCompanion(string id)` → CharacterInstance
  - `CreateEnemy(string id)` → CharacterInstance
  - `GetCharacter(string runtimeId)` → CharacterInstance
  - `GetAllAlive()` → IReadOnlyList<CharacterInstance>
  - `RemoveCharacter(string runtimeId)` — 敌人死亡/同伴离队
- `CharacterInstance` 类:
  - 持有 `CharacterAttributes` + `ModifierStack`
  - 暴露 GDD 定义的全部下游接口: `GetAttackForType(type)`, `GetDefense()`, `GetSpeed()`, `GetCritRate()`, `GetMaxHp()`, `GetMaxNeiXi()`, `GetNeiXiRecovery()`, `GetTotalPower()`
- EventBus 集成:
  - 创建时 → `EventBus.Publish(new CharacterCreatedEvent(id, type))`
  - 移除时 → `EventBus.Publish(new CharacterRemovedEvent(id, reason))`
- 从 `CharacterTemplate` (DataRegistry) 实例化为运行时 `CharacterInstance`

### Out of Scope
- 存档序列化（Platform 层 ISaveable 实现）→ 后续 Story
- 具体成长逻辑 → cd-007
- 战斗中的实时属性变化 → 战斗系统

## Technical Notes

- 路径: `src/FengZhi.Foundation/CharacterData/CharacterRegistry.cs`
- 作为 Godot Autoload 节点注册（全局单例）
- 遵循 ADR-0001: 跨层通知走 EventBus，不直接引用上层
- 遵循 ADR-0001: 事件类型为 `record CharacterCreatedEvent : GameEvent`
- RuntimeId 生成: `"{type}_{templateId}_{guid_short}"`

## Acceptance Criteria

- [ ] **AC1**: `CreatePlayer()` 从 DataRegistry 读取 player 模板，返回正确初始化的 CharacterInstance
- [ ] **AC2**: `CreateEnemy("bandit_swordsman")` 创建敌人实例，五维属性与 YAML 模板一致
- [ ] **AC3**: 创建角色时发布 `CharacterCreatedEvent`，订阅者收到通知
- [ ] **AC4**: `GetCharacter(runtimeId)` 返回对应实例; 不存在的 id 返回 null
- [ ] **AC5**: `RemoveCharacter` 后 `GetAllAlive()` 不再包含该角色
- [ ] **AC6**: `GetAttackForType(MoveType.Gang)` 正确调用 FormulaEngine.F1 并传入力量值

## Test Evidence Path

`tests/Foundation/CharacterData/CharacterRegistryTests.cs`
