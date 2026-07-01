# Epic: 误会系统

> **Layer**: Feature
> **GDD**: design/gdd/misunderstanding-system.md
> **Architecture Module**: `Feature/Misunderstanding/`
> **Status**: Done (Foundation logic complete; Feature/Presentation layers pending external runtime)
> **Stories**: 9 (9 Complete)

## Overview

误会系统实现叙事信息差的逻辑层：注册误会实例、追踪严重度与透明度、递减澄清窗口、写入 NPC 态度修正、触发诀别和澄清弹回。表现层通过朦胧化 UI/对话/音频呈现"隐隐不对劲"，但不暴露倒计时、严重度或澄清条件。

Foundation 层（状态机 + 数据结构 + 事件接口）可独立实施并用 mock/stub 测试。依赖活江湖层(#16)、NPC 状态管理(#10)、感情系统(#13) runtime 的 stories 标为 Blocked。

## Governing ADRs

| ADR | Decision Summary | Engine Risk |
|-----|-----------------|-------------|
| ADR-0012: Misunderstanding UI | 采用 SignalRouter + Dialogue/Panel/Ambience channels 呈现透明度信号 | HIGH |
| ADR-0014: Living Jianghu Layer | 误会可由世界事件、传闻和 day_advanced 驱动 | LOW |
| ADR-0015: Romance System | 普通误会受关系里程碑地板保护，SEVERE 可 force_break | LOW |
| ADR-0002: UI Framework | 关系面板和手柄焦点遵循 Godot dual-focus 框架 | HIGH |

## Stories

| # | Story | Layer | Type | Status | Blocked By |
|---|-------|-------|------|--------|------------|
| ms-001 | Misunderstanding Data Model & State Machine | Foundation | Logic | Complete | — |
| ms-002 | Mod Calculation & NPC State Write | Foundation | Logic | Complete | — |
| ms-003 | Window Countdown & Permanence | Foundation | Logic | Complete | — |
| ms-004 | Transparency Progression | Foundation | Logic | Complete | — |
| ms-005 | Resolution & Bounce-back | Foundation | Logic | Complete | — |
| ms-006 | Trigger Integration — Jianghu & Dialogue | Feature | Integration | Complete | — |
| ms-007 | Romance Floor Protection & Force Break | Feature | Integration | Complete | — |
| ms-008 | Transparency Signal UI (ADR-0012) | Presentation | Visual/Feel | Complete | — |
| ms-009 | Content: Chapter 0 Misunderstanding Instances | Config/Data | Config/Data | Complete | — |

## GDD Requirements

| Requirement | ADR Coverage | Story |
|-------------|--------------|-------|
| 活江湖事件命中 trigger 创建 ACTIVE 误会 | ADR-0014 | ms-006 |
| 对话选择 `mis_trigger` 创建误会 | ADR-0012 | ms-006 |
| 缺席超过阈值触发缺席误解 | GDD 覆盖 | ms-006 |
| `misunderstanding_mod` 取最大贡献并限制在 [-2,0] | GDD 覆盖 | ms-002 |
| 窗口每日递减，归零后转 PERMANENT 或 BROKEN | GDD 覆盖 | ms-003 |
| SEVERE 窗口归零调用 `force_break()` | ADR-0015 | ms-003, ms-007 |
| 透明度 HIDDEN/HINTED/PERCEIVED/URGENT 升级 | ADR-0012 | ms-004 |
| resolution_conditions 满足时 RESOLVED 并重算 mod | GDD 覆盖 | ms-005 |
| 澄清弹回加成持续后消失 | GDD 覆盖 | ms-005 |
| 恶化事件提升 severity | GDD 覆盖 | ms-001 |
| 同 NPC 活跃误会数不超过上限 | GDD 覆盖 | ms-002 |
| 普通误会不击穿感情里程碑地板 | ADR-0015 | ms-007 |
| M_BREAK 状态下不注册新误会 | ADR-0015 | ms-001 |
| 存档/读档后窗口期正确补算 | ADR-0004 | ms-003 |

## Trace Notes

`docs/architecture/tr-registry.yaml` 当前没有 `TR-misunderstanding-*` 条目。此 Epic 拆分为 Logic（Foundation 层可独立测试）、Integration（需 runtime 对接）、Visual/Feel 和 Config/Data 四种 story 类型，确保 HIGH-risk UI 不拖入纯逻辑 story。

## Definition of Done

This epic is complete when:
- All stories are implemented, reviewed, and closed via `/story-done`
- All 14 acceptance criteria from `design/gdd/misunderstanding-system.md` are verified
- Misunderstanding lifecycle, severity, mod calculation, transparency, force_break, floor protection and save catch-up have tests
- No UI exposes countdown days, severity levels, resolution checklist or misunderstanding log
- HIGH-risk UI presentation has manual evidence or focused Godot spike validation

## Next Step

Epic complete. Remaining work for full integration:
- Feature layer: implement `IScenePresenceQuery`, `IRomanceService` real adapters when those systems ship
- Presentation layer: ADR-0012 spike → implement shader/audio channels in Godot
