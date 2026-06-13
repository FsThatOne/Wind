# Story: ds-007 — 书信节点、书信匣与重读规则

> **Epic**: dialogue-system
> **类型**: Integration
> **优先级**: P1 — 书信叙事与暗号承载
> **Estimate**: M（约 6h）
> **依赖**: ds-002, ds-004, ds-006
> **阻塞**: 无
> **ADR 指引**: ADR-0005（letter 节点），ADR-0001（书信相关事件）
> **GDD 来源**: design/gdd/dialogue-system.md §Detailed Design, §Interactions with Other Systems, §Edge Cases, §Acceptance Criteria
> **TR-ID**: TR-dialogue-system-007
> **Control Manifest Version**: 2026-06-10
> **状态**: Complete

## 目标

实现 Letter 节点、书信匣和重读规则。书信首次到达时打开信笺并存入书信匣；重读时完整显示内容，但不再次触发游戏状态变更。

## 范围

### 包含
- Letter 节点进入 LetterReading 状态
- 书信正文、寄信人、收信人、暗号、情感线索元数据
- 收到书信后加入书信匣
- 首次查看含暗号书信时自动加入暗号簿
- 书信重读不触发事件、不重复学习暗号
- 对话进行中到达的书信排队，当前对话结束后按顺序通知

### 不包含
- 信笺 UI 的视觉样式 → ds-008
- 自然日系统调度书信到达的实现
- 存档系统持久化书信匣

## 技术说明

- 书信作为对话特殊节点处理，但状态应可序列化
- 首次查看和重读必须有明确状态区分
- 书信事件入队仍遵守 ds-004 的批量分发规则
- 对应 GDD 的明确要求：Letter 节点触发时在独立信笺 UI 中打开，同时存入书信匣；玩家可随时重读已收书信
- 含暗号书信只在首次查看时自动加入暗号簿；重读必须完整显示已发现内容，但不得再次触发暗号学习、心境位移、关系变化或其他游戏状态事件
- 对话进行中到达的书信必须排队等待，不打断当前对话；对话结束后按到达顺序通知玩家
- 本 story 只定义信笺 UI 通道的运行时打开/关闭与数据传递，不实现信笺视觉样式、版式和输入细节（见 ds-008）
- 本 story 接收自然日系统发来的书信到达事件，但不实现自然日调度规则
- 本 story 维护可序列化的书信匣、已读标记、已触发事件历史与暗号学习状态；实际存档写入/读取接入由 save-system 负责
- YAML 约定：letter 节点使用节点 id 作为书信 id；`code_phrase_ids` 表示首次查看时学习的暗号；`emotional_clues` 记录情感线索标签
- Control Manifest 约束：跨层通知必须通过 ADR-0001 的 `EventBus.Publish<T>()`；书信状态机推进必须复用 ADR-0008 风格的强类型 FSM

## 验收标准

- [x] Letter 节点被触发时，运行时进入 LetterReading 状态
- [x] 书信在独立信笺 UI 通道中打开，并存入书信匣
- [x] 书信含暗号时，首次查看自动加入暗号簿
- [x] 玩家在书信匣中重读已收书信时，完整内容可见
- [x] 重读不会触发任何游戏状态变更，包括暗号学习、心境位移、关系变化和 quest_flag
- [x] 对话进行中到达的书信不会打断当前对话，而是在结束后排队通知

## QA 测试用例

- **AC-1**：首次收信
  - Given：对话运行到 Letter 节点
  - When：节点被激活
  - Then：状态进入 LetterReading，书信加入书信匣
  - 边界：同一书信重复到达不重复插入

- **AC-2**：暗号学习
  - Given：书信包含暗号 `white_reed`
  - When：玩家首次查看书信
  - Then：暗号加入暗号簿
  - 边界：重读同一书信不重复添加暗号

- **AC-3**：重读无副作用
  - Given：书信已读且已触发过首次事件
  - When：玩家从书信匣重读
  - Then：完整内容可见，但事件队列无新增事件
  - 边界：含心境位移事件的书信也不重复触发

## 测试证据路径

`tests/Core/Dialogue/LetterInboxTests.cs`

## 完成记录

- 扩展 letter YAML 节点元数据：`code_phrase_ids` 与 `emotional_clues`。
- 新增 `DialogueLetterInbox`、`DialogueLetterSnapshot`、`PendingDialogueLetter` 和可序列化状态类型，维护书信匣、已读状态与对话中到达的通知队列。
- `DialogueRuntime` 进入 Letter 节点时保持 `LetterReading` 状态，暴露 `CurrentLetter`，并将书信存入书信匣。
- 含暗号书信首次查看时通过 `DialogueCodePhraseBook` 学习暗号；重读只返回完整快照，不重复学习暗号、不入队事件。
- 对话进行中到达书信可通过 `QueueArrival` 排队，结束后按到达顺序 `DequeuePendingNotification`。
- 验证：`/Users/bytedance/.dotnet/dotnet test tests/Foundation/Foundation.Tests.csproj --filter LetterInboxTests`，6/6 通过。
- 验证：`/Users/bytedance/.dotnet/dotnet test tests/Foundation/Foundation.Tests.csproj`，497/497 通过。

## 依赖关系

- Depends on: ds-002, ds-004, ds-006
- Unlocks: ds-008
