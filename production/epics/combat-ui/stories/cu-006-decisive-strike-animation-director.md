# Story: cu-006 — 一击决胜演出编排

> **Epic**: combat-ui
> **类型**: Visual/Feel
> **优先级**: P0 — 高风险演出 Spike 与实现
> **Estimate**: L（约 8h）
> **依赖**: cu-001, cu-003, cu-005
> **阻塞**: 无
> **ADR 指引**: ADR-0011（CombatAnimationDirector / TimeScaleController / CameraRequestBus），ADR-0009（音效同步点）
> **GDD 来源**: design/gdd/combat-ui.md §Detailed Design / 一击决胜演出流程, §Formulas, §Edge Cases
> **TR-ID**: TR-combat-ui-???（待 architecture review 写入稳定编号）
> **Control Manifest Version**: 2026-06-10
> **状态**: Ready

## 目标

实现一击决胜 7 阶段演出，让玩家选择“决胜一击”后进入慢动作、推镜、体系专属动画、最大伤害浮字和恢复流程，并验证 TimeScale/Tween/Camera 的 Godot 4.6.3 风险点。

## 范围

### 包含
- `CombatAnimationDirector` 接收决胜请求并排队演出命令
- Phase 1：接收 Core 已完成的预结算结果
- Phase 2：`Engine.TimeScale` 慢进至 0.2，约 0.3s
- Phase 3：Camera2D 推进并锁定目标
- Phase 4：播放刚/柔/巧体系专属动画入口
- Phase 5：一击决胜伤害数字弹出
- Phase 6：TimeScale 慢出恢复至 1.0
- Phase 7：Camera 归位并恢复默认跟随
- 演出期间屏蔽玩家输入，不可跳过
- 暂停菜单或其他 TimeScale 请求打断时，恢复后演出不重置
- 音效系统通过同源事件或阶段标记同步，不由 UI 直接拥有音频逻辑

### 不包含
- 决胜伤害结算
- 最终体系专属动画美术资源
- 音效资产制作

## 技术说明

- TimeScale 控制必须通过 `TimeScaleController` 优先级栈，决胜优先级 50
- 修改 `Engine.TimeScale` 的 Tween 必须 `SetProcessMode(Tween.TweenProcessMode.Always)`，避免自锁
- Camera 控制必须通过 `CameraRequestBus`，决胜优先级 50
- 演出期间禁用 Camera smoothing，归位后重新启用
- 输入屏蔽应使用战斗 cinematic lock，不得通过破坏 InputMap 实现
- 决胜演出默认不可跳过；未来如开放跳过，必须由 tuning knob 控制
- 必须为 Godot 4.6.3 的 TimeScale + Tween 行为留下手动验证证据

## 验收标准

- [ ] 选择“决胜一击”后按 7 个 Phase 顺序播放
- [ ] TimeScale 慢进到 0.2 并慢出恢复到 1.0
- [ ] Camera 推进、锁定目标并在结束后归位
- [ ] 演出期间玩家输入被屏蔽且不可跳过
- [ ] 一击决胜伤害数字使用最大尺寸和深金样式
- [ ] 暂停或更高优先级 TimeScale 请求释放后，决胜演出能继续并正确恢复
- [ ] 演出结束后 TimeScale、Camera、输入锁都恢复正常

## QA 手动检查

- **AC-1**：完整演出
  - Setup：制造目标破绽 ≥5，选择“决胜一击”
  - Verify：慢动作、推镜、动画、伤害浮字、恢复依序发生
  - Pass condition：流程完整，无卡死、无提前恢复、无重复演出

- **AC-2**：输入屏蔽
  - Setup：演出期间连续按确认、取消、方向键
  - Verify：演出不被跳过，战斗选择不被误触
  - Pass condition：演出结束前输入不改变战斗状态

- **AC-3**：TimeScale 冲突
  - Setup：演出中打开/模拟暂停菜单 TimeScale 请求
  - Verify：暂停优先级生效，释放后决胜演出继续
  - Pass condition：最终 TimeScale 回到 1.0，Camera 回到默认跟随

## QA Test Cases

**Automated test path**: `tests/integration/combat-ui/combat_ui_decisive_animation_director_test.cs`

Required automated coverage:
- `CombatAnimationDirector` queues the seven decisive phases in order.
- TimeScale request uses priority 50, reaches configured minimum 0.2, and restores to 1.0.
- TimeScale tween uses always-processing semantics so the slow-motion tween does not self-lock.
- Camera request uses priority 50, locks target, disables smoothing during cinematic, and restores after completion.
- Player input is blocked via combat cinematic lock and InputMap is not mutated.
- Max-size deep-gold decisive damage feedback is requested from the visual feedback layer.
- Pause or higher-priority TimeScale request safely overrides/pauses and decisive animation continues or recovers after release.

Manual evidence required:
- Capture `production/qa/evidence/cu-006-decisive-strike-animation-director-evidence.md`.
- Include a short clip of seven-phase sequence, TimeScale/camera restoration log, and pause conflict test.

## 测试证据路径

`production/qa/evidence/cu-006-decisive-strike-animation-director-evidence.md`

## 依赖关系

- Depends on: cu-001, cu-003, cu-005
- Unlocks: None
