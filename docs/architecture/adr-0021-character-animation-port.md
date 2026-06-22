# ADR-0021: Character Animation Port (Project-Wide)

## Status
Accepted (Partially Superseded by [ADR-0022](adr-0022-isometric-projection-and-iso4-animator.md) — §扩展点 1)

## Date
2026-06-22

## Last Verified
2026-06-22 (扩展点 1 落地 + feng-zhi CavePlayer 迁移 + 实机走查通过)

## Superseded Sections
- §扩展点 1 (8 方向序列帧子接口 / `EightDirection` enum / `EightDirectionAnimatedSprite2DAnimator` / 32-frame asset spec) — 由 ADR-0022 替换为 4 斜向 `Iso4Direction` + `IIso4CharacterAnimator` + `Iso4AnimatedSprite2DAnimator` + 16-frame asset spec
- 主端口 `ICharacterAnimator` + `Facing` 2 向契约、§扩展点 2（骨骼动画）等其他全部章节 **保持有效**

## Summary

为防止角色动画后端（AnimatedSprite2D / Skeleton2D / DragonBones / Spine 等）
直接渗透到业务逻辑层，定义一个项目级 Port `ICharacterAnimator`，
所有 Presentation 层调用角色动画必须经此接口；具体后端以 Adapter 形式
落到 `Animation/GodotIntegration/`，按场景需要注入。本 ADR 锁定该端口契约
与首个 Adapter 的边界，并预留 8 方向序列帧、骨骼动画两类已知扩展点。

## Engine Compatibility

| Field | Value |
|-------|-------|
| **Engine** | Godot 4.7-stable |
| **Domain** | Animation, Sprite/Skeleton, Presentation Layer Port |
| **Knowledge Risk** | **MEDIUM** — `SpriteFrames.SetAnimationLoop` 在 4.7 弃用，新 API 为 `SetAnimationLoopMode(name, LoopMode)` 且枚举值为 `None / Linear / Pingpong`（**未在 breaking-changes.md 记录**，本 ADR 落地时通过反射探针确认） |
| **References Consulted** | `src/FengZhi.Foundation/Animation/*`, `src/FengZhi.Foundation/CombatUi/GodotIntegration/*` (Bridge 模式参考), ADR-0011, ADR-0020 |
| **Post-Cutoff APIs Used** | `SpriteFrames.SetAnimationLoopMode(StringName, LoopMode)` (4.7) |
| **Verification Required** | 1) 验证 `AnimatedSprite2DAnimator` 在标准 4 状态（Idle/Walk/Attack/Hurt）下事件正确触发；2) 验证 `Finished` 事件在 `Sprite.AnimationFinished` 上的转译；3) 验证 `NormalizedProgress` 在动画切换瞬间无除零异常 |

## ADR Dependencies

| Field | Value |
|-------|-------|
| **Depends On** | ADR-0011 (Combat UI Animation — Director 编排策略), ADR-0020 (Pure 2D Wuxia Direction — 资产形态决定首个 Adapter 选型) |
| **Enables** | Sprint 5/6 战斗角色实装、未来探索场景 NPC 动画、过场系统角色动画轨道、骨骼动画引入 |
| **Blocks** | Presentation 层任何新增直接 `using AnimatedSprite2D` 的角色脚本（迁移前不允许新增） |
| **Ordering Note** | 本 ADR 在 `src/FengZhi.Foundation/Animation/` 落地后追写；不阻塞 Sprint 5 收尾 |

## Context

### Problem Statement

项目当前仅有 `feng-zhi/scripts/CavePlayer.cs` 直接 `using AnimatedSprite2D`。Sprint 5/6 即将批量产出战斗与探索角色，
未来还会评估 Skeleton2D / DragonBones / Spine 切换路径。若每个角色脚本都直接持有具体动画节点 API，
后续切换成本将随角色数量线性放大；此外，逻辑层无法在不启动 Godot 的情况下做单元测试。

### Constraints

