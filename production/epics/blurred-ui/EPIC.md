# Epic: 朦胧化 UI

> **Layer**: Presentation
> **GDD**: design/gdd/blurred-ui.md
> **Architecture Module**: `Presentation/BlurredUi/`
> **Status**: Ready
> **Stories**: Not yet created — run `/create-stories blurred-ui`

## Overview

朦胧化 UI 是战斗外数据表达翻译层，将角色功力、心境区域和关系变化转换为文学化标签、色调、内心独白和环境叙事卡。它不修改上游 gameplay 数据，并严格执行“战斗内清晰、战斗外朦胧”的分治规则。

## Governing ADRs

| ADR | Decision Summary | Engine Risk |
|-----|-----------------|-------------|
| ADR-0002: UI Framework & Dual-Focus | 使用 Godot Control、CanvasLayer、FocusManager 和 shader 不阻断输入 | HIGH |
| ADR-0012: Misunderstanding UI | 误会透明度信号通过 Blurred UI channels 呈现 | HIGH |
| ADR-0001: EventBus | CH-1/CH-2/CH-3 通过事件订阅上游变化 | LOW |

## GDD Requirements

| Requirement | ADR Coverage |
|-------------|--------------|
| 非战斗角色面板以境界文字显示功力，隐藏数值 | ADR-0002 ✅ |
| 战斗 HUD 精确显示 HP/伤害/buff，朦胧组件隐藏 | ADR-0002 ✅ |
| 战斗结束后恢复朦胧模式 | ADR-0002 ✅ |
| total_power 映射境界并播放破境/连破数境文本 | ADR-0002 ⚠️ 需 UI story 验证 |
| 心境 zone 在新场景中渐变为对应色调 | ADR-0002 ✅ |
| 对话中 zone 切换延迟到下一场景 | ADR-0002 ✅ |
| 关系变化通过环境叙事卡/语气变化体现，无数字提示 | ADR-0012 ✅ |
| `force_break()` 立即强制演出，不进入 pending 队列 | ADR-0012 ✅ |
| pending_reveals 队列超过阈值时合并 | GDD 覆盖，需 Logic/UI story 验证 |
| pending_reveals 随存档恢复 | GDD + ADR-0004 ⚠️ 需 save integration story |
| 相对境界以文学模板显示 | GDD 覆盖，需 UI story 验证 |
| Tint Overlay 与队列查询满足性能预算 | ADR-0002 ⚠️ 需 profiling evidence |
| 关闭色调偏移时以文字提示替代 | GDD 覆盖，需 settings integration story |

## Trace Notes

`docs/architecture/tr-registry.yaml` 当前没有 `TR-blurred-ui-*` 条目。此 Epic 横跨 UI、心境、关系和存档，创建 stories 时应分离 CH-1/CH-2/CH-3 与误会信号集成。

## Definition of Done

This epic is complete when:
- All stories are implemented, reviewed, and closed via `/story-done`
- All acceptance criteria from `design/gdd/blurred-ui.md` are verified
- Non-combat UI exposes no forbidden precise growth/mindset/relationship values
- Dual-focus, shader input pass-through, tint overlay and queued reveal behavior have evidence
- Pending reveal persistence is covered by integration tests

## Next Step

Run `/create-stories blurred-ui` to break this epic into implementable stories.
