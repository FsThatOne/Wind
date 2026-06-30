# Story: ms-006 — Trigger Integration: Jianghu & Dialogue

> **Epic**: misunderstanding-system
> **Status**: Complete (Foundation layer; Feature layer pending external runtime)
> **Last Updated**: 2026-06-30
> **Layer**: Feature
> **Type**: Integration
> **Priority**: P1
> **Estimate**: 2 days
> **Manifest Version**: 2026-06-30
> **Blocked By**: 活江湖层 (#16), 对话系统 (#5)
> **GDD 来源**: design/gdd/misunderstanding-system.md §Core Rules 2, §Interactions with Other Systems, §Edge Cases E6/E9
> **TR-ID**: 待 architecture-review 分配

## Context

误会的三大触发来源（活江湖层事件、对话选择、缺席检测）需要与外部系统的真实 runtime 对接。此 story 实现 `misunderstanding_trigger` 注册、世界事件监听与匹配、对话 `mis_trigger` 标记解析、缺席检测逻辑（含级联熔断器）。依赖活江湖层和对话系统的 runtime 接口实现，当前标记为 Blocked。

**ADR Governing Implementation**: ADR-0014 (Living Jianghu Layer)
**Engine**: Godot 4.7-stable | **Risk**: MEDIUM
**Engine Notes**: 需要与 EventBus（ADR-0001）集成，订阅 `on_world_event` 和 `on_dialogue_choice` 信号。

## Acceptance Criteria

- [ ] AC-1: 活江湖层事件命中已注册的 `misunderstanding_trigger` 时，正确创建 ACTIVE 状态的误会实例，severity 与 trigger 配置一致。（GDD AC1）
- [ ] AC-2: 对话选择标记 `mis_trigger: {npc, severity}` 后，对应 NPC 生成误会实例，source_type=DIALOGUE_CHOICE。（GDD AC2）
- [ ] AC-3: 缺席超过 `absence_trigger_threshold_days` 天后，自动触发缺席误解，source_type=ABSENCE。（GDD AC3）
- [ ] AC-4: Edge Case E6 — 玩家与 NPC 同场景时不触发缺席误解。
- [ ] AC-5: Edge Case E9 — 同一对话选择标记对两个不同 NPC 的 mis_trigger 时，为每个 NPC 独立创建实例。
- [ ] AC-6: 级联熔断器 — 战败后 2 天保护期内，缺席误解触发器被跳过（immunity 机制）。

## Implementation Notes

**Control Manifest Rules (Feature Layer)**:
- Required: 通过 EventBus 订阅 `WorldEventOccurred` 和 `DialogueChoiceMade` 信号。
- Required: trigger 配置为数据驱动（JSON/Resource），不硬编码在代码中。
- Required: 缺席检测需要知道"玩家当前场景"和"NPC 所在场景"——通过 `IScenePresenceQuery` 接口获取。
- Required: 级联熔断器状态（absence_immunity）在存档中持久化。
- Forbidden: 不硬编码具体误会内容——所有触发条件从配置加载。

**GDD 触发来源**:
- 活江湖层事件: `register_misunderstanding_trigger(trigger_config)` → `on_world_event(event)` 匹配
- 对话选择: `on_dialogue_choice(choice)` → 检查 `mis_trigger` 字段
- 缺席: 每日 tick 检查玩家是否离开 NPC 区域超过阈值天数

**Tuning Knobs 相关**:
- `absence_trigger_threshold_days` (默认 3，范围 1-7)

## Files to Create/Modify

- `src/FengZhi.Feature/Misunderstanding/MisunderstandingTriggerService.cs`
- `src/FengZhi.Feature/Misunderstanding/TriggerConfig.cs`
- `src/FengZhi.Feature/Misunderstanding/AbsenceDetector.cs`
- `src/FengZhi.Feature/Misunderstanding/IScenePresenceQuery.cs`

## Test Evidence

**Required evidence**:
- `tests/integration/misunderstanding/MisunderstandingTriggerIntegrationTest.cs`

**Test cases**:
- 注册 trigger → 广播匹配事件 → 实例创建 (state=ACTIVE, severity=配置值)
- 广播不匹配事件 → 无实例创建
- 对话选择带 mis_trigger → 实例创建 (source_type=DIALOGUE_CHOICE)
- 玩家离开 NPC 区域 3 天 → 缺席误解触发
- 玩家与 NPC 同场景 → 不触发缺席
- 同一对话对两 NPC 的 trigger → 两个独立实例
- 战败后 2 天内 → 缺席触发被跳过

## Out of Scope

- 具体误会内容配置（ms-009）
- 透明度 UI 表现（ms-008）
- force_break 对接（ms-007）

## Dependencies

- Depends on: ms-001, ms-002, ms-003; 活江湖层 (#16), 对话系统 (#5)
- Unlocks: ms-009
