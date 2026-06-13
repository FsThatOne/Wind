# Epic: 战斗 UI

> **Layer**: Presentation
> **GDD**: design/gdd/combat-ui.md
> **Architecture Module**: `Presentation/CombatUi/`
> **Status**: Ready
> **Stories**: Not yet created — run `/create-stories combat-ui`

## Overview

战斗 UI 是 Burst+Read 战斗的信息平台，负责意图图标、HUD 汇总、招式选择面板、资源条、反制提示、破绽与一击决胜演出、协同提示、回合警戒和完整键鼠/手柄导航。它只呈现和收集输入，不拥有战斗结算逻辑。

## Governing ADRs

| ADR | Decision Summary | Engine Risk |
|-----|-----------------|-------------|
| ADR-0002: UI Framework & Dual-Focus | 原生 Godot Control + FocusManager + BaseUiPanel 支持键鼠/手柄双焦点 | HIGH |
| ADR-0011: Combat UI Animation Pipeline | CombatAnimationDirector + Tween + TimeScaleController + CameraRequestBus + 对象池 | HIGH |
| ADR-0009: Dynamic Audio | 战斗 UI 音效通过音频系统订阅/播放 | LOW |

## GDD Requirements

| Requirement | ADR Coverage |
|-------------|--------------|
| `OnIntentRevealed` 后敌方头顶与 HUD 汇总区显示体系图标 | ADR-0011 ✅ |
| 玩家行动时招式面板显示 6 招、消耗、置灰和默认焦点 | ADR-0002 + ADR-0011 ✅ |
| 预览卡显示克制关系和反制提示，但不显示伤害预测 | ADR-0011 ✅ |
| 破绽 ≥5 时高亮一击决胜并显示“破绽！” | ADR-0011 ✅ |
| 一击决胜 TimeScale 降低、动画播放、输入屏蔽和恢复 | ADR-0011 ✅ |
| 两名己方协同克制时浮现“协同！” | ADR-0011 ✅ |
| 12/14 回合计数警戒 | GDD 覆盖，需 UI story 验证 |
| 手柄导航循环且焦点不逃出面板 | ADR-0002 ✅ |
| Godot 4.6 dual-focus 下鼠标 hover 与手柄焦点互不干扰 | ADR-0002 ⚠️ 需 spike evidence |
| 已落败目标收到意图事件时图标槽消隐且不崩溃 | ADR-0011 ✅ |

## Trace Notes

`docs/architecture/tr-registry.yaml` 当前没有 `TR-combat-ui-*` 条目。此 Epic 为 HIGH engine risk，创建 stories 前应优先安排 Godot 4.6 dual-focus / TimeScale Tween spike。

## Definition of Done

This epic is complete when:
- All stories are implemented, reviewed, and closed via `/story-done`
- All acceptance criteria from `design/gdd/combat-ui.md` are verified
- Interaction stories have manual evidence or UI automation where practical
- TimeScale, camera priority, object pool and dual-focus behavior are validated on Godot 4.6.3
- Combat UI does not compute gameplay outcomes or display forbidden damage prediction numbers

## Next Step

Run `/create-stories combat-ui` to break this epic into implementable stories.
