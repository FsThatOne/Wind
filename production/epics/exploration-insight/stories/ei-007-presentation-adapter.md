# ei-007: EI Presentation — ProximityDetector Godot Adapter + 水墨视觉提示

> **Status**: Complete
> **Last Updated**: 2026-06-29
> **Type**: Integration
> **Layer**: Presentation
> **Estimate**: 2.0 days (校准后预期 ~0.8d actual)
> **GDD 来源**: design/gdd/exploration-insight.md §4 洞察检定流程 step 4
> **TR-ID**: TR-exploration-insight-001, TR-exploration-insight-002
> **ADR Governing Implementation**: docs/architecture/adr-0018-exploration-insight.md
> **Manifest Version**: 2026-06-10
> **Dependencies**: ei-006 (Debt Paydown, Complete)

## Summary

将 Foundation 层的 ProximityDetector 接入 Godot 场景循环。创建一个 Godot Node 在
`_PhysicsProcess` 中驱动 `Tick()`，并订阅事件总线渲染/隐藏水墨风格的洞察提示。

## Acceptance Criteria

- [ ] AC-1: `InsightDetectorBridge` Node 在 `_PhysicsProcess` 中获取玩家位置和 insight 属性，调用 `ProximityDetector.Tick(playerPos, insight, delta)`
- [ ] AC-2: 订阅 `InsightCueShownEvent` → 在节点 Position 处实例化水墨提示 VFX（淡入动画）
- [ ] AC-3: 订阅 `InsightCueHiddenEvent` → 移除/淡出对应 nodeId 的视觉提示
- [ ] AC-4: 对话激活时调用 `PauseDetection()`，对话结束时调用 `ResumeDetection()`
- [ ] AC-5: 场景卸载（`_ExitTree`）时调用 `OnSceneUnloaded()` 清理所有活跃 cue
- [ ] AC-6: 水墨提示使用半透明圈 + 粒子/脉冲动画（v0 可用 Sprite2D + Tween，无需 shader）
- [ ] AC-7: 在 BackMountainCliffCave 场景中可运行，无 null 异常或孤立节点泄漏

## Implementation Notes

- 从 ADR-0018 D3: Adapter 不持有游戏状态，只做 Godot ↔ Foundation 桥接
- Player insight 从 GameFlow.PlayerInstance.Attributes.Insight 获取；若 GameFlow 不存在（单元测试/独立场景）则使用默认值 10
- 视觉提示节点使用 PackedScene 实例化（节点数 ≤10，对象池非必需）
- 事件订阅通过 GameFlow.EventBus（若存在）或 standalone EventBus
- PauseDetection 触发时机：DialogueManager.DialogueStarted / DialogueEnded

## Out of Scope

- 玩家追查交互（ei-008 负责）
- InnerMonologue 显示（ei-008 负责）
- 实际 InsightNode 内容配置（ei-009 负责）
- Shader-based 水墨晕染效果（Polish 阶段）
- 战斗锁集成（当前探索场景无战斗）

## QA Test Cases

1. **Integration test**: 在测试场景中放置 3 个 InsightNode（threshold 5/15/30），玩家 insight=10 时应只检测到 threshold≤10 的节点，且视觉提示正确出现/消失
2. **Lock test**: 启动对话后，检测器暂停，已显示的提示消失；对话结束后恢复检测
3. **Scene cleanup**: 切换场景后，旧场景的视觉提示节点全部被清理，无孤立引用

## Test Evidence

- Integration: playtest record at `production/qa/evidence/ei-007-presentation-adapter-evidence.md`

## Completion Notes
**Completed**: 2026-06-29
**Criteria**: 7/7 passing
**Deviations**: None
**Test Evidence**: Integration tests at tests/integration/exploration/insight_save_scene_lock_test.cs; playtest evidence file pending (ADVISORY)
**Code Review**: Complete (2 rounds, all issues resolved)
**Effort**: estimate 6.4 h / actual 6.0 h (variance -6%)
