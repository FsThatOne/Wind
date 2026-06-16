# cu-001 战斗 UI 基础层与事件适配 — 测试证据

> 日期：2026-06-14
> Story：`production/epics/combat-ui/stories/cu-001-combat-ui-foundation-and-event-adapter.md`
> 自动化测试：`tests/integration/combat-ui/combat_ui_foundation_event_adapter_test.cs`

## 覆盖范围

- `WorldIntentLayer` 与 `HUDLayer` 的层级契约：WorldUi = 10，HUD = 20。
- 最小 Godot Presentation 节点壳：`CombatUiRoot : Node`、`WorldIntentLayer : CanvasLayer`、`CombatHudLayer : CanvasLayer`。
- 两个基础面板均继承 `BaseUiPanel`，提供 `_dirty + _Process` 合批刷新入口。
- 战斗事件适配为只读 UI 快照，UI 不回写 Core 战斗状态。
- UI 状态流：`Inactive -> RoundStart -> PlayerDecision -> Resolving -> RoundEnd -> BattleEnd`。
- 伤害、破绽、内息、一击决胜目标均来自战斗事件，不在 UI 重新计算。
- `Dispose()` 模拟离开战斗场景时清理订阅。
- 多个事件通过脏标记合批刷新，HUD 刷新预算目标记录为 `1.0ms/frame`。

## 自动化检查

已本地执行：

```bash
dotnet test --filter CombatUiFoundationEventAdapterTest
dotnet test
```

结果：

- `CombatUiFoundationEventAdapterTest`：9/9 通过。
- Foundation 全量测试：1223/1223 通过。

## 手动检查留档

本 story 已补齐最小 Godot `CanvasLayer` / `BaseUiPanel` 类型壳与进入战斗创建入口。当前 xUnit 宿主不启动 Godot 原生场景树，因此自动化验证采用反射确认类型继承、入口签名、层级描述与事件适配行为；真实 Godot 场景中的可见性、dual-focus 手柄走查与视觉交互细节会在后续 `cu-008` 和具体 HUD story 中留证据。
