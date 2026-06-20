# Cross-GDD Review Report

**Date**: 2026-06-16  
**Mode**: full  
**Verdict**: FAIL  
**Engine Context**: Godot 4.7-stable + C# (.NET 8+)  

---

## Scope

Reviewed current GDDs in `design/gdd/*.md`, excluding historical `gdd-cross-review-*` reports, `design/gdd/reviews/*`, and `_deprecated/*`.

**GDDs Reviewed**: 25 system GDDs + `game-concept.md` + `systems-index.md`

**Registry Baseline**:

- `design/registry/entities.yaml` exists.
- `entities` and `items` are empty.
- `formulas` and `constants` still encode the old Burst+Read combat model, including `intent_insight_formula`, `combat_action_types`, `counter_*`, and `max_combat_rounds`.

**Important Context**:

- Current design discussion has shifted combat toward a lightweight wuxia tactics model driven by `行气条`.
- Formal GDDs still mostly describe the older Burst+Read simultaneous-resolution model.
- This review treats the formal GDDs as source documents, but flags the newly identified direction mismatch as blocking because architecture cannot safely proceed while combat fundamentals are unsettled.

---

## Systems Covered

- 角色属性 / 功力
- 回合制战斗
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

#### C-01 — Combat Model Drift: Simultaneous Resolution vs 行气/行动序

**Files**:

- `combat-system.md`
- `character-attributes.md`
- `combat-ui.md`
- `enemy-ai.md`
- `systems-index.md`
- `game-concept.md`

**Evidence**:

- `combat-system.md` states all actions resolve simultaneously and speed only determines presentation order.
- `character-attributes.md` defines `speed = agility × 1.0 + insight × 0.3` and says speed determines initiative/先手.
- `combat-ui.md` still models round states as `ROUND_START → PLAYER_DECISION → RESOLVING → ROUND_END`.
- `systems-index.md` and `game-concept.md` still identify combat as Burst+Read.
- New design notes define a different model: independent `行气条`, movement + action, tactics board, and no base counter button.

**Why Blocking**:

Architecture cannot define combat state, event order, UI state machines, AI scheduling, or tests until the combat model is unified.

**Required Resolution**:

Choose and rewrite around one combat spine:

- Old spine: Burst+Read simultaneous resolution.
- New spine: 行气驱动的轻量武侠战棋.

Given current design direction, the likely action is to rewrite `combat-system.md`, `combat-ui.md`, `enemy-ai.md`, `character-attributes.md`, `systems-index.md`, and `game-concept.md` combat sections toward the 行气战棋 model.

---

#### C-02 — 洞察 Participates in Speed, Contradicting Attribute Responsibility

**Files**:

- `character-attributes.md`
- `combat-system.md`
- `exploration-insight.md`
- `dialogue-system.md`
- `design/notes/2026-06-16_combat-redesign-brainstorm.md`

**Evidence**:

- `character-attributes.md` currently defines `speed = agility × 1.0 + insight × 0.3`.
- `combat-system.md` uses `insight` for intent insight probability.
- Exploration and dialogue systems use insight for noticing hidden information.
- Current approved design direction says: 洞察不影响行气速度；行气速度只受敏捷和明确轻功词条影响.

**Why Blocking**:

洞察 currently gives both information advantage and action-speed advantage. This violates clean attribute ownership and risks creating a dominant high-insight build.

**Required Resolution**:

- Remove `insight` from action-speed / 行气 calculations.
- Add `xingqi_gain` or `xingqi_speed` as a combat-facing derived value.
- Define `xingqi_gain = base_xingqi_gain × agility_factor × qinggong_xingqi_multiplier`.
- Keep `insight` scoped to 气机识破, 招路规律判断, exploration hidden cues, and dialogue insight.

---

#### C-03 — Enemy AI Tell Mechanics Are Not Closed Through Combat and UI

**Files**:

- `enemy-ai.md`
- `combat-system.md`
- `combat-ui.md`

**Evidence**:

- `enemy-ai.md` defines player-readable mechanics such as weakness type, charge announcement, counter-read, type lock, read break, and weakness hit.
- `combat-system.md` interaction table does not fully define these as formal events or resolution inputs.
- `combat-ui.md` only covers basic intent icons, move panel, counter, decisive strike, and resource bars.

