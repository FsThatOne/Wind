# Epic: 误会系统

> **Layer**: Feature
> **GDD**: design/gdd/misunderstanding-system.md
> **Architecture Module**: `Feature/Misunderstanding/`
> **Status**: Ready
> **Stories**: Not yet created — run `/create-stories misunderstanding-system`

## Overview

误会系统实现叙事信息差的逻辑层：注册误会实例、追踪严重度与透明度、递减澄清窗口、写入 NPC 态度修正、触发诀别和澄清弹回。表现层通过朦胧化 UI/对话/音频呈现“隐隐不对劲”，但不暴露倒计时、严重度或澄清条件。

## Governing ADRs

| ADR | Decision Summary | Engine Risk |
|-----|-----------------|-------------|
| ADR-0012: Misunderstanding UI | 采用 SignalRouter + Dialogue/Panel/Ambience channels 呈现透明度信号 | HIGH |
| ADR-0014: Living Jianghu Layer | 误会可由世界事件、传闻和 day_advanced 驱动 | LOW |
| ADR-0015: Romance System | 普通误会受关系里程碑地板保护，SEVERE 可 force_break | LOW |
| ADR-0002: UI Framework | 关系面板和手柄焦点遵循 Godot dual-focus 框架 | HIGH |

## GDD Requirements

| Requirement | ADR Coverage |
|-------------|--------------|
| 活江湖事件命中 trigger 创建 ACTIVE 误会 | ADR-0014 + ADR-0012 ⚠️ 需逻辑 story 验证 |
| 对话选择 `mis_trigger` 创建误会 | ADR-0012 ⚠️ 需与 dialogue story 对接 |
| 缺席超过阈值触发缺席误解 | GDD 覆盖，需 story 明确场景/区域契约 |
| `misunderstanding_mod` 取最大贡献并限制在 [-2,0] | GDD 覆盖，需 Logic story 实现 |
| 窗口每日递减，归零后转 PERMANENT 或 BROKEN | GDD 覆盖，需 Logic story 实现 |
| SEVERE 窗口归零调用 `force_break()` | ADR-0015 + ADR-0012 ✅ |
| 透明度 HIDDEN/HINTED/PERCEIVED/URGENT 升级 | ADR-0012 ✅ |
| resolution_conditions 满足时 RESOLVED 并重算 mod | GDD 覆盖，需 Logic story 实现 |
| 澄清弹回加成持续后消失 | GDD 覆盖，需 Logic story 实现 |
| 恶化事件提升 severity | GDD 覆盖，需 Logic story 实现 |
| 同 NPC 活跃误会数不超过上限 | GDD 覆盖，需 Logic story 实现 |
| 普通误会不击穿感情里程碑地板 | ADR-0015 ✅ |
| M_BREAK 状态下不注册新误会 | ADR-0015 ✅ |
| 存档/读档后窗口期正确补算 | ADR-0004 ⚠️ 需 save integration story |

## Trace Notes

`docs/architecture/tr-registry.yaml` 当前没有 `TR-misunderstanding-*` 条目。此 Epic 同时包含逻辑层与 Presentation 信号层，创建 stories 时应拆分为 Logic、Integration、UI/Visual evidence，避免把 UI 高风险拖入纯逻辑 story。

## Definition of Done

This epic is complete when:
- All stories are implemented, reviewed, and closed via `/story-done`
- All acceptance criteria from `design/gdd/misunderstanding-system.md` are verified
- Misunderstanding lifecycle, severity, mod calculation, transparency, force_break, floor protection and save catch-up have tests
- No UI exposes countdown days, severity levels, resolution checklist or misunderstanding log
- HIGH-risk UI presentation has manual evidence or focused Godot spike validation

## Next Step

Run `/create-stories misunderstanding-system` to break this epic into implementable stories.
