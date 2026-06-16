# Architecture Review Report

> **Date**: 2026-06-08
> **Engine**: Godot 4.6.3 (C# / .NET 8+)
> **GDDs Reviewed**: 25
> **ADRs Reviewed**: 10
> **Verdict**: ⚠️ CONCERNS

---

## Traceability Summary

| 指标 | 数量 | 百分比 |
|------|------|--------|
| 总系统数 | 25 | 100% |
| ✅ ADR 充分覆盖 | 7 | 28% |
| ⚠️ 部分覆盖 | 12 | 48% |
| ❌ 无 ADR 覆盖 | 6 | 24% |

---

## System-Level Traceability Matrix

| # | System | GDD | ADR Coverage | Status |
|---|--------|-----|--------------|--------|
| 1 | 角色属性/功力 | character-attributes.md | ADR-0003 (数据格式) | ⚠️ Partial |
| 2 | 回合制战斗 | combat-system.md | ADR-0001 (事件), ADR-0008 (FSM) | ⚠️ Partial |
| 3 | 武学组合 | martial-arts-system.md | ADR-0003 (数据格式) | ⚠️ Partial |
| 4 | 敌方 AI | enemy-ai.md | ADR-0003 (数据格式) | ⚠️ Partial |
| 5 | 对话系统 | dialogue-system.md | ADR-0005 (对话格式) | ✅ Covered |
| 6 | 心境双轴 | mindset-dual-axis.md | ADR-0001 (事件) | ⚠️ Partial |
| 7 | 战斗 UI | combat-ui.md | ADR-0002 (UI 框架) | ✅ Covered |
| 8 | 存档系统 | save-system.md | ADR-0004 (完整) | ✅ Covered |
| 9 | 主线叙事 | main-narrative.md | ADR-0005 (部分) | ⚠️ Partial |
| 10 | NPC 状态 | npc-state.md | ADR-0001 (事件), ADR-0008 (FSM) | ✅ Covered |
| 11 | 自然日+体力 | natural-day-stamina.md | ADR-0001 (事件), ADR-0003 (数据) | ⚠️ Partial |
| 12 | 地图/场景 | map-scene-management.md | ADR-0006 (加载), ADR-0010 (TileMap) | ✅ Covered |
| 13 | 感情系统 | romance-system.md | ADR-0001 (事件引用) | ❌ GAP |
| 14 | 朦胧化 UI | blurred-ui.md | ADR-0002 (UI 框架) | ✅ Covered |
| 15 | 物品/道具 | item-system.md | ADR-0003 (数据格式) | ⚠️ Partial |
| 16 | 活江湖层 | living-jianghu-layer.md | ADR-0003 (数据格式) | ❌ GAP |
| 17 | 顿悟突破 | epiphany-breakthrough.md | ADR-0001 (事件引用) | ❌ GAP |
| 18 | 误会系统 | misunderstanding-system.md | ADR-0001, ADR-0008 (FSM) | ⚠️ Partial |
| 19 | 探索/洞察 | exploration-insight.md | — | ❌ GAP |
| 20 | CG/演出 | cutscene-system.md | — | ❌ GAP |
| 21 | 音乐/音效 | audio-system.md | ADR-0009 (完整) | ✅ Covered |
| 22 | 教学/引导 | tutorial-onboarding.md | ADR-0002, ADR-0003 | ⚠️ Partial |
| 23 | 设置/选项 | settings-options.md | ADR-0007 (输入) | ⚠️ Partial |
| 24 | 成就/Steam | achievement-steam.md | ADR-0002 (弹窗) | ⚠️ Partial |
| 25 | 队伍管理 | party-management.md | — | ❌ GAP |

---

## Coverage Gaps (无 ADR 存在)

| # | System | 建议 ADR 标题 | 领域 | 引擎风险 | 优先级 |
|---|--------|--------------|------|---------|--------|
| 13 | 感情系统 | Comet Model & Intimacy State Architecture | Feature/State | LOW | P2 |
| 16 | 活江湖层 | Jianghu Simulation Engine (Rumor Propagation + Passphrase Rotation) | Feature/Simulation | LOW | P2 |
| 17 | 顿悟突破 | Epiphany Trigger & Breakthrough Resolution | Feature/Combat | LOW | P3 |
| 19 | 探索/洞察 | Exploration & Clue Registry Architecture | Feature/Gameplay | LOW | P3 |
| 20 | CG/演出 | Cutscene Timeline & Camera Architecture | Presentation/Animation | LOW | P3 |
| 25 | 队伍管理 | Party Management & Catch-up Calculator | Feature/State | LOW | P2 |

> **注**：所有 GAP 均在 Feature/Presentation 层（非 Foundation/Core），引擎风险全 LOW。
> 这些系统在对应 Sprint 开始前创建 ADR 即可，不阻塞 Sprint 1-5 基础设施实现。

---

## Cross-ADR Conflicts

**✅ 无冲突检测到。**

- 无数据所有权冲突
- 无接口契约矛盾
- 无性能预算竞争
- 无依赖循环
- 无架构模式冲突

---

## ADR Dependency Order (拓扑排序)

### Recommended Implementation Order

```
Foundation (无依赖):
  1. ADR-0001: Event Bus Architecture
  2. ADR-0003: Data Configuration Format (YAML)
  3. ADR-0008: Generic Finite State Machine

Depends on Foundation:
  4. ADR-0002: UI Framework & Dual-Focus (requires ADR-0001)
  5. ADR-0004: Save Encryption (requires ADR-0003)
  6. ADR-0005: Dialogue Data Format (requires ADR-0003)
  7. ADR-0006: Scene Loading Strategy (requires ADR-0003)
  8. ADR-0009: Dynamic Music System (requires ADR-0008, ADR-0001)

Feature layer:
  9. ADR-0007: Input System (requires ADR-0002)
  10. ADR-0010: TileMapLayer Usage (requires ADR-0006)
```

- **无未解析依赖** — 所有 Depends On 引用的 ADR 均已存在
- **无循环** — DAG 结构完整
- **全部状态: Proposed** — 实现前需逐个 Accept

---

## GDD Revision Flags

**None — 所有 GDD 假设与已验证的引擎行为和 ADR 决策一致。**

---

## Engine Compatibility Audit

| 指标 | 结果 |
|------|------|
| ADRs with Engine Compatibility section | **10/10** ✅ |
| 引擎版本一致性 | 全部 Godot 4.6.3 ✅ |
| Deprecated API 引用 | **0** ✅ |
| Stale Version 引用 | **0** ✅ |
| Post-Cutoff API 冲突 | **0** ✅ |

### HIGH RISK — Verification Required

| ADR | Post-Cutoff API | 验证内容 | 建议时机 |
|-----|-----------------|---------|---------|
| ADR-0001 | `[Signal] delegate` C# event syntax (4.5+) | 验证泛型 EventBus Autoload 下 GC 行为；验证场景卸载时订阅清理 | Sprint 1 Spike |
| ADR-0002 | Dual-focus system (4.6) | ① grab_focus() 不影响鼠标悬停高亮 ② 键盘/鼠标焦点可同时存在于不同 Control ③ shader 不阻断输入 | Sprint 1 Spike |
| ADR-0010 | Scene tile rotation (4.6) | 验证 C# 中 TileMapLayer rotation 属性对 scene tiles 的行为 | Sprint 5 Spike |

### MEDIUM RISK

| ADR | 域 | 说明 |
|-----|---|------|
| ADR-0007 | SDL3 gamepad driver | 引擎层透明，对游戏代码无影响。仅需 Steam Deck 实机验证按键映射。 |
| ADR-0010 | TileMapLayer API | 4.3 替代 TileMap，4.6 新增 scene tile rotation。C# 绑定行为需确认。 |

### Engine Specialist Findings

> 由于本项目为 2D 像素游戏（Compatibility 渲染器），以下 4.6 高风险项对本项目 **不适用**：
> - Jolt Physics 3D（默认引擎切换）— 项目纯 2D，无影响
> - D3D12 默认 Windows 后端 — Compatibility 模式使用 GL ES 3.0，不受影响
> - Glow 渲染顺序变更 — 像素风格不使用 Glow
>
> **实际风险集中在**：Dual-focus UI + C# Signal delegate + TileMapLayer rotation，
> 均已在 ADR 中标记为 Sprint 1/5 Spike 验证任务。

---

## Architecture Document Coverage

| 检查项 | 结果 |
|--------|------|
| systems-index 全部 25 系统在 architecture.md 层映射中出现 | ✅ |
| 数据流覆盖所有跨系统通信 (7 条 DF 路径) | ✅ |
| API 边界支持所有集成需求 (5 层接口表) | ✅ |
| Namespace/Directory 结构覆盖全部系统 | ✅ |
| 孤立架构（无对应 GDD 的模块） | **0** ✅ |
| 缺失系统（有 GDD 但架构中未出现） | **0** ✅ |

---

## Verdict: ⚠️ CONCERNS

### 通过项

- ✅ 10 个基础设施 ADR 全部编写完成，覆盖 EventBus/数据格式/存档/UI/FSM/音频/场景/对话/输入/TileMap
- ✅ Foundation + Core 层全部有架构覆盖
- ✅ 无跨 ADR 冲突或循环依赖
- ✅ 引擎兼容性审计通过（无 deprecated API，版本一致）
- ✅ architecture.md 全量系统映射完整

### 关注项

- ⚠️ Feature 层 6/25 系统无架构覆盖（均为 Sprint 6+ 系统，非阻塞）
- ⚠️ 3 项 HIGH RISK 引擎 API 需 Sprint 1 Spike 验证
- ⚠️ 全部 10 个 ADR 状态仍为 Proposed（需逐个 Accept 后方可实现）
- ⚠️ 测试基础设施、可达性文档、UX 交互模式文档尚未建立

### 结论

基础设施架构决策完整，无阻塞性问题。CONCERNS 级别允许进入下一步（Accept ADRs → 建立测试基础设施 → 开始 Sprint 1），但需在 Sprint 6 前补齐 Feature 层 ADR。

---

## Pre-Gate Checklist (进入 Production 前必须)

| 项目 | 状态 | 所需操作 |
|------|------|---------|
| `tests/unit/` + `tests/integration/` | ❌ | `/test-setup` |
| `.github/workflows/tests.yml` | ❌ | `/test-setup` |
| `design/accessibility-requirements.md` | ❌ | `/ux-design` |
| `design/ux/interaction-patterns.md` | ❌ | `/ux-design` |

---

## Immediate Actions

1. **Accept P0 ADRs** — 将 ADR-0001 (EventBus), ADR-0003 (Data Format), ADR-0008 (FSM) 标记为 Accepted（Foundation 层无依赖，可率先实现）
2. **运行 `/test-setup`** — 建立 GdUnit4 测试框架和 CI pipeline
3. **Sprint 1 Spike** — 验证 dual-focus UI + C# Signal delegate 行为

---

*Generated by `/architecture-review` (full mode). Re-run after new ADRs are written to track coverage improvement.*
