# pc-002: 师姐采药/采矿教学内容

> **Epic**: 序章内容生产
> **Status**: In Progress
> **Last Updated**: 2026-06-30
> **Type**: Config/Data
> **Layer**: Content / Presentation Integration
> **Estimate**: 0.5 day
> **Design Spec**: `docs/superpowers/specs/2026-06-29-prologue-a-day-and-night-design.md`
> **GDD 来源**: `design/gdd/tutorial-onboarding.md` 序章·师门日常；`design/gdd/main-narrative.md` 序章：风止
> **TR-IDs**: TR-dialogue-system-004, TR-dialogue-system-008, TR-dialogue-system-009
> **ADR**: ADR-0005: Dialogue Data Format; ADR-0003: Data Configuration Format
> **Manifest Version**: 2026-06-10
> **Depends On**: pc-001

## Context

师姐白檀是序章情感核心。本 story 制作 P00-02 和 P00-03：师姐带主角采药、采矿，解释主角此前被保护得太好所以没学过采集，并通过动物足迹埋下未来坐骑系统伏笔。

这段必须保持幸福、美好和安全，不提前透露山庄外面的事，也不引入可疑外人痕迹。

## Scope

**In scope:**

- 编写或配置师姐采药教学对话。
- 编写或配置师姐采矿教学对话。
- 添加动物足迹调查文本和坐骑伏笔台词。
- 写入 `prologue_gathering_taught` 和 `prologue_mount_foreshadowed` 状态触发说明或实际事件配置。
- 明确采集提示使用文学化底部提示或当前项目已有提示方式，不使用强制弹窗。

**Out of scope:**

- 新增完整采集系统逻辑。
- 新增坐骑系统。
- 新场景大地图制作。
- 战斗教学。

## Acceptance Criteria

- AC-1: 师姐明确解释“主角小时候被保护得太好，她怕主角受伤，所以此前一直没正式教采药/采矿”。
- AC-2: 采药和采矿各有一次可完成的引导动作或等价内容节点。
- AC-3: 动物足迹被解释为动物痕迹，不是外人脚印或敌人痕迹。
- AC-4: 师姐自然提到“有些动物通人性，驯好了能驮人远行”，只作为坐骑系统伏笔，不展开系统教学。
- AC-5: 本段不透露山庄外部阴谋、不出现师姐欲言又止式危险暗示。
- AC-6: 完成后能设置或记录 `prologue_gathering_taught` 与 `prologue_mount_foreshadowed`。

## Implementation Notes

- 对话语气应是亲密、轻松、带一点师姐式管束。
- 采药/采矿动作可以先复用现有 interactable 或占位交互；本 story 关注内容和状态，不要求新增采集系统。
- 对话 YAML 必须符合 ADR-0005：`id`、`version`、`entry_node`、`nodes`，节点引用有效。
- 若使用现有 `chapter_00` 对话目录，文件命名建议：
  - `sister_gather_herb_01.yaml`
  - `sister_gather_ore_01.yaml`
  - `animal_tracks_mount_foreshadow_01.yaml`

## Files to Create/Modify

**Likely create or modify:**

- `feng-zhi/assets/data/dialogues/chapter_00/sister_gather_herb_01.yaml`
- `feng-zhi/assets/data/dialogues/chapter_00/sister_gather_ore_01.yaml`
- `feng-zhi/assets/data/dialogues/chapter_00/animal_tracks_mount_foreshadow_01.yaml`
- `production/qa/evidence/pc-002-sister-gathering-tutorial-content-evidence.md`

## QA Test Cases

1. **保护理由**: 读对话或试玩节点，确认师姐解释此前不教采集的原因是保护主角，不是主角无能或系统遗忘。
2. **采集双教学**: 确认采药和采矿均有独立内容节点或交互步骤。
3. **动物足迹**: 确认足迹文本指向动物，并且没有“外人”“敌人”“陌生人”等阴谋导向词。
4. **坐骑伏笔**: 确认台词只埋“动物可驮人远行”的世界观伏笔，不开启坐骑 UI 或坐骑任务。
5. **状态记录**: 完成内容后能在配置或 evidence 中确认两个状态 key 的写入路径。

## Test Evidence

- Config/Data: `production/qa/evidence/pc-002-sister-gathering-tutorial-content-evidence.md`

## Dependencies

- Depends on: pc-001
- Unlocks: pc-003
