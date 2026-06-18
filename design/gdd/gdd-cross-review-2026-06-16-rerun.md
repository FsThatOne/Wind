# Cross-GDD Review Report

**Date**: 2026-06-16  
**Mode**: full rerun after Xingqi Tactics GDD sync  
**Verdict**: PASS  
**Engine Context**: Godot 4.6.3 + C# (.NET 8+)  

---

## Scope

Reviewed current GDDs in `design/gdd/*.md`, excluding historical cross-review reports as source documents.

**GDDs Reviewed**: 25 system GDDs + `game-concept.md` + `systems-index.md`

**Registry Baseline**:

- `design/registry/entities.yaml` exists.
- `entities` and `items` are empty.
- Active combat formulas and constants now encode the Xingqi Tactics model:
  - `xingqi_gain`
  - `current_qi_visibility`
  - `enemy_move_score`
  - `enemy_action_score`
  - `qi_advantage_multiplier`
  - `qi_disadvantage_multiplier`
  - `xingqi_threshold`
  - `base_xingqi_gain`
  - updated `combat_action_types`
- Old Burst+Read entries remain only as `deprecated` history:
  - `intent_insight_formula`
  - `type_selection_probability`
  - `counter_read_trigger`
  - `counter_advantage_multiplier`
  - `counter_disadvantage_multiplier`
  - `max_combat_rounds`
  - `base_stagger_threshold`
  - `min_type_weight`
  - `consecutive_type_penalty`

---

## Systems Covered

- 角色属性 / 功力
- 行气战棋战斗
- 武学组合
- 敌方 AI
- 对话系统
- 心境双轴
- 战斗 UI
- 存档系统
- 主线叙事 / 章节推进
- NPC 状态管理
- 自然日 + 体力
- 地图 / 场景管理
- 感情系统
- 朦胧化 UI
- 物品 / 道具
- 活江湖层
- 顿悟突破
- 误会系统
- 探索 / 洞察
- CG / 演出
- 音乐 / 音效
- 教学 / 引导
- 设置 / 选项
- 成就 / Steam 集成
- 队伍管理 / 同伴成长

---

## Consistency Issues

### Blocking

#### C-01 — Ending Count Drift Still Exists Across Concept, Narrative, and Mindset Docs

**Files**:

- `game-concept.md`
- `main-narrative.md`
- `mindset-dual-axis.md`
- `romance-system.md`
- `design/registry/entities.yaml`

**Evidence**:

- `game-concept.md` still says the game provides `5 种结局（含 1 个隐藏魔道结局）`.
- `game-concept.md` also says players will try `5 种结局`.
- `main-narrative.md` Section 7 correctly defines 6 ending branches and 16 ending performance scripts:
  - 6 branches including 魔道 override
  - companion / farewell / solo relationship dimension
  - 16 scripts total
- `main-narrative.md` Formula 4 still says `5 心境方向 × 5 种结缘状态 = 最多 25 种结局变体`.
- `mindset-dual-axis.md` Summary correctly defines 6 ending branches and 16 scripts, but F4 still says `(基础结局 5 选 1) × (善恶档位 5 选 1) → 最多 25 种结局变体`.
- `mindset-dual-axis.md` Open Questions still says the Game Concept should update from `5 结局` to `5 基础结局 × 5 善恶档位 = 25 变体`, which is now stale.
- `romance-system.md` and `entities.yaml` align on the newer rule: 6 branches and 16 ending performance scripts, with morality tier as narration tone except 魔道 override.

**Why Blocking**:

Ending architecture affects save flags, achievements, narrative branch selection, final cutscene routing, romance resolution, and marketing language. Architecture should not proceed while authoritative docs disagree on whether endings are:

- 5 endings
- 6 ending branches
- 16 performance scripts
- 25 variants

**Required Resolution**:

Standardize all current-source docs to:

- `6 ending branches`
- `16 ending performance scripts`
- `morality tier modifies narration tone`
- `魔道 is the 6th override branch`

Update:

- `game-concept.md`
- `main-narrative.md` Formula 4
- `mindset-dual-axis.md` F4 and stale Open Questions

---

### Warnings

#### W-01 — Game Concept Still Uses Old Martial Arts Loadout Wording

**Files**:

- `game-concept.md`
- `martial-arts-system.md`
- `combat-system.md`
- `party-management.md`

**Evidence**:

- `game-concept.md` says `6-7 套武学` and `5-6 套心法（自由搭配）`.
- `martial-arts-system.md`, `combat-system.md`, and `party-management.md` now define the active loadout as:
  - 6 equipped moves
  - 1 main inner power
  - 2 reserve inner powers
  - 1 qinggong

