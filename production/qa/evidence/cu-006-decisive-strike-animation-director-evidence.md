# cu-006 — 一击决胜演出编排 Evidence

> **🔴 2026-06-24 状态更新（VS 视觉 evidence 待录）**
> - 本 evidence 已捕获 Foundation 自动 8/8 + Godot 集成 6/6 AC（2026-06-20 cu-006-godot-integration BUILD）
> - 但 **VS 江南战斗场景 `battle_jiangnan_bandit.tscn` 尚未集成决胜演出**（MVP-A 用 2 Button + Label 占位）
> - VS 场景内决胜演出集成 + 视觉录屏拆为 2 子 story：`cu-006-vs-integration` (3h) + `cu-visual-evidence-recording` (含 4 cu 录屏)
> - scope 拆解见 [`harness spec`](../../docs/superpowers/specs/2026-06-24-cu-visual-evidence-harness.md)
> - Q3=a：pause priority 验证由 Foundation `combat_ui_decisive_animation_director_test.cs` 覆盖，VS 录屏不重复

> **Story**: [cu-006-decisive-strike-animation-director.md](../../epics/combat-ui/stories/cu-006-decisive-strike-animation-director.md)
> **TR-ID**: TR-combat-ui-006
> **Status**: Foundation Captured (2026-06-18) + Godot Integration Captured (2026-06-20, harness panel) — Foundation 自动化 8/8 + Godot 实机集成 6/6 AC ✅；**VS 江南战斗场景视觉 artifact 待录**（依赖 `cu-006-vs-integration` done → `cu-visual-evidence-harness` done → `cu-visual-evidence-recording`）

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

## Godot Integration Captured (2026-06-20)

> 经 cu-006-godot-integration BUILD（Sprint 6）落地，原 Foundation Captured 段保持只读；本节是实机集成增量。

- 自动化覆盖（Godot 4.7-stable 实机）：[CombatUiDecisiveGodotIntegrationTest.cs](../../../prototypes/sprint5-combat-ui-harness/scripts/tests/cu006/CombatUiDecisiveGodotIntegrationTest.cs) — cu006 6/6 PASS（headless quit code 0）+ smoke 2/2 PASS + Foundation 1359/1359 PASS（不退步）
- 入口脚本：`./prototypes/sprint5-combat-ui-harness/scripts/run_godot_tests.sh cu006`
- Bridge 三件：`TimeScaleEngineBridge` / `CameraRequestBusBridge` / `CombatCinematicLockInputFilter`（路径见 ADR-0011 §实机集成）
- Facade：`ICombatService.RequestDecisiveStrike(actorId, targetId)` + `IDecisiveContextProvider`（Foundation 不动原则；BattleFacade 不耦合）
- **Filter 语义修订**：`CombatCinematicLockInputFilter.ShouldConsume` 初版 first-match → any-allowed（任一命中 action 在白名单即放行）。背景：`Key.Escape` 同映射 Godot 内置 `ui_cancel` + 项目 `ui_pause` 时 first-match 错误屏蔽白名单。详见 [ADR-0011 §实机集成（cu-006 BUILD, 2026-06-20）](../../../docs/architecture/adr-0011-combat-ui-animation.md) 硬约束 #3。
- BUILD evidence note：[build-cu-006-godot-integration-evidence-2026-06-20.md](../../notes/build-cu-006-godot-integration-evidence-2026-06-20.md)
- Sign-off：lead-programmer __pending__ / designer __pending__（自动化基线已绿；视觉 artifact 待录屏后补齐 + 双签字）

---

## Visual Captured (Sprint 7 carryover — Pending)

> **2026-06-22 改派**：原计划 Sprint 6 在 `prototypes/sprint5-combat-ui-harness` 的 cu-006 panel 上录 4 个 fixture (`decisive_gang` / `decisive_rou` / `decisive_qiao` / `decisive_pause_conflict`)。harness 已于 commit `c8d8c14` 主动删除（与 burst-read spike / 134 个旧 protagonist 资产同清理）。本段 visual evidence 推迟到 Sprint 7 的 ADR-0020 全循环 VS 重建上录制，确保 evidence 代表当前架构（纯 2D 武侠 + 行气战棋）。Foundation Captured 段（2026-06-18）与 Godot Integration Captured 段（2026-06-20）保持只读。

### Metadata

