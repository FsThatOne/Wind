# pc-004: 崖洞回忆互动与夜宿

> **Epic**: 序章内容生产
> **Status**: In Progress
> **Last Updated**: 2026-06-30
> **Type**: Config/Data
> **Layer**: Content / Feature Integration
> **Estimate**: 0.5 day
> **Design Spec**: `docs/superpowers/specs/2026-06-29-prologue-a-day-and-night-design.md`
> **GDD 来源**: `design/gdd/exploration-insight.md`; `design/gdd/main-narrative.md` 灭门事件
> **TR-IDs**: TR-exploration-insight-005, TR-dialogue-system-008
> **ADR**: ADR-0018: Exploration / Insight; ADR-0005: Dialogue Data Format
> **Manifest Version**: 2026-06-10
> **Depends On**: pc-001, pc-003, ei-009

## Context

现有 `ei-009` 已为后山崖洞配置 4 个 InsightNode。本 story 将崖洞内容调整为批准设计：崖洞不是阴谋空间，而是主角和师姐的小秘密基地；每个互动点都勾起主人公与师姐的回忆。

特别需要将“松动砖缝”改为“墙上招式刻画”或等价回忆点。

## Scope

**In scope:**

- 调整崖洞洞察节点文案，使每处互动都服务师姐回忆。
- 将 `cave_loose_brick` 替换为墙上招式刻画，或新增正式节点并废弃旧节点。
- 保留三年陈药酒、草席/小凳、药壶残香等回忆触发点。
- 配置取酒完成和夜宿推进，设置或记录 `prologue_wine_obtained`。
- 确认崖洞夜版氛围是安静和回忆，不是藏宝、矿洞、怪物或阴谋副本。

**Out of scope:**

- 新增崖洞地图资产。
- 新增战斗 encounter。
- 新增 Loot / CodePhrase / MartialFragment 奖励。
- 灭门后山庄搜证。

## Acceptance Criteria

- AC-1: 崖洞至少有 4 个互动/洞察点：墙上招式刻画、三年陈药酒、草席/小凳、药壶残香或等价内容。
- AC-2: `cave_loose_brick` 不再表达松动砖缝或暗藏空间，改为墙上招式刻画/师姐回忆。
- AC-3: 每个互动点的文本都勾起主角与师姐的具体回忆。
- AC-4: 药酒取得后能设置或记录 `prologue_wine_obtained`。
- AC-5: 夜宿原因明确来自天黑路险、山雨、崖路湿滑或体力不支，不来自敌人阻拦。
- AC-6: 崖洞内容不包含凶手线索、外人痕迹、暗号奖励或可直接解释灭门的证据。

## Implementation Notes

- 参考现有 `BackMountainCliffCaveGame` 中 InsightNode 注册。
- EnvironmentDetail 节点无奖励，仅显示内心独白。
- 若存在 Clue 类型节点，应确认它不会变成凶手线索；本 story 推荐使用 EnvironmentDetail 为主。
- 文案要温暖、具体、短，不写长篇回忆。

## Files to Create/Modify

**Likely modify:**

- `feng-zhi/scripts/BackMountainCliffCaveGame.cs`
- `feng-zhi/assets/data/dialogues/chapter_00/wine_pickup_01.yaml`
- `feng-zhi/assets/data/dialogues/chapter_00/rest_spot_01.yaml`
- `feng-zhi/assets/data/dialogues/chapter_00/memory_marker_01.yaml`
- `production/qa/evidence/pc-004-cave-memory-and-overnight-evidence.md`

## QA Test Cases

1. **回忆点覆盖**: 试玩或检查配置，确认至少 4 个互动点均为师姐相关回忆。
2. **砖缝替换**: 搜索 `cave_loose_brick` 或其显示文本，确认不再出现松动砖缝/暗格表达。
3. **取酒推进**: 触发取酒后能进入夜宿或设置 `prologue_wine_obtained`。
4. **无阴谋污染**: 检查崖洞文本不出现外人痕迹、凶手线索、暗令、门派徽记。
5. **端到端追查**: 靠近洞察点、追查、显示独白，流程正常。

## Test Evidence

- Config/Data: `production/qa/evidence/pc-004-cave-memory-and-overnight-evidence.md`

## Dependencies

- Depends on: pc-001, pc-003, ei-009
- Unlocks: pc-005
