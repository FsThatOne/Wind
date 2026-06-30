# Story: ms-007 — Romance Floor Protection & Force Break

> **Epic**: misunderstanding-system
> **Status**: Complete (Foundation layer; Feature layer pending Romance runtime)
> **Last Updated**: 2026-06-30
> **Layer**: Feature
> **Type**: Integration
> **Priority**: P1
> **Estimate**: 1 day
> **Manifest Version**: 2026-06-30
> **Blocked By**: 感情系统 (#13)
> **GDD 来源**: design/gdd/misunderstanding-system.md §Interactions - 地板保护规则, §Core Rules 3, §Edge Cases E4
> **TR-ID**: 待 architecture-review 分配

## Context

感情系统的里程碑机制为 NPC 态度设定了"地板"——普通误会不应击穿这个地板。同时，SEVERE 误会窗口归零时需要真正调用感情系统的 `force_break()` 触发诀别。此 story 实现地板保护逻辑（钳位 effective_mod）和 force_break 集成，以及地板保护下的独立对话效果层。

**ADR Governing Implementation**: ADR-0015 (Romance System)
**Engine**: Godot 4.7-stable | **Risk**: LOW
**Engine Notes**: 需要感情系统的 `IRomanceService` 接口提供 `GetMilestoneFloor` 和 `ForceBreak`。

## Acceptance Criteria

- [ ] AC-1: 施加 misunderstanding_mod 前检查里程碑地板：`effective_mod = max(mod, floor - current_score)`，确保态度不低于 floor。（GDD AC12）
- [ ] AC-2: SEVERE 误会窗口归零时调用 `IRomanceService.ForceBreak(npc_id)`，无视地板保护。（GDD AC6）
- [ ] AC-3: Edge Case E4 — `force_break()` 与正面里程碑在同一 tick 时，force_break 优先级最高。
- [ ] AC-4: 地板保护下（mod 被钳位为 0）时，误会仍通过独立对话效果层产生影响（措辞回退 + 部分选项锁定标记）。
- [ ] AC-5: NPC 已处于 M_BREAK 状态时，force_break 不重复调用（幂等性）。

## Implementation Notes

**Control Manifest Rules (Feature Layer)**:
- Required: `IRomanceService` 接口定义 `GetMilestoneFloor(npcId) -> int` 和 `ForceBreak(npcId)`。
- Required: 地板保护为 before-hook 模式——在 SetMisunderstandingMod 调用前拦截并钳位。
- Required: 独立对话效果层通过 `IDialogueEffectWriter.SetMisunderstandingEffect(npcId, effectLevel)` 接口写出。
- Required: force_break 调用后，该 NPC 的所有活跃误会应立即转为 BROKEN。
- Forbidden: 地板保护不可硬编码数值——必须从 IRomanceService 实时查询。

**GDD 关键规则**:
```
before_apply_misunderstanding_mod(npc_id, mod):
    floor = romance.get_milestone_floor(npc_id)
    current_score = npc_state.get_attitude_score(npc_id)
    effective_mod = max(mod, floor - current_score)
    # 例外：SEVERE 触发 force_break 时无视地板
```

## Files to Create/Modify

- `src/FengZhi.Feature/Misunderstanding/FloorProtectionService.cs`
- `src/FengZhi.Feature/Misunderstanding/IRomanceService.cs`
- `src/FengZhi.Feature/Misunderstanding/IDialogueEffectWriter.cs`
- `src/FengZhi.Feature/Misunderstanding/ForceBreakIntegration.cs`

## Test Evidence

**Required evidence**:
- `tests/integration/misunderstanding/FloorProtectionTest.cs`
- `tests/integration/misunderstanding/ForceBreakIntegrationTest.cs`

**Test cases**:
- 里程碑 floor=5, current_score=5, mod=-2 → effective_mod=0（不击穿）
- 里程碑 floor=3, current_score=5, mod=-2 → effective_mod=-2（正常下降）
- SEVERE window=0 → force_break 被调用，无视 floor
- force_break 与正面里程碑同 tick → force_break 生效
- 地板钳位为 0 时 → DialogueEffectWriter 仍被调用写入 effect
- NPC 已 M_BREAK → force_break 不重复调用

## Out of Scope

- 感情系统的具体里程碑实现
- NPC 态度公式的完整实现
- 诀别叙事演出（ms-008/cutscene-system）

## Dependencies

- Depends on: ms-001, ms-002, ms-003; 感情系统 (#13)
- Unlocks: ms-008, ms-009