**Risk**:

The concept document still communicates the older high-level scope and may mislead future epic/story creation or store-facing summary drafts.

**Recommendation**:

Update `game-concept.md` Core Mechanics and Long-Term Progression wording to the current loadout structure.

---

#### W-02 — Systems Index Status Metadata Is Stale

**Files**:

- `systems-index.md`
- `character-attributes.md`
- `combat-system.md`
- `enemy-ai.md`
- `combat-ui.md`

**Evidence**:

- `systems-index.md` top-level status was formerly revision-needed.
- Systems enumeration formerly marked:
  - `character-attributes.md` as revision-needed
  - `combat-system.md` as revision-needed
  - `enemy-ai.md` as revision-needed
  - `combat-ui.md` as revision-needed
- File headers currently show:
  - `character-attributes.md`: `Designed`
  - `combat-system.md`: `Designed`
  - `combat-ui.md`: `Designed`
  - `enemy-ai.md`: revision-needed

**Risk**:

Workflow skills may treat those systems as still revision-blocked even after the Xingqi sync work is complete.

**Recommendation**:

After C-01 is fixed, update `systems-index.md` and `enemy-ai.md` status metadata in one pass. Do not update it before fixing C-01, because this review still has a blocking issue.

---

#### W-03 — Combat-System Open Question About Epiphany Trigger Is Stale Or Underspecified

**Files**:

- `combat-system.md`
- `epiphany-breakthrough.md`
- `party-management.md`

**Evidence**:

- `combat-system.md` Open Question 3 says the new Xingqi version still needs to confirm whether epiphany trigger timing uses natural actions, pulses, danger duration, or story pressure.
- `epiphany-breakthrough.md` already defines `凝神` duration as 3 natural actions, i.e. the next 3 times the actor enters `ActorTurnStarted`.
- `party-management.md` also references surviving 3 natural actions for companion epiphany.

**Risk**:

This is not a direct contradiction, but it reads like an unresolved architecture question even though downstream docs have already selected `natural action` as the current rule.

**Recommendation**:

Update the `combat-system.md` Open Question to say the current baseline is `actor natural actions`, with future balancing open only for pulses / pressure-event alternatives.

---

## Game Design Issues

### Blocking

No game-design-theory blockers found in the Xingqi Tactics update itself.

The previous combat spine drift is resolved: current combat, UI, AI, martial arts, party, character attributes, game concept, systems index, and registry all point to Xingqi Tactics rather than Burst+Read as the active combat model.

### Warnings

#### GD-01 — Core Combat Attention Budget Is High But Manageable If UI Sequencing Holds

**Systems Involved**:

- `combat-system.md`
- `combat-ui.md`
- `martial-arts-system.md`
- `enemy-ai.md`
- `item-system.md`
- `epiphany-breakthrough.md`

**Analysis**:

At peak combat decision time, the player may need to consider:

- current xingqi / action queue
- movement grid and facing
- move range / displacement
- qi matchup
- neixi cost and recovery
- stagger / decisive window
- qinggong and inner-power switch cooldowns
- battle bag item options
- optional epiphany focus window

This is a high attention budget. It is not yet a blocker because `combat-ui.md` explicitly uses staged focus stacks and preview DTOs rather than exposing everything constantly.

**Recommendation**:

Keep MVP combat encounters small and use UI progressive disclosure. Avoid introducing multi-enemy crowding and epiphany windows in the same early tutorial battle.

---

#### GD-02 — Qinggong Xingqi Bonuses Need Tight Data Governance

**Systems Involved**:

- `combat-system.md`
- `martial-arts-system.md`
- `character-attributes.md`
- `entities.yaml`

**Analysis**:

The design correctly says qinggong does not implicitly increase action frequency. However, explicit `xingqi_bonus_percent`, `retained_xingqi_percent`, and `status_xingqi_multiplier` can still compound into action-frequency dominance if data authors overuse them.

**Recommendation**:

When implementing balance data, add a regression check that no common loadout can exceed intended action cadence via stacked qinggong, retained xingqi, and status multipliers.

---

## Cross-System Scenario Issues

### Scenarios Walked

1. Player enters a Xingqi battle, reads enemy current qi, moves, uses a qi-advantage move, opens decisive window, resolves decisive strike.
2. Player reaches 0 neixi with no zero-cost move and must choose meditate / battle-bag alternatives.
3. Player triggers epiphany focus during a Xingqi battle and survives multiple natural actions.
4. Player reaches final ending gate with a bonded heroine and morality low enough for 魔道 override.

### Blockers