**Why Blocking**:

The AI GDD’s readability mechanics cannot be implemented or surfaced consistently. Boss tells, weakness windows, and “读破” moments may remain invisible or unresolvable.

**Required Resolution**:

- Add formal combat events such as `OnChargeAnnounced`, `OnTypeLockActivated`, `OnReadBreak`, `OnWeaknessHit`, and any replacements required by the new 行气战棋 model.
- Add UI specifications for these tells, including iconography, priority, animation, audio hooks, accessibility, and acceptance criteria.
- Reconcile these mechanics with the new “current inner-power qi” model rather than old “next intent type”.

---

### Warnings

#### W-01 — Ending Count Terminology Still Drifts

**Files**:

- `game-concept.md`
- `mindset-dual-axis.md`
- `main-narrative.md`
- `save-system.md`
- `systems-index.md`

**Evidence**:

- `game-concept.md` still describes “5 种结局（含 1 个隐藏魔道结局）”.
- `mindset-dual-axis.md` and `main-narrative.md` describe 6 ending branches and 16 performance scripts when romance state is included.

**Risk**:

Scope estimates, ending content, achievement logic, save flags, and marketing copy may count endings differently.

**Recommendation**:

Standardize terminology:

- `6 ending branches`
- `16 ending performance scripts`
- `morality tier modifies narration tone, except 魔道 override`

---

#### W-02 — Combat Tuning Ownership Is Split Across Too Many GDDs

**Files**:

- `combat-system.md`
- `character-attributes.md`
- `enemy-ai.md`
- `design/registry/entities.yaml`

**Evidence**:

- `base_stagger_threshold` is owned by `character-attributes.md` in the registry.
- Counter/stagger tuning is owned by `combat-system.md`.
- Boss overrides and AI phase thresholds live in `enemy-ai.md`.

**Risk**:

The timing of `门户大开` / decisive windows can drift when one document is tuned without the others.

**Recommendation**:

Make `combat-system.md` the SSoT for combat tempo, including stagger/portal thresholds and decisive timing. Let character attributes store state fields only; let enemy AI reference named combat parameters and declare allowed overrides.

---

#### W-03 — Combat UI Still Over-Indexes on HUD Panels

**Files**:

- `combat-ui.md`
- `combat-system.md`
- `design/notes/2026-06-16_combat-redesign-brainstorm.md`

**Evidence**:

- Current `combat-ui.md` is built around `WorldIntentLayer`, `HUDLayer`, intent icons, move panel, counter labels, and resource bars.
- New direction expects 棋盘为主, 沉浸极简, unit head `行气条`, current qi display, range preview, and movement/action selection.

**Risk**:

UI architecture may be designed for the old simultaneous-round model and fail to support the tactical board interaction.

**Recommendation**:

Rewrite the combat UI GDD after combat rules are settled. Prioritize board-first interaction, unit head information, range overlays, facing indicators, and minimal modal HUD.

---

## Game Design Issues

### Blocking

No purely design-theory blocker was found independent of the combat consistency blockers.

### Warnings

#### D-01 — `稳妥取胜` May Dominate `凝神顿悟`

**Files**:

- `epiphany-breakthrough.md`
- `combat-system.md`

**Evidence**:

- Skip rewards include strong immediate combat benefits: healing, inner-power recovery, stagger clearing, guaranteed crit, and mitigation.
- `凝神` asks the player to accept several turns of vulnerability for future growth.

**Risk**:

In hard fights, players may rationally choose the safer immediate reward, making “顿悟风险抉择” less meaningful.

**Recommendation**:

Reduce `skip_reward` to one clear defensive benefit, make it consume a meaningful action opportunity, or add future-opportunity cost when skipping.

---

#### D-02 — Global Pressure Budget Is Missing

**Files**:

- `npc-state.md`
- `misunderstanding-system.md`
- `main-narrative.md`
- `living-jianghu-layer.md`
- `romance-system.md`

**Evidence**:

- Fly letters can mark unread and request help.
- Misunderstandings have windows and permanent consequences.
- Main narrative can contain limited-time clue windows.
- Living Jianghu can queue rumors and off-screen events.
- Romance events can trigger during breathing periods.

**Risk**:

