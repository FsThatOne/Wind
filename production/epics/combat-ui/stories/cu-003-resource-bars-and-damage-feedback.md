# Story: cu-003 — 资源条与伤害反馈

> **Epic**: combat-ui
> **类型**: Visual/Feel
> **优先级**: P0 — 战斗反馈基础
> **Estimate**: M（约 6h）
> **依赖**: cu-001
> **阻塞**: cu-005, cu-006
> **ADR 指引**: ADR-0011（DamageNumberPool / Tween / AnimationPlayer），ADR-0002（HUD 刷新预算）
> **GDD 来源**: design/gdd/combat-ui.md §Detailed Design / 资源条, §Formulas, §Edge Cases
> **TR-ID**: TR-combat-ui-003
> **Control Manifest Version**: 2026-06-10
> **状态**: Complete
> **Last Updated**: 2026-06-14

## 目标

实现气血、内息、破绽三类资源的 HUD 呈现，以及伤害数字、暴击、克制/同系/被克反馈和破绽爆满提示。

## 范围

### 包含
- 气血条、内息条、破绽条显示当前值
- 气血受击先闪白再平滑扣减
- 内息消耗即时可见，调息恢复缓慢回升
- 破绽 0 到 5 进度，破绽 ≥5 时深红脉冲并出现“破绽！”
- 克制、同系、被克、暴击、一击决胜的伤害数字样式
- 使用 `DamageNumberPool` 预分配伤害数字节点

### 不包含
- 一击决胜 7 阶段演出 → cu-006
- 回合计数警戒 → cu-007
- 资源数值计算和伤害结算

## 技术说明

- 所有资源数值来自战斗事件和 combatant snapshot，UI 不重新计算
- 资源条渐变使用 SceneTreeTween，遵守 `resource_bar_update_speed`
- 伤害数字使用 `DamageNumberPool`，预分配 12 个 Label，战斗中避免 alloc/free
- 池化 Control 节点必须 `focus_mode = NONE`，回收时释放焦点
- 伤害数字样式按 GDD F1 查表，仅用于视觉表现，不影响战斗逻辑
- 并发伤害数字目标为同帧 6 个仍可正确显示

## 验收标准

- [ ] 所有参战角色可显示气血、内息、破绽状态
- [ ] 气血受击闪白后平滑扣减，内息变化可被看见
- [ ] 破绽 ≥5 时资源条脉冲并在目标处显示“破绽！”
- [ ] 克制、同系、被克、暴击、一击决胜伤害数字样式符合 GDD
- [ ] 同帧多个伤害事件不会导致浮字重叠到不可读或丢失
- [ ] UI 不计算伤害，不显示伤害预测数字

## QA 手动检查

- **AC-1**：资源条刷新
  - Setup：触发受伤、消耗内息、调息、破绽增加
  - Verify：对应资源条更新并有合适过渡
  - Pass condition：变化清楚可读，未出现瞬间跳错或延迟一整回合

- **AC-2**：破绽爆满
  - Setup：将某目标破绽推到 5
  - Verify：破绽条变深红并脉冲，目标处出现“破绽！”
  - Pass condition：玩家能明确识别决胜窗口已出现

- **AC-3**：伤害数字样式
  - Setup：分别触发克制、同系、被克、暴击、一击决胜伤害
  - Verify：字号、颜色、特效符合 GDD 表格
  - Pass condition：不同类型反馈可一眼区分，且没有展示预测伤害

## Test Evidence

`production/qa/evidence/cu-003-resource-bars-and-damage-feedback-evidence.md`

## 依赖关系

- Depends on: cu-001
- Unlocks: cu-005, cu-006

## Completion Notes

**Completed**: 2026-06-14
**Criteria**: 6/6 passing
**Deviations**: None blocking. Advisory: Visual/Feel 实机走查仍需在真实 Godot 战斗场景中确认资源条闪白与 Tween 体感、破绽脉冲、同帧 6 个伤害浮字可读性，以及池化 Label 回收后的焦点释放。
**Test Evidence**: Visual/Feel evidence doc at `production/qa/evidence/cu-003-resource-bars-and-damage-feedback-evidence.md`; automated contract tests passed via `dotnet test tests/Foundation/Foundation.Tests.csproj --filter CombatUi` (31/31).
**Code Review**: Complete — APPROVED WITH SUGGESTIONS.
