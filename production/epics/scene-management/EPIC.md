# Epic: 地图/场景管理

> **Layer**: Foundation
> **GDD**: design/gdd/map-scene-management.md
> **Architecture Module**: `Foundation/SceneManagement/`
> **Status**: Ready
> **Stories**: Not yet created — run `/create-stories scene-management`

## Overview

地图/场景管理系统是《风止》的空间骨架——负责 18 个命名地点的加载、切换、解锁和环境基调控制。实现 `ISceneDirector` 接口，采用 Godot ResourceLoader 异步预加载 + 邻接场景预热 + LRU 缓存（ADR-0006），多 TileMapLayer 五层规范 + CanvasModulate 色调控制（ADR-0010），以及 4 状态过渡状态机（Active/TransitionOut/Loading/TransitionIn）。

## Governing ADRs

| ADR | Decision Summary | Engine Risk |
|-----|-----------------|-------------|
| ADR-0006: Scene Loading Strategy | ResourceLoader 异步预加载 + LRU 缓存(5) + 过渡状态机 | LOW |
| ADR-0010: TileMapLayer Usage Pattern | 多 TileMapLayer 五层规范 + CanvasModulate 色调 | MEDIUM |

## GDD Requirements (from Acceptance Criteria)

| Requirement | ADR Coverage |
|-------------|--------------|
| 异步场景加载和卸载 | ADR-0006 ✅ |
| 邻接场景预热策略 | ADR-0006 ✅ |
| LRU 缓存（最多 5 个 PackedScene） | ADR-0006 ✅ |
| Fade/ink_wash 过渡动画 | ADR-0006 ✅ |
| 场景连通图 (YAML 配置) | ADR-0006 ✅ |
| 区域解锁/访问控制 | ADR-0006 ✅ |
| 五层 TileMapLayer 规范 | ADR-0010 ✅ |
| 章节色调控制 (CanvasModulate) | ADR-0010 ✅ |
| 输入锁定 (过渡期间) | ADR-0006 ✅ |
| 失败重试机制 | ADR-0006 ✅ |

## Engine Risk Notes

- **TileMapLayer scene tile rotation (4.6 新增)** — 需要 Sprint 1 spike 验证 C# API 行为
- ResourceLoader async API 自 4.0+ 稳定，风险 LOW

## Definition of Done

This epic is complete when:
- All stories are implemented, reviewed, and closed via `/story-done`
- All acceptance criteria from `design/gdd/map-scene-management.md` are verified
- All Logic and Integration stories have passing test files in `tests/`
- 18 个场景的连通图正确配置
- 异步加载 + LRU 缓存通过性能测试（≤2s 加载）
- 过渡动画流畅且期间输入被正确锁定
- TileMapLayer spike 通过并记录结果

## Next Step

Run `/create-stories scene-management` to break this epic into implementable stories.