Each system individually supports “choices have weight”, but together they may convert breathing periods into a hidden checklist and produce anxiety rather than Jianghu flow.

**Recommendation**:

Add a global urgency/pressure budget:

- One high-urgency event per game day by default.
- Severe misunderstanding, urgent fly letter, and main-story deadline share one `urgency_channel`.
- Passive flavor letters should not use strong red-dot urgency.

---

#### D-03 — Irreversible Consequences Need Minimum Fairness Signals

**Files**:

- `misunderstanding-system.md`
- `main-narrative.md`
- `blurred-ui.md`

**Evidence**:

- Misunderstanding system intentionally hides timers and exact clearing requirements.
- Main narrative relies on diegetic urgency rather than explicit system prompts.
- Blurred UI intentionally obscures some long-term state.

**Risk**:

If multiple irreversible systems remain implicit at once, players may perceive outcomes as unfair rather than literary.

**Recommendation**:

Require every irreversible consequence to pass through at least three feedback stages:

- `HINTED`
- `PERCEIVED`
- `URGENT`

The copy can stay literary, but the player must be given enough signal to understand stakes.

---

#### D-04 — Silver Economy Needs Stronger Long-Term Sinks

**Files**:

- `item-system.md`
- `natural-day-stamina.md`
- `living-jianghu-layer.md`

**Evidence**:

- Silver sources include combat rewards, exploration pickup, task rewards, and decomposition.
- Sinks include shops, lodging, crafting/alchemy, and auctions.

**Risk**:

If equipment upgrades are intentionally capped and auctions are occasional, silver may lose meaning in mid/late game.

**Recommendation**:

Add narrative sinks such as:

- buying intelligence
- paying express messengers
- inn rumor inquiries
- relationship-repair etiquette costs
- auction deposits
- clue verification fees

Avoid pure numerical upgrade sinks that fight the wuxia tone.

---

#### D-05 — Combat Attention Budget Risks Exceeding 3-4 Active Focuses

**Files**:

- `combat-system.md`
- `combat-ui.md`
- `martial-arts-system.md`
- `party-management.md`

**Evidence**:

The old combat UI asks players to track HP, inner power, stagger, intent type, move type, counter status, decisive windows, and up to five party members.

**Risk**:

The tactical-board redesign will add movement, facing, range, unit `行气条`, and board state. Without pruning, combat may become a resource dashboard rather than “读人 / 破势”.

**Recommendation**:

For the redesigned combat UI:

- Default display: board, active actor, target qi, portal status, movement/range preview.
- Details on focus only: exact HP/inner power/status effects.
- Use UX tests to keep simultaneous active attention items around 3-4.

---

## Cross-System Scenario Issues

### Scenarios Walked

- Battle victory with epiphany completion and reward settlement
- Concentration epiphany with actor defeat and battle failure
- Inn rest crossing day boundary with delayed events and Living Jianghu tick
- Exploration insight discovered after Living Jianghu rumor
- Breathing period start with accumulated rumors, letters, and romance candidates

### Blockers

No scenario-specific blocker independent of the combat model blockers was found.

### Warnings

#### S-01 — Battle End Reward and Epiphany Reward Ordering Is Ambiguous

**Systems**:

- Combat
- Epiphany Breakthrough
- Combat UI
- Character Attributes

**Issue**:

If `凝神` completes on the same action sequence that ends battle, the documents do not define whether epiphany reward, battle reward, UI result panel, and growth writes are ordered or aggregated.

**Recommendation**:

Add `BattleRewardResolution`:

1. Resolve actor survival and failed concentration.
2. Resolve epiphany completion/failure.
3. Aggregate battle rewards.
4. Emit one UI result package.
5. Persist state.

---

#### S-02 — Rest Completion Can Trigger Too Many Presentation Events

**Systems**:

- Natural Day + Stamina
- Living Jianghu Layer
- NPC State
- Main Narrative
- Romance

**Issue**:

Day advancement, delayed events, fly letters, rumors, inner monologue, and relationship events can all become ready after one rest.

**Recommendation**:

Add `PostRestPresentationQueue` with priority:

1. Critical narrative
2. Relationship milestone
3. Companion letter / urgent request
4. Living Jianghu rumor
5. Inner monologue
6. Ambient pulse

