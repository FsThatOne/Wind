# Epic: 活江湖层

> **Layer**: Feature
> **GDD**: design/gdd/living-jianghu-layer.md
> **Architecture Module**: `Feature/Jianghu/`
> **Status**: Ready
> **Stories**: Not yet created — run `/create-stories living-jianghu-layer`

## Overview

活江湖层实现每日世界事件调度、传闻传播、暗号流转、同伴代办和呼吸期内容填充。它是事件表 + 条件引擎 + 呈现队列的组合逻辑层，只决定“何时发生什么”，不直接拥有 UI 或叙事文本呈现。

## Governing ADRs

| ADR | Decision Summary | Engine Risk |
|-----|-----------------|-------------|
| ADR-0014: Living Jianghu Layer | 采用 ConditionEvaluator + JianghuScheduler + EventRegistry + DeliveryQueue | LOW |
| ADR-0001: EventBus | 订阅 day_advanced / season_changed / chapter_changed，发布世界事件 | LOW |
| ADR-0003: Data Configuration Format | 事件表、条件、触发副作用以 YAML 配置 | LOW |
| ADR-0004: Save Encryption | 事件状态、Flag、DeliveryQueue 随存档持久化 | LOW |

## GDD Requirements

| Requirement | ADR Coverage |
|-------------|--------------|
| 每日 Tick 产出不超过 daily_event_cap 的事件 | ADR-0014 ✅ |
| 9 种 precondition 原语和 AND 组合 | ADR-0014 ✅ |
| priority、tag relevance、backlog bonus 和类型均衡 | ADR-0014 ✅ |
| 传闻传播延迟与 pending 呈现 | ADR-0014 ✅ |
| inactive → pending → triggered → delivered → expired 生命周期 | ADR-0014 ✅ |
| 呼吸期容量倍增、breathing_only 和积压释放 | ADR-0014 ✅ |
| 章节切换时清理过期事件 | ADR-0014 ✅ |
| 同伴代办触发与结果投递 | ADR-0014 + ADR-0016 ⚠️ 需集成 story 验证 |
| on_trigger 新事件不在同日连锁触发 | ADR-0014 ✅ |

## Trace Notes

`docs/architecture/tr-registry.yaml` 当前没有 `TR-living-jianghu-*` 条目。创建 stories 时应从 `living-jianghu-layer.md` AC-1 至 AC-9 建立临时 AC trace，并优先补足 Flag namespace 与事件表 schema 相关测试。

## Definition of Done

This epic is complete when:
- All stories are implemented, reviewed, and closed via `/story-done`
- All acceptance criteria from `design/gdd/living-jianghu-layer.md` are verified
- Daily tick, condition evaluation, prioritization, propagation delay, delivery queue and save restore have automated tests
- Event effects are atomic enough to avoid partial flag/NPC-state corruption
- Scheduler remains presentation-agnostic and only pushes events to downstream channels

## Next Step

Run `/create-stories living-jianghu-layer` to break this epic into implementable stories.