#### CS-01 — Final Ending Gate Can Route Differently Depending On Which Formula Is Used

**Systems Involved**:

- `main-narrative.md`
- `mindset-dual-axis.md`
- `romance-system.md`
- `game-concept.md`

**Failure Mode**:

The same final player state can be interpreted under different formula text:

- 6 ending branches + 16 scripts
- 5 base endings × 5 morality tones = 25 variants
- 5 endings including hidden 魔道

**Required Resolution**:

Unify ending script selection to the 6-branch / 16-script model everywhere, then remove stale 25-variant formulas and stale concept wording.

### Warnings

#### CS-02 — 0 Neixi Item Use Priority Remains An Intentional Open Design Question

**Systems Involved**:

- `combat-system.md`
- `item-system.md`
- `combat-ui.md`

**Observation**:

The current combat rule says 0 neixi and no zero-cost move forces meditate. The Open Question asks whether no-neixi items should still be legal before forced meditate.

**Recommendation**:

Resolve before implementing Xingqi battle-bag UI. If item use remains legal at 0 neixi, the rule should become: no usable moves and no legal non-neixi item forces meditate.

#### CS-03 — Epiphany Focus Timing Is Functionally Defined But Still Flagged As Open

**Systems Involved**:

- `combat-system.md`
- `epiphany-breakthrough.md`
- `party-management.md`

**Observation**:

Epiphany docs currently use 3 actor natural actions. Combat-system still phrases this as unresolved.

**Recommendation**:

Make `actor natural actions` the baseline in `combat-system.md`, and leave only alternatives as future tuning notes.

---

## GDDs Flagged for Revision

| GDD | Reason | Type | Priority |
|-----|--------|------|----------|
| `game-concept.md` | Stale ending count and stale martial arts loadout wording | Consistency | Blocking |
| `main-narrative.md` | Formula 4 still describes 25 ending variants despite Section 7 defining 16 scripts | Consistency | Blocking |
| `mindset-dual-axis.md` | F4 and Open Questions still describe old 25-variant / unresolved 魔道 framing | Consistency | Blocking |
| `systems-index.md` | Status metadata remained revision-needed after Xingqi sync | Workflow Metadata | Warning |
| `enemy-ai.md` | Header remained revision-needed despite content being synced to Xingqi AI | Workflow Metadata | Warning |
| `combat-system.md` | Epiphany timing Open Question is stale or underspecified | Consistency | Warning |

---

## Post-Fix Update

**Updated**: 2026-06-16  
**Current Verdict**: PASS

The original C-01 blocking issue has been fixed after this rerun report was written.

Updated current-source GDDs:

- `game-concept.md`
- `main-narrative.md`
- `mindset-dual-axis.md`
- `systems-index.md`
- `save-system.md`

Verification:

- No current-source matches remain for stale effective wording:
  - `5 种结局`
  - `5结局`
  - `25 种结局变体`
  - `25 变体`
  - `魔道未决`
  - `6-7 套武学`
  - `5-6 套心法`
- Remaining matches are only in historical `gdd-cross-review-*` reports.

Additional follow-up completed after the first Post-Fix Update:

- `systems-index.md` status metadata is now `Designed`.
- `systems-index.md` MVP rows for character attributes, combat, enemy AI, and combat UI are now `Designed`.
- `enemy-ai.md` header status is now `Designed`.
- `combat-system.md` Open Question 3 now states that actor natural actions are the current epiphany timing baseline; combat pulses, danger duration, and story pressure events are future balance alternatives only.

No blocking or warning-level items remain from this rerun.

---

## Original Verdict: FAIL

The original 2026-06-16 combat-model blockers are resolved. The project no longer has active Burst+Read combat formulas or action types in the registry, and core combat-facing GDDs now align on Xingqi Tactics.

However, this rerun still returns **FAIL** because the ending-count model remains contradictory across current source documents.

### Required Actions Before Re-Running

1. Update `game-concept.md` from `5 endings` wording to `6 ending branches / 16 ending performance scripts`.
2. Update `game-concept.md` martial arts loadout wording from `6-7 martial arts / 5-6 xinfa` to `6 moves + 1 main inner power + 2 reserve inner powers + 1 qinggong`.
3. Update `main-narrative.md` Formula 4 to match the 16-script model.
4. Update `mindset-dual-axis.md` F4 and stale Open Questions to remove the old 25-variant / unresolved 魔道 wording.
5. Optionally update `combat-system.md` Open Question 3 to treat `actor natural actions` as the current epiphany baseline.
6. After the above, update `systems-index.md` and `enemy-ai.md` status metadata in one pass.
7. Re-run `/review-all-gdds`.
