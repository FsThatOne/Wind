# ADR-0005: Dialogue Data Format

## Status
Accepted

## Date
2026-06-08

## Engine Compatibility

| Field | Value |
|-------|-------|
| **Engine** | Godot 4.7-stable |
| **Domain** | Core / Narrative |
| **Knowledge Risk** | LOW — 自定义数据格式，不依赖 post-cutoff 引擎 API |
| **References Consulted** | `docs/engine-reference/godot/current-best-practices.md` |
| **Post-Cutoff APIs Used** | None |
| **Verification Required** | None |
| **4.7 Re-verification (2026-06-20)** | Engine pin upgraded 4.6.3 → 4.7-stable. Re-verify all post-cutoff APIs above against Godot 4.7-stable; flag any regressions or behavior changes in next `/architecture-review`. |

## ADR Dependencies

| Field | Value |
|-------|-------|
| **Depends On** | ADR-0003 (数据格式整体策略 — 对话选择与 YAML 一致还是专用格式) |
| **Enables** | Core/Dialogue 模块实现, 叙事内容编写工作流 |
| **Blocks** | Sprint 4 (Dialogue 模块实现), 叙事写作启动 |
| **Ordering Note** | 必须在 12-15 万字叙事内容编写开始前确定 |

## Context

### Problem Statement

《风止》有 12-15 万字的对话文本，对话结构为图（支持回环、合流、多条件跳转），包含 7 种节点类型（Speech, Choice, InnerMonologue, Narration, Letter, InsightPrompt, CodePhrase）。每个节点可附加事件和条件。需要决定对话数据的存储格式和编辑工具链。

### Constraints

- 12-15 万字写作量 — 编辑效率是首要考量
- 图结构（非纯线性）— 需要表达分支、回环、合流
- 7 种节点类型 + 事件 + 条件 — 结构复杂
- 条件为 `(source, operator, threshold)` 三元组，支持 AND/OR
- 对话序列有版本号（存档兼容）
- 防死循环：单次对话最大 500 节点访问
- 团队需直接编辑（无专用可视化编辑器）

### Requirements

- 人类可读可编辑（12万字不可能全用可视化工具）
- 支持图结构（goto/jump 到任意节点）
- 支持条件分支和事件触发
- Git 友好（line-by-line diff）
- 运行时高效遍历
- 与 YAML 配置体系一致（ADR-0003）

## Decision

采用 **YAML 图节点格式**（自研 schema），不引入外部对话工具（Ink/Yarn Spinner）。

### 格式规范

```yaml
# assets/data/dialogues/ch01/meeting-master.yaml
id: meeting_master_001
version: 3
entry_node: node_01

nodes:
  - id: node_01
    type: speech
    speaker: master_li
    text: "你来得正好。有一件事，为师思量许久……"
    next: node_02

  - id: node_02
    type: choice
    prompt: "师父欲言又止。"
    options:
      - text: "师父请讲。"
        next: node_03
        events:
          - { type: mindset_shift, axis: kindness, delta: 1 }
      - text: "（沉默等待）"
        next: node_04
        conditions:
          - { source: mindset.firmness, op: gte, value: 30 }
        events:
          - { type: mindset_shift, axis: firmness, delta: 2 }

  - id: node_03
    type: speech
    speaker: master_li
    text: "江湖传闻……"
    conditions:
      - { source: npc.master_li.attitude, op: gte, value: 60 }
    next: node_05
    fallback: node_06  # 条件不满足时跳转

  - id: node_05
    type: inner_monologue
    text: "师父的眼中有我从未见过的忧虑。"
    next: node_07
    events:
      - { type: set_flag, key: "master_worried_seen" }

  - id: node_07
    type: narration
    text: "夕阳渐沉，远处传来晚钟。"
    next: END
```

### 节点类型映射

