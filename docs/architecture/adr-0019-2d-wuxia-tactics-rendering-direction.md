# ADR-0019: 2D Wuxia Tactics Rendering Direction

## Status
Accepted

## Date
2026-06-11

## Engine Compatibility

| Field | Value |
|-------|-------|
| **Engine** | Godot 4.7-stable |
| **Domain** | 2D Rendering, TileMapLayer, Lighting, VFX |
| **Knowledge Risk** | **HIGH** — Godot 4.6 在 LLM 训练截止后发布；渲染后端、Glow、Shader Baker、TileMapLayer 等信息必须以本地 engine-reference 为准 |
| **References Consulted** | `docs/engine-reference/godot/VERSION.md`, `docs/engine-reference/godot/modules/rendering.md`, ADR-0010, `design/art/art-bible.md`, `design/gdd/game-concept.md` |
| **Post-Cutoff APIs Used** | TileMapLayer, Godot 4.6 2D Canvas/渲染设置；不依赖实验性 3D/HD-2D 管线 |
| **Verification Required** | 1) 验证 TileMapLayer 多层场景在目标分辨率下的可读性; 2) 验证 CanvasModulate + PointLight2D + 2D shader 的昼夜/灯光效果; 3) 验证移动端兼容目标下的 shader/VFX 预算 |
| **4.7 Re-verification (2026-06-20)** | Engine pin upgraded 4.6.3 → 4.7-stable. Re-verify all post-cutoff APIs above against Godot 4.7-stable; flag any regressions or behavior changes in next `/architecture-review`. |

## ADR Dependencies

| Field | Value |
|-------|-------|
| **Depends On** | ADR-0010 (TileMapLayer Usage Pattern), ADR-0006 (Scene Loading Strategy), ADR-0011 (Combat UI Animation Pipeline) |
| **Enables** | 场景美术制作标准、战棋地图模板、招式 VFX 规范、后续移动端适配评估 |
| **Blocks** | 大规模地图 tileset、角色 sprite、战斗场景 VFX 批量生产 |
| **Ordering Note** | 必须在批量美术资产生产前锁定，避免误按《歧路旅人》式 HD-2D 或 3D/PBR 方向制作资产 |

## Context

项目概念将《风止》定义为 2D 像素武侠 RPG / 叙事驱动 / 回合制战斗，并以《逸剑风云决》作为主要可比作品之一。近期战斗方向进一步收束为战棋式多人战斗，因此视觉目标需要在"江湖氛围"与"战棋可读性"之间取得平衡。

用户已明确决策：本项目参考《逸剑风云决》这类像素与场景融合的武侠表现方式，但不追求《歧路旅人》式 HD-2D 的完整技术标准。

这意味着技术架构应服务于：

- 清楚的战棋格、移动范围、攻击范围和遮挡关系。
- 2D 武侠地图的多层空间感。
- 局部光影、雾效、水墨 shader、招式 VFX 带来的伪 2.5D 江湖氛围。
- 可控的独立开发成本与后续移动端兼容空间。

同时应避免误进入高成本路线：

- 不以《歧路旅人》式 HD-2D 的完整制作标准作为主路线。
- 不以体积光、真实景深、复杂 PBR 材质作为基础资产要求。
- 不把画面表现建立在大量 3D 场景建模、法线贴图、复杂后处理链之上。

## Decision

采用 **2D 武侠战棋 + 轻量伪 2.5D 渲染方向**。

目标表述：

> 本项目采用《逸剑风云决》这类像素与场景融合的武侠呈现思路：以清晰可读的战棋地图、角色朝向、招式范围和武侠氛围为优先，通过多层 TileMapLayer、遮挡层、局部光影、环境特效和招式 VFX 营造伪 2.5D 江湖感；不追求《歧路旅人》式 HD-2D 的完整制作标准与高成本体积光表现。

### D1. 主渲染路线

- 场景以 `Node2D` + 多个 `TileMapLayer` 构建。
- 基础层级沿用 ADR-0010：`Ground`、`Terrain`、`Structures`、`Overlay`、`Collision`。
- `Overlay` 层承担屋檐、树冠、门楼、山石前景等遮挡表现。
- 角色为 2D sprite / sprite sheet / cutout animation，不要求 3D 模型。
- 地图表现采用 2D 透视错觉、层级遮挡、视差、投影贴图、局部灯光来营造空间深度。

