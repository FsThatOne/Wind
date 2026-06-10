# Story: cd-007 — Growth System

> **Epic**: character-data
> **Type**: Logic
> **Priority**: P1 — 叙事驱动角色发展的核心
> **Depends On**: cd-001, cd-003, cd-004
> **Blocked By**: cd-004 (境界突破检查)
> **ADR Guidance**: ADR-0003 (成长节点配置), ADR-0001 (EventBus 通知)
> **GDD Source**: design/gdd/character-attributes.md §Detailed Design (成长系统), §Edge Cases
> **Control Manifest Version**: 2026-06-10
> **Status**: Done

## Goal

实现四种叙事驱动的属性成长路径：章节成长、顿悟突破、队伍成长和末尾追赶。

## Scope

### In Scope
- `GrowthSystem` 类:
  - `ApplyGrowthNode(character, nodeId)` — 章节固定节点
  - `ApplyEpiphanyReward(character, rewardConfig)` — 顿悟奖励 + 境界检查
  - `ApplyPartyGrowthNode(characterId, nodeId)` — 同伴成长节点
  - `ApplyCatchupGrowth(characterId, reason)` — 末尾追赶
  - `GetChapterBaselinePower(chapter)` → int — 返回章节推荐基线
- 章节成长规则:
  - 19 个节点，总 budget = 107 属性点
  - 每节点 +3~8 属性点（分配到多个五维属性）
- 顿悟成长规则:
  - 固定奖励（不可玩家选择）
  - 成功后调用 `RealmSystem.CheckBreakthrough`
- 追赶规则:
  - 每章上限 `catchup_growth_cap_per_chapter = 8`
  - 只对落后于基线的同伴生效
- 通过 Modifier (Permanent 层) 写入属性
- EventBus 通知: `GrowthAppliedEvent`, `RealmBreakthroughEvent`

### Out of Scope
- 成长节点的具体数值配置 → 后续由策划填表
- 顿悟事件触发条件 → 顿悟系统 Epic
- 同伴个人旅程触发 → 队伍管理 Epic
- UI 表现 → Presentation 层

## Technical Notes

- 路径: `src/FengZhi.Foundation/CharacterData/GrowthSystem.cs`
- 成长节点配置: `assets/data/growth/chapter-nodes.yaml`
- 追赶公式: `catchup = min(cap, baseline - currentPower)`
- 五维总和上限 250 → 达到后顿悟不再给属性点，改为解锁内容（GDD Edge Case）
- 写入路径: GrowthSystem → ModifierStack.Add(Permanent, source="growth:{nodeId}")

## Acceptance Criteria

- [ ] **AC1**: GDD AC#5 — ApplyEpiphanyReward 使功力从 114→116，触发境界突破
- [ ] **AC2**: GDD AC#6 — 功力已达 115 但叙事未满足 → 不触发突破，返回 PendingNarrative
- [ ] **AC3**: ApplyGrowthNode("ch3_node2") 正确增加指定属性（永久修改器）
- [ ] **AC4**: ApplyCatchupGrowth 对落后 15 点的同伴施加 8 点追赶（受上限约束）
- [ ] **AC5**: ApplyCatchupGrowth 对不落后的同伴 → 不施加任何成长
- [ ] **AC6**: GetChapterBaselinePower(5) 返回第 5 章推荐基线值
- [ ] **AC7**: 五维总和已达 250 → ApplyEpiphanyReward 不增加属性点（返回 content unlock flag）

## Test Evidence Path

`tests/Foundation/CharacterData/GrowthSystemTests.cs`
