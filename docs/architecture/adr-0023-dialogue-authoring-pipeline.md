# ADR-0023: Dialogue 三层数据流 — .dlg 文本 → YAML 中间表示 → Foundation Runtime + Scene-Agnostic Provider

## Status
Accepted（端到端落地 2026-06-24，chapter_00 4 段对话 + 1 工具 + 1 Provider 已 ship）

## Date
2026-06-24

## Supersedes
None（首个 dialogue 系统正式 ADR；此前散落在 `production/epics/dialogue-system/` 的设计意图本 ADR 收口）

## Summary

《风止》对话系统采用**三层数据流**：

1. **Author layer** — `.dlg` 类 Markdown 纯文本（作者层语法，可读、易写、低门槛）
2. **Storage layer** — `.yaml`（`DialogueSchema`-compliant 中间表示，入 git 版本控制 + diff-friendly）
3. **Runtime layer** — Foundation `DialogueRuntime` 内存对象（Engine 加载 YAML 时反序列化）

配套两个支撑组件：

- **`tools/dialogue_compiler.py`** — `.dlg` → `.yaml` 单向编译器（397 行，无 runtime 依赖）
- **`SceneConditionValueProvider`** — 实现 `IDialogueConditionValueProvider`，可被任意场景/章节复用查询 `mindset.*` / `flag.*` 条件（取代早期假设 cave-only 的 `CaveConditionValueProvider`）

## Engine Compatibility

| Field | Value |
|-------|-------|
| **Engine** | Godot 4.7-stable |
| **Domain** | Asset Pipeline, Resource Loading, Runtime Data |
| **Knowledge Risk** | **LOW** — YAML 解析走 `YamlDotNet`（已沉淀于 Foundation 多个系统），.dlg 编译器是纯 Python 文本处理，不依赖任何 Godot/Engine API |
| **References Consulted** | `src/FengZhi.Foundation/Dialogue/DialogueSchema.cs`, `tools/dialogue_compiler.py`, `tools/README-dialogue-compiler.md`, `production/epics/dialogue-system/` |
| **Post-Cutoff APIs Used** | 无（YamlDotNet long-stable；Python `argparse` / `dataclasses` 标准库） |
| **Verification Required** | 1) `.dlg` 编译产物与 chapter_00 现有 4 个手写 YAML 行为等价；2) `SceneConditionValueProvider` 在非-cave 场景（如战斗场景）能正确路由条件查询；3) `FengZhi.csproj` `CopyLocalLockFileAssemblies` 确保 `YamlDotNet.dll` 进 Godot 运行时 |

## ADR Dependencies

| Field | Value |
|-------|-------|
| **Depends On** | 无强依赖（Foundation `DialogueRuntime` / `DialogueSchema` / `IDialogueConditionValueProvider` / `IDialogueEvent` 是更早期已存在的契约层，本 ADR 不修改它们） |
| **Enables** | 大批量对话内容生产；chapter 01+ 江南章节 dialogue；战斗场景 dialogue；任意 quest_flag / mindset 条件分支对话 |
| **Blocks** | 任何「在 .cs 代码里硬编码 dialogue 字符串 / 选项分支」的方案（如本 ADR 之前 `BackMountainCliffCaveGame.cs` 的 `ShowMessage` 直调） |
| **Ordering Note** | 必须在「chapter 01+ 任何要写对话内容的 story」之前 settle，否则作者会回退去手撸 YAML 或者直接硬编码 .cs 字符串 |

## Context

### Problem Statement

`production/epics/dialogue-system/` 早期设计明确了：

- 数据要外置成可热加载的 YAML（不进 .cs）
- Foundation 必须有 `DialogueRuntime` 控制状态机（Idle / Entering / Displaying / WaitingForInput / ProcessingChoice / Exiting）+ 标准化 `DialogueSchema`
- `IDialogueConditionValueProvider` / `IDialogueEvent` 让对话与外部状态（mindset / quest_flag / inventory）解耦

但是落地到 chapter_00 4 段对话（`memory_marker_01` / `rest_spot_01` / `storage_shelf_01` / `wine_pickup_01`）时暴露两个新问题：

#### 问题 1：手撸 YAML 成本爆炸

`DialogueSchema` 要求每个 node 是显式对象，嵌套深、字段多。一段 5 节点 + 2 个 choice 的对话手写 YAML ≥ 50 行，作者需要记住：

- 节点 `type` 枚举（narration / inner_monologue / choice）
- `next` / `fallback` / `conditions[]` 字段顺序与缩进
- `events: [{ name, params: {...} }]` 嵌套结构
- 选项的 `choices: [{ text, next, conditions[], events[] }]` 形态

