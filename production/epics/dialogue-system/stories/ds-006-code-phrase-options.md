# Story: ds-006 — 暗号簿与暗号选项

> **Epic**: dialogue-system
> **类型**: Logic
> **优先级**: P1 — 武侠暗语分支
> **Estimate**: S（约 4h）
> **依赖**: ds-003
> **阻塞**: 无
> **ADR 指引**: ADR-0005（code_phrase 节点与条件）
> **GDD 来源**: design/gdd/dialogue-system.md §Detailed Design, §Edge Cases, §Acceptance Criteria
> **TR-ID**: TR-dialogue-system-006
> **Control Manifest Version**: 2026-06-10
> **状态**: Complete

## 目标

实现暗号簿和暗号选项匹配：玩家已学会暗号且当前上下文匹配时，暗号自动作为额外选项出现在选项列表中。

## 范围

### 包含
- 暗号簿数据结构：已学暗号、已使用标记、来源上下文
- 当前 NPC / 场景 / 章节 / 条件上下文匹配
- 暗号选项自动追加到可见选项列表
- 多个匹配暗号全部显示，互不排斥
- 使用暗号后标记为已使用
- 暗号选项可跳转隐藏分支

### 不包含
- 暗号选项视觉样式 → ds-009
- 书信首次查看自动加入暗号簿 → ds-007
- 暗号来源的探索发现逻辑

## 技术说明

- 暗号本质是特殊条件选项，不应绕过 ds-003 的条件过滤规则
- 同一暗号在不同场景可有不同效果
- 暗号簿需要为存档系统提供可序列化状态，但实际存档接入不在本 story
- 对应 GDD 的明确要求：玩家已学会相关暗号且当前上下文匹配时，暗号自动出现为额外选项；多个匹配暗号全部显示，互不排斥
- 使用暗号后只标记当前上下文的使用状态，不影响同一暗号在其他上下文的独立效果
- YAML 约定：Choice 节点使用 `code_phrase_nexts` 声明暗号可能进入的隐藏分支，用于静态引用校验与孤立节点检测；实际可见性由暗号簿和上下文匹配决定
- 运行时可见选项使用 `DialogueOptionKind.CodePhrase` 标记暗号选项，供 ds-009 UI 样式消费
- 本 story 不实现暗号选项视觉样式，也不实现书信首次查看自动学习暗号的规则

## 验收标准

- [x] 玩家已学会某暗号且当前上下文匹配时，暗号选项自动出现
- [x] 玩家未学会暗号时，该暗号选项完全不可见
- [x] 多个暗号匹配当前上下文时，所有匹配暗号均显示
- [x] 使用暗号后，该暗号在当前上下文标记为已使用
- [x] 暗号选项可进入隐藏对话分支

## QA 测试用例

- **AC-1**：暗号显示
  - Given：玩家暗号簿含 `white_reed`，当前 NPC 和场景匹配
  - When：生成 Choice 可见选项
  - Then：暗号选项出现在列表中
  - 边界：暗号存在但场景不匹配时不显示

- **AC-2**：多暗号并存
  - Given：玩家拥有 3 个暗号，其中 2 个匹配当前上下文
  - When：生成 Choice 可见选项
  - Then：2 个暗号选项均显示，并保持稳定顺序
  - 边界：暗号与普通选项同名时仍保留类型标记

- **AC-3**：使用标记
  - Given：暗号选项可见
  - When：玩家选择该暗号
  - Then：运行时进入隐藏分支，并将当前上下文的暗号使用状态标记为已使用
  - 边界：同一暗号在另一上下文仍可独立使用

## 测试证据路径

`tests/Core/Dialogue/CodePhraseTests.cs`

## 完成记录

- 新增 `DialogueCodePhraseBook` 和 `DialogueCodePhraseBookState`，保存已学暗号与按上下文独立使用状态，并支持导出/恢复。
- 新增 `IDialogueCodePhraseProvider`、`DialogueCodePhraseMatch` 和 `DialogueCodePhraseProvider`，由运行时按当前 Choice 节点追加暗号选项。
- 扩展 `VisibleDialogueOption`，增加 `DialogueOptionKind`、暗号 ID 和上下文键，后续 UI 可据此做暗号样式。
- 扩展 YAML schema：Choice 节点可通过 `code_phrase_nexts` 声明暗号隐藏分支，保持引用校验和孤立节点检测有效。
- 选择暗号选项后进入隐藏分支，并只标记当前暗号上下文为已使用；同一暗号其他上下文仍可独立使用。
- 验证：`/Users/bytedance/.dotnet/dotnet test tests/Foundation/Foundation.Tests.csproj --filter CodePhraseTests`，6/6 通过。
- 验证：`/Users/bytedance/.dotnet/dotnet test tests/Foundation/Foundation.Tests.csproj`，491/491 通过。

## 依赖关系

- Depends on: ds-003
- Unlocks: ds-007, ds-009
