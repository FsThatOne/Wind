# Smoke Check — Audio System Regression

> **Date**: 2026-06-27
> **Mode**: `/smoke-check sprint`
> **Scope**: `production/epics/audio-system/` Story 001-008
> **Engine**: Godot 4.7-stable Mono
> **Verdict**: PASS

## Environment

- Test directory: found at `tests/`
- CI configured: yes — `.github/workflows/tests.yml`
- Smoke checklist: found at `tests/smoke/critical-paths.md`
- QA plan: latest existing plan is `production/qa/qa-plan-sprint-6-2026-06-18.md`; audio regression used audio-system story evidence instead of the stale Sprint 6 plan.
- Godot executable used for headless launch: `/Applications/Godot_mono.app/Contents/MacOS/Godot`

## Automated Results

| Check | Command | Result |
|-------|---------|--------|
| Audio regression tests | `dotnet test tests/Foundation/Foundation.Tests.csproj --filter "FullyQualifiedName~Audio"` | PASS — 165 passed / 0 failed / 0 skipped |
| Foundation full suite | `dotnet test tests/Foundation/Foundation.Tests.csproj` | PASS — 1568 passed / 0 failed / 0 skipped |
| Godot C# build | `dotnet build feng-zhi/FengZhi.csproj` | PASS — 0 warnings / 0 errors |
| Godot headless launch | `/Applications/Godot_mono.app/Contents/MacOS/Godot --headless --path /Users/bytedance/my-game/feng-zhi --log-file /tmp/fengzhi-audio-smoke-godot-direct.log --quit-after 2` | PASS — project booted to `MainMenu` |
| Whitespace diff check | `git diff --check` | PASS |

## Godot Headless Notes

The command `godot --headless ...` initially failed with `.NET: Assemblies not found` when launched through the `/usr/local/bin/godot` symlink. Directly launching the `.app` executable succeeded:

```text
Godot Engine v4.7.stable.mono.official.5b4e0cb0f
[GameFlow] Boot. EventBus + MindsetService autoload ready.
[MainMenu] Ready. Entry animation kicked off.
```

For future smoke runs on this machine, use `/Applications/Godot_mono.app/Contents/MacOS/Godot` directly or replace the symlink with a shell wrapper that execs the real app executable.

## Audio Story Coverage

| Story | Status | Evidence |
|-------|--------|----------|
| Story 001 — 音频状态 FSM + AudioBus 初始化 | Complete | `tests/unit/audio/audio_state_fsm_test.cs` |
| Story 002 — BGM 管理 + 等功率 Crossfade | Complete | `tests/unit/audio/bgm_crossfade_test.cs` |
| Story 003 — 自适应战斗音乐 | Complete | `tests/unit/audio/combat_music_test.cs` |
| Story 004 — 环境音三层系统 | Complete | `tests/unit/audio/ambient_layers_test.cs` |
| Story 005 — SFX 优先级仲裁 + 并发池 | Complete | `tests/unit/audio/sfx_pool_test.cs` |
| Story 006 — 演出音频接管与跳过恢复 | Complete | `tests/integration/audio/cutscene_audio_test.cs` |
| Story 007 — 女主 Motif 叠加 | Complete | `tests/unit/audio/motif_overlay_test.cs` |
| Story 008 — 设置音量响应 + 持久化 | Complete | `tests/integration/audio/volume_settings_test.cs` |

## Manual Smoke Confirmation

| Batch | Result | Notes |
|-------|--------|-------|
| Core stability | PASS | 用户确认没问题 |
| Audio sprint regression | PASS | 用户确认没问题 |
| Data integrity and performance | PASS | 用户确认没问题 |

## Decision

Audio system regression smoke check passes. Build is ready for QA hand-off for the audio-system slice.
