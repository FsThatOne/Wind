# cu-006 — 一击决胜演出编排 Evidence

> **Story**: [cu-006-decisive-strike-animation-director.md](../../epics/combat-ui/stories/cu-006-decisive-strike-animation-director.md)
> **TR-ID**: TR-combat-ui-006
> **Status**: Foundation Captured (2026-06-18) — 自动化 8/8 + harness panel 就位；Godot 实机 TimeScale/Tween/Camera2D 验证 deferred 到下一 sprint

## Capture Checklist

- [x] AC-1：7 阶段顺序 — `Director_RequestDecisiveStrike_QueuesAllSevenPhasesInOrder`
- [x] AC-2：TimeScale 慢进 0.2 + 优先级 50 — `TimeScaleController_DecisiveRequest_UsesPriority50AndReachesPoint2`；从 0.2 恢复到 1.0 由 Phase6 dispose handle 后栈空 → Default 1.0 自动覆盖（同 fact 第二阶段断言）
- [x] AC-3：暂停冲突恢复 — `TimeScaleController_PauseStackPreemptsDecisiveAndRestoresOnRelease` + `DecisiveSequence_PauseDuringSlowMotion_ContinuesFromInterruptedPhase`
- [x] AC-4：Camera 锁定目标 + 关闭 smoothing — `CameraRequestBus_DecisiveRequest_LocksTargetAndDisablesSmoothing`
- [x] AC-5：CinematicLock 屏蔽战斗输入 + 白名单 ui_pause / ui_system_back — `CinematicLock_AcquireDuringDecisive_BlocksCombatActionWhitelistsPause`
- [x] AC-6：Phase5 浮字使用 Decisive 样式 — `DecisiveDamageNumber_AtPhase5_PublishesPhaseAdvancedSoStyleDecisiveCanRender`
- [x] AC-7：Dispose / Cancel 反向释放所有 handle — `Director_DisposeReleasesAllHandlesEvenIfSequenceUncompleted`
- [ ] **手动验证（deferred）**：Godot 4.7-stable `Engine.TimeScale` + `Tween(TweenProcessMode.Always)` + Camera2D 实机集成的 7 阶段演出录屏、暂停冲突手动复现、Phase4 体系专属动画接入

## Capture Targets — Foundation 自动化

| AC | Test |
|----|------|
| AC-1 | [Director_RequestDecisiveStrike_QueuesAllSevenPhasesInOrder](../../../tests/integration/combat-ui/combat_ui_decisive_animation_director_test.cs) |
| AC-2 | [TimeScaleController_DecisiveRequest_UsesPriority50AndReachesPoint2](../../../tests/integration/combat-ui/combat_ui_decisive_animation_director_test.cs) |
| AC-3 / AC-7 | [TimeScaleController_PauseStackPreemptsDecisiveAndRestoresOnRelease](../../../tests/integration/combat-ui/combat_ui_decisive_animation_director_test.cs) |
| AC-3 / AC-7 | [DecisiveSequence_PauseDuringSlowMotion_ContinuesFromInterruptedPhase](../../../tests/integration/combat-ui/combat_ui_decisive_animation_director_test.cs) |
| AC-4 | [CameraRequestBus_DecisiveRequest_LocksTargetAndDisablesSmoothing](../../../tests/integration/combat-ui/combat_ui_decisive_animation_director_test.cs) |
| AC-5 | [CinematicLock_AcquireDuringDecisive_BlocksCombatActionWhitelistsPause](../../../tests/integration/combat-ui/combat_ui_decisive_animation_director_test.cs) |
| AC-6 | [DecisiveDamageNumber_AtPhase5_PublishesPhaseAdvancedSoStyleDecisiveCanRender](../../../tests/integration/combat-ui/combat_ui_decisive_animation_director_test.cs) |
| 通用回滚 | [Director_DisposeReleasesAllHandlesEvenIfSequenceUncompleted](../../../tests/integration/combat-ui/combat_ui_decisive_animation_director_test.cs) |

## Capture Targets — Harness Manual Replay

| 场景 | 工具 | Fixture | 备注 |
|------|------|---------|------|
| 刚体系决胜（7 阶段顺序、TimeScale 0.2、Camera 锁定 bandit） | `prototypes/sprint5-combat-ui-harness` cu-006 panel | `decisive_gang` | 点 Tick 0.3s × 6 完成全流程 |
| 柔体系决胜（PhaseAdvanced.MoveType=Rou） | 同上 | `decisive_rou` | 用于确认事件 MoveType 正确投射 |
| 巧体系决胜 | 同上 | `decisive_qiao` | 浮字样式 Decisive 与体系无关，恒为深金 |
| 决胜遇暂停冲突（TimeScale 优先级 100 抢占 → 释放回 0.2） | 同上 | `decisive_pause_conflict` | Hold ui_pause 后再 Tick 不推进；Release 后继续 |

## Notes

- 自动化覆盖：[combat_ui_decisive_animation_director_test.cs](../../../tests/integration/combat-ui/combat_ui_decisive_animation_director_test.cs) — 8/8 facts pass；全 CombatUi 测试 95/95 pass；`dotnet build FengZhi.slnx` 0 错误；`dotnet build prototypes/sprint5-combat-ui-harness/Sprint5CombatUiHarness.csproj` 0 错误 0 警告
- Manifest rules 校验：UI 不持有 Core Combat 句柄；演出依赖事件总线 + 4 个 controller 数值契约（`TimeScaleController` / `CameraRequestBus` / `CombatCinematicLock` / `DecisiveTuning`）
- Godot 集成层（deferred）：修改 `Engine.TimeScale` 的 `Tween` 必须 `SetProcessMode(TweenProcessMode.Always)` 防自锁；Camera2D 集成层订阅 `CameraRequestBus.ActiveRequestChanged` 把 zoom/disableSmoothing 投射到节点；`CombatCinematicLock.IsAllowedDuringLock` 在 InputMap 拦截层调用，**不**触碰 InputMap 配置
- ADR-0009 音效同步：`DecisiveStrikePhaseAdvancedEvent` 是音效系统的挂载点；UI 不直接拥有音频逻辑
- 截图采集：harness `cu-006 Decisive Strike Director` panel 实时显示 Phase / TimeScale / Camera / CinematicLock / 事件日志，可直接截图覆盖 7 阶段 + 暂停冲突
