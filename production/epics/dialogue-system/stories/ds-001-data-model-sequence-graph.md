# Story: ds-001 — Data Model & Sequence Graph

> **Epic**: dialogue-system
> **Status**: Ready
> **Last Updated**: 2026-06-30
> **Layer**: Foundation
> **Type**: Logic
> **Priority**: P0
> **Estimate**: 1 day
> **GDD 来源**: dialogue-system.md §Core Rules 1-2, §Edge Cases E2/E8
> **TR-ID**: 待分配

## Context

对话数据模型是整个对话系统的基础。定义 7 种节点类型（Speech, Choice, InnerMonologue, Narration, Letter, InsightPrompt, CodePhrase）的数据结构、序列图的节点-边模型、以及 YAML 格式的序列化/反序列化。

## Acceptance Criteria

- [ ] AC-1: 7 种节点类型均有对应的 C# 数据类，包含 GDD 中定义的所有必要字段。
- [ ] AC-2: DialogueSequence 采用图结构（节点 + 有向边），支持线性流、条件分支和回环。
- [ ] AC-3: 每个节点有唯一 ID、类型标识、内容字段、出边列表和可选条件列表。
- [ ] AC-4: Choice 节点支持 2-6 个选项，每个选项含文字、条件列表、目标节点 ID。
- [ ] AC-5: Speech 节点包含 speaker_id、表情 tag、对话文本。
- [ ] AC-6: 序列设置最大节点访问次数上限（默认 500），提供循环检测。
- [ ] AC-7: DialogueSequence 可从 YAML 字符串反序列化为完整图结构，且能序列化回 YAML。

## Implementation Notes

- 纯 C# 数据模型，无 Godot 依赖
- 使用 YamlDotNet 进行序列化（项目已引用）
- 节点使用多态继承：`DialogueNode`（base）→ 各 `XxxNode` 子类
- 边模型：`DialogueEdge { TargetNodeId, Conditions? }`
- 序列模型：`DialogueSequence { Id, Version, Nodes, StartNodeId }`

## Files to Create/Modify

- `src/FengZhi.Foundation/Dialogue/DialogueNode.cs`
- `src/FengZhi.Foundation/Dialogue/DialogueSequence.cs`
- `src/FengZhi.Foundation/Dialogue/DialogueEdge.cs`
- `src/FengZhi.Foundation/Dialogue/DialogueEnums.cs`
- `src/FengZhi.Foundation/Dialogue/DialogueSerializer.cs`

## Test Evidence

- `tests/unit/dialogue/DialogueDataModelTest.cs`

**Test cases**:
- 创建 Speech 节点 → 所有字段正确赋值
- 创建 Choice 节点含 3 选项 → 选项数量和内容正确
- 构建线性序列（3 节点链）→ 可遍历
- 构建分支序列（Choice → 2 分支 → 合流）→ 图结构正确
- 构建含回环的图 → 循环检测返回 true
- YAML 序列化往返 → 反序列化后节点/边完全一致
- 超过 max_visit 上限 → 抛出异常或返回错误

## Dependencies

- Depends on: 无
- Unlocks: ds-002~ds-009
