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
| Engine | Godot 4.7-stable Mono |
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

---

## Visual Captured (Sprint 6 — Pending)

> 本段为 Sprint 6 `cu-visual-evidence` story 的模板占位；待 designer 录屏 / 截图后填入并切换为 Visual Captured。

### Metadata

| 项 | 值 |
|---|---|
| Capture date | TBD |
| Captured by | TBD（designer 名 / 工号） |
| Engine | Godot 4.7-stable Mono |
| Build | TBD（local dev / commit hash） |
| Platform | TBD（macOS / Windows / Linux） |
| Recording tool | TBD |
| Harness | `prototypes/sprint5-combat-ui-harness` |
| Fixture used | `EnemyCurrentQiGang` / `EnemyCurrentQiRou` / `StaggerAndDecisive` |

### Required Shots（per qa-plan-sprint-6 §cu-visual-evidence — 4 张）

| # | 类型 | 内容 | 文件 | 状态 |
|---|---|---|---|---|
| 1 | 截图 | 反制可用 — 敌方当前内功气机被玩家招克制 + 玩家内息 ≥ 3（fixture: `EnemyCurrentQiGang` 或 `EnemyCurrentQiRou`） | `production/qa/evidence/media/cu-005-counter-available.png` | TBD |
| 2 | 截图 | 反制置灰 — 内息不足（同 fixture，玩家内息 < 反制 cost） | `production/qa/evidence/media/cu-005-counter-disabled-neixi.png` | TBD |
| 3 | 截图 | 决胜行 — 当前目标破绽 ≥ 5，`▶ 决胜一击` 行高亮可选（fixture: `StaggerAndDecisive`） | `production/qa/evidence/media/cu-005-decisive-row.png` | TBD |
| 4 | 截图 | 多目标切换 — 切换敌方目标后，反制 / 决胜提示按新目标当前内功属性更新（不是出招属性） | `production/qa/evidence/media/cu-005-multi-target-switch.png` | TBD |

### Capture Checklist

- [ ] 截图 ≥ 4 张（4 个 required shots 全覆盖）
- [ ] 所有截图挂在 `production/qa/evidence/media/` 下并被本文件相对引用
- [ ] 文件名使用 kebab-case；分辨率 ≥ 1280×720
- [ ] 验证点：UI 只提交行动意图，不在 UI 层执行扣费 / 伤害 / 破绽结算（截图含一致的玩家资源数值）
- [ ] 验证点：提示基于敌方**当前内功气机 / 当前内功属性**，**不**基于敌方下一招或出招属性
- [ ] header `证据状态` 字段升级为 `Visual Captured`
- [ ] story 文件 `production/epics/combat-ui/stories/cu-005-counter-and-decisive-action-prompts.md` 内 `Foundation Captured` / `Visual evidence not yet captured` 升级为 `Visual Captured`
- [ ] tech-debt-register 中 cu-005 Visual evidence deferred 条目改为 `Resolved`

### Sign-off

| 角色 | 姓名 | 日期 | 签字 |
|---|---|---|---|
| Designer | TBD | TBD | __pending__ |
| QA Lead | TBD | TBD | __pending__ |
