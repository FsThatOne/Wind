# ADR-0020: Pure 2D Wuxia Rendering Direction (Daxia Lizhi Zhuan Style)

## Status
Accepted

## Date
2026-06-22

## Last Verified
2026-06-22

## Supersedes
[ADR-0019](adr-0019-2d-wuxia-tactics-rendering-direction.md) — 2D Wuxia Tactics Rendering Direction (伪 2.5D / 《逸剑风云决》方向)

## Summary

将《风止》的美术与渲染方向从"《逸剑风云决》式伪 2.5D / 准 HD-2D"调整为"《大侠立志传》式纯 2D"：场景、角色、UI 全部以原生 2D 资产呈现，**不**再使用伪 3D 视差、动态 2D 灯光、3D 化构图等需要技术美术与复杂后处理才能撑起的表现手段。该决定基于团队规模、独立开发节奏和现有 AI 美术工具链的可控性。

## Engine Compatibility

| Field | Value |
|-------|-------|
| **Engine** | Godot 4.7-stable |
| **Domain** | 2D Rendering, TileMapLayer, UI, Sprite Animation |
| **Knowledge Risk** | **MEDIUM** — 仅使用 Godot 经典 2D 子集（Node2D、TileMapLayer、CanvasModulate、Sprite2D、AnimatedSprite2D），均为长期稳定 API；新版本回归风险低 |
| **References Consulted** | `docs/engine-reference/godot/VERSION.md`, `docs/engine-reference/godot/modules/rendering.md`, ADR-0010, ADR-0019, `design/art/art-bible.md`, `design/gdd/game-concept.md` |
| **Post-Cutoff APIs Used** | TileMapLayer（4.4+），其他均为 4.0 长期稳定 2D API |
| **Verification Required** | 1) 验证纯 2D 战棋场景在 1080p / 720p 下的可读性；2) 验证立绘对话 + 场景小人切换的视觉一致性；3) 验证关闭所有 2D 动态光照后的氛围表现是否足够撑场 |

## ADR Dependencies

| Field | Value |
|-------|-------|
| **Depends On** | ADR-0010 (TileMapLayer Usage Pattern), ADR-0006 (Scene Loading Strategy) |
| **Enables** | 场景美术制作标准（纯 2D 版）、战棋地图模板、立绘资产规范、招式 VFX 规范（轻量 2D sprite / particle）、移动端兼容评估 |
| **Blocks** | 大规模地图 tileset、立绘批量生产、角色 sprite、战斗场景 VFX 批量生产 |
| **Ordering Note** | 必须在批量美术资产生产前锁定。本 ADR 取代 ADR-0019，避免误按伪 2.5D / HD-2D / 3D 方向制作资产 |

## Context

### Problem Statement

项目此前以《逸剑风云决》作为主要视觉可比作品，其呈现方式介于 2D 战棋与"伪 2.5D 场景"之间——3D 拼接背景、动态光照、轻度后处理、像素角色 Billboard。这条路线对独立 + AI 协作团队的隐性成本远高于预期：

- 场景需要技术美术介入处理"2D 角色融入有光照的场景"的违和感
- 多层视差遮挡的搭建会拖慢地图迭代速度
- PointLight2D + CanvasModulate + 后处理的调参经验成本高
- AI 生成像素素材直接放进伪 2.5D 场景时一致性差，需要大量手工修补

经评估后，团队决定将参考目标切换为《大侠立志传》——典型的"立绘对话 + Q 版小人探索 + 2D 战棋"风格。该风格美术成本可预测、画师交付即所见、无需技术美术保驾，更契合独立开发节奏。

### Constraints

- 单人 + AI 协作，无专职技术美术
- 8–14 个月独立开发时间窗口
- 目标平台 PC（Steam），后续考虑 Switch / 移动端 — 不能为高成本后处理上限做赌注
- 必须沿用既有 GDD 中的"战棋战斗 + 心境演变 + 章节色调"诉求

### Requirements

- 视觉风格在《大侠立志传》可参考范围内可达
- 纯 2D 资产管线：立绘、sprite、tileset、UI、特效 sprite — 无 3D 模型、无烘焙
- 战棋可读性保持最高优先级（沿用 ADR-0019 的 D3）
- 章节色调演变（江南 / 北方 / 塞外 / 终幕）通过分层背景 + CanvasModulate 整体调色实现，不依赖动态光源
- 招式 VFX、心境演变等情绪节点继续支持，但用 2D sprite sheet / GPUParticles2D / shader 实现，不引入"伪 3D 体积感"

## Decision

采用 **纯 2D 武侠战棋（《大侠立志传》风格）** 渲染方向。

目标表述：

> 本项目采用《大侠立志传》式的纯 2D 武侠呈现：场景由手绘 tileset + 分层 2D 背景拼接而成，角色为 Q 版 sprite + 高头身立绘组合，对话以立绘 + 文本框呈现，战斗为 2D 战棋格 + sprite 小人 + 2D VFX；不使用伪 2.5D 视差遮挡、动态 2D 光照、3D 化构图或重后处理链；情绪与章节氛围通过分层背景、CanvasModulate 整体调色和 VFX 实现。

