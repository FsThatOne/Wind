# pc-001: chapter_00 主线节点骨架与状态 key

> **Epic**: 序章内容生产
> **Status**: Complete
> **Last Updated**: 2026-06-30
> **Type**: Config/Data
> **Layer**: Content / Core Integration
> **Estimate**: 0.5 day
> **Design Spec**: `docs/superpowers/specs/2026-06-29-prologue-a-day-and-night-design.md`
> **GDD 来源**: `design/gdd/main-narrative.md` 序章：风止；序章尾段：师兄误会与分别
> **TR-IDs**: TR-main-narrative-001, TR-main-narrative-002, TR-main-narrative-003
> **ADR**: ADR-0005: Dialogue Data Format; ADR-0001: Event Bus Architecture; ADR-0003: Data Configuration Format
> **Manifest Version**: 2026-06-30

## Context

序章设计已确定为“一日一夜”结构。这个 story 只建立 P00-01 到 P00-10 的主线内容骨架与状态 key，不编写全部对白，不接战斗和误会完整逻辑。

目标是让后续内容 stories 有稳定的节点编号、推进顺序和状态命名，避免对话、洞察、灭门搜证和师兄尾段各写各的。

## Scope

**In scope:**

- 定义 P00-01 到 P00-10 主线节点列表、节点类型、前置和完成条件。
- 定义序章内容状态 key 命名。
- 明确哪些节点由 dialogue、arrival、choice、gate 或 scene trigger 驱动。
- 写入或更新 chapter_00 主线配置的最小骨架，或在项目当前缺少主线 YAML 接入口时创建设计对齐的内容清单文件。

**Out of scope:**

- 完整对白文本。
- 师姐采集、山庄跑腿、崖洞、灭门、师兄尾段的具体场景实现。
- 误会系统 FSM 接入。
- 战斗教学实现。

## Acceptance Criteria

- AC-1: P00-01 到 P00-10 均有唯一 id、标题、节点类型、前置条件和完成条件。
- AC-2: 节点顺序符合批准设计：醒来 → 师姐采集 → 山庄跑腿 → 拖到天黑 → 崖洞取酒夜宿 → 回庄搜证 → 师兄误会与分别。
- AC-3: 明确状态 key：`prologue_gathering_taught`、`prologue_mount_foreshadowed`、`prologue_wine_delayed`、`prologue_wine_obtained`、`prologue_massacre_discovered`、`prologue_blood_letter_obtained`、`prologue_sister_missing_known`、`mis_senior_brother_survivor_suspicion`、`senior_brother_mis_resolved`、`senior_brother_letter_contact_unlocked`。
- AC-4: 灭门前节点不得包含正式战斗教学或外来阴谋线索。
- AC-5: P00-10 标注依赖误会系统和战斗教学内容，但不阻塞前 9 个节点的内容制作。

## Implementation Notes

- 优先复用 `Core/Narrative` 的 YAML 兼容节点图格式。
- 若当前项目尚无 chapter_00 主线 YAML 数据目录，先创建内容清单或 seed 文件，字段命名保持与 `main-narrative` runtime 模型可迁移。
- 状态 key 使用小写 snake_case，便于对话条件、洞察奖励和后续存档查询复用。
- 所有跨系统副作用在后续实现中通过 EventBus 或现有服务接口触发，不在内容文件中写硬编码 C# 行为。

## Files to Create/Modify

**Likely create or modify:**

- `assets/data/narrative/chapter_00.yaml` or equivalent project-local narrative data file
- `production/qa/evidence/pc-001-chapter-00-mainline-graph-evidence.md`

## QA Test Cases

1. **节点覆盖**: 检查配置或内容清单包含 P00-01 到 P00-10，且 id 无重复。
2. **顺序检查**: 从入口节点沿 next / gate 可到达 P00-10，中间无断链。
3. **状态 key 检查**: story AC-3 中列出的 key 均存在且无拼写变体。
4. **范围检查**: 灭门前节点内容不包含 combat/tutorial_combat、外人脚印、凶手线索或师姐欲言又止式阴谋提示。

## Test Evidence

- Config/Data: `production/qa/evidence/pc-001-chapter-00-mainline-graph-evidence.md`

## Dependencies

- Depends on: approved prologue design spec
- Unlocks: pc-002, pc-003, pc-004, pc-005, pc-006

## Completion Notes

**Completed**: 2026-06-30  
**Criteria**: 5/5 passing.  
**Deviations**: None.  
**Test Evidence**: `tests/unit/narrative/chapter_00_mainline_graph_test.cs`; `production/qa/evidence/pc-001-chapter-00-mainline-graph-evidence.md`.  
**Code Review**: Skipped in lean mode; content/config closure verified by automated tests.  
**Effort**: estimate 4.00 h / actual approx. 0.75 h (batch implementation and closure; variance -81%).
