# Epic: 探索 / 洞察

> **Layer**: Feature
> **GDD**: design/gdd/exploration-insight.md
> **Architecture Module**: `Feature/Exploration/`
> **Status**: Ready
> **Stories**: 5 stories

## Overview

探索 / 洞察系统实现场景中的 InsightNode 主动发现机制，让洞察属性在世界探索中发挥作用。它负责节点注册、距离检测、洞察门槛、发现奖励分派、瞬态提示恢复和已发现状态存档，并与活江湖传闻共享 flag 以避免重复信息推送。

## Governing ADRs

| ADR | Decision Summary | Engine Risk |
|-----|-----------------|-------------|
| ADR-0018: Exploration & Insight | 采用 InsightNode Resource + Registry + ProximityDetector + DiscoveryDispatcher | LOW |
| ADR-0014: Living Jianghu Layer | 复用 ConditionEvaluator，并通过 `insight_*` flag 与传闻互斥 | LOW |
| ADR-0006: Scene Loading Strategy | 场景加载/卸载时注册和清理洞察节点 | MEDIUM |
| ADR-0004: Save Encryption | 仅持久化 INVESTIGATED 终态 | LOW |

## GDD Requirements

| Requirement | ADR Coverage |
|-------------|--------------|
| insight 足够时进入 detection_radius 显示水墨提示并可追查 | ADR-0018 ✅ |
| insight 不足时无提示 | ADR-0018 ✅ |
| 洞察提升后重访可重新发现 | ADR-0018 ✅ |
| 多节点同时接近时按距离 stagger 触发 | ADR-0018 ✅ |
| Clue 节点设置主线 quest_flag | ADR-0018 ✅ |
| CodePhrase 节点写入暗号簿 | ADR-0018 ✅ |
| 存档加载后已发现节点保持 INVESTIGATED | ADR-0018 + ADR-0004 ✅ |
| 战斗/对话期间提示暂停，结束后恢复 | ADR-0018 ✅ |
| linger 超时后提示自动消失，下次接近可再触发 | ADR-0018 ✅ |

## Stories

| # | Story | Type | Status | ADR |
|---|-------|------|--------|-----|
| ei-001 | InsightNode 数据模型、注册表与场景激活 | Logic | Ready | ADR-0018 |
| ei-002 | 洞察距离检测、门槛检定与重访发现 | Logic | Ready | ADR-0018 |
| ei-003 | 多节点 stagger、忽略与 linger 恢复 | Logic | Ready | ADR-0018 |
| ei-004 | 发现奖励分派：Clue 与 CodePhrase | Integration | Ready | ADR-0018 / ADR-0014 |
| ei-005 | 存档恢复、场景卸载清理与战斗/对话锁恢复 | Integration | Ready | ADR-0018 / ADR-0004 / ADR-0006 |

## Trace Notes

`docs/architecture/tr-registry.yaml` 已建立 `TR-exploration-insight-001` 到 `TR-exploration-insight-005`。Stories 从 `exploration-insight.md` AC1-AC9 拆分，并特别标明 Scene Loading、Narrative、CodePhrase、Save 与 LockMode 的集成边界。

## Definition of Done

This epic is complete when:
- All stories are implemented, reviewed, and closed via `/story-done`
- All acceptance criteria from `design/gdd/exploration-insight.md` are verified
- Insight detection, prerequisite evaluation, reward dispatch, transient state reset and save restore have tests
- Scene cleanup prevents stale nodes or leaked prompts across scene transitions
- Exploration discovery does not create HUD radar/quest-marker style UI

## Next Step

Run `/story-readiness production/epics/exploration-insight/stories/ei-001-insight-node-registry-and-scene-activation.md` to begin implementation readiness.