| 类型 | YAML type | 特殊字段 |
|------|-----------|---------|
| 对白 | `speech` | `speaker` |
| 选项 | `choice` | `prompt`, `options[]` |
| 内心独白 | `inner_monologue` | — |
| 旁白 | `narration` | — |
| 书信 | `letter` | `sender`, `recipient` |
| 洞察提示 | `insight_prompt` | `insight_level_required` |
| 暗号 | `code_phrase` | `correct_phrase`, `response` |

### 条件系统

```yaml
conditions:
  - { source: mindset.善恶, op: gte, value: 50 }     # 心境轴
  - { source: npc.师姐.attitude, op: gte, value: 40 } # NPC 态度
  - { source: item.has, id: broken_sword }             # 物品持有
  - { source: flag, key: ch01_completed }              # 叙事标记
  - { source: time.day, op: gte, value: 5 }           # 游戏天数

# AND (默认): 所有条件必须满足
# OR: 使用 any_of 包裹
conditions_any_of:
  - { source: flag, key: route_a }
  - { source: flag, key: route_b }
```

### 文件组织

```
assets/data/dialogues/
├── prologue/           # 序章
│   ├── opening.yaml
│   └── farewell.yaml
├── ch01/               # 第一章
│   ├── meeting-master.yaml
│   └── ...
├── companions/         # 同伴对话（跨章节）
│   ├── shijie-talks.yaml
│   └── ...
└── side/               # 支线对话
    └── ...
```

### 为何不用 Ink / Yarn Spinner

| 工具 | 优点 | 拒绝原因 |
|------|------|---------|
| Ink | 成熟叙事引擎，语法简洁 | C# 运行时需额外维护；自有条件系统与我们的 Mindset/NPC/Item 查询不兼容；学习专用语法成本 |
| Yarn Spinner | Unity 生态强，Godot 有社区移植 | 社区移植质量不确定；不支持图回环；条件表达受限 |

## Consequences

### Positive
- 与 ADR-0003 YAML 体系一致 — 无额外工具链
- 条件系统直接对接游戏状态（Mindset/NPC/Item）— 无翻译层
- 图结构自由（任意 `next`/`fallback` 跳转）
- 12万字按章节分文件 — Git 合并冲突最小化

### Negative
- 无可视化编辑器 — 复杂分支需脑补图结构
- 自研 schema 需要验证工具（防 YAML 格式错误、死循环检测）
- 写作者必须学 YAML 语法

### Risks

| 风险 | 缓解 |
|------|------|
| YAML 编写效率低于专用工具 | 后续可开发简单 VS Code 插件做节点预览 |
| 死循环/孤立节点 | 启动时图遍历检测 + 500 节点访问上限 |
| 12万字出现格式错误 | CI 校验脚本（检查 node id 唯一性、next 引用有效性） |

## GDD Requirements Addressed

| GDD System | Requirement | How This ADR Addresses It |
|------------|-------------|--------------------------|
| dialogue-system.md | 图结构 + 7 种节点类型 | YAML 节点定义 + type 字段 |
| dialogue-system.md | 条件三元组 (source, op, threshold) | `conditions` 列表直接映射 |
| dialogue-system.md | 对话版本号 | 顶层 `version` 字段 |
| dialogue-system.md | 事件附加 | `events` 列表 per node/option |

## Performance Implications
- **CPU**: 对话文件按需加载（进入场景时），单文件 < 5ms 解析
- **Memory**: 当前章节对话常驻 ~100KB
- **Load Time**: 无全局预加载需求

## Migration Plan
首次实现，无迁移。

## Validation Criteria
1. 加载对话文件后可正确遍历到 END
2. 条件求值正确触发分支
3. CI 检测：孤立节点、无效 next 引用、重复 id
4. 500 节点上限触发死循环保护

## Related Decisions
- [ADR-0003](adr-0003-data-configuration-format.md) — YAML 格式一致性
- [ADR-0001](adr-0001-event-bus-architecture.md) — 对话事件通过 EventBus 发布
