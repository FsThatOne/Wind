# Sprint 5 Combat UI Harness

这是 Sprint 5 Combat UI 的新实机 QA 目标，用于替代已过期的 `prototypes/fengzhi-vertical-slice`。

## 用途

- 验证 `cu-004`：招式选择面板、基础行动、不可用原因、预览卡。
- 验证 `cu-005`：反制 / 识破提示是否符合最新“敌方当前内功气机”GDD 合同。
- 验证 `cu-008`：键盘 / 手柄 focus 与鼠标 hover 的双焦点行为。

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

使用 Godot 4.6.3 打开：

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

旧 `fengzhi-vertical-slice` 不再作为当前 Sprint 5 Combat UI sign-off 的正式测试目标。
