# ADR-0010: TileMapLayer Usage Pattern

## Status
Accepted

## Date
2026-06-08

## Engine Compatibility

| Field | Value |
|-------|-------|
| **Engine** | Godot 4.7-stable |
| **Domain** | 2D / Rendering |
| **Knowledge Risk** | **MEDIUM** — TileMapLayer 在 4.3 替代 TileMap，4.6 新增 scene tile rotation |
| **References Consulted** | `docs/engine-reference/godot/breaking-changes.md` (4.2→4.3: TileMapLayer replaces TileMap; 4.6: scene tile rotation) |
| **Post-Cutoff APIs Used** | TileMapLayer scene tile rotation (4.6) |
| **Verification Required** | 验证 C# 中 TileMapLayer 的 rotation 属性对 scene tiles 的行为 |
| **4.7 Re-verification (2026-06-20)** | Engine pin upgraded 4.6.3 → 4.7-stable. Re-verify all post-cutoff APIs above against Godot 4.7-stable; flag any regressions or behavior changes in next `/architecture-review`. |

## ADR Dependencies

| Field | Value |
|-------|-------|
| **Depends On** | ADR-0006 (场景加载策略 — 场景结构已定义) |
| **Enables** | 场景美术制作, 关卡设计工作流 |
| **Blocks** | 场景资产制作（需确定层结构后开始） |
| **Ordering Note** | 在美术开始制作 tileset 前确定 |

## Context

### Problem Statement

《风止》使用 2D 像素 tilemap 构建 54 个场景（18 地点 × 3 场景），每场景有 4 光照变体。Godot 4.3 用 TileMapLayer 替代了旧 TileMap（单节点多层 → 每层一个节点）。4.6 新增 scene tile rotation。需要确定 TileMapLayer 的使用模式和层级规范。

## Decision

采用 **多 TileMapLayer 节点 + 固定层级规范**。

### 层级规范

每个场景包含以下 TileMapLayer 节点（从下到上）：

| 层名 | Z-Index | 用途 | tile_shape (ADR-0022) |
|------|---------|------|----------------------|
| `Ground` | 0 | 地面基底（泥土、石板、水面） | Isometric (DiamondDown, 128×64) |
| `Terrain` | 1 | 地形装饰（草丛、碎石、花） | Isometric (同上) |
| `Structures` | 2 | 建筑、墙壁、家具 | Isometric (同上) |
| `Overlay` | 3 | 屋顶、树冠（遮挡玩家） | Isometric (同上) |
| `Collision` | — | 碰撞层（不可见，仅物理） | Isometric (同上) |

> **ADR-0022 增量**：所有 TileMapLayer 的 `tile_shape = Isometric`、`tile_layout = DiamondDown`、`tile_size = 128×64`、`y_sort_enabled = true`、`y_sort_origin = tile 中心`。详见 [ADR-0022 §4](adr-0022-isometric-projection-and-iso4-animator.md#L132)。该尺寸与丹房完整菱形地块一致，是后续所有地图场景的硬约束。

### 光照变体管理

```
scenes/jiangnan_inn/
├── jiangnan_inn.tscn       # 主场景
├── tilesets/
│   ├── inn_day.tres        # 日间 tileset (正常色调)
│   ├── inn_dawn.tres       # 晨 (暖色偏移)
│   ├── inn_dusk.tres       # 昏 (橙色偏移)
│   └── inn_night.tres      # 夜 (蓝色偏移 + 灯光 tile)
└── ...
```

**切换策略**：不实例化 4 个 tileset → 而是通过 **CanvasModulate + shader** 实现色调偏移：

```csharp
// Foundation/SceneManagement/LightingManager.cs
public void ApplyTimeSlot(TimeSlot slot)
{
    var tint = slot switch
    {
        TimeSlot.Day => new Color(1, 1, 1),
        TimeSlot.Dawn => new Color(1.1f, 0.9f, 0.8f),
        TimeSlot.Dusk => new Color(1.0f, 0.8f, 0.6f),
        TimeSlot.Night => new Color(0.6f, 0.7f, 1.0f),
        _ => Colors.White
    };
    _canvasModulate.Color = tint;
    // 特殊 tile（灯光）在 Night 时 visible = true
}
```

这避免了 4× tileset 资产量（216 → 54 场景 + shader 色调）。

### Scene Tile Rotation (4.6)

- **用途**：重复利用同一 scene tile（如树木、NPC 摊位）的不同朝向
- **规范**：仅用于装饰性 scene tiles，不用于碰撞相关 tiles
- **风险缓解**：sprint 1 spike 验证 rotation 在 C# 中的 API 行为

### 碰撞层规范

| Physics Layer | 用途 |
|---------------|------|
| Layer 1 | 地形碰撞（墙壁、边界） |
| Layer 2 | 交互区域（NPC、物品、出口） |
| Layer 3 | 水域（触发游泳/无法通行） |

### 场景模板

```
SceneRoot (Node2D)
├── TileMapLayer "Ground" (z=0)
├── TileMapLayer "Terrain" (z=1)
├── TileMapLayer "Structures" (z=2)
├── TileMapLayer "Overlay" (z=3)
├── TileMapLayer "Collision" (invisible)
├── CanvasModulate "LightingTint"
├── Node2D "SpawnPoints"
│   ├── Marker2D "door_east"
│   └── Marker2D "door_west"
├── Node2D "NPCs"
│   └── (runtime populated)
└── Node2D "Interactables"
    └── (exits, items, triggers)
```

## Consequences

### Positive
- 多节点层级 — 每层独立控制 z-order、visibility、碰撞
- CanvasModulate 色调 — 避免 4× tileset 资产（节省 162 套额外 tileset）
- Scene tile rotation — 减少重复资产
- 固定模板 — 所有场景结构统一

### Negative
- CanvasModulate 影响整个 Canvas — 如需局部光照（如灯笼）需额外 PointLight2D
- 每场景 5 个 TileMapLayer 节点 — 编辑器中稍显拥挤

## GDD Requirements Addressed

| GDD System | Requirement | How This ADR Addresses It |
|------------|-------------|--------------------------|
| map-scene-management.md | 4 种光照变体 | CanvasModulate 色调切换 |
| map-scene-management.md | 季节变体 | 扩展 LightingManager 支持季节色调 |
| map-scene-management.md | 出口点/入口点 | SpawnPoints/Marker2D 节点 |
| map-scene-management.md | 54 场景统一结构 | 固定场景模板 |

## Validation Criteria
1. Spike：验证 TileMapLayer scene tile rotation C# API
2. Spike：CanvasModulate 色调在像素风格下视觉效果可接受
3. 5 层 TileMapLayer 渲染性能无瓶颈（< 1ms/frame）
4. 碰撞层正确阻挡玩家移动

## Related Decisions
- [ADR-0006](adr-0006-scene-loading-strategy.md) — 场景加载策略
- [ADR-0003](adr-0003-data-configuration-format.md) — 场景元数据 YAML 格式