| 项 | 值 |
|---|---|
| Capture date | TBD |
| Captured by | TBD（designer 名 / 工号） |
| Engine | Godot 4.7-stable Mono |
| Build | TBD（local dev / commit hash） |
| Platform | TBD（macOS / Windows / Linux） |
| Recording tool | TBD（macOS Screen Recording / OBS / etc.） |
| Harness | `prototypes/sprint5-combat-ui-harness` cu-006 panel |
| Fixture used | `decisive_gang` / `decisive_rou` / `decisive_qiao` / `decisive_pause_conflict` |

### Required Shots（per qa-plan-sprint-6 §cu-006-godot-integration manual checklist L86-92 + cu-visual-evidence）

| # | 类型 | 内容 | 文件 | 状态 |
|---|---|---|---|---|
| 1 | 录屏 | 7 阶段顺序 — 决胜一击在 Godot 实机 wall-clock 与录屏帧表对齐（Phase1→Phase7） | `production/qa/evidence/media/cu-006-7-phase-sequence.mp4` | TBD |
| 2 | 截图/录屏 | TimeScale overlay 数值轨迹 — 0.4s 内降到 ~0.2，hold ~1.0s，0.4s 内回到 1.0（用 logger 或 overlay 印数） | `production/qa/evidence/media/cu-006-timescale-curve.mp4` | TBD |
| 3 | 录屏 | Camera2D 推进 + 锁定目标 → 演出结束 1 帧内恢复默认跟随（fixture: `decisive_gang`） | `production/qa/evidence/media/cu-006-camera-push-restore.mp4` | TBD |
| 4 | 截图 | 演出期间输入屏蔽 — 按方向键 / 确认 / 取消，战斗状态不变（log 或 HUD 印证） | `production/qa/evidence/media/cu-006-input-blocked-during-cinematic.png` | TBD |
| 5 | 录屏 | ESC 暂停冲突 — 演出冻结，关闭暂停后从被中断 phase 继续（fixture: `decisive_pause_conflict`） | `production/qa/evidence/media/cu-006-pause-conflict-resume.mp4` | TBD |
| 6 | 截图 | 决胜伤害字（深金 + 黑边）对比普通伤害字 — Phase5 浮字样式明显大于普通伤害字 | `production/qa/evidence/media/cu-006-decisive-vs-normal-damage.png` | TBD |
| 7 | 截图 | 演出结束后 ESC 恢复正常 + `ICombatService.RequestDecisiveStrike` 从 cu-005 路径触达（log 截图，确认走 Facade 不绕过） | `production/qa/evidence/media/cu-006-facade-path-and-inputmap-restored.png` | TBD |

### Capture Checklist

- [ ] ≥1 段录屏 + ≥3 张截图（必备：#1 全程录屏 + 至少 2 个细节截图 + 1 段暂停冲突录屏）
- [ ] 所有素材挂在 `production/qa/evidence/media/` 下并被本文件相对引用
- [ ] 文件名使用 kebab-case；视频 ≥ 1280×720 30fps；截图 ≥ 1280×720
- [ ] 验证点：7 phase 顺序与 wall-clock 对齐（覆盖 cu-006 GDD AC-1 / Foundation Director_RequestDecisiveStrike_QueuesAllSevenPhasesInOrder 实机映射）
- [ ] 验证点：TimeScale 终值 = 1.0（演出后必须恢复，覆盖 GDD §Edge Cases "TimeScale 栈在演出最后一帧恢复时和 Tween 回调发生竞争"）
- [ ] 验证点：Camera2D 演出后 1 帧内归位（不 lerp 残留）
- [ ] 验证点：CinematicLock 期间普通战斗输入被吞，`ui_pause` / `ui_system_back` 仍可用（白名单 any-allowed 语义）
- [ ] 验证点：`ICombatService.RequestDecisiveStrike(actorId, targetId)` 从 cu-005 决胜行触达，不绕过 Facade
- [ ] header `Status` 字段升级为 `Visual Captured`
- [ ] story 文件 `production/epics/combat-ui/stories/cu-006-decisive-strike-animation-director.md` 内 `Foundation Captured` / `Godot Integration Captured` / `Visual evidence not yet captured` 升级为 `Visual Captured`
- [ ] tech-debt-register 中 cu-006 Visual evidence deferred 条目改为 `Resolved`

### Sign-off

| 角色 | 姓名 | 日期 | 签字 |
|---|---|---|---|
| Designer | TBD | TBD | __pending__ |
| Lead Programmer | TBD | TBD | __pending__ |
| QA Lead | TBD | TBD | __pending__ |
