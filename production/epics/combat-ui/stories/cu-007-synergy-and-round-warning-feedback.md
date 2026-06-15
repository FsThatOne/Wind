# Story: cu-007 — 协同与回合警戒反馈

> **Epic**: combat-ui
> **类型**: Visual/Feel
> **优先级**: P1 — 战斗节奏与团队感反馈
> **Estimate**: S（约 4h）
> **依赖**: cu-001, cu-003
> **阻塞**: 无
> **ADR 指引**: ADR-0011（浮字与并行动画），ADR-0002（HUD 呈现）
> **GDD 来源**: design/gdd/combat-ui.md §Detailed Design / 协同提示 / 回合计数
> **TR-ID**: TR-combat-ui-???（待 architecture review 写入稳定编号）
> **Control Manifest Version**: 2026-06-10
> **状态**: Ready

## 目标

实现多人战斗中的“协同！”提示和回合计数警戒，让玩家感到队友配合与战斗时间压力。

## 范围

### 包含
- 两名己方角色在同一回合对同一目标选择克制体系招式时显示“协同！”
- “协同！”在目标头顶浮现，金色 + 双拳图标
- 结算时协同提示随伤害弹出一起显示
- 右上角显示「第 [N] 回合」
- 第 12 回合计数变橙色
- 第 14 回合计数变红色

### 不包含
- 协同判定逻辑和收益结算
- 回合上限失败逻辑
- 伤害数字基础样式 → cu-003

## 技术说明

- 协同是否成立由战斗系统事件或结算快照提供，UI 不自行判断收益
- “协同！”可复用浮字池或独立轻量池，但必须避免战斗中频繁分配
- 回合计数来自 `TurnAdvancedEvent` 或 CombatService `CurrentTurn`
- 回合警戒只做视觉提示，不修改战斗节奏
- 并行动画使用 ADR-0011 的 `ParallelCommand` 思路，避免阻塞伤害反馈队列

## 验收标准

- [ ] 同回合同目标协同克制成立时显示“协同！”
- [ ] 协同提示位置跟随目标并与伤害反馈同时可见
- [ ] 非协同场景不会误显示“协同！”
- [ ] HUD 右上角显示当前回合数
- [ ] 第 12 回合变橙色，第 14 回合变红色
- [ ] 回合颜色变化不影响其他 HUD 信息可读性

## QA 手动检查

- **AC-1**：协同提示
  - Setup：两名己方角色同回合用克制招式攻击同一目标
  - Verify：目标头顶出现“协同！”
  - Pass condition：提示时机与结算一致，未遮挡关键伤害数字

- **AC-2**：非协同不误报
  - Setup：两名角色攻击不同目标或使用不克制招式
  - Verify：不出现“协同！”
  - Pass condition：玩家不会被错误提示误导

- **AC-3**：回合警戒
  - Setup：推进到第 12 和第 14 回合
  - Verify：回合计数分别变橙、变红
  - Pass condition：颜色警戒清楚，但不喧宾夺主

## QA Test Cases

**Automated test path**: `tests/integration/combat-ui/combat_ui_synergy_round_warning_test.cs`

Required automated coverage:
- Valid same-round, same-target cooperative counter snapshot displays `协同！`.
- Synergy feedback follows the target and remains readable alongside damage feedback.
- Non-synergy cases do not display false-positive `协同！`.
- HUD displays current turn count.
- Turn 12 switches warning color to orange.
- Turn 14 switches warning color to red.
- Warning color changes do not modify combat pacing or other HUD values.

Manual evidence required:
- Capture `production/qa/evidence/cu-007-synergy-and-round-warning-feedback-evidence.md`.
- Include screenshot or clip for synergy feedback, non-synergy absence, turn 12 orange, and turn 14 red.

## 测试证据路径

`production/qa/evidence/cu-007-synergy-and-round-warning-feedback-evidence.md`

## 依赖关系

- Depends on: cu-001, cu-003
- Unlocks: None