实测 chapter_00 4 段对话用纯手撸 YAML 写完 ≈ 3-4h，且 review 阶段 5+ 次 YAML 结构错误（缩进 / 字段拼写）。**作者层与运行时层格式耦合**导致内容生产无法 scale。

#### 问题 2：Provider 假设场景为 cave

首版集成在 `BackMountainCliffCaveGame.cs` 落地，第一版 Provider 命名 `CaveConditionValueProvider`，内部条件 source 字符串硬编码 cave 上下文（`cave.timer` / `cave.lit_torches`）。

但 dialogue runtime 接下来要在以下场景复用：

- 战斗胜利/失败 outcome 场景的"师姐独白"
- 江南章节的 NPC 对话
- 探索场景的物品/触发器交互

如果每个场景都派生一个 `XxxConditionValueProvider`，会出现 N 个相似类 + 重复 mindset/flag 查询逻辑。

### Constraints

- **不修改 Foundation 契约层** — `DialogueRuntime` / `DialogueSchema` / `IDialogueConditionValueProvider` / `IDialogueEvent` 在更早 sprint 已 ship，被多个系统消费，本 ADR 不动它们
- **保持 YAML 作为 git 主格式** — diff-friendly + 与 spec / GDD 的引用关系不变；不能为了"作者层简化"把 YAML 移出版本控制
- **作者工具不能引入 runtime 依赖** — `.dlg` 编译器必须纯 Python / 纯文本处理，让作者用任意 IDE 即可工作，不必装 .NET / Godot
- **Provider 必须 scene-agnostic** — 一个 Provider 实例支持任意场景的 mindset / flag 查询
- **CavePlayer 输入屏蔽** — 对话期间需要屏蔽 explore 输入（避免角色一边走一边对话）

## Decision

### 三层数据流

```
   Author                Storage              Runtime
   ─────                 ───────              ───────
   .dlg                  .yaml                DialogueRuntime
   (类 Markdown)    ─→   (DialogueSchema)  ─→ (Foundation 内存对象)
                  编译                  反序列化
                  build-time           load-time
   tools/                feng-zhi/assets/     src/FengZhi.Foundation/
   dialogue_compiler.py  data/dialogues/      Dialogue/DialogueRuntime.cs
```

### .dlg 语法（作者层）

参见 `tools/README-dialogue-compiler.md` 完整规范。核心要点：

| 节点类型 | 语法 | YAML 等价 |
|---|---|---|
| dialogue_id | `# memory_marker_01` 文件头 | `id: memory_marker_01` |
| narration | `旁白：天色已暗。` | `type: narration, text: "..."` |
| inner_monologue | `内心：师姐说过……` | `type: inner_monologue, text: "..."` |
| choice | `选择：你怎么办？\n  - 取酒坛 → take_path\n  - 看封泥 → examine_path` | `type: choice, prompt, options[]` |
| condition | `[cond: flag.variant == day]` 行尾标记 | `conditions: [{ source, key, op, value }]` |
| event | `event(mindset_shift, axis=resolve, delta=+1)` | `events: [{ name, params: {...} }]` |
| fallback | `[fallback: inner_night]` 行尾标记 | `fallback: inner_night` |

`.dlg` → `.yaml` 是**幂等**的：相同 .dlg 输入产生相同 yaml 输出（字段顺序固定、字符串转义稳定），可以放心进 git diff / review。

### Provider scene-agnostic 化

**改名**：`CaveConditionValueProvider` → `SceneConditionValueProvider`

**改语义**：condition source 字符串从场景特定（如 `cave.timer`）改为通用域名空间：

| source | 含义 | 用途 |
|---|---|---|
| `mindset.resolve` / `mindset.worldly` | Mindset 双轴数值 | 任意场景的 mindset 条件分支 |
| `mindset.zone` | Mindset 当前 zone 名 | zone-aware 对白 |
| `flag.<key>` | 通用 quest_flag（运行时 setter 注入） | 任意场景的进度/状态条件 |

Cave 场景独有的 `cave.timer` / `cave.lit_torches` 等不再走 Provider，而是改为：
- 通过 `flag.cave_torches_lit` / `flag.cave_timer_phase` 这类**通用 flag** 注入
- BackMountainCliffCaveGame 在交互前调 `Provider.SetFlag("cave_torches_lit", "3")` 显式写入

这样 Provider 实例可一次性 new，跨 cave / jiangnan / 战斗等所有场景共享。

