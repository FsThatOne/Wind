# Epic: CG / 演出

> **Layer**: Presentation
> **GDD**: design/gdd/cutscene-system.md
> **Architecture Module**: `Presentation/Cutscene/`
> **Status**: Ready
> **Stories**: Not yet created — run `/create-stories cutscene-system`

## Overview

CG / 演出系统是全局演出播放器，负责加载 CutsceneScript、按步骤播放画面/文字/动画/音频、管理全局 LockMode、处理排队、串联与跳过，并保证跳过后 gameplay 副作用仍完整执行。

## Governing ADRs

| ADR | Decision Summary | Engine Risk |
|-----|-----------------|-------------|
| ADR-0013: Cutscene System | CutsceneService + CutsceneQueue + CutsceneDirector + GameStateLock + StepExecutor | MEDIUM |
| ADR-0011: Combat UI Animation | 复用 TimeScaleController 和 CameraRequestBus | HIGH |
| ADR-0009: Dynamic Audio | 演出脚本通过音频系统执行 BGM/SFX 接管 | LOW |
| ADR-0002: UI Framework | CanvasLayer 90+ 覆盖 HUD/Menu/Dialog 等 UI 层 | HIGH |

## GDD Requirements

| Requirement | ADR Coverage |
|-------------|--------------|
| 一击决胜调用 cutscene 后返回战斗画面 | ADR-0013 + ADR-0011 ✅ |
| Tier 2 长按跳过后执行所有 on_complete GameplayEffect | ADR-0013 ✅ |
| 短按确认不跳过 | ADR-0013 ✅ |
| first_view_unskippable 首次观看保护 | ADR-0013 ✅ |
| 顿悟+境界突破串联播放且每段独立 on_complete | ADR-0013 ✅ |
| 两个 Tier 2 请求同时到达时 FIFO 排队 | ADR-0013 ✅ |
| FULL 锁定时阻止存档 | ADR-0013 ✅ |
| Tier 4 微演出不阻止移动 | ADR-0013 ✅ |
| 素材缺失时显示占位而非崩溃 | ADR-0013 ✅ |

## Trace Notes

`docs/architecture/tr-registry.yaml` 当前没有 `TR-cutscene-*` 条目。创建 stories 时应将 GameStateLock、StepExecutor、Skip Safety、Queue/Chain、ResourceFallback 拆分，以便单元和集成测试独立验证。

## Definition of Done

This epic is complete when:
- All stories are implemented, reviewed, and closed via `/story-done`
- All acceptance criteria from `design/gdd/cutscene-system.md` are verified
- Skip safety, lock mode, queueing, chain playback, resource fallback and HUD visibility have tests or manual evidence
- FULL locks reliably block save/system tick while preserving skip input
- Cutscene scripts never own gameplay condition logic; callers own when to request playback

## Next Step

Run `/create-stories cutscene-system` to break this epic into implementable stories.
