# ADR-0022: Isometric Diamond Projection & 4-Directional Character Animator

## Status
Accepted

## Date
2026-06-22

## Supersedes
[ADR-0021](adr-0021-character-animation-port.md) — §扩展点 1 (8 方向序列帧子接口)
仅 **§扩展点 1 / 32-frame asset spec / EightDirection enum + Adapter** 部分被本 ADR 取代；
ADR-0021 的主端口 `ICharacterAnimator` + `Facing` 2 向契约 **保持不变并继续生效**。

## Summary

将《风止》的几何投影从「正交方格 + 2 / 8 朝向」改为「等距菱形（Isometric Diamond）+ 4 斜方向（NE / SE / SW / NW）」，
对齐《大侠立志传》战棋 / 《暗黑破坏神》系列的经典 isometric 表现。同步：

1. 锁定坐标系 / Tile 几何 / 深度排序规则（cart↔iso 投影 + y-sort）
2. 取代 ADR-0021 §扩展点 1，将 8 方向兄弟接口收窄为 4 斜方向 `IIso4CharacterAnimator`
3. 输入映射：WASD → NW / SW / SE / NE（屏幕直觉式 isometric 约定）
4. ADR-0010 五层结构 + ADR-0020 纯 2D 路线 **不变**；仅 TileMapLayer 的 `tile_shape` 从 `Square` 改为 `Isometric`

## Engine Compatibility

| Field | Value |
|-------|-------|
| **Engine** | Godot 4.7-stable |
| **Domain** | 2D Rendering, TileMapLayer, Sprite Animation, Input Mapping |
| **Knowledge Risk** | **MEDIUM** — TileMapLayer `Isometric` shape 在 4.3+ 稳定；`y_sort_enabled` + `y_sort_origin` 行为在 4.6 有微调，需验证 |
| **References Consulted** | ADR-0010, ADR-0020, ADR-0021, `docs/engine-reference/godot/modules/rendering.md`, `design/gdd/combat-system.md` §战棋空间规则 |
| **Post-Cutoff APIs Used** | TileMapLayer.TileShape = Isometric (4.3+), Node2D.YSortEnabled (long-stable) |
| **Verification Required** | 1) iso TileMapLayer 在 1080p / 720p 下 5×5 战棋格的可读性；2) 多角色站位的 y-sort 正确性；3) `CavePlayer` 4 斜向 WASD 输入的体感；4) 鼠标 hover ↔ tile 拾取的 iso 反投影精度 |

## ADR Dependencies

| Field | Value |
|-------|-------|
| **Depends On** | ADR-0010 (TileMapLayer 五层结构), ADR-0020 (纯 2D 路线), ADR-0021 (主端口 `ICharacterAnimator` 不变) |
| **Enables** | Sprint 7 VS 江南 explore / 战斗场景的 iso 重建；后续所有战棋关卡 / 探索场景一致使用 iso 投影 |
| **Blocks** | 新增任何方格俯视场景；ADR-0021 §扩展点 1 路径的 8 方向资产生产 |
| **Ordering Note** | 必须在 S7-VS-Combat-Loop 之前接受；否则 VS 战斗场景会按旧方格投影搭建造成返工 |

## Context

### Problem Statement

