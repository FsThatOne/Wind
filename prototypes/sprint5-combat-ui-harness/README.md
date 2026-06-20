# Sprint 5 Combat UI Harness

这是 Sprint 5 Combat UI 的新实机 QA 目标，用于替代已过期的 `prototypes/fengzhi-vertical-slice`。

## 用途

- 验证 `cu-004`：招式选择面板、基础行动、不可用原因、预览卡。
- 验证 `cu-005`：反制 / 识破提示是否符合最新“敌方当前内功气机”GDD 合同。
- 验证 `cu-008`：键盘 / 手柄 focus 与鼠标 hover 的双焦点行为。
- 验证最小战斗决策 playtest loop：观察敌方当前气机，选择招式 / 调息 / 道具 / 决胜，确认后查看本回合决策摘要。

## 重要说明

本 harness 使用“双层对照”：

- 左侧 `Current Foundation Output`：显示当前 `FengZhi.Foundation` 的真实 DTO / Presenter 输出。
- 右侧 `GDD Expected Contract`：显示最新 GDD 的期望合同。

如果当前 Foundation 仍使用旧 `RevealedEnemyMoveType` / 招式类型反制语义，而 GDD 期望“敌方当前内功气机”，页面会显示 `CONTRACT DRIFT`。这不是 harness 失败，而是在提示当前实现合同需要更新。

## 操作

- 左 / 右：切换 fixture。
- 上 / 下：移动 focus。
- 空格 / Enter：确认当前 focus。
- 鼠标悬停左侧行动：更新 hover 与 preview。

## 启动

使用 Godot 4.7-stable 打开：

```text
prototypes/sprint5-combat-ui-harness
```

主场景：

```text
res://scenes/Sprint5CombatUiHarness.tscn
```

## QA 口径

- `DefaultAvailable`、`InsufficientNeixi`、`NoCombatItem` 主要用于 `cu-004`。
- `EnemyCurrentQiGang`、`EnemyCurrentQiRou`、`StaggerAndDecisive` 主要用于 `cu-005`。
- `DualFocus` 主要用于 `cu-008`。
- `DecisionLoopBalanced`、`DecisionLoopResourcePressure`、`DecisionLoopDecisiveWindow` 用于 targeted playtest，不用于替代完整 vertical slice。

旧 `fengzhi-vertical-slice` 不再作为当前 Sprint 5 Combat UI sign-off 的正式测试目标。

## Combat Decision Loop Playtest 口径

这组 fixture 只验证当前行气战棋的最小战斗决策闭环：

```text
观察敌方当前内功气机 -> hover/focus 预览 -> 选择行动 -> 确认 -> 查看决策摘要
```

允许观察：

- 玩家是否理解敌方当前气机。
- 玩家是否理解内息不足、无道具、决胜窗口等限制。
- 玩家是否能区分克制提示、调息、道具和决胜入口。
- 玩家是否误以为仍有普通攻击或下一招猜拳。

限制：

- 不是完整新手流程。
- 不验证叙事、探索、场景切换或完整战斗结算。
- 不显示胜率、EV、敌方下一招或最终伤害。
- 如果用于 `production/playtests/`，报告中的 `Gate Result` 最高只能写 `CONCERNS`，直到后续有当前口径完整 build 覆盖新手流程。
