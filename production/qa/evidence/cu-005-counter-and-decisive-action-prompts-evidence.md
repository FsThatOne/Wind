# cu-005 反制与决胜行动提示 — QA Evidence

> 日期：2026-06-17
> Story：`production/epics/combat-ui/stories/cu-005-counter-and-decisive-action-prompts.md`
> TR-ID：`TR-combat-ui-005`
> 类型：Integration / UI
> 证据状态：新 harness 手测通过；旧 `fengzhi-vertical-slice` 结果已判定为 stale target
> QA 签字：User manual QA

## 验证目标

- 验证 `cu-005` 使用最新 GDD 口径：提示基于敌方当前内功气机 / 当前内功属性，而不是敌方下一招或出招属性。
- 验证决胜提示与识破 / 克制提示分离。
- 验证 UI 只提交行动意图，不在 UI 层执行扣费、伤害或破绽结算。

## 测试目标

| 项目 | 记录 |
|------|------|
| Target | `prototypes/sprint5-combat-ui-harness` |
| Engine | Godot 4.6.3 Mono |
| Platform | macOS local dev build |
| Input | Mouse + keyboard |
| Fixture | `EnemyCurrentQiGang`, `EnemyCurrentQiRou`, `StaggerAndDecisive` |

## 手动结果

用户在新 harness 中手动测试 `cu-005`，结果：**PASS**。

确认点：

- `EnemyCurrentQiGang` / `EnemyCurrentQiRou` 按敌方当前内功气机口径展示。
- 测试目标不再依赖过期 `fengzhi-vertical-slice` 的 Boss 战流程。
- 旧 slice 中“敌方出招属性 / 招式属性反制”的失败记录保留为 stale-target evidence，不再作为当前 `cu-005` blocker。

## 自动测试覆盖

- `CombatUiCounterDecisivePromptTest`
- `CombatUiMoveSelectionPanelTest`
- `CombatUiDualFocusNavigationTest`

最近验证：

- `dotnet test FengZhi.slnx` passed `1344 / 1344`。

## 结论

当前结论：**PASS VIA HARNESS**。

`cu-005` 不再阻塞 Sprint 5 Must Have close-out。后续如果当前 harness 或 Foundation 再复现“按敌方下一招属性反制”的行为，应开新 active bug；旧 `BUG-0003` 保留为 stale-target 记录。
