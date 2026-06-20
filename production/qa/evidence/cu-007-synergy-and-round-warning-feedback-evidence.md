# cu-007 — 协同与回合警戒反馈 Evidence

> **Story**: [cu-007-synergy-and-round-warning-feedback.md](../../epics/combat-ui/stories/cu-007-synergy-and-round-warning-feedback.md)
> **TR-ID**: TR-combat-ui-007
> **Status**: Captured (2026-06-18) — 自动化通过 + 4 张 harness 截图就位

## Capture Checklist

- [x] AC-1：协同提示出现 — 同回合两名己方角色对同一目标使用克制体系招式（[cu-007-synergy-gold-double-fist.png](media/cu-007-synergy-gold-double-fist.png)）
- [x] AC-2：非协同场景未误显示『协同！』（[cu-007-no-synergy-baseline.png](media/cu-007-no-synergy-baseline.png)）
- [x] AC-3a：第 12 回合 HUD 计数变橙（[cu-007-round-12-caution-orange.png](media/cu-007-round-12-caution-orange.png)）
- [x] AC-3b：第 14 回合 HUD 计数变红（[cu-007-round-14-critical-red.png](media/cu-007-round-14-critical-red.png)）
- [x] AC-3c：颜色变化期间 HP/内息/破绽与伤害浮字未受影响（自动化覆盖：[`TurnWarningTransition_DoesNotMutateResourcesOrDamageNumbers`](../../../tests/integration/combat-ui/combat_ui_synergy_round_warning_test.cs)）

## Capture Targets

| 场景 | 工具 | Fixture | 截图 |
|------|------|---------|------|
| Synergy gold double-fist | `prototypes/sprint5-combat-ui-harness` cu-007 panel | `synergy_gold` (RoundStart 3 → DamageDealt → SynergyDeclared) | [cu-007-synergy-gold-double-fist.png](media/cu-007-synergy-gold-double-fist.png) |
| Non-synergy null state | 同上 | `no_synergy_baseline` (RoundStart 3 → DamageDealt 单源) | [cu-007-no-synergy-baseline.png](media/cu-007-no-synergy-baseline.png) |
| Round 12 caution | 同上 | `round_12_caution` (RoundStart 11 → RoundStart 12) | [cu-007-round-12-caution-orange.png](media/cu-007-round-12-caution-orange.png) |
| Round 14 critical | 同上 | `round_14_critical` (RoundStart 12 → RoundStart 14) | [cu-007-round-14-critical-red.png](media/cu-007-round-14-critical-red.png) |

## Media

```
production/qa/evidence/media/cu-007-synergy-gold-double-fist.png
production/qa/evidence/media/cu-007-no-synergy-baseline.png
production/qa/evidence/media/cu-007-round-12-caution-orange.png
production/qa/evidence/media/cu-007-round-14-critical-red.png
```

## Notes

- 自动化覆盖：[combat_ui_synergy_round_warning_test.cs](../../../tests/integration/combat-ui/combat_ui_synergy_round_warning_test.cs) — 7/7 facts pass
- Manifest rules 校验：UI 不自行计算协同收益；协同与回合数源自 `SynergyDeclaredEvent` / `RoundStartEvent` / `RoundEndEvent`
- 性能：浮字复用现有池；回合颜色为 modulate-only 切换，无每帧分配
- 截图采集环境：harness panel 直接渲染 `CombatUiSnapshot.SynergyCueEntries` / `TurnWarning` 字段（fixture 序列见 [Sprint5CombatUiAdapterFixtures.cs](../../../prototypes/sprint5-combat-ui-harness/scripts/testdata/Sprint5CombatUiAdapterFixtures.cs)）；最终美术资源（金色双拳 sprite、回合警戒 modulate 曲线）将在 Polish phase 替换，contract 不变