### 作者工作流

新写一段对话：

```bash
# 1. 写 .dlg 文本（10-20 行）
$ vim my_dialogue.dlg

# 2. 编译产出 YAML
$ python3 tools/dialogue_compiler.py my_dialogue.dlg \
    -o feng-zhi/assets/data/dialogues/chapter_01/my_dialogue.yaml

# 3. YAML 入 git, 由 DialogueManager 加载
$ git add feng-zhi/assets/data/dialogues/chapter_01/my_dialogue.yaml
```

YAML 是**源头**还是**派生物**？— 当前阶段（chapter_00 4 段是手写 YAML）**YAML 是源头**，.dlg 是后续作者层简化工具。Sprint 8+ 新对话才考虑用 .dlg 起手；老 YAML 不强制反向迁移。

### Engine 集成层（Godot 端，本 ADR 不修改 Foundation）

```
Godot Scene (e.g. BackMountainCliffCave.tscn)
  └─ Node BackMountainCliffCaveGame.cs
     ├─ holds: SceneConditionValueProvider (scene-agnostic, scoped to 玩家 session)
     ├─ holds: DialogueManager (新建于 feng-zhi/scripts/dialogue/)
     └─ trigger 时调:
        DialogueManager.StartDialogue(dialogueId, ctx)
          ├─ load YAML → DialogueSchema
          ├─ instantiate DialogueRuntime + Provider
          ├─ show DialoguePanel.tscn (CanvasLayer 40, ADR-0002 合规)
          ├─ block CavePlayer 输入直到 Exited
          └─ raise EventBus signals on mindset_shift / quest_flag events
```

`DialoguePanel.tscn` 挂 CanvasLayer 40 是 ADR-0002 双焦框架的 dialog 层规范，**不与 HUD（layer 20）/ HUD modal（layer 30）打架**。

## Consequences

### Positive

- **作者门槛极低**：5 节点对话从手写 50 行 YAML 降到写 10 行 .dlg；选项嵌套 / 条件 / 事件用极简语法
- **Provider 复用率最大化**：一个 `SceneConditionValueProvider` 实例服务所有场景，新场景接 dialogue 几乎零额外代码
- **YAML 仍是 git 主格式**：spec / GDD 仍可引用 YAML node id；review 仍 diff YAML；.dlg 是可选的"作者糖"
- **运行时零变化**：Foundation `DialogueRuntime` / `DialogueSchema` 契约不变，所有改动集中在「作者工具 + Godot 集成层」
- **测试覆盖**：chapter_00 4 个手写 YAML 是 compiler 输出格式的 ground truth，未来新 .dlg 可以「编译 + diff」验证

### Negative

- **维护两套语法**：作者需要学 .dlg；compiler 自身也需要测试与文档持续维护
- **.dlg 是非标准格式**：没有 IDE 语法高亮 / lint；语法错误反馈在 compile-time
- **mixed sources 的风险**：同一对话 YAML 可能由 .dlg 编译产出 OR 手写，git 历史会混合两条线（mitigation：commit message 标注源头）
- **Provider 通用化让 condition source 字符串爆炸**：所有场景特定状态都要先翻译到 `flag.*` 命名空间

### Risks

| Risk | Probability | Impact | Mitigation |
|---|---|---|---|
| compiler 输出 YAML 与 Foundation `DialogueSchema` 反序列化字段不匹配 | Low | High | chapter_00 4 个手写 YAML 是 ground truth；新增对话先用 compiler 跑一遍 diff 验证再 commit |
| `.dlg` 语法演化导致老 .dlg 无法编译 | Medium | Low | 当前 .dlg 不入 git（YAML 才是源头），即使语法演化 .yaml 仍稳定 |
| Provider `flag.*` 命名空间冲突（cave 与 jiangnan 用同名 flag 但语义不同） | Medium | Medium | 命名约定：场景前缀 `cave_*` / `jiangnan_*`；约定写入 art-bible / dialogue-system epic README |
| `YamlDotNet.dll` 在 Godot 运行时缺失（.NET 工程发布默认不复制 lock 包到输出） | Medium | High | `FengZhi.csproj` 加 `CopyLocalLockFileAssemblies=true`（commit 255a510 已落） |
| 对话期间输入未屏蔽，角色在 cave 里一边走一边对话 | Low | Medium | `CavePlayer.cs` 已加输入屏蔽逻辑（commit 1129350）；后续场景接入时强制 review |

## Migration Plan

