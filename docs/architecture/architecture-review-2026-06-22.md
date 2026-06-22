# Architecture Review Report — 2026-06-22

> **Mode**: `/architecture-review` full (delta review w.r.t. 2026-06-08 baseline)
> **Engine**: Godot 4.7-stable (C# / .NET 8+)
> **GDDs Reviewed**: 25
> **ADRs Reviewed**: 20 (ADR-0001 ~ ADR-0020；ADR-0019 现 Superseded、ADR-0020 新增)
> **Verdict**: ✅ **PASS WITH MINOR CONCERNS**

---

## Scope of This Review

本次 review 在 [架构评审 2026-06-08](architecture-review-2026-06-08.md) baseline 之上做 **delta review**，聚焦：

1. **ADR-0019 → ADR-0020 渲染方向变更**（从《逸剑风云决》式伪 2.5D → 《大侠立志传》式纯 2D）
2. ADR-0020 与既有 ADR / GDD 的一致性
3. Engine 4.7 兼容性核对（沿用 2026-06-20 4.7 升级后的统一注记）

未对 25 个 GDD 做全量 TR 重抽取——`tr-registry.yaml` 中现有条目自 2026-06-14 起未变，且本次新增 ADR-0020 不引入新 GDD 需求。

---

## Traceability Summary

| 指标 | 2026-06-08 baseline | 2026-06-22 | 变化 |
|---|---|---|---|
| 总系统数 | 25 | 25 | 0 |
| ✅ Covered | 15 | 15 | 0 |
| ⚠️ Partial | 10 | 10 | 0 |
| ❌ Gaps | 0 | 0 | 0 |
| ADR 总数 | 18 | 20 | +2 (ADR-0019, ADR-0020) |

---

## ADR-0020 Mapping Delta

| 系统 (#) | ADR 变更 | 说明 |
|---|---|---|
| #12 地图/场景管理 | + ADR-0020 (governance) | 与 ADR-0010 互补：ADR-0010 定义 TileMapLayer 5 层结构，ADR-0020 限定渲染风格为纯 2D，并在场景根追加 `Background` Sprite2D |
| #2 回合制战斗 | ADR-0019 → ADR-0020 | EPIC.md 已同步更新（划线 ADR-0019、追加 ADR-0020） |
| #7 战斗 UI | + ADR-0020 (风格约束) | 不修改 ADR-0011 接口，仅约束 VFX 不引入 3D 化效果 |
| #20 CG/演出 | + ADR-0020 (风格约束) | 不修改 ADR-0013，仅约束 cutscene 演出走 2D sprite + 立绘特写路线 |
| `design/art/art-bible.md` | 参考方向同步 | Reference Board / Rendering Style 已替换 |

---

## Cross-ADR Conflicts

### 🟡 Fixed during this review

#### CONFLICT: ADR-0020 (initial draft) vs ADR-0010 — TileMapLayer 层级

- **Type**: Integration contract / 层级定义冲突
- **What happened**: ADR-0020 初稿 D1 节误写为"基础层级简化为 4 层：Background / Ground / Structures / Overlay"，丢弃了 ADR-0010 已定义的 `Terrain`（草丛/碎石/花装饰层）与 `Collision`（碰撞层）。
- **Resolution**: 在本 review 期间已修正 ADR-0020 D1 节为"沿用 ADR-0010 五层结构 + 在场景根追加一个 `Background` Sprite2D 节点（z < Ground）"，并同步更新 §Related Decisions 描述。
- **Pattern**: 跨 ADR 引用既有层级结构时，**新增**优于**重新定义**——保持下层 ADR 的稳定 schema，扩展点必须显式注明。

### ✅ No unresolved conflicts

完整扫描 ADR-0001 ~ ADR-0018 后，无与 ADR-0020 不兼容的内容：

- 无 ADR 引用 `PointLight2D` / `DirectionalLight2D` 作为常规需求（仅 ADR-0010 §Negative 提到"如需局部光照需额外 PointLight2D"，与 ADR-0020 D2 的"特例 + PR 评审"门槛兼容）
- 无 ADR 引用视差 / parallax / 体积光 / 景深 / bloom / HD-2D
- 无 ADR 假设 3D 场景或 PBR 材质

---

## ADR Dependency Order (Updated)

```
Foundation (no deps):
  ADR-0001  Event Bus
  ADR-0003  Data Configuration Format
  ADR-0008  FSM

Core / Platform:
  ADR-0002  UI Framework (dual-focus)
  ADR-0004  Save Encryption
  ADR-0005  Dialogue Format
  ADR-0006  Scene Loading Strategy
  ADR-0007  Input System
  ADR-0009  Dynamic Audio
  ADR-0010  TileMapLayer Usage
  ADR-0011  Combat UI Animation
  ADR-0012  Misunderstanding UI
  ADR-0013  Cutscene System
  ADR-0020  ⭐ Pure 2D Rendering Direction (Supersedes ADR-0019)

Feature layer (unchanged from baseline):
  ADR-0014  Living Jianghu Layer
  ADR-0015  Romance System
  ADR-0016  Party Management
  ADR-0017  Epiphany Breakthrough
  ADR-0018  Exploration & Insight

Superseded:
  ADR-0019  2D Wuxia Tactics Rendering Direction (伪 2.5D, retired 2026-06-22)
```

无依赖环；无 Proposed 状态阻塞。

---

## GDD Revision Flags

### Flag 1: map-scene-management.md 引用 `WorldEnvironment`（3D 节点）

```540:542:design/gdd/map-scene-management.md
- Godot 场景树（SceneTree）— 场景加载/卸载
- Godot 资源异步加载（ResourceLoader）— 预加载机制
- 着色器/环境资源（Environment/WorldEnvironment）— 色调和光照控制
```

| 字段 | 内容 |
|---|---|
| GDD | `design/gdd/map-scene-management.md` line 542 |
| Assumption | 用 `Environment` / `WorldEnvironment` 实现色调和光照控制 |
| Reality | `WorldEnvironment` 是 Godot 3D 节点；纯 2D 项目用 `CanvasModulate` + 2D shader（ADR-0010 / ADR-0020） |
| Action | 改为：`CanvasModulate + 2D shader / ColorRect overlay — 色调和光照控制（详见 ADR-0010 §光照变体管理、ADR-0020 §D2）` |
| 状态 | systems-index.md 中 `#12 地图/场景管理` 标 `Needs Revision` |

### Flag 2 (pre-existing, low priority): map-scene-management.md "216 套" 估算

```572:572:design/gdd/map-scene-management.md
| 场景背景 | 每个场景需提供 4 种光照变体（日/晨/昏/夜） | 18地点 × 平均3场景 × 4变体 ≈ 216 套 |
```

- ADR-0010 通过 `CanvasModulate` 色调切换避免 4× tileset 资产；GDD 此处估算偏高。
- Action（建议非阻塞）：将估算改为 "54 套场景背景 + 4 套色调 shader 参数"，并加注 ADR-0010 引用。
- 本次不阻塞 PASS verdict；建议下次 design-review 时一并处理。

---

## Engine Compatibility

| 项 | 结果 |
|---|---|
| Engine pinned | Godot 4.7-stable ✅ |
| ADRs with Engine Compatibility section | 20 / 20 ✅ |
| Deprecated API references | 0 ✅ |
| Stale version references | 0 ✅（全部 ADR 已在 2026-06-20 加 4.7 re-verification 注记） |
| Post-cutoff API conflicts | 0 ✅ |

### Engine Specialist Consultation

本次跳过 `godot-specialist` 二次咨询，理由：

1. ADR-0020 使用的全部为 Godot 4.0 长期稳定 2D API（Node2D / TileMapLayer / Sprite2D / AnimatedSprite2D / CanvasModulate / GPUParticles2D）
2. 高风险 4.7 API（Engine.TimeScale / Tween Always / Camera2D smoothing / InputMap）在 cu-006 spike (2026-06-20) 已实机验证：`production/notes/spike-cu-006-godot-4.7-api-verify-2026-06-20.md`
3. ADR-0020 D5（立绘 / Sprite 双轨制）属美术资产规范，非引擎 API 选择

如需后续执行，可单独触发 `Task → godot-specialist`。

---

## Architecture Document Coverage

`docs/architecture/architecture.md` 状态：

- ✅ §3 System Layer Mapping：25 系统全部覆盖
- ✅ §7 API Boundaries：所有层接口都有定义
- ⚠️ §12 ADR Roadmap：**stale** — 仅列出 ADR-001 ~ ADR-010；ADR-0011 ~ ADR-0020 未收录
- 文档头标注 `Status: Draft — Pending TD Sign-off`，与现实一致

**建议**：下一次 architecture-review 或 TD sign-off 时同步 §12。本次 review 不强行修改 architecture.md，避免越权改写 master doc。

---

## Verdict: ✅ PASS WITH MINOR CONCERNS

**通过项**：
- ✅ 无 ❌ Gap
- ✅ 无未解决 Cross-ADR Conflict
- ✅ Engine 兼容性一致
- ✅ ADR 依赖图无环

**Minor Concerns**：
- ⚠️ 1 GDD revision flag（map-scene-management.md WorldEnvironment）— 非阻塞，建议本周内修复
- ⚠️ 1 pre-existing low-priority 估算偏差（216 套 vs 54+shader）
- ⚠️ architecture.md §12 ADR Roadmap stale，建议下次 TD sign-off 同步

**Blocking Issues**：无。

**Required ADRs**：无。

---

## Reflexion / Pattern Note

本次 review 发现并即时修复的 ADR-0020 vs ADR-0010 层级冲突，提炼为一条架构 pattern：

> **当新 ADR 引用既有下层 ADR 的 schema（如层级、枚举、接口契约）时，新增（additive）优于重新定义（redefine）。**
> 若必须修改既有 schema，应在新 ADR 的 §Migration Plan 中显式声明并同步修改既有 ADR，禁止悄悄改写下游已确认的契约。

该 pattern 已追加到 `docs/consistency-failures.md`，供未来 ADR 作者参考。

---

## Next Steps

1. 修复 GDD Flag 1（map-scene-management.md WorldEnvironment）
2. 在 systems-index.md 中将 `#12 地图/场景管理` 标 `Needs Revision`
3. （可选）下次 design-review 时处理 Flag 2 的资产估算
4. （可选）下次 TD sign-off 时同步 architecture.md §12 ADR Roadmap

---

*Generated by `/architecture-review` (full mode, delta review).*
