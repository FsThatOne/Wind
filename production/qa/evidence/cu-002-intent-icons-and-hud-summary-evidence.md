# CU-002 测试证据 — 意图图标与 HUD 汇总

> **日期**: 2026-06-14
> **Story**: production/epics/combat-ui/stories/cu-002-intent-icons-and-hud-summary.md
> **类型**: UI
> **结论**: 自动契约已覆盖；真实 Godot 场景动画仍需后续手动走查

## 自动验证

- `CombatUiIntentIconsHudSummaryTest`：9/9 通过
- `CombatUiFoundationEventAdapterTest`：9/9 通过
- 运行命令：`dotnet test tests/Foundation/Foundation.Tests.csproj --filter CombatUi`
- 结果：18/18 通过

## 验收标准对应

- `OnIntentRevealed` 后敌方头顶显示对应体系图标：由 `WorldIntentEntries` 与 `WorldIntentPanel.ApplyIntentSnapshot` 覆盖。
- HUD 汇总区同步显示全部敌方意图：由 `HudIntentEntries` 与 `CombatHudPanel.ApplyIntentSummary` 覆盖。
- 未识破意图显示 `?` 灰色迷雾，不泄露真实体系：未知条目只暴露 `Unknown`、`?`、`intent_unknown_fog`，且 `RevealedMoveName` 为 `null`。
- `?` 到已识破体系的变化使用过渡动画：未知转已知时标记 `ShouldPlayRevealTransition`，槽位通过 `SceneTreeTween` 执行淡入与轻微上升动画。
- 已落败目标收到意图事件时槽位消隐或变暗，且不崩溃：`MarkCombatantDefeated` 后收到意图事件会生成 `Dimmed` 槽位并跳过过渡。
- 多敌人同时刷新意图时 HUD 汇总顺序稳定：按敌人首次出现顺序维护 `StableOrder`。
- 后续 snapshot 不再包含的敌人不会残留在 World/HUD 意图快照中。

## 手动走查待办

- 在 Godot 测试战斗场景验证 SceneTreeTween 淡入与轻微上升动画。
- 验证已释放或隐藏的战场目标不会留下悬空图标。
- 验证所有非交互意图图标 `focus_mode = NONE`，手柄焦点不会落到图标槽位。
