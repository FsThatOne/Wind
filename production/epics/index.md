# Epics Index

> **Last Updated**: 2026-06-13
> **Engine**: Godot 4.7-stable (C# / .NET 8+)
> **Source**: `/create-epics layer: foundation`, `/create-epics dialogue-system save-system`, `/create-epics layer: core`, `/create-epics layer: feature`, `/create-epics layer: presentation`
> **状态同步**: 2026-06-11 已根据 EPIC/story 文件、实现证据和 Core 层补齐结果同步状态

---

## Foundation Layer

| Epic | Layer | System | GDD | Stories | Status |
|------|-------|--------|-----|---------|--------|
| [character-data](character-data/EPIC.md) | Foundation | 角色属性/功力 | character-attributes.md | 7/7 Done | Done |
| [npc-state](npc-state/EPIC.md) | Foundation | NPC 状态管理 | npc-state.md | 7/7 Done | Done |
| [scene-management](scene-management/EPIC.md) | Foundation | 地图/场景管理 | map-scene-management.md | 7/7 Done | Done |
| [time-system](time-system/EPIC.md) | Foundation | 自然日+体力 | natural-day-stamina.md | 6/6 Done | Done |

## Core 层

| Epic | Layer | System | GDD | Stories | Status |
|------|-------|--------|-----|---------|--------|
| [combat-system](combat-system/EPIC.md) | Core | 回合制战斗 | combat-system.md | 10/10 Complete | Done |
| [martial-arts-system](martial-arts-system/EPIC.md) | Core | 武学组合 | martial-arts-system.md | 8/8 Complete | Done |
| [enemy-ai](enemy-ai/EPIC.md) | Core | 敌方 AI | enemy-ai.md | 8/8 Complete | Done |
| [dialogue-system](dialogue-system/EPIC.md) | Core | 对话系统 | dialogue-system.md | 9/9 Complete | Done |
| [mindset-dual-axis](mindset-dual-axis/EPIC.md) | Core | 心境双轴 | mindset-dual-axis.md | 7/7 Complete | Done |
| [main-narrative](main-narrative/EPIC.md) | Core | 主线叙事 / 章节推进 | main-narrative.md | 8/8 Complete | Done |
| [item-system](item-system/EPIC.md) | Core | 物品 / 道具 | item-system.md | 8/8 Complete | Done |

## Platform 层

| Epic | Layer | System | GDD | Stories | Status |
|------|-------|--------|-----|---------|--------|
| [save-system](save-system/EPIC.md) | Platform | 存档系统 | save-system.md | 6/6 Complete | Done |

## Feature 层

| Epic | Layer | System | GDD | Stories | Status |
|------|-------|--------|-----|---------|--------|
| [romance-system](romance-system/EPIC.md) | Feature | 感情系统（彗星模型） | romance-system.md | 7 stories | Ready |
| [living-jianghu-layer](living-jianghu-layer/EPIC.md) | Feature | 活江湖层 | living-jianghu-layer.md | Not yet created | Ready |
| [epiphany-breakthrough](epiphany-breakthrough/EPIC.md) | Feature | 顿悟突破 | epiphany-breakthrough.md | Not yet created | Ready |
| [misunderstanding-system](misunderstanding-system/EPIC.md) | Feature | 误会系统 | misunderstanding-system.md | Not yet created | Ready |
| [exploration-insight](exploration-insight/EPIC.md) | Feature | 探索 / 洞察 | exploration-insight.md | 5 stories | Ready |
| [party-management](party-management/EPIC.md) | Feature | 队伍管理 / 同伴成长 | party-management.md | Not yet created | Ready |

## Presentation 层

| Epic | Layer | System | GDD | Stories | Status |
|------|-------|--------|-----|---------|--------|
| [combat-ui](combat-ui/EPIC.md) | Presentation | 战斗 UI | combat-ui.md | 8 stories | Ready |
| [blurred-ui](blurred-ui/EPIC.md) | Presentation | 朦胧化 UI | blurred-ui.md | Not yet created | Ready |
| [cutscene-system](cutscene-system/EPIC.md) | Presentation | CG / 演出 | cutscene-system.md | Not yet created | Ready |
| [audio-system](audio-system/EPIC.md) | Presentation | 音乐 / 音效 | audio-system.md | Not yet created | Ready |

---

## Summary

| Layer | Epics | Ready | In Progress | Done |
|-------|-------|-------|-------------|------|
| Foundation | 4 | 0 | 0 | 4 |
| Core | 7 | 0 | 0 | 7 |
| Platform | 1 | 0 | 0 | 1 |
| Feature | 6 | 6 | 0 | 0 |
| Presentation | 4 | 4 | 0 | 0 |
| **Total** | **22** | **10** | **0** | **12** |

## 下一步

Feature / Presentation 层 Epic 已创建，下一步运行 `/create-stories [epic-slug]` 将 Ready Epic 拆分为可实现 stories。