- 不引入新的第三方动画库依赖（与 ADR-0011 一致）
- 接口必须能在纯 .NET (xUnit) 环境下被 Fake 替身完整模拟，配合既有 1367 测试基线无回归
- 不打破 `project_memory.md` 中 "逻辑层 Port/Adapter 严格分层、保持无状态" 的硬约束
- 新接口在视觉上应与既有 `IAnimationCommand` (ADR-0011 Director 命令) 区分清晰，避免阅读混淆

### Requirements

- **R1** Presentation 层角色动画调用统一通过 Port，**禁止**直接 `using AnimatedSprite2D / Skeleton2D / 第三方库`
- **R2** Port 暴露 `Play / Stop / SetFacing / Finished / FrameEvent / NormalizedProgress` 6 个核心动词，足以覆盖 idle/walk/attack/hurt/die 标准动作
- **R3** 至少提供一个真实 Adapter（`AnimatedSprite2DAnimator`）和一个测试 Fake (`FakeCharacterAnimator`)
- **R4** 接口可扩展容纳未来骨骼动画后端（Skeleton2D / DragonBones）而无需破坏现有调用方

## Decision

采用 **`ICharacterAnimator` 单一项目级 Port + 多 Adapter** 模式，落点 `src/FengZhi.Foundation/Animation/`。

### 1. Port 契约

```csharp
// FengZhi.Foundation.Animation.ICharacterAnimator
public interface ICharacterAnimator
{
    CharacterAnimState? Current { get; }
    bool IsPlaying { get; }
    float NormalizedProgress { get; }   // 0..1，跨后端统一含义

    void Play(CharacterAnimState state, bool loop = true);
    void Stop();
    void SetFacing(Facing facing);

    event Action<CharacterAnimState>? Finished;
    event Action<AnimationFrameEvent>? FrameEvent;
}
```

辅助类型：

| 类型 | 取值 | 备注 |
|---|---|---|
| `CharacterAnimState` | `Idle / Walk / Run / Attack / Cast / Block / Hurt / Stagger / Die / Victory / Defeat / Custom` | 粗分类动作意图，**不**是动画文件名 |
| `Facing` | `Left / Right` | 仅 2 向；8 方向场景见 §扩展点 1 |
| `AnimationFrameEvent` | `(string Tag, float Progress)` | 序列帧后端无原生方法轨道，订阅器永不触发；骨骼后端真正支持 |

`Custom` 状态走专用入口 `PlayCustom(string animationName, bool loop = true)`，避免在公共接口塞 `string` 参数污染语义。

### 2. 当前 Adapter

- `AnimatedSprite2DAnimator` (`src/FengZhi.Foundation/Animation/GodotIntegration/AnimatedSprite2DAnimator.cs`) — 默认实现
  - `[Export] AnimatedSprite2D Sprite` 显式注入子节点
  - `CharacterAnimState.Idle → "idle"`，小写映射
  - `loop` 参数通过 `SpriteFrames.SetAnimationLoopMode` 在每次播放前覆写循环标志（**会**覆盖 `.tres` 资源中的设置，调用方需明确意图）
  - `Finished` 转译自 `AnimatedSprite2D.AnimationFinished` 信号
  - `FrameEvent` 静默（已 XML doc 注明）

### 3. Fake (测试替身)

- `FakeCharacterAnimator` (`src/FengZhi.Foundation/Animation/Fakes/FakeCharacterAnimator.cs`) 纯内存实现
  - 暴露 `EmitFinished(state)` / `EmitFrameEvent(tag, progress)` / `SetProgress(p)` 让上层测试驱动事件
  - 计数 `PlayCallCount`、镜像 `LoopRequested` 用于断言

### 4. 调用方分布

| 模块 | 路径 | 用法 |
|---|---|---|
| 战斗 UI | `Presentation/CombatUi/` (ADR-0011) | `CombatAnimationDirector` 触发受击/决胜调 `Play()` |
| 探索移动 | `Presentation/Exploration/` (ADR-0018) | 角色寻路 `Play(Walk)` |
| 过场 | `Presentation/Cutscene/` (ADR-0013) | 时间轴按角色 ID 调 `Play()` |
| 江湖层 NPC | `Presentation/Jianghu/` (ADR-0014) | 待机/工作动画 |
| 顿悟 | `Presentation/Epiphany/` (ADR-0017) | 突破入定动画 |
| 关系 | `Presentation/Romance/` (ADR-0015) | 同伴反应 |

