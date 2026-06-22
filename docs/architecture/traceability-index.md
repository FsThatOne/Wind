# Architecture Traceability Index

> **Last Updated**: 2026-06-22
> **Engine**: Godot 4.7-stable (C# / .NET 8+)
> **Source**: `/architecture-review` full mode (delta review)

---

## Coverage Summary

| 指标 | 数量 | 百分比 |
|------|------|--------|
| 总系统数 | 25 | 100% |
| ✅ Covered (ADR 充分覆盖) | 15 | 60% |
| ⚠️ Partial (仅基础设施覆盖) | 10 | 40% |
| ❌ Gap (无 ADR) | 0 | 0% |

---

## Full Matrix

| # | System | Layer | GDD | ADR(s) | Status |
|---|--------|-------|-----|--------|--------|
| 1 | 角色属性/功力 | Foundation | character-attributes.md | ADR-0003 | ⚠️ |
| 2 | 回合制战斗 | Core | combat-system.md | ADR-0001, ADR-0008, ADR-0020 (战棋可读性) | ⚠️ |
| 3 | 武学组合 | Core | martial-arts-system.md | ADR-0003 | ⚠️ |
| 4 | 敌方 AI | Core | enemy-ai.md | ADR-0003 | ⚠️ |
| 5 | 对话系统 | Core | dialogue-system.md | ADR-0005 | ✅ |
| 6 | 心境双轴 | Core | mindset-dual-axis.md | ADR-0001 | ⚠️ |
| 7 | 战斗 UI | Presentation | combat-ui.md | ADR-0002, ADR-0011 | ✅ |
| 8 | 存档系统 | Platform | save-system.md | ADR-0004 | ✅ |
| 9 | 主线叙事 | Core | main-narrative.md | ADR-0005 | ⚠️ |
| 10 | NPC 状态 | Foundation | npc-state.md | ADR-0001, ADR-0008 | ✅ |
| 11 | 自然日+体力 | Foundation | natural-day-stamina.md | ADR-0001, ADR-0003 | ⚠️ |
| 12 | 地图/场景 | Foundation | map-scene-management.md | ADR-0006, ADR-0010, ADR-0020 | ✅ |
| 13 | 感情系统 | Feature | romance-system.md | ADR-0001, ADR-0008, ADR-0014, ADR-0015 | ✅ |
| 14 | 朦胧化 UI | Presentation | blurred-ui.md | ADR-0002 | ✅ |
| 15 | 物品/道具 | Core | item-system.md | ADR-0003 | ⚠️ |
| 16 | 活江湖层 | Feature | living-jianghu-layer.md | ADR-0001, ADR-0003, ADR-0004, ADR-0014 | ✅ |
| 17 | 顿悟突破 | Feature | epiphany-breakthrough.md | ADR-0001, ADR-0003, ADR-0004, ADR-0008, ADR-0013, ADR-0014, ADR-0017 | ✅ |
| 18 | 误会系统 | Feature | misunderstanding-system.md | ADR-0001, ADR-0008, ADR-0012 | ✅ |
| 19 | 探索/洞察 | Feature | exploration-insight.md | ADR-0001, ADR-0003, ADR-0004, ADR-0006, ADR-0014, ADR-0018 | ✅ |
| 20 | CG/演出 | Presentation | cutscene-system.md | ADR-0001, ADR-0002, ADR-0009, ADR-0011, ADR-0013 | ✅ |
| 21 | 音乐/音效 | Presentation | audio-system.md | ADR-0009 | ✅ |
| 22 | 教学/引导 | Presentation | tutorial-onboarding.md | ADR-0002, ADR-0003 | ⚠️ |
| 23 | 设置/选项 | Platform | settings-options.md | ADR-0007 | ⚠️ |
| 24 | 成就/Steam | Platform | achievement-steam.md | ADR-0002 | ⚠️ |
| 25 | 队伍管理 | Feature | party-management.md | ADR-0001, ADR-0003, ADR-0004, ADR-0008, ADR-0014, ADR-0016 | ✅ |

---

## Known Gaps (需创建的 ADR)

_全部原"无 ADR"系统已覆盖。剩余 Partial 系统仅依赖基础设施 ADR，待 Sprint 6+ 视需要补充专项 ADR。_

## Cross-ADR Conventions (跨 ADR 共享契约)

| 契约 | 定义位置 | 引用方 | 备注 |
|---|---|---|---|
| `LockMode` 共享枚举 | ADR-0013 §GameStateLock | ADR-0014, ADR-0017, ADR-0018 | Foundation 层 enum，修改需同步所有引用方 |
| `ICombatService` Facade | ADR-0011 §ICombatService 接口契约 | ADR-0013 (Cutscene SuspendLogic), ADR-0017 (Epiphany 状态查询) | Combat 模块对外稳定接口 |
| Flag Namespace Registry | ADR-0014 §Flag Namespace Registry | 全部使用 `IFlagService` 的系统 | 强前缀命名规范，10 个前缀已分配 |

---

## ADR Dependency Graph

```
ADR-0001 (EventBus) ──→ ADR-0002 (UI) ──→ ADR-0007 (Input)
                    └──→ ADR-0009 (Audio)
                    └──→ ADR-0011 (Combat UI Animation)
                    └──→ ADR-0012 (Misunderstanding UI)
                    └──→ ADR-0013 (Cutscene System)

ADR-0002 (UI) ──→ ADR-0011 (Combat UI Animation)
             └──→ ADR-0012 (Misunderstanding UI)
             └──→ ADR-0013 (Cutscene System)

ADR-0003 (Data) ──→ ADR-0004 (Save)
               └──→ ADR-0005 (Dialogue)
               └──→ ADR-0006 (Scene Loading) ──→ ADR-0010 (TileMap)

ADR-0008 (FSM) ──→ ADR-0009 (Audio)
              └──→ ADR-0011 (Combat UI Animation)

ADR-0009 (Audio) ──→ ADR-0012 (Misunderstanding UI)
                └──→ ADR-0013 (Cutscene System)

ADR-0011 (Combat UI Animation) ──→ ADR-0013 (Cutscene System)

ADR-0001 (EventBus) ──→ ADR-0014 (Living Jianghu Layer)
ADR-0003 (Data) ──→ ADR-0014 (Living Jianghu Layer)
ADR-0004 (Save) ──→ ADR-0014 (Living Jianghu Layer)

ADR-0001 (EventBus) ──→ ADR-0015 (Romance System)
ADR-0008 (FSM) ──→ ADR-0015 (Romance System)
ADR-0014 (Living Jianghu) ──→ ADR-0015 (Romance System)

ADR-0001 (EventBus) ──→ ADR-0016 (Party Management)
ADR-0003 (Data) ──→ ADR-0016 (Party Management)
ADR-0004 (Save) ──→ ADR-0016 (Party Management)
ADR-0008 (FSM) ──→ ADR-0016 (Party Management)
ADR-0014 (Living Jianghu) ──→ ADR-0016 (Party Management)

ADR-0001 (EventBus) ──→ ADR-0017 (Epiphany Breakthrough)
ADR-0003 (Data) ──→ ADR-0017 (Epiphany Breakthrough)
ADR-0004 (Save) ──→ ADR-0017 (Epiphany Breakthrough)
ADR-0008 (FSM) ──→ ADR-0017 (Epiphany Breakthrough)
ADR-0013 (Cutscene) ──→ ADR-0017 (Epiphany Breakthrough)
ADR-0014 (Living Jianghu) ──→ ADR-0017 (Epiphany Breakthrough)

ADR-0001 (EventBus) ──→ ADR-0018 (Exploration & Insight)
ADR-0003 (Data) ──→ ADR-0018 (Exploration & Insight)
ADR-0004 (Save) ──→ ADR-0018 (Exploration & Insight)
ADR-0006 (Scene Loading) ──→ ADR-0018 (Exploration & Insight)
ADR-0014 (Living Jianghu) ──→ ADR-0018 (Exploration & Insight)
```

---

## Superseded Requirements

None — first review run.

---

## History

| Date | Coverage | Notes |
|------|----------|-------|
| 2026-06-08 | 28% fully covered | Initial architecture review; 10 infrastructure ADRs |
| 2026-06-08 | 28% fully covered | +ADR-0011 Combat UI Animation (reinforces #7 coverage) |
| 2026-06-08 | 36% fully covered | +ADR-0012 Misunderstanding UI (#18 → ✅) |
| 2026-06-08 | 40% fully covered | +ADR-0013 Cutscene System (#20 → ✅) |
| 2026-06-08 | 44% fully covered | +ADR-0014 Living Jianghu Layer (#16 → ✅) |
| 2026-06-08 | 48% fully covered | +ADR-0015 Romance System (#13 → ✅) |
| 2026-06-08 | 52% fully covered | +ADR-0016 Party Management (#25 → ✅) |
| 2026-06-08 | 56% fully covered | +ADR-0017 Epiphany Breakthrough (#17 → ✅) |
| 2026-06-08 | 60% fully covered | +ADR-0018 Exploration & Insight (#19 → ✅); 全部原 Gap 系统已覆盖 |
| 2026-06-08 | 60% fully covered | Minor 缺口补齐：MI-1 (ADR-0011 ICombatService 接口契约) + MI-2 (LockMode 共享 enum 在 ADR-0013/0014/0017/0018 标注) + MI-3 (ADR-0014 Flag Namespace Registry，10 个前缀正式分配；ADR-0015/0017 同步前缀)；ADR-0011~0018 状态升级为 Accepted |
| 2026-06-11 | 60% fully covered | +ADR-0019 2D Wuxia Tactics Rendering Direction（伪 2.5D / 《逸剑风云决》方向）；状态 Accepted |
| 2026-06-22 | 60% fully covered | ADR-0019 → **Superseded** by ADR-0020 (Pure 2D Wuxia Rendering Direction / 《大侠立志传》方向)；ADR-0020 状态 Accepted；#12 地图/场景 与 #2 回合制战斗 同步追加 ADR-0020 引用；art-bible / game-concept / item-system / EPIC.md 同步；详见 [架构评审 2026-06-22](architecture-review-2026-06-22.md) |

---

*Re-run `/architecture-review` after creating new ADRs to update this index.*
