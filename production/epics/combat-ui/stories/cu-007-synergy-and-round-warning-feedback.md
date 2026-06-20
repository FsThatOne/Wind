# Story: cu-007 — 协同与回合警戒反馈

> **Epic**: combat-ui
> **类型**: Visual/Feel
> **优先级**: P1 — 战斗节奏与团队感反馈
> **Estimate**: S（约 4h）
> **依赖**: cu-001, cu-003
> **阻塞**: 无
> **ADR 指引**: ADR-0011（浮字与并行动画），ADR-0002（HUD 呈现）
> **GDD 来源**: design/gdd/combat-ui.md §Detailed Design / 协同提示 / 回合计数
> **TR-ID**: TR-combat-ui-007
> **Control Manifest Version**: 2026-06-10
> **状态**: Complete
> **Last Updated**: 2026-06-18

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
- Manifest rules（manifest 2026-06-10）：Presentation/CombatUi MUST NOT 自行计算协同收益或伤害；协同成立与回合数必须来自 `TurnAdvancedEvent` / 战斗结算快照；浮字与回合警戒动画使用 ADR-0011 的 `ParallelCommand`，不得阻塞主队列。
- Performance：协同浮字复用现有伤害浮字池或同等轻量池，不在战斗循环中分配；回合计数颜色仅做 `modulate` 切换，不做逐帧更新。预期无可测性能影响（无新分配、无每帧 work）。

## 验收标准

- [x] 同回合同目标协同克制成立时显示“协同！”
- [x] 协同提示位置跟随目标并与伤害反馈同时可见
- [x] 非协同场景不会误显示“协同！”
- [x] HUD 右上角显示当前回合数
- [x] 第 12 回合变橙色，第 14 回合变红色
- [x] 回合颜色变化不影响其他 HUD 信息可读性

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

## Completion Notes

- **Completed**: 2026-06-18
- **Criteria**: 6/6 通过 — 全部 AC 由 [combat_ui_synergy_round_warning_test.cs](../../../../tests/integration/combat-ui/combat_ui_synergy_round_warning_test.cs) 7 个 fact 覆盖；4 张 harness 截图捕获 Visual/Feel 表现
- **Deviations**:
  - OUT OF SCOPE: 在 [Combat/BattleEventBus.cs](../../../../src/FengZhi.Foundation/Combat/BattleEventBus.cs) 新增 `SynergyDeclaredEvent` 作为 UI 入站契约（避免 UI 自判收益）
  - OUT OF SCOPE: 扩展 `prototypes/sprint5-combat-ui-harness` 加入 [Sprint5CombatUiAdapterFixtures.cs](../../../../prototypes/sprint5-combat-ui-harness/scripts/testdata/Sprint5CombatUiAdapterFixtures.cs) + cu-007 panel 用于 evidence 采集；harness move-selection pipeline 未受影响
- **Test Evidence**: [cu-007-synergy-and-round-warning-feedback-evidence.md](../../../qa/evidence/cu-007-synergy-and-round-warning-feedback-evidence.md) — 4 张截图 + 自动化覆盖 7/7
- **Code Review**: APPROVED (lean mode, 2026-06-18) — 3 项 INFO suggestion 已记入 [tech-debt-register.md](../../../../docs/tech-debt-register.md)（string→enum tokens、synergy dedup、harness panel 长期归属）