| 阶段 | 内容 | 状态 |
|---|---|---|
| 1 | Foundation `DialogueRuntime` + `DialogueSchema` + `IDialogueConditionValueProvider` + `IDialogueEvent` 契约层 | ✅ 此前已 ship（pre-Sprint 7） |
| 2 | Godot 集成层：`DialogueManager.cs` + `DialoguePanel.cs` + `DialoguePanel.tscn` (CanvasLayer 40) | ✅ commit 255a510 / 2a4618b |
| 3 | chapter_00 4 段对话 YAML（手写）+ `SceneConditionValueProvider`（从 `CaveConditionValueProvider` 重命名 + 泛化）+ CavePlayer 输入屏蔽 | ✅ commit 1129350 |
| 4 | smoke test 场景（`DialogueSmokeTestScene`）自动触发验证端到端链路 | ✅ commit 6d53546 |
| 5 | `.dlg` 作者工具：`tools/dialogue_compiler.py` + `tools/README-dialogue-compiler.md` | ✅ commit 3b1dfd2 / 48a1ce7 |
| 6 | Sprint 8+：chapter 01 第一段对话用 .dlg 起手编译 → 验证 author workflow 跑通 | ⏳ 留 Sprint 8 之后任一 chapter-01 dialogue story 自然消费 |
| 7 | dialogue-system epic README 落「.dlg 语法 / Provider 命名空间约定 / 新对话生产流程」 | ⏳ 候选 S8-Dialogue-System-Epic-Doc-Refresh，纳入 backlog |

## Validation Criteria

1. ✅ chapter_00 4 段 YAML 在 Godot 4.7 运行时反序列化无 schema 错误（commit 1129350 端到端跑通）
2. ✅ `SceneConditionValueProvider` 支持 `mindset.resolve` / `mindset.worldly` / `mindset.zone` / `flag.*` 全部条件（实测 4 段对话覆盖 3 个 source 类型）
3. ✅ `DialoguePanel` 挂 CanvasLayer 40 不与 HUD（20）/ modal（30）打架（commit 2a4618b ADR-0002 合规）
4. ✅ 对话期间 `CavePlayer` 输入屏蔽生效（commit 1129350）
5. ⏳ partial — `.dlg` 编译产出与手写 YAML 行为等价：4 段手写 YAML 是 ground truth，但尚未做「写一段 .dlg 编译后 diff 手写 YAML」的对比验证。留 Sprint 8 首段 chapter-01 dialogue 自然覆盖
6. ⏳ partial — Provider 跨场景实战：当前仅在 BackMountainCliffCave 实测；战斗场景 / jiangnan 场景接入留 Sprint 8

## GDD Requirements Addressed

| GDD System | Requirement | How This ADR Addresses It |
|---|---|---|
| dialogue-system epic | "对话外置 YAML、可热加载、可分支、可触发事件" | 三层流：YAML 作 storage；分支 = choice + condition；事件 = events[] |
| mindset-dual-axis epic | "战斗 / 探索 / 对话三处都能引发 mindset 位移" | Dialogue events: `mindset_shift(axis, delta)` → EventBus → MindsetService |
| living-jianghu-layer epic | "NPC 对话受 quest_flag / time-of-day 影响" | Provider `flag.*` 命名空间 + `flag.variant` / `flag.time_phase` 等约定 |

## Related Decisions

- [ADR-0002](adr-0002-ui-framework-dual-focus.md) — `DialoguePanel` CanvasLayer 40 来源
- [ADR-0010](adr-0010-tilemaplayer-usage.md) — 与 dialogue 无直接耦合，但 Provider `flag.scene_layer_state` 形态可能在 Sprint 8+ 演化时挂钩
- [ADR-0017](adr-0017-epiphany-breakthrough.md) — 顿悟事件可能由 dialogue events 触发
- [ADR-0018](adr-0018-exploration-insight.md) — 探索洞察对话由本 ADR 的 dialogue runtime 承载

## Open Questions（留 future ADR / story）

- **Q1**：dialogue YAML 是否需要从 `feng-zhi/assets/data/dialogues/` 迁移到 Foundation 资源目录，让单元测试可以直接加载？— 当前留 feng-zhi 因为属于"游戏内容"
- **Q2**：`.dlg` 是否最终也入 git？— 当前不入；如果作者反馈"YAML 不友好需要回看 .dlg"再调整
- **Q3**：Provider `flag.*` 命名空间是否需要 schema 校验？— 当前未做；如果发现拼写错误频繁触发 fallback 再考虑加 lint
- **Q4**：dialogue 是否支持本地化（locale switching）？— 当前 hard-code 中文；i18n 留更晚期决策