### 5. 已知扩展点（**本 ADR 不实现，仅锁定方向**）

#### 扩展点 1：8 方向序列帧（`CavePlayer.cs` 类场景）

> **⚠️ Superseded by [ADR-0022](adr-0022-isometric-projection-and-iso4-animator.md)** — 全项目从「方格 + 8 向」pivot 至「isometric diamond + 4 斜向」，本节技术路线作废。新路线为 `Iso4Direction { NE, SE, SW, NW }` + `IIso4CharacterAnimator` + `Iso4AnimatedSprite2DAnimator`，16-frame asset spec。以下文字保留作历史决策记录。

`Facing` 仅 2 向不足以覆盖 8 方向行走 + 朝向迟滞 + 帧相位保持的需求。处置：

- **不**扩张 `Facing` 枚举到 8 向（保持公共契约简洁）
- 引入兄弟接口 `IDirectionalCharacterAnimator : ICharacterAnimator`，新增 `SetMovementVector(Vector2)`
- 实现 `EightDirectionAnimatedSprite2DAnimator` Adapter 内部承担方向分区、迟滞、帧相位保持
- `CavePlayer` 迁移作为该 Adapter 的首个消费方

#### 扩展点 2：骨骼动画 (Skeleton2D / DragonBones / Spine)

- `Skeleton2DAnimator`、`DragonBonesAnimator`、`SpineAnimator` 各自落到 `Animation/GodotIntegration/`
- `FrameEvent` 在骨骼后端真正生效（AnimationPlayer 方法轨道 / Spine event）
- 接口本身**不**变更；如需暴露骨骼专属能力（如 `LookAt(Vector2)`），通过 `IBoneControllableAnimator` 兄弟接口扩展，调用方按需向下转型

## Alternatives Considered

### Alternative 1：业务代码直接持有 `AnimatedSprite2D`

- **Pros**：零抽象成本
- **Cons**：切换成本随角色数量线性放大；逻辑层单元测试必须启动 Godot
- **Rejection Reason**：与项目既定 Port/Adapter 模式冲突

### Alternative 2：把 8 方向迟滞、帧相位保持等能力一并吃进 `ICharacterAnimator`

- **Pros**：CavePlayer 可直接迁移
- **Cons**：接口被序列帧实现细节污染（`Frame`、`FrameProgress`、8 向 `Facing`）；Skeleton2D / DragonBones Adapter 难以提供这些概念的有意义实现
- **Rejection Reason**：**特例反推接口**会破坏抽象，按 §扩展点 1 用兄弟接口承担

### Alternative 3：每个 Adapter 自定义专属接口（`IAnimatedSpriteAdapter` / `ISkeletonAdapter`）

- **Pros**：每个后端契约最贴合
- **Cons**：上层不能用统一类型注入；切换后端时所有调用方都要改
- **Rejection Reason**：违反 R1 / R4

## Consequences

### Positive

- 切换动画后端的成本被压到 Adapter 与场景节点级别，业务代码零改动
- 逻辑层单元测试可注入 `FakeCharacterAnimator`，无需 Godot 引擎
- 接口与现有 `IAnimationCommand` (ADR-0011) 视觉区分清晰（前缀 `Character` + 命名空间隔离）
- `loop` 在每次 `Play()` 时显式覆写，行为可预测

### Negative

- 调用方必须经接口；不允许便利地直接读取 `Sprite.Frame`
- `loop` 覆写 `.tres` 资源中的循环标志，美术若希望在 `.tres` 中固化设置需配合接口约定
- `Custom` 状态需走 `PlayCustom`，调用方略繁琐

### Risks