### D1. 主渲染路线（更新）

- 场景以 `Node2D` + 多个 `TileMapLayer` 构建，**沿用 ADR-0010 的 5 层结构**：`Ground`、`Terrain`、`Structures`、`Overlay`、`Collision`（详见 ADR-0010 §层级规范）。
- 在 ADR-0010 五层之外，再在场景根下增加一个 `Background` 节点（普通 `Sprite2D` / `TextureRect`，**不是** TileMapLayer），用于承载手绘远景背景画；其 z-index 低于 `Ground`。这是本 ADR 相对 ADR-0010 唯一的层级扩展。
- **不再**使用多层视差背景或滚动 ParallaxLayer 模拟纵深；远景就是一张（或固定的几张）静态分层 2D 画。
- 角色：
  - 探索 / 战斗：2D sprite / sprite sheet（Q 版 3–4 头身）
  - 对话 / 关键演出：6.5–7.5 头身立绘 + 表情切换
- 地图深度通过**美术构图**（透视消失点 + 大小关系 + 色调递减）表现，不通过引擎层视差或光照表现。

### D2. 光影与氛围（重大变更）

- **场景级色调**：仅使用 `CanvasModulate` 做整体偏色（昼夜 / 章节 / 心境）。
- **不使用** `PointLight2D` / `DirectionalLight2D` 作为常规氛围手段；如果未来某些演出需要点光（如灯笼特写、洞窟火把），按"特例 VFX"逐个 PR 评审。
- **不做**动态光照渲染屋檐 / 墙体投影；阴影由 tile 美术直接画进贴图。
- 远景雾、云、雪、落叶：使用循环滚动的 2D sprite 或 GPUParticles2D，不使用 shader 模拟体积。
- 战斗 VFX：sprite sheet + GPUParticles2D + 短时 screen overlay；shader 仅用于水墨笔触、纸纹叠加等纯 2D 后处理（不涉及景深 / bloom / 体积光）。

### D3. 战棋可读性优先级（沿用）

任何场景美术和 VFX 必须遵循以下优先级（与 ADR-0019 一致）：

1. 当前可移动格、攻击范围、技能范围清晰可读
2. 角色阵营、朝向、可行动状态清晰可读
3. 遮挡关系不会隐藏关键操作对象，必要时提供半透明或轮廓显示
4. 招式 VFX 不遮挡结算信息、伤害数字、关键 UI
5. 氛围表现服从战斗信息表达

### D4. 明确不做（更新）

- **不做**伪 2.5D / HD-2D / 准 HD-2D 任何变体
- **不做** 3D 场景建模、PBR 材质、法线贴图、体积光、真实景深
- **不使用**动态 2D 光照（PointLight2D / DirectionalLight2D）作为常规氛围手段
- **不使用**多层视差背景模拟空间纵深
- **不建立** 复杂后处理 compositor 管线
- **不做** Billboard 化 2D 角色融入"有立体感场景"的视觉融合工作

### D5. 立绘 / Sprite 双轨制（新增）

借鉴《大侠立志传》明确两套角色资产规范：

| 用途 | 资产 | 头身比 | 表现 |
|------|------|--------|------|
| 对话 / 关键剧情 / UI 头像 | 立绘 + 多表情 | 6.5–7.5 | 高细节，水墨风手绘 |
| 场景探索 / 战棋战斗 | Sprite / Sprite Sheet | 3–4 | Q 版，朝向 + 武器剪影清晰 |

两者**不混用**：场景中不出现立绘比例角色；立绘镜头中不出现 Q 版角色。

## Alternatives Considered

### Alternative 1: 继续 ADR-0019 的伪 2.5D 路线

- **Description**：保持《逸剑风云决》风格，伪 2.5D + 多层视差 + 局部 2D 光照
- **Pros**：画面上限更高，氛围沉浸感更强
- **Cons**：技术美术依赖、调参成本、AI 素材一致性差、迭代慢
- **Rejection Reason**：超出独立 + AI 协作的可控范围

### Alternative 2: 折中方案 — 部分场景伪 2.5D，部分纯 2D

- **Description**：城镇 / 关键演出场景采用伪 2.5D，普通战棋地图采用纯 2D
- **Cons**：两套美术管线并行，工作量翻倍且风格割裂
- **Rejection Reason**：违背 KISS 与范围纪律

### Alternative 3: 走向更朴素的 RPG Maker 式 2D

- **Description**：完全使用网格化 2D 资产，无立绘双轨
- **Cons**：损失关键剧情演出感和"情感锚点"质量
- **Rejection Reason**：违背 GDD 中 "Narrative 优先级 1" 的美学诉求

## Consequences

### Positive

- 美术管线大幅简化，画师 / AI 出图所见即所得
- 资产生产可预测，便于 8–14 个月时间窗口排期
- 移动端兼容空间更大
- 立绘 + sprite 双轨制让"对话演出"和"战棋玩法"各自能在最合适的尺度上呈现
- 无需技术美术介入，降低人力门槛

### Negative

