# Story: ms-004 — Transparency Progression

> **Epic**: misunderstanding-system
> **Status**: Ready
> **Last Updated**: 2026-06-30
> **Layer**: Foundation
> **Type**: Logic
> **Priority**: P1
> **Estimate**: 1 day
> **Manifest Version**: 2026-06-30
> **GDD 来源**: design/gdd/misunderstanding-system.md §Core Rules 4, §Formulas F3
> **TR-ID**: 待 architecture-review 分配

## Context

透明度系统是误会系统的"玩家感知层逻辑"——它决定玩家在什么时候、以什么程度感知到误会的存在。4 阶段升级（HIDDEN → HINTED → PERCEIVED → URGENT）由时间驱动，并在特定阈值时通知外部表现系统（通过接口抽象）。此 story 实现纯升级逻辑和通知触发，不实现具体 UI 表现。

**ADR Governing Implementation**: ADR-0012（透明度信号规则），但本 story 仅实现逻辑层
**Engine**: Godot 4.7-stable | **Risk**: LOW
**Engine Notes**: 纯 C# 逻辑层。通知通过 `ITransparencySignalEmitter` 接口抽象。

## Acceptance Criteria

- [ ] AC-1: 创建后初始透明度为 HIDDEN。经过 `hidden_duration_days`（默认 1 天）后自动升级为 HINTED，并触发 `ITransparencySignalEmitter.EmitHint(npc_id)`。（GDD AC7）
- [ ] AC-2: HINTED 阶段，当 `days_elapsed >= initial_window * perceived_threshold_ratio`（默认 0.5）时升级为 PERCEIVED，并触发 `ITransparencySignalEmitter.EmitPerceived(npc_id)`。（GDD AC7）
- [ ] AC-3: PERCEIVED 阶段，当 `window_remaining <= 3` 时升级为 URGENT，并触发 `ITransparencySignalEmitter.EmitUrgent(npc_id)`。（GDD AC7）
- [ ] AC-4: 玩家与该 NPC 交互时（`OnPlayerInteraction(npc_id)` 调用），若当前为 HIDDEN 可提前升级为 HINTED。
- [ ] AC-5: 透明度升级为单向递进（不会回退），且每个阶段的通知只触发一次。

## Implementation Notes

**Control Manifest Rules (Foundation Layer)**:
- Required: `ITransparencySignalEmitter` 接口定义 emit 方法，本 story 用 mock 验证。
- Required: 透明度检查可在 day tick 中执行，也可在玩家交互事件中执行（双触发路径）。
- Required: 阈值参数从 `MisunderstandingConfig` 读取（`hidden_duration_days`, `perceived_threshold_ratio`）。
- Forbidden: 不实现 UI 渲染或 shader 效果（ms-008 负责）。
- Forbidden: 不回退透明度（单向 progression）。

**GDD 关键公式引用**:
- F3: check_transparency_upgrade 逻辑
- HIDDEN 持续 1 天 → HINTED
- HINTED + 半窗口 → PERCEIVED
- PERCEIVED + window_remaining ≤ 3 → URGENT

**Tuning Knobs 相关**:
- `hidden_duration_days` (默认 1，范围 0-3)
- `perceived_threshold_ratio` (默认 0.5，范围 0.3-0.8)

## Files to Create/Modify

- `src/FengZhi.Foundation/Misunderstanding/TransparencyProgressionManager.cs`
- `src/FengZhi.Foundation/Misunderstanding/ITransparencySignalEmitter.cs`

## Test Evidence

**Required evidence**:
- `tests/unit/misunderstanding/TransparencyProgressionTest.cs`

**Test cases**:
- 创建实例 → transparency=HIDDEN
- tick 1 天 → transparency=HINTED, EmitHint 被调用一次
- initial_window=6 → tick 3 天（过半窗口）→ PERCEIVED, EmitPerceived 调用一次
- PERCEIVED + window_remaining=3 → URGENT, EmitUrgent 调用一次
- 已在 URGENT 阶段 → 再次检查不重复触发通知
- 玩家交互时 HIDDEN → HINTED 提前升级
- hidden_duration_days=0 时，创建即为 HINTED

## Out of Scope

- UI 表现（称呼回退、关系面板标记、脉动效果）→ ms-008
- 音频信号实现 → ms-008
- 对话系统 inject_transparency_signal 真实调用 → ms-006/ms-008

## Dependencies

- Depends on: ms-001
- Unlocks: ms-008
