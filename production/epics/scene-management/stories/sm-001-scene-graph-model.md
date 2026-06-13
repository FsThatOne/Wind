# Story: sm-001 — Scene Graph Model

> **Epic**: scene-management
> **Type**: Logic
> **Priority**: P0
> **Depends On**: DataRegistry (cd-005)
> **GDD Source**: design/gdd/map-scene-management.md §Core Rules 2 (场景连接图)
> **Status**: Done

## Goal

场景连接图数据模型：SceneConnection record, SceneGraph 类 (邻接查询、路径可达性)，YAML 加载。

## Acceptance Criteria

- [ ] AC1: SceneConnection 包含 from/to/exit_point/entry_point/connection_type/unlock_conditions/transition_type/bidirectional
- [ ] AC2: SceneGraph.GetConnections(sceneId) 返回所有从该场景出发的连接
- [ ] AC3: SceneGraph.GetAdjacentScenes(sceneId) 返回邻接场景列表
- [ ] AC4: bidirectional=true 的连接自动注册反向
- [ ] AC5: 从 YAML 正确加载场景连接配置
- [ ] AC6: 格式错误 → DataLoadException

## Test Evidence Path

`tests/Foundation/SceneManagement/SceneGraphTests.cs`