- 画面上限低于伪 2.5D 路线
- 场景空间深度完全依赖美术构图，对 tileset 画师水平要求更高
- 失去动态光照带来的"灯笼下行人 / 烛火摇曳"等小品感，需要靠特例 VFX 弥补

### Neutral

- 沿用 ADR-0010 TileMapLayer 架构，不需要重写场景层级代码
- 现有立绘资产规范无需变更
- VFX 管线技术栈不变（仅约束不引入 3D 化效果）

## Risks

| Risk | Probability | Impact | Mitigation |
|------|------------|--------|-----------|
| 纯 2D 场景在表现章节色调演变时缺少层次 | 中 | 中 | 通过分层背景 + CanvasModulate + 章节专属 tileset 三层叠加 |
| 失去动态光照后夜景 / 洞窟氛围不足 | 中 | 中 | 美术层级烘焙阴影；特例场景允许 PointLight2D 但需 PR 评审 |
| 立绘 / sprite 双轨制造成同一角色两次设计 | 高 | 低 | 在 art-bible 中明确两套规范的设计对齐流程 |
| 已生产的伪 2.5D 倾向资产需要重制 | 低 | 中 | 当前尚未批量生产，仅 art-bible + ADR-0019 + 概念稿需要调整 |

## Migration Plan

1. **本次提交**：
   - 标记 ADR-0019 为 Superseded（已完成）
   - 创建本 ADR（ADR-0020）
   - 更新 `design/art/art-bible.md` 的 Reference Board / Rendering Style / LOD 表 / Audit History
   - 更新 `design/gdd/game-concept.md` 的 Comparable Titles / Inspiration 表 / 用户画像 / Market Risk
   - 更新 `design/gdd/item-system.md` 的设计哲学与参考体验段
2. **后续工作（非本 ADR 范围）**：
   - 美术 spike：制作 1 屏《大侠立志传》风格的 vertical slice，验证场景 + 立绘对话 + 战棋格的协同
   - 复审 `production/epics/combat-system/EPIC.md` 中对 ADR-0019 的引用，必要时追加 ADR-0020 的引用
   - 复审 architecture-review，在下次评审中纳入 ADR-0020

**Rollback plan**：若 vertical slice 表明纯 2D 表现力不足以承载 "Narrative 优先级 1" 与 "心境视觉演变"，可以：
- 先在不重构既有资产的前提下，恢复 CanvasModulate + 极少量 PointLight2D（关键演出特例）
- 仍然不重新启用伪 2.5D 视差与 3D 拼接背景（这是回滚红线）

## Validation Criteria

1. 制作 1 屏《大侠立志传》风格的战棋场景 spike，包含地面 / 建筑 / 树冠遮挡（仅画在贴图里）/ 章节色调（CanvasModulate）。
2. 同场景中至少放置 5 名 sprite 角色，验证阵营 / 朝向 / 可行动状态在 1080p 和 720p 下可读。
3. 触发一次立绘对话 → 战斗 → 战后立绘对话的完整流程，验证立绘 ↔ sprite 切换的视觉过渡。
4. 实现 1 个招式 VFX（近战剑气）+ 1 个章节色调切换演出，验证不使用动态光照仍能传达情绪。
5. 在目标平台上记录性能，确认 60 FPS 下 2D 渲染和 VFX 不超过预算（应显著优于 ADR-0019 路线）。

## GDD Requirements Addressed

| GDD System | Requirement | How This ADR Addresses It |
|------------|-------------|--------------------------|
| game-concept.md | 2D 像素武侠 RPG / 回合制战斗 / 《大侠立志传》可比作品 | 直接采纳《大侠立志传》的纯 2D 表现路线 |
| combat-system.md | 战棋式移动、多人战斗、招式范围表达 | 沿用战棋可读性最高优先级 |
| map-scene-management.md | 多场景、昼夜 / 季节 / 心境变体 | 通过 TileMapLayer + CanvasModulate + 章节 tileset 实现，不依赖动态光 |
| combat-ui.md | 战斗 UI、范围、意图、结算反馈 | 限制 VFX 不遮挡关键战斗信息 |
| art-bible.md | 水墨武侠视觉与资产生产标准 | Art Bible 的渲染标准重写为纯 2D + 立绘 sprite 双轨制 |
| cutscene-system.md | 关键演出 Tier 分级 | 立绘特写演出 + 2D sprite 镜头运动，无需 HD-2D 化处理 |

## Related Decisions

- [ADR-0019](adr-0019-2d-wuxia-tactics-rendering-direction.md) — **Superseded by this ADR**
- [ADR-0010](adr-0010-tilemaplayer-usage.md) — TileMapLayer Usage Pattern（**沿用** 5 层结构；ADR-0020 仅在场景根下追加一个 `Background` Sprite2D 节点）
- [ADR-0011](adr-0011-combat-ui-animation.md) — Combat UI Animation Pipeline
- [ADR-0006](adr-0006-scene-loading-strategy.md) — Scene Loading Strategy
- [ADR-0013](adr-0013-cutscene-system.md) — Cutscene System