| 风险 | 缓解 |
|------|------|
| Adapter 与具体节点结构耦合（`[Export] Sprite` 缺失会 PushError 后行为退化为 noop） | `_Ready` 显式 `GD.PushError`，集成测试将 sprite 引用的存在性作为前置 |
| 未来骨骼后端 `NormalizedProgress` 语义偏移（如 AnimationPlayer 的 "position / length" 换算） | ADR 锁定 0..1 含义；Adapter 自行换算并以单元测试守护 |
| 8 方向场景被错误塞进 `ICharacterAnimator` | 通过 lint 守护 + 本 ADR §扩展点 1 明确路径 |
| `SpriteFrames.SetAnimationLoop` 4.7 弃用未记录入 breaking-changes.md | 本 ADR 在 §Engine Compatibility 留档；建议 `/architecture-review` 批量补录 |

## GDD Requirements Addressed

本 ADR 是**基础设施性 Port**，不直接绑定单一 GDD 系统，而是为以下系统的角色动画实施提供共享端口：
combat-ui.md、exploration-insight.md、cutscene-system.md、living-jianghu-layer.md、epiphany-breakthrough.md、romance-system.md、party-management.md。

## Performance Implications

- Port 调用为单层虚分派，CPU 可忽略
- `AnimatedSprite2DAnimator.NormalizedProgress` 每次访问做一次除法 + 字段读取，O(1)；调用方应避免高频轮询
- `Finished` 事件订阅基于 Godot 信号，与既有项目模式一致
- 无新内存分配（仅一次 `event Action<>` 订阅）

## Migration Plan

| 阶段 | 内容 |
|---|---|
| 1 (已完成) | Port + 类型 + Fake + Adapter + 8 单元测试 |
| 2 | Sprint 5/6 新增战斗角色场景一律使用 `AnimatedSprite2DAnimator`，作为教科书示例 |
| 3 ✅ (2026-06-22) | 引入 `EightDirectionAnimatedSprite2DAnimator`（兄弟接口路径），迁移 `CavePlayer.cs`；feng-zhi 项目加 `<ProjectReference>` 接通 Foundation；32 帧 PNG 落到 `feng-zhi/assets/character/main_character_16bit_8dir/`；SpriteFrames 资源 `main_character.tres` 含 idle + 8 方向 walk_* |
| 4 | 评估 Skeleton2D / DragonBones / Spine，按需新增 Adapter；接口本体保持不变 |

## Validation Criteria

1. ✅ `FakeCharacterAnimator` 8 单元测试全绿（Foundation 1367/1367）
2. ⏳ `AnimatedSprite2DAnimator` 集成测试随 Sprint 5/6 首个消费方落地（覆盖 Play 4 状态 + Finished + Stop + Facing）
3. ⏳ Lint 守护脚本：扫描 `src/`、`feng-zhi/scripts/`、`prototypes/*/scripts/` 下新增的 `using.*AnimatedSprite2D`，仅允许列入白名单的 Adapter 与 `CavePlayer.cs` 待迁移特例
4. ✅ (2026-06-22) 8 方向兄弟接口 `IDirectionalCharacterAnimator` 扩展无破坏现有 `ICharacterAnimator` 契约（Foundation 1378/1378 PASS，原 1367 + 8 方向新增 11）；feng-zhi 实机走查方向键 8 向切换 + 报错 `There is no animation with name ''.` 消失

## Related Decisions

- [ADR-0011](adr-0011-combat-ui-animation.md) — Combat UI Director 通过本 Port 触发角色动画
- [ADR-0013](adr-0013-cutscene-system.md) — Cutscene 时间轴角色动作轨道使用本 Port
- [ADR-0014](adr-0014-living-jianghu-layer.md) — NPC 待机/工作动画使用本 Port
- [ADR-0017](adr-0017-epiphany-breakthrough.md) — 突破演出角色入定动画使用本 Port
- [ADR-0018](adr-0018-exploration-insight.md) — 探索场景角色行走/交互动画使用本 Port
- [ADR-0020](adr-0020-pure-2d-wuxia-rendering-direction.md) — 决定首个 Adapter 选型为 AnimatedSprite2D