### D2. 光影与氛围

- 时间与区域色调优先使用 `CanvasModulate`。
- 灯笼、烛火、洞窟光源等局部效果使用 `PointLight2D` 或等价 2D 光效节点。
- 远景雾、云、水面、雪、落叶使用 2D shader / 粒子 / 分层贴图。
- 战斗技能特效使用 `GPUParticles2D`、sprite sheet、shader material 和短时 screen overlay。
- 后处理只用于轻量水墨、纸纹、色调偏移、低血量暗角等效果，不建立《歧路旅人》式复杂 HD-2D compositor 管线。

### D3. 战棋可读性优先级

任何场景美术和 VFX 必须遵循以下优先级：

1. 当前可移动格、攻击范围、技能范围清晰可读。
2. 角色阵营、朝向、可行动状态清晰可读。
3. 遮挡关系不会隐藏关键操作对象，必要时提供半透明或轮廓显示。
4. 招式 VFX 不遮挡结算信息、伤害数字、关键 UI。
5. 氛围表现服从战斗信息表达。

### D4. 明确不做

- 不做《歧路旅人》式 HD-2D 完整标准。
- 不要求 3D 场景建模作为地图主生产方式。
- 不要求 PBR 材质、法线贴图、体积光、真实景深作为基础标准。
- 不为每个场景制作 3D 灯光烘焙流程。
- 不把移动端兼容建立在高成本桌面后处理效果上。

## Consequences

### Positive

- 美术生产量更适合独立开发和 AI 辅助生产。
- 与现有 ADR-0010 TileMapLayer 场景架构一致。
- 战棋玩法的信息表达更稳定，不被高成本景深和复杂光影干扰。
- 后续移动端兼容空间更大。
- 可先做 vertical slice 验证，再逐步提高美术精度。

### Negative

- 画面上限低于《歧路旅人》式 HD-2D。
- 需要依靠 tileset、构图、色调、VFX 和角色动作来撑起高级感。
- 如果角色 sprite 或地图 tileset 品质不足，画面容易显得廉价。
- 伪 2.5D 遮挡层需要严格模板，否则容易影响寻路和可读性。

## GDD Requirements Addressed

| GDD System | Requirement | How This ADR Addresses It |
|------------|-------------|--------------------------|
| game-concept.md | 2D 像素武侠 RPG / 回合制战斗 / 《逸剑风云决》可比作品 | 参考《逸剑风云决》的像素与场景融合观感，但不采用《歧路旅人》式 HD-2D 完整标准 |
| combat-system.md | 战棋式移动、多人战斗、招式范围表达 | 将战棋可读性列为最高视觉优先级 |
| map-scene-management.md | 多场景、昼夜/季节/光照变体 | 采用 TileMapLayer + CanvasModulate + 2D 光效 |
| combat-ui.md | 战斗 UI、范围、意图、结算反馈 | 限制 VFX 不遮挡关键战斗信息 |
| art-bible.md | 水墨武侠视觉与资产生产标准 | 将 Art Bible 的渲染标准修正为 2D/伪 2.5D |

## Validation Criteria

1. 制作一个 1 屏战棋场景 spike，包含地面、建筑、屋檐遮挡、树冠遮挡、昼夜色调和局部灯光。
2. 场景中至少放置 5 名角色，验证阵营、朝向、可行动状态在 1080p 和 720p 下可读。
3. 实现移动范围、攻击范围、AOE 范围三种 overlay，验证不被地图美术和 VFX 干扰。
4. 实现 2 个招式 VFX：一个近战剑气，一个内功范围技，验证技能表现有武侠味且不遮挡结算信息。
5. 在目标平台上记录性能，确认 60 FPS 下 2D 渲染和 VFX 不超过预算。

## Related Decisions

- [ADR-0010](adr-0010-tilemaplayer-usage.md) — TileMapLayer Usage Pattern
- [ADR-0011](adr-0011-combat-ui-animation.md) — Combat UI Animation Pipeline
- [ADR-0006](adr-0006-scene-loading-strategy.md) — Scene Loading Strategy
