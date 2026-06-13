# Story: ds-004 — 批量事件队列与 EventBus 分发

> **Epic**: dialogue-system
> **类型**: Integration
> **优先级**: P0 — 对话影响世界的出口
> **Estimate**: M（约 6h）
> **依赖**: ds-001, ds-002
> **阻塞**: 无
> **ADR 指引**: ADR-0001（EventBus），ADR-0005（节点/选项 events）
> **GDD 来源**: design/gdd/dialogue-system.md §Detailed Design, §Interactions with Other Systems, §Edge Cases, §Acceptance Criteria
> **TR-ID**: TR-dialogue-system-004
> **Control Manifest Version**: 2026-06-10
> **状态**: Complete
> **Last Updated**: 2026-06-11

## 目标

实现对话事件队列，让节点和选项附加事件在对话 UI 关闭、世界恢复后按编辑顺序通过 EventBus 分发。

## 范围

### 包含
- 节点事件与选项事件入队
- 支持事件类型：mindset_shift、relationship_change、quest_flag、npc_state_change、insight_discovered、code_phrase_learned、item_grant、combat_trigger、chapter_advance
- 对话进行中不立即分发下游事件
- 对话结束且世界恢复后按编辑顺序分发
- 每个事件恰好触发一次
- combat_trigger 在当前节点事件完成后进入 Exiting，再由战斗系统接手

### 不包含
- 下游系统对事件的具体处理逻辑
- 洞察追查的触发条件 → ds-005
- 书信首次查看学暗号的业务规则 → ds-007

## 技术说明

- 跨层通知必须走 `EventBus.Publish<T>()`
- 事件类型必须为不可变 record，继承 `GameEvent`
- 事件命名使用 `{Domain}{Action}Event`
- 不得用 string-based 事件名连接下游系统
- 对应 GDD 的明确要求：`对话系统只负责在正确时机发送正确数据`，并且 `combat_trigger` 必须在当前节点所有事件完成后再触发
- 批量事件必须保持编辑顺序，且每个事件恰好发布一次；同一批次内不得提前派发给下游系统
- 本 story 仅负责事件排队与 EventBus 分发，不实现下游系统的响应逻辑

## 验收标准

- [x] 对话节点触发下游事件时，UI 未关闭前不分发事件
- [x] 对话序列结束且世界恢复后，所有批量事件按编辑顺序分发
- [x] 每个事件恰好触发一次
- [x] 选项事件和节点事件可进入同一批次，并保持稳定顺序
- [x] combat_trigger 事件在当前节点所有事件完成后触发，且对话进入 Exiting

## QA 测试用例

- **AC-1**：延迟分发
  - Given：节点包含 mindset_shift 和 quest_flag
  - When：节点确认但对话 UI 尚未关闭
  - Then：EventBus 未收到事件
  - 边界：对话异常退出时队列应清理或明确丢弃

- **AC-2**：顺序与去重
  - Given：一个序列按顺序产生 3 个事件
  - When：对话结束
  - Then：EventBus 收到 3 个事件，顺序与 YAML 一致，且每个事件只收到一次
  - 边界：重复 quest_flag 允许重复发布，由下游幂等处理

- **AC-3**：combat_trigger
  - Given：当前节点同时包含 quest_flag 和 combat_trigger
  - When：节点事件批次分发
  - Then：quest_flag 先发布，combat_trigger 后发布，对话状态进入 Exiting
  - 边界：combat_trigger 后续节点标记为战斗后继续或丢弃时，运行时保留该标记

## 测试证据路径

`tests/Core/Dialogue/DialogueEventDispatchTests.cs`

## 依赖关系

- Depends on: ds-001, ds-002
- Unlocks: ds-005, ds-007

## 完成记录

- Implementation: `src/FengZhi.Foundation/Dialogue/DialogueEvents.cs`, `src/FengZhi.Foundation/Dialogue/DialogueRuntime.cs`
- Tests: `tests/Core/Dialogue/DialogueEventDispatchTests.cs`
- Evidence: `/Users/bytedance/.dotnet/dotnet test tests/Foundation/Foundation.Tests.csproj --filter DialogueEventDispatchTests` → 4/4 通过
- Regression: `/Users/bytedance/.dotnet/dotnet test tests/Foundation/Foundation.Tests.csproj` → 478/478 通过
