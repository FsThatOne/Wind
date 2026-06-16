# Story: ds-001 — 对话 YAML 图节点模型与加载校验

> **Epic**: dialogue-system
> **类型**: Config/Data
> **优先级**: P0 — 对话系统数据入口
> **Estimate**: S（约 4h）
> **依赖**: 无
> **阻塞**: 无
> **ADR 指引**: ADR-0005（对话 YAML 图节点格式），ADR-0003（YAML 配置）
> **GDD 来源**: design/gdd/dialogue-system.md §Detailed Design, §Edge Cases, §Acceptance Criteria
> **TR-ID**: TR-dialogue-system-001
> **Control Manifest Version**: 2026-06-10
> **状态**: Complete
> **Last Updated**: 2026-06-11

## 目标

定义并加载对话 YAML 图节点数据，让对话序列具备 `id`、`version`、`entry_node`、`nodes`、7 种节点类型、跳转引用和构建期校验能力。

## 范围

### 包含
- `DialogueSequence`、`DialogueNode`、`DialogueOption`、`DialogueEventSpec`、`DialogueConditionSpec` 等数据模型
- 7 种节点类型：speech、choice、inner_monologue、narration、letter、insight_prompt、code_phrase
- YAML 文件路径约定：`assets/data/dialogues/{chapter}/`
- 加载时校验：节点 id 唯一、entry_node 存在、next/fallback 引用有效、孤立节点检测
- 单次对话最多 500 节点访问的配置常量
- Choice 节点必须存在默认无条件选项的校验警告或错误

### 不包含
- 运行时状态机和节点推进 → ds-002
- 条件求值 → ds-003
- 事件派发 → ds-004
- UI 呈现 → ds-008 / ds-009

## 技术说明

- 必须使用 YAML 1.2 + YamlDotNet
- 禁止引入 Ink / Yarn Spinner
- 对话文件头必须包含 `id` / `version` / `entry_node` / `nodes`
- 对应 GDD 的明确要求：`对话头字段必须含 id / version / entry_node / nodes`，并且 `节点 id 唯一性、next 引用有效性、孤立节点检测` 必须在构建期校验
- 必须遵守 ADR-0003 的 YAML 1.2 + YamlDotNet + 启动时快速失败规则
- 对话解析性能目标：单文件 < 5ms；当前章节常驻约 100KB

## 验收标准

- [x] 可加载包含 7 种节点类型的合法 YAML 对话文件，并保留节点类型、文本、跳转、条件和事件字段
- [x] 缺少 `id`、`version`、`entry_node` 或 `nodes` 时快速失败，并报告文件名和字段名
- [x] 重复节点 id、无效 next/fallback 引用、孤立节点会被校验工具捕获
- [x] Choice 节点所有选项均有条件且没有默认选项时，校验工具报告“选择节点无默认选项”
- [x] 图结构存在循环时不会在校验阶段误判为非法，但运行时可使用 500 节点上限防御

## QA 测试用例

- **AC-1**：加载合法 YAML
  - Given：一个包含 speech、choice、inner_monologue、narration、letter、insight_prompt、code_phrase 的 YAML 文件
  - When：调用对话加载器
  - Then：返回完整 `DialogueSequence`，节点数量、类型和跳转字段与文件一致
  - 边界：空 nodes、仅一个 END 前节点、带 fallback 的节点

- **AC-2**：结构字段缺失
  - Given：缺少 `entry_node` 的 YAML 文件
  - When：调用对话加载器
  - Then：加载失败，并返回包含文件名和 `entry_node` 的错误
  - 边界：缺少 `id`、`version`、`nodes`

- **AC-3**：图引用校验
  - Given：存在重复 id、无效 next、孤立节点的 YAML 文件
  - When：运行图校验
  - Then：每类错误均被报告，不静默通过
  - 边界：next 指向 `END` 视为合法

## 测试证据路径

`tests/Core/Dialogue/DialogueSchemaTests.cs`

## 依赖关系

- Depends on: 无
- Unlocks: ds-002, ds-003, ds-004, ds-005, ds-006, ds-007

## 完成记录

- Implementation: `src/FengZhi.Foundation/Dialogue/DialogueSchema.cs`
- Tests: `tests/Core/Dialogue/DialogueSchemaTests.cs`
- Evidence: `/Users/bytedance/.dotnet/dotnet test tests/Foundation/Foundation.Tests.csproj --filter DialogueSchemaTests` → 9/9 通过
- Regression: `/Users/bytedance/.dotnet/dotnet test tests/Foundation/Foundation.Tests.csproj` → 460/460 通过
