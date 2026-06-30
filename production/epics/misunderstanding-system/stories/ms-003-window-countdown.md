# Story: ms-003 — Window Countdown & Permanence

> **Epic**: misunderstanding-system
> **Status**: Ready
> **Last Updated**: 2026-06-30
> **Layer**: Foundation
> **Type**: Logic
> **Priority**: P0
> **Estimate**: 1.5 days
> **Manifest Version**: 2026-06-30
> **GDD 来源**: design/gdd/misunderstanding-system.md §Formulas F2, §Core Rules 5, §Edge Cases E2/E8
> **TR-ID**: 待 architecture-review 分配

## Context

每条活跃误会有一个倒计时窗口（window_remaining），每日 tick 递减。窗口归零后，根据严重度转为 PERMANENT 或触发 force_break → BROKEN。此 story 实现窗口递减逻辑、PERMANENT/BROKEN 状态转换、以及存档补算（跨多日读档时逐日结算）。day_tick 驱动源使用 stub（`ITimeAdvancer` 接口），不依赖活江湖层 runtime。

**ADR Governing Implementation**: 通用架构 + ADR-0004（存档恢复补算）
**Engine**: Godot 4.7-stable | **Risk**: LOW
**Engine Notes**: 纯 C# 逻辑层。存档补算需要知道"当前游戏天数"和"存档时的游戏天数"差值。

## Acceptance Criteria

- [ ] AC-1: 每次 `OnDayAdvanced()` 调用时，所有 ACTIVE/ESCALATED 状态且 window_remaining > 0 的实例递减 1。（GDD AC5）
- [ ] AC-2: window_remaining 降至 0 时，MINOR/MODERATE 实例转为 PERMANENT 状态。（GDD AC5）
- [ ] AC-3: window_remaining 降至 0 时，SEVERE 实例调用 `IForceBreakHandler.ForceBreak(npc_id)` 并转为 BROKEN 状态。（GDD AC6）
- [ ] AC-4: 存档补算：读档时计算跳过天数，逐天执行窗口递减和状态转换（可能在一次补算中连续触发多个定型/诀别）。（GDD AC14）
- [ ] AC-5: Edge Case E2 — 最后一天恶化时，先执行恶化（severity+1, 可能重设 window=3），再执行窗口归零检查。

## Implementation Notes

**Control Manifest Rules (Foundation Layer)**:
- Required: `ITimeAdvancer` 接口抽象 day tick，提供 `OnDayAdvanced()` 回调。本 story 用 stub 驱动测试。
- Required: `IForceBreakHandler` 接口抽象 force_break 调用。本 story 用 mock 验证。
- Required: 存档补算必须逐天模拟（不可简单减法），因为中间天可能触发状态转换。
- Required: 状态转换后自动调用 `recompute_mod`（复用 ms-002 的逻辑）。
- Forbidden: 不可跳过中间天数的逐天结算——GDD E8 明确要求"逐天触发窗口检查"。

**GDD 关键公式引用**:
- F2: on_day_advanced() 递减 + PERMANENT/BROKEN 转换
- Edge Case E8: 存档跳天补算

**Tuning Knobs 相关**:
- `minor_window_days` (默认 7)
- `moderate_window_days` (默认 5)
- `severe_countdown_days` (默认 3)

## Files to Create/Modify

- `src/FengZhi.Foundation/Misunderstanding/MisunderstandingWindowManager.cs`
- `src/FengZhi.Foundation/Misunderstanding/ITimeAdvancer.cs`
- `src/FengZhi.Foundation/Misunderstanding/IForceBreakHandler.cs`

## Test Evidence

**Required evidence**:
- `tests/unit/misunderstanding/MisunderstandingWindowTest.cs`

**Test cases**:
- 创建 window=3 的 MINOR → tick 3 天 → state=PERMANENT
- 创建 window=1 的 SEVERE → tick 1 天 → ForceBreak 被调用 + state=BROKEN
- 创建 window=5 → 模拟存档跳 3 天 → window=2 且中间无状态转换
- 创建 window=2 的 SEVERE → 存档跳 3 天 → 第 2 天即触发 BROKEN（不会跳到第 3 天）
- E2: window=1 + 同天恶化 MINOR→MODERATE → 恶化后 window 仍为 1 → 次日 tick → PERMANENT
- RESOLVED/PERMANENT/BROKEN 实例不参与递减

## Out of Scope

- 活江湖层 `on_day_advanced` 真实事件对接（ms-006）
- force_break 的感情系统真实实现（ms-007）
- 透明度升级（ms-004 负责，虽然也在 day tick 中执行，但逻辑独立）

## Dependencies

- Depends on: ms-001, ms-002（recompute_mod）
- Unlocks: ms-006, ms-007
