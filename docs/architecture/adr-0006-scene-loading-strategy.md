# ADR-0006: Scene Loading Strategy

## Status
Accepted

## Date
2026-06-08

## Engine Compatibility

| Field | Value |
|-------|-------|
| **Engine** | Godot 4.6.3 |
| **Domain** | Foundation / Scene Management |
| **Knowledge Risk** | LOW — ResourceLoader async API 稳定自 4.0+ |
| **References Consulted** | `docs/engine-reference/godot/current-best-practices.md`, `docs/engine-reference/godot/breaking-changes.md` |
| **Post-Cutoff APIs Used** | None (ResourceLoader API unchanged) |
| **Verification Required** | 验证 C# 中 ResourceLoader.LoadThreadedRequest/GetStatus 的绑定正确性 |

## ADR Dependencies

| Field | Value |
|-------|-------|
| **Depends On** | ADR-0003 (场景连通图配置格式) |
| **Enables** | Foundation/SceneManagement 模块, 所有依赖场景切换的系统 |
| **Blocks** | Sprint 5 (SceneManagement 实现) |
| **Ordering Note** | None |

## Context

### Problem Statement

《风止》有 18 地点 × 平均 3 场景 ≈ 54 个场景，每场景有 4 种光照变体（日/晨/昏/夜），部分场景有季节变体。场景切换需要异步加载 + 过渡动画，不能阻塞主线程导致卡顿。需要决定加载策略、预加载范围和过渡状态管理。

## Decision

采用 **Godot ResourceLoader 异步预加载 + 邻接场景预热 + 过渡状态机**。

### 加载策略

| 策略 | 实现 |
|------|------|
| 当前场景 | 完全加载并实例化 |
| 相邻场景 | 异步预加载为 PackedScene（不实例化），存入缓存 |
| 非相邻场景 | 不预加载，进入相邻场景后再预加载其邻居 |
| 缓存淘汰 | LRU，最多缓存 5 个 PackedScene |

### 过渡状态机

```
Active → TransitionOut → Loading → TransitionIn → Active
  ↑                                                  │
  └──────────────────────────────────────────────────┘
```

| 状态 | 行为 |
|------|------|
| Active | 玩家可操作 |
| TransitionOut | 播放过渡动画（fade 1.0s / ink_wash 2.0s），锁定输入 |
| Loading | 黑屏/墨屏，异步加载目标场景（已预加载则跳过） |
| TransitionIn | 应用光照+天气 → 播放入场动画，解锁输入 |

### Key Interfaces

```csharp
public interface ISceneDirector
{
    SceneId CurrentScene { get; }
    TransitionState State { get; }
    Task TransitionTo(SceneId target, string spawnPoint, TransitionType type = TransitionType.Fade);
    bool IsSceneUnlocked(SceneId scene);
    void UnlockScene(SceneId scene);
    IReadOnlyList<SceneId> GetAdjacentScenes(SceneId scene);
}

public enum TransitionType { Fade, InkWash }
public enum TransitionState { Active, TransitionOut, Loading, TransitionIn }
```

### 场景连通图（数据驱动）

```yaml
# assets/data/scenes/connections.yaml
- from: jiangnan_town_center
  to: jiangnan_inn
  exit_point: door_east
  entry_point: door_west
  transition: fade
  bidirectional: true
  unlock_conditions: []

- from: jiangnan_town_center
  to: saibei_pass
  exit_point: north_gate
  entry_point: south_gate
  transition: ink_wash
  bidirectional: true
  unlock_conditions:
    - { source: narrative.node, id: ch01_complete }
```

### 预加载失败处理

- 重试 3 次，间隔 500ms
- 3 次失败：回退到上一场景 + Toast 提示"场景加载失败，建议存档"
- 不崩溃，不丢失游戏状态

## Alternatives Considered

### Alternative 1: 全量预加载（启动时加载所有场景）
- **Rejection**: 54 场景 × 4 变体 = 216 资源，启动时间和内存不可接受

### Alternative 2: 纯按需加载（无预加载）
- **Rejection**: 切换场景时会有明显加载等待（100-500ms），破坏沉浸感

## Consequences

### Positive
- 相邻场景切换几乎无感知延迟（已预加载）
- LRU 缓存控制内存上限
- 数据驱动场景图 — 新增场景不改代码

### Negative
- 首次进入新区域时可能有短暂加载（无相邻缓存）
- 预加载消耗后台 IO 带宽

## GDD Requirements Addressed

| GDD System | Requirement | How This ADR Addresses It |
|------------|-------------|--------------------------|
| map-scene-management.md | 异步预加载相邻场景 | ResourceLoader + 邻接预热 |
| map-scene-management.md | 两种过渡效果 (fade/ink_wash) | TransitionType enum + 状态机 |
| map-scene-management.md | 过渡期间锁定输入 | TransitionOut/Loading 状态下屏蔽 InputEvent |
| map-scene-management.md | 场景连通图数据驱动 | connections.yaml 配置 |
| map-scene-management.md | 加载失败重试 3 次 | preload_retry_count = 3 |

## Performance Implications
- **Memory**: 当前场景 + 5 缓存 PackedScene ≈ 50-100MB
- **CPU**: 异步加载在后台线程，不阻塞主线程
- **Load Time**: 预加载命中时切换 < 50ms；未命中时 300-1000ms（黑屏遮盖）

## Validation Criteria
1. 相邻场景切换无卡顿（帧率不降）
2. 过渡动画完整播放
3. 3 次预加载失败后正确回退
4. LRU 缓存超 5 个时正确淘汰最旧项

## Related Decisions
- [ADR-0003](adr-0003-data-configuration-format.md) — connections.yaml 格式
- [ADR-0010](adr-0010-tilemaplayer-usage.md) — 场景内 TileMapLayer 使用