Only one modal interaction should appear at a time; the rest enter queue/log.

---

#### S-03 — Rumor vs InsightNode Reward De-Duplication Is Underdefined

**Systems**:

- Exploration / Insight
- Living Jianghu Layer
- Item System
- Main Narrative

**Issue**:

Living Jianghu suppresses duplicate information after active exploration, but reward ownership is not fully split. A player who heard a rumor and later explores the node may receive a “new clue” reward twice.

**Recommendation**:

Split discovery rewards into:

- `information_reward`
- `material_reward`
- `ability_reward`

Rumors may consume or preview `information_reward`; physical rewards can remain on the node; key flags must be single-fire by `source_event_id`.

---

#### S-04 — Romance Lock-In Should Not Be Determined by Passive Queue Order

**Systems**:

- Romance
- Living Jianghu Layer
- Main Narrative
- NPC State

**Issue**:

The “first triggered” rule for simultaneous romance candidates can become queue-order dependent when Living Jianghu and breathing-period events release several candidates together.

**Recommendation**:

Relationship lock-in should require player-active selection. Passive encounter order must not silently determine final romance commitment.

---

## GDDs Flagged for Revision

| GDD | Reason | Type | Priority |
|---|---|---|---|
| `combat-system.md` | Combat spine contradicts current 行气战棋 direction and attribute/UI assumptions | Consistency | Blocking |
| `character-attributes.md` | `speed` includes `insight`; action-speed responsibility is wrong | Consistency | Blocking |
| `combat-ui.md` | UI state model and event subscriptions fit old Burst+Read model | Consistency | Blocking |
| `enemy-ai.md` | AI tells/weaknesses not closed through combat/UI; old intent model likely stale | Consistency | Blocking |
| `systems-index.md` | Still frames core combat as Burst+Read | Consistency | Blocking |
| `game-concept.md` | Still frames core loop as Burst+Read and has stale ending count wording | Consistency | Warning |
| `epiphany-breakthrough.md` | `稳妥取胜` may dominate `凝神顿悟`; reward ordering ambiguity | Design Theory | Warning |
| `npc-state.md` | Contributes to global pressure overload without shared urgency budget | Design Theory | Warning |
| `misunderstanding-system.md` | Irreversible consequences need minimum fairness signaling | Design Theory | Warning |
| `main-narrative.md` | Timed pressure must share global urgency budget | Design Theory | Warning |
| `living-jianghu-layer.md` | Post-rest presentation queue and rumor reward de-dup need protocol | Scenario | Warning |
| `exploration-insight.md` | Insight rewards need information/material/ability split | Scenario | Warning |
| `romance-system.md` | Romance commitment should not be queue-order dependent | Scenario | Warning |
| `item-system.md` | Silver economy needs stronger long-term narrative sinks | Design Theory | Warning |

---

## Required Actions Before Architecture

1. Decide formally that the combat system is moving from Burst+Read simultaneous resolution to 行气驱动的轻量武侠战棋.
2. Rewrite `combat-system.md` around 行气, movement + action, board position, facing, current qi, portal opening, and decisive strike.
3. Rewrite `character-attributes.md` combat timing section:
   - remove `insight` from action-speed/行气;
   - define `xingqi_gain`;
   - clarify whether old `speed` remains or is deprecated.
4. Rewrite `combat-ui.md` as board-first tactical UI:
   - unit head `行气条`;
   - current qi symbols;
   - range and movement preview;
   - portal status;
   - minimal HUD.
5. Rewrite `enemy-ai.md` around current inner-power qi, board movement, readable patterns, and formal tell events.
6. Update `systems-index.md` and `game-concept.md` to remove stale Burst+Read descriptions.
7. Standardize ending terminology across concept, mindset, narrative, save, achievements, and systems index.
8. Add cross-system queue and reward protocols:
   - `BattleRewardResolution`;
   - `PostRestPresentationQueue`;
   - single-fire reward ownership by `source_event_id`;
   - shared urgency budget.

---

## Verdict

**FAIL**

The project is not ready for architecture creation because multiple core GDDs still encode a combat model that the current design direction has superseded. The blocking issues are focused and resolvable, but they must be resolved before `/create-architecture`, otherwise architecture will be built on stale mechanics.

