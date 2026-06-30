# Story: ms-001 — Misunderstanding Data Model & State Machine

> **Epic**: misunderstanding-system
> **Status**: Ready
> **Last Updated**: 2026-06-30
> **Layer**: Foundation
> **Type**: Logic
> **Priority**: P0
> **Estimate**: 1.5 days
> **Manifest Version**: 2026-06-30
> **GDD 来源**: design/gdd/misunderstanding-system.md §Core Rules, §States and Transitions, §Edge Cases
> **TR-ID**: 待 architecture-review 分配

## Context

误会系统的基础数据模型和状态机是所有后续 story 的共享核心。此 story 建立 `MisunderstandingInstance` 数据结构、5 状态状态机（DORMANT → ACTIVE → ESCALATED / RESOLVED / PERMANENT → BROKEN）和 severity 升级逻辑。纯逻辑层实现，无外部系统依赖，可独立测试。

**ADR Governing Implementation**: 通用架构（无专属 ADR），遵循分层规范
**Engine**: Godot 4.7-stable | **Risk**: LOW
**Engine Notes**: 纯 C# 逻辑层，不涉及引擎 API。

## Acceptance Criteria

- [ ] AC-1: 定义 `MisunderstandingInstance` 数据结构，字段覆盖 GDD 中的 id、target_npc、source_type、severity、transparency、initial_window、window_remaining、escalation_count、created_chapter、created_day、resolution_conditions、state。
- [ ] AC-2: 实现 5 状态状态机（DORMANT, ACTIVE, ESCALATED, RESOLVED, PERMANENT, BROKEN），所有合法转换路径可执行，非法转换抛出明确错误。
- [ ] AC-3: severity 升级逻辑：恶化事件触发时 MINOR→MODERATE→SEVERE 正确递进；MODERATE→SEVERE 时 window_remaining 被钳制为 min(current, 3)。（GDD AC10）
- [ ] AC-4: M_BREAK 状态下的 NPC 不可注册新误会，尝试注册返回拒绝结果。（GDD AC13）
- [ ] AC-5: 枚举定义完整：SourceType(JIANGHU_EVENT, DIALOGUE_CHOICE, ABSENCE)、Severity(MINOR, MODERATE, SEVERE)、Transparency(HIDDEN, HINTED, PERCEIVED, URGENT)、MisunderstandingState(DORMANT, ACTIVE, ESCALATED, RESOLVED, PERMANENT, BROKEN)。

## Implementation Notes

**Control Manifest Rules (Foundation Layer)**:
- Required: 数据模型为纯 C# POCO，不依赖 Godot Node 或 Resource。
- Required: 状态机使用显式枚举 + 转换方法，禁止字符串状态。
- Required: severity_to_mod 映射作为 static 方法提供（MINOR→-1, MODERATE→-2, SEVERE→-2）。
- Forbidden: 不可在此 story 中引入任何外部系统接口调用（mod 写入、force_break 等均不在此实现）。
- Guardrail: 状态转换失败必须产生明确的错误信息（InvalidStateTransitionException 或 Result pattern）。

**GDD 关键公式引用**:
- F4 恶化判定：escalation_count++, severity 递升, SEVERE 时 window 钳制为 3。
- Edge Cases E2: 最后一天恶化 → 先执行恶化再检查窗口归零。
- Edge Cases E5: M_BREAK 下不注册新误会。

## Files to Create/Modify

- `src/FengZhi.Foundation/Misunderstanding/MisunderstandingInstance.cs`
- `src/FengZhi.Foundation/Misunderstanding/MisunderstandingEnums.cs`
- `src/FengZhi.Foundation/Misunderstanding/MisunderstandingStateMachine.cs`
- `src/FengZhi.Foundation/Misunderstanding/IMisunderstandingRegistry.cs`

## Test Evidence

**Required evidence**:
- `tests/unit/misunderstanding/MisunderstandingDataModelTest.cs`
- `tests/unit/misunderstanding/MisunderstandingStateMachineTest.cs`

**Test cases**:
- 构造 MisunderstandingInstance 全字段赋值后读取一致
- 合法状态转换（DORMANT→ACTIVE, ACTIVE→ESCALATED, ACTIVE→RESOLVED, ACTIVE→PERMANENT, ESCALATED→BROKEN 等）成功
- 非法状态转换（RESOLVED→ACTIVE, BROKEN→任意）失败
- MINOR 恶化后 severity=MODERATE, MODERATE 恶化后 severity=SEVERE 且 window ≤ 3
- M_BREAK NPC 注册新误会返回拒绝

## Out of Scope

- mod 计算与写入 NPC 状态（ms-002）
- 窗口递减逻辑（ms-003）
- 透明度升级逻辑（ms-004）
- 澄清/解除逻辑（ms-005）
- 外部系统触发对接（ms-006）

## Dependencies

- Depends on: None
- Unlocks: ms-002, ms-003, ms-004, ms-005
