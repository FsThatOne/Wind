# Story: cd-004 — Realm & Power Comparison

> **Epic**: character-data
> **Type**: Logic
> **Priority**: P1 — 朦胧化 UI 和叙事系统的直接依赖
> **Depends On**: cd-001, cd-002
> **Blocked By**: cd-002 (需要 TotalPower 计算)
> **ADR Guidance**: ADR-0003 (境界阈值可配置化)
> **GDD Source**: design/gdd/character-attributes.md §Detailed Design (功力境界), §Formulas F8, §Edge Cases
> **Control Manifest Version**: 2026-06-10
> **Status**: Done

## Goal

实现功力境界映射和相对强度比较，支持叙事条件门控的境界突破逻辑。

## Scope

### In Scope
- `RealmSystem` 类:
  - `GetRealmName(totalPower)` → string (四字境界名)
  - `GetRealmIndex(totalPower)` → int (0-8)
  - `CheckBreakthrough(character, narrativeConditionMet)` → BreakthroughResult
  - `ComparePower(selfPower, targetPower)` → PowerComparisonResult
- 9 个境界定义:
  - 初学乍练 (≤24), 初窥门径 (25-34), 小有所成 (35-49), 登堂入室 (50-69)
  - 融会贯通 (70-89), 驾轻就熟 (90-114), 炉火纯青 (115-139)
  - 登峰造极 (140-169), 返璞归真 (170-199), *(200+ 超凡入圣?)*
- `BreakthroughResult`: { Triggered, PendingNarrative, NoChange }
- `PowerComparisonResult`: 包含 PowerLevel enum + 文学描述 string
- 战斗中临时增益不触发境界检查（只看永久属性总值）
- 阈值数组: [25, 35, 50, 70, 90, 115, 140, 170, 200]

### Out of Scope
- 突破动画播放 → Presentation 层
- 突破音效 → Presentation 层
- 叙事条件具体判定 → 主线叙事系统
- 顿悟发放 → cd-007

## Technical Notes

- 路径: `src/FengZhi.Foundation/CharacterData/RealmSystem.cs`
- 阈值数组应为 `readonly int[]` 常量（后续 cd-005 外部化到 YAML）
- 事件: 突破时发布 `RealmBreakthroughEvent` (via EventBus, ADR-0001)
- "你隐约感到体内有股力量正在蕴积" 提示由 UI 层根据 PendingNarrative 状态渲染

## Acceptance Criteria

- [ ] **AC1**: GDD AC#1 — 五维总和=40 → GetRealmName 返回 "初窥门径" (40 ∈ [35,50))
- [ ] **AC2**: GDD AC#5 — 功力=114, +2 后=116, 叙事条件满足 → CheckBreakthrough 返回 Triggered, 新境界="炉火纯青"
- [ ] **AC3**: GDD AC#6 — 功力=115, 叙事条件未满足 → CheckBreakthrough 返回 PendingNarrative
- [ ] **AC4**: GDD AC#7 — selfPower=80, targetPower=160 → ComparePower 返回 FarWeaker + "此人气势如渊，你感到难以抗衡"
- [ ] **AC5**: 战斗临时 buff 使功力跨阈值 → 不触发突破（只看永久层总值）
- [ ] **AC6**: 功力=200+ → 返璞归真之后无更高境界

## Test Evidence Path

`tests/Foundation/CharacterData/RealmSystemTests.cs`