项目 GDD 早期设定为「方形棋盘 + 4 朝向（上下左右）」（[combat-system.md §战棋空间规则](../../design/gdd/combat-system.md#L52-L61)），
ADR-0021 §扩展点 1 为探索场景（CavePlayer）额外引入了 8 方向序列帧子接口与 Adapter，
导致 **美术风格定位** 与 **技术实现路径** 之间出现两套并行约定：

- 战斗：方形棋盘 + 上下左右 4 向
- 探索：方形俯视 + 8 向（NE / SE / SW / NW + N / S / E / W）

用户 2026-06-22 决策将整体投影改为《大侠立志传》《暗黑》《仙剑》系列的 **isometric diamond + 4 斜向**：

- 一套投影规则同时支撑战斗与探索
- 4 斜向即可完整覆盖屏幕的 ↗ ↘ ↙ ↖ 四个直觉方向
- 避免 8 向资产的过度生产（每名角色 32 张行走帧 × N 状态）
- 保留 ADR-0020 纯 2D 路线（不引入伪 3D / 深度图等技术）

### Constraints

- 逻辑层（FengZhi.Foundation.Combat / Animation）保持无状态、Port/Adapter 严格分层
- 不破坏 ADR-0021 主端口 `ICharacterAnimator` 已稳定的 2 向 `Facing` 契约
- Foundation 测试无回归（当前基线 1378/1378）
- 不引入 Godot 之外的几何库；cart↔iso 变换以纯 C# 实现并单元测试覆盖

### Requirements

- **R1** 战棋逻辑坐标保持 `(int X, int Y)`，渲染层负责投影到屏幕；**逻辑层零 isometric 知识**
- **R2** 引入 4 斜方向枚举 `Iso4Direction { NE, SE, SW, NW }` 与子接口 `IIso4CharacterAnimator`
- **R3** 至少一个真实 Adapter `Iso4AnimatedSprite2DAnimator` + Fake `FakeIso4CharacterAnimator`
- **R4** 输入：WASD 默认映射 W=NW / A=SW / S=SE / D=NE；可在 InputMap 重映射
- **R5** TileMapLayer `tile_shape = Isometric`，单 tile 几何参数公开为常量（`TileWidth = 64 / TileHeight = 32`）

## Decision

### 1. 投影几何

```
              (0,0) cart → screen (0, 0)
                ◇
              ◇   ◇         cart (1, 0) → screen (+W/2, +H/2)
            ◇   X   ◇       cart (0, 1) → screen (-W/2, +H/2)
              ◇   ◇         cart (1, 1) → screen (   0,    +H)
                ◇
```

变换公式（C# 纯函数 / Foundation 层）：

```csharp
// FengZhi.Foundation.Geometry.IsoProjection
public static class IsoProjection
{
    public const float TileWidth = 64f;
    public const float TileHeight = 32f;

    public static Vector2 CartToScreen(Vector2 cart) => new(
        (cart.X - cart.Y) * TileWidth  * 0.5f,
        (cart.X + cart.Y) * TileHeight * 0.5f);

    public static Vector2 ScreenToCart(Vector2 screen) => new(
         screen.X / TileWidth  + screen.Y / TileHeight,
        -screen.X / TileWidth  + screen.Y / TileHeight);
}
```

### 2. 4 斜方向枚举与子接口

```csharp
public enum Iso4Direction { NE, SE, SW, NW }

public interface IIso4CharacterAnimator : ICharacterAnimator
{
    Iso4Direction? CurrentDirection { get; }
    void SetMovementVector(Vector2 movement);  // cart 空间向量
}
```

方向选区（cart 空间，atan2 后分 4 区间，含 ±15° 迟滞抑制抖动）。
Sector 边界在 cart 对角 (±π/4, ±3π/4)，**右闭左开**区间把 §3 WASD 对角向量稳定地置入正确 sector：

| atan2(y, x) 区间 | Iso4Direction | 屏幕方向 | 中心 (cart 卡式轴) | 含 §3 WASD 边界点 |
|---|---|---|---|---|
| (-3π/4, -π/4] | NE | ↗ | cart (0, -1) atan2 = -π/2 | D cart (+1, -1) atan2 = -π/4 |
| (-π/4, π/4]   | SE | ↘ | cart (+1, 0) atan2 = 0    | S cart (+1, +1) atan2 = +π/4 |
| (π/4, 3π/4]   | SW | ↙ | cart (0, +1) atan2 = +π/2 | A cart (-1, +1) atan2 = +3π/4 |
| (3π/4, π] ∪ [-π, -3π/4] | NW | ↖ | cart (-1, 0) atan2 = ±π | W cart (-1, -1) atan2 = -3π/4 |

> **Erratum 2026-06-23**：原稿区间表用左闭右开（`[a, b)`），使 W key 的 atan2 = -3π/4
> 落入 NE 区间 [-3π/4, -π/4) 而非 §3 期望的 NW。本表改为右闭左开（`(a, b]`）保持 §2/§3 一致；
> Sector 几何不变，仅边界包含性反转。
> 由 `Iso4AnimatedSprite2DAnimator` / `FakeIso4CharacterAnimator` 实现兜底验证，
> 含 `WASD_CartDiagonals_FallInSectorCenters` 4 case Theory 测试。

Adapter 内部动画名约定：`walk_ne` / `walk_se` / `walk_sw` / `walk_nw`（+ `idle` 兜底）。

### 3. 输入映射

| 键 | cart 向量（归一化前） | 方向 |
|---|---|---|
| W | (-1, -1) | NW |
| A | (-1, +1) | SW |
| S | (+1, +1) | SE |
| D | (+1, -1) | NE |

WASD → 屏幕直觉方向（W=屏幕上、D=屏幕右…）通过 cart 向量 + iso 投影自然得到。
玩家在 Iso 世界中"按 W 角色走屏幕的左上"——这是 isometric 游戏的标准约定。

### 4. TileMapLayer 配置（ADR-0010 增量）

ADR-0010 五层结构沿用；每层 TileMapLayer 节点的属性：

| 属性 | 旧（方格） | 新（iso） |
|---|---|---|
| `tile_shape` | `Square` | `Isometric` |
| `tile_layout` | n/a | `DiamondDown` |
| `tile_size` | 32×32 暂定 | `64×32`（W/H 比 2:1） |
| `y_sort_enabled` | false | **true**（让 sprite 按 y 自动深度排序） |
| `y_sort_origin` | 0 | tile 中心 |

> **2026-06-23 校准**：经 64×32 与 128×64 可视化对比后，当前默认 isometric tile 规格改为 64×32。该尺寸用于 Sprint 7 iso foundation、后山涯洞试制 tileset 与 Tiled 分层地图，以优先验证探索可读性、碰撞、逻辑标记和角色脚底对齐。128×64 保留为后续高清/正式战棋镜头的重制候选，不作为当前默认规格。

`Background` 节点（ADR-0020 D1 追加项）保持 `Sprite2D`，但 `y_sort_origin` 应设为远低于战棋面，避免被遮挡。

### 5. 与 ADR-0021 的关系

| ADR-0021 元素 | 命运 |
|---|---|
| `ICharacterAnimator` 主接口 | ✅ **保留不变** |
| `Facing { Left, Right }` 2 向 | ✅ **保留不变**（用于 sprite FlipH，与 4 斜向正交） |
| `CharacterAnimState` 枚举 | ✅ **保留不变** |
| §扩展点 1 `IDirectionalCharacterAnimator` (8 向) | ❌ **被本 ADR 替换** |
| `EightDirection` enum | ❌ **删除**（替换为 `Iso4Direction`） |
| `EightDirectionAnimatedSprite2DAnimator` | ❌ **删除**（替换为 `Iso4AnimatedSprite2DAnimator`） |
| §扩展点 2 (Skeleton2D / DragonBones / Spine) | ✅ **保留方向**（与 iso 投影正交） |
| 32-frame 8-direction sprite spec | ❌ **作废**（替换为 16-frame 4-direction spec） |

## Alternatives Considered

### Alternative 1：保留 8 向 Adapter，只把 TileMap 改为 isometric
- **Pros**：动画契约不动
- **Cons**：4 个正交方向（N / S / E / W）在 iso 屏幕上视觉对应"半身侧旋"，与 NE / SE / SW / NW 真正的"正面斜走"风格冲突；浪费 4 张资产
- **Rejection Reason**：违背"屏幕 ↗ ↘ ↙ ↖ 与角色朝向直觉一致"的核心诉求

### Alternative 2：菱形 tile + 上下左右 4 正交向（保留 GDD 原始描述）
- **Pros**：GDD 描述零改动
- **Cons**：isometric 视角下"上下左右"是斜方向；GDD 文本中的"上下左右"本意就是逻辑坐标轴，与屏幕方向无关——保留旧文字会造成长期歧义
- **Rejection Reason**：GDD §战棋空间规则后续用 "屏幕 NE / SE / SW / NW（逻辑 X+ / Y+ / X- / Y-）" 双轴标注更清晰

### Alternative 3：Dimetric 2:1（菱形但更扁，类 Tactics Ogre）
- **Pros**：观感介于两者之间
- **Cons**：tile_size 校准更复杂；与"《大侠立志传》"参考目标偏离
- **Rejection Reason**：用户已明确选 isometric diamond

## Consequences

### Positive
- 视觉与《大侠立志传》战棋 / 仙剑《大唐》一致，玩家"看到就懂"
- 一套投影统一战斗 / 探索，不再两套朝向约定
- 4 斜向资产 = 16 帧（4 方向 × 4 帧）vs 旧 8 向 32 帧，**资产量砍半**
- 逻辑层零 iso 知识，未来若换回方格仅需替换投影函数与 Adapter

### Negative
- ADR-0021 §扩展点 1 路径的代码 / 资产作废（11 单测、Foundation 2 个类文件、`feng-zhi/assets/character/main_character_16bit_8dir/` 32 帧）
- 角色 sprite 美术需重新生成 4 张斜向 walk（或在现有 8 张中保留 NE / SE / SW / NW 4 张）
- VS 主场景搭建需用 isometric TileMapLayer，scene-markup-tool 可能需要小幅适配 iso 坐标拾取

### Risks

| Risk | Probability | Impact | Mitigation |
|---|---|---|---|
| `y_sort_enabled` 在多 TileMapLayer 之间表现不一致 | Medium | High | Spike 阶段在 5×5 grid + 3 角色 + Overlay 树冠验证；不行则改用手动 z_index |
| WASD → 斜方向映射玩家陌生（习惯了正交） | Medium | Medium | 默认 iso 映射 + 设置中允许切换到"屏幕直觉模式"（按 W 走世界 N+）— 留为 backlog 不本 ADR 实现 |
| iso 投影下鼠标精确拾取 tile 复杂度 | Low | Medium | `ScreenToCart` 已单测；战棋落子用 `Vector2.Floor()` |
| 现有 SpriteFrames 资源 .tres 引用 8 个动画名 | High | Low | 重建 `.tres` 同时迁移，新增 lint 守护 |

## Migration Plan

| 阶段 | 内容 |
|---|---|
| 1 | 本 ADR 接受 + S7-Iso-Pivot-Foundation story 拆解（同批落盘） |
| 2 | Foundation 层：新增 `Geometry/IsoProjection.cs` + `Animation/Iso4Direction.cs` + `Animation/IIso4CharacterAnimator.cs` + `Animation/GodotIntegration/Iso4AnimatedSprite2DAnimator.cs` + `Animation/Fakes/FakeIso4CharacterAnimator.cs`；删除 8 向同名文件；测试从 11 → 8 重写 |
| 3 | Asset：重建 `feng-zhi/assets/character/main_character.tres` 为 `idle + walk_ne + walk_se + walk_sw + walk_nw`；新 sprite 由 generate2dsprite 生成 4 张斜向走路 sheet（保留旧 8 向中 4 张斜向可作首版） |
| 4 | feng-zhi：`StartCave.tscn` 改 TileMapLayer 为 Isometric；`CavePlayer.cs` 切换到 `IIso4CharacterAnimator`；输入映射 WASD → cart 向量 |
| 5 | Doc：GDD `combat-system.md` §战棋空间规则补 iso 标注；ADR-0010 §层级规范追加 iso tile_shape 列；ADR-0021 Status 改为 "Partially Superseded by ADR-0022 (§扩展点 1)"；art-bible.md Reference Board 补 isometric 注 |

**Rollback plan**：保留 8 向代码于 `archive/animation-8dir-2026-06-22/` 一个 sprint（不入构建），Sprint 8 retro 时若 isometric playtest 失败可一次性回滚。

## Validation Criteria

1. Foundation 测试 1378 ± Δ：删除 11 旧测试，新增 ≥ 8 测试覆盖 `IsoProjection` 双向变换 + `Iso4Direction` 选区 + 迟滞 + 帧相位保持
2. `feng-zhi/StartCave.tscn` 实机：WASD 走 4 斜向，sprite 切换无残影，y-sort 正确
3. VS 战棋场景 spike：5×5 iso grid + 3 角色 + 1 棵树（Overlay），角色绕树走时遮挡正确
4. 鼠标 hover 落子：屏幕坐标 → cart 坐标 → tile (0..4, 0..4) 误差 ≤ 1 像素

## GDD Requirements Addressed

| GDD System | Requirement | How This ADR Addresses It |
|---|---|---|
| combat-system.md §战棋空间规则 | 方格 + 4 朝向 | 改为 iso diamond + 4 斜向；逻辑坐标 (X, Y) 不变 |
| map-scene-management.md | 场景视觉一致性 | 所有 TileMapLayer 统一 iso |
| art-bible.md Reference Board | 《大侠立志传》 | 补 "isometric diamond projection" 注 |

## Related Decisions

- [ADR-0010](adr-0010-tilemaplayer-usage.md) — TileMapLayer 五层结构（追加 iso tile_shape）
- [ADR-0020](adr-0020-pure-2d-wuxia-rendering-direction.md) — 纯 2D 路线（兼容）
- [ADR-0021](adr-0021-character-animation-port.md) — §扩展点 1 被本 ADR 替换
