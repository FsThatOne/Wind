# Epic: 序章内容生产

> **Layer**: Content / Presentation Integration
> **Design Spec**: `docs/superpowers/specs/2026-06-29-prologue-a-day-and-night-design.md`
> **Primary GDD**: `design/gdd/main-narrative.md`
> **Supporting GDDs**: `design/gdd/tutorial-onboarding.md`, `design/gdd/dialogue-system.md`, `design/gdd/exploration-insight.md`, `design/gdd/misunderstanding-system.md`, `design/gdd/combat-system.md`
> **Architecture Modules**: `Core/Narrative/`, `Core/Dialogue/`, `Feature/Exploration/`, `Feature/Misunderstanding/`, `Presentation/Tutorial/`
> **Status**: Ready
> **Stories**: 6 Ready

## Overview

本 epic 将已批准的“序章一日一夜设计”落成可制作内容：P00-01 到 P00-10 主线节点骨架、chapter_00 对话与内心独白、师姐采集教学、山庄寿宴跑腿、崖洞回忆互动、灭门后搜证路径，以及师兄采买回山后的误会、共同埋葬、临别传承和书信约定。

本 epic 不新增系统架构。它复用已完成或已规划的主线叙事、对话、地图/场景、探索/洞察、教学、误会、战斗和书信能力，将序章内容拆成可独立实施和验收的 stories。

## Governing ADRs

| ADR | Decision Summary | Engine Risk |
|-----|-----------------|-------------|
| ADR-0005: Dialogue Data Format | 章节对话与叙事内容使用 YAML 图节点格式、条件三元组、事件附加和 CI 校验 | LOW |
| ADR-0001: Event Bus Architecture | 叙事节点、对话完成、误会触发、书信解锁等跨层副作用通过类型安全事件传播 | HIGH |
| ADR-0003: Data Configuration Format | 静态剧情、对话、节点和配置数据使用 YAML 1.2，启动时加载和校验 | LOW |
| ADR-0018: Exploration / Insight | 洞察节点使用 InsightNode、DiscoveryType、阈值和 narrative_context 表达场景发现 | MEDIUM |
| ADR-0012: Misunderstanding UI | 误会通过称呼、语气、关系信号等间接方式呈现，不直接暴露具体数值 | HIGH |

## Requirements Trace

| Requirement | Source | Story |
|-------------|--------|-------|
| 序章采用一日一夜结构，建立温暖日常后通过灭门制造不可逆动机 | `main-narrative.md` + approved spec | pc-001, pc-003, pc-005 |
| 战斗教学后移到师兄回山、误会解除和共同埋葬之后 | `tutorial-onboarding.md` + approved spec | pc-006 |
| 师姐负责采药/采矿教学，并解释主角此前被保护得太好 | approved spec | pc-002 |
| 师姐采集段发现动物足迹，埋坐骑系统伏笔，不提前污染幸福感 | approved spec | pc-002 |
| 主角拖延取酒，形成灭门后的自责根 | approved spec | pc-003 |
| 崖洞互动点全部改为与师姐相关的回忆触发 | approved spec + `ei-009` follow-up | pc-004 |
| 灭门不是战斗关，玩家通过搜证确认无人拔剑、暗格空、血书和师姐失踪 | `main-narrative.md` + approved spec | pc-005 |
| 师兄采买回山触发“唯主角独活”的误会，并通过对话澄清，不进入生死战 | `main-narrative.md` + `misunderstanding-system.md` | pc-006 |

## Story List

| ID | Title | Type | Status | Depends On |
|----|-------|------|--------|------------|
| pc-001 | chapter_00 主线节点骨架与状态 key | Config/Data | Complete | main-narrative complete |
| pc-002 | 师姐采药/采矿教学内容 | Config/Data | Complete | pc-001 |
| pc-003 | 山庄寿宴跑腿与拖延取酒 | Config/Data | Complete | pc-001 |
| pc-004 | 崖洞回忆互动与夜宿 | Config/Data | Complete | pc-001, ei-009 |
| pc-005 | 灭门后回庄搜证路径 | Config/Data | Complete | pc-001, pc-004 |
| pc-006 | 师兄误会、共同埋葬与临别传承 | Integration | Complete with Dependency Notes | pc-005; misunderstanding-system stories pending |

## Definition of Done

This epic is complete when:

- P00-01 到 P00-10 的序章节点骨架可被主线系统加载或等价地由当前 Godot 场景流程驱动。
- chapter_00 至少包含师姐采集、山庄跑腿、崖洞回忆、灭门搜证、师兄对峙和临别的首版对话/独白内容。
- 灭门前不出现正式战斗教学、外人脚印或明显阴谋提示。
- 崖洞现有 `cave_loose_brick` 内容被替换为“墙上招式刻画”或等价回忆点。
- 灭门后搜证路径能让玩家确认血书、暗格空、师姐失踪和无人拔剑。
- 师兄尾段有可验收的误会对峙、共同埋葬、临别传承和书信约定骨架。
- 每个 story 均有 `production/qa/evidence/` 下的证据文档或对应测试记录。

## Dependency Notes

`pc-006` 依赖误会系统的完整逻辑和 UI 表现。若误会系统 stories 尚未实现，允许先以对话条件、状态 key 和叙事信号完成内容骨架；正式 `MisunderstandingInstance`、透明度状态和 NPC 态度修正接入必须作为后续补强项验收。

## Next Step

Run `/story-readiness production/epics/prologue-content/stories/pc-001-chapter-00-mainline-graph.md`, then `/dev-story` for the selected story.
