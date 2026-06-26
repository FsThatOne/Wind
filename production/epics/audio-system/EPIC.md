# Epic: 音乐 / 音效

> **Layer**: Presentation
> **GDD**: design/gdd/audio-system.md
> **Architecture Module**: `Presentation/Audio/`
> **Status**: Ready
> **Stories**: 8 stories created

## Stories

| # | Story | Type | Status | ADR |
|---|-------|------|--------|-----|
| 001 | 音频状态 FSM + AudioBus 初始化 | Logic | Complete | ADR-0009, ADR-0008 |
| 002 | BGM 管理 + 等功率 Crossfade | Logic | Ready | ADR-0009 |
| 003 | 自适应战斗音乐（6 段水平分层） | Logic | Ready | ADR-0009 |
| 004 | 环境音三层系统 | Logic | Ready | ADR-0009 |
| 005 | SFX 优先级仲裁 + 并发池 | Logic | Ready | ADR-0009 |
| 006 | 演出音频接管与跳过恢复 | Integration | Ready | ADR-0009, ADR-0013 |
| 007 | 女主 Motif 叠加 | Logic | Ready | ADR-0009 |
| 008 | 设置音量响应 + 持久化 | Integration | Ready | ADR-0009 |

## Overview

音乐 / 音效系统实现三轨音频架构、6 状态音频 FSM、等功率 crossfade、自适应战斗音乐、环境音分层、BGM override 栈、SFX 优先级仲裁和设置音量响应。系统是被动服务方，只响应上游系统的音频请求，不主动驱动 gameplay。

## Governing ADRs

| ADR | Decision Summary | Engine Risk |
|-----|-----------------|-------------|
| ADR-0009: Dynamic Audio | 自研 C# 音频状态机 + Godot AudioServer bus + override stack + SFX pool | LOW |
| ADR-0008: Finite State Machine | 6 状态音频 FSM 复用泛型状态机 | LOW |
| ADR-0001: EventBus | 通过事件接收战斗、场景、演出、对话和 UI 音频请求 | LOW |

## GDD Requirements

| Requirement | ADR Coverage |
|-------------|--------------|
| 场景 BGM 差异切换时无明显空白或爆音 | ADR-0009 ✅ |
| 战斗音乐随 Prep/Clash/Advantage 等段落切换 | ADR-0009 ✅ |
| 一击决胜触发 Finisher 段落和 P1 音效 | ADR-0009 ✅ |
| 演出跳过时音频淡出并恢复 BGM 栈 | ADR-0009 ✅ |
| Master=0 时状态机仍运转，恢复音量后接当前状态 | ADR-0009 ✅ |
| 对话开始/结束时 BGM 与 Ambient 衰减/恢复 | ADR-0009 ✅ |
| 单帧 SFX 洪水按 P0/P1 优先级保留 | ADR-0009 ✅ |
| 三层 Ambient 同时可辨 | ADR-0009 ✅ |
| 女主偶遇 motif 叠加并 duck 场景 BGM | ADR-0009 ✅ |
| 设置中音量滑块实时生效并持久化 | ADR-0009 ⚠️ 依赖 settings/options 后续 Epic |

## Trace Notes

`docs/architecture/tr-registry.yaml` 当前没有 `TR-audio-*` 条目。创建 stories 时应将 FSM/Crossfade、Combat Segments、Ambient Layers、SFX Arbitration、Settings Integration 拆分。

## Definition of Done

This epic is complete when:
- All stories are implemented, reviewed, and closed via `/story-done`
- All acceptance criteria from `design/gdd/audio-system.md` are verified
- FSM transitions, override stack, crossfade math, combat segment switching and SFX arbitration have tests or runtime evidence
- Audio system remains passive: upstream systems request playback; Audio never queries gameplay state except settings volume
- Asset loading strategy respects BGM streaming and SFX preload constraints

## Next Step

按依赖顺序实施：Story 001 → 002/004/005 → 003/006/007/008。
运行 `/story-readiness production/epics/audio-system/story-001-audio-state-fsm.md` 检查就绪度，然后 `/dev-story` 开始实现。
