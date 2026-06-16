# Story: ds-005 — 洞察门槛、视觉提示与追查分支

> **Epic**: dialogue-system
> **类型**: Integration
> **优先级**: P1 — 对话内洞察玩法
> **Estimate**: M（约 6h）
> **依赖**: ds-002, ds-003, ds-004
> **阻塞**: 无
> **ADR 指引**: ADR-0005（insight_prompt 节点与条件），ADR-0001（insight_discovered 事件）
> **GDD 来源**: design/gdd/dialogue-system.md §Detailed Design, §Formulas, §Edge Cases, §Acceptance Criteria
> **TR-ID**: TR-dialogue-system-005
> **Control Manifest Version**: 2026-06-10
> **状态**: Complete

## 目标

实现对话内洞察判断与追查分支。玩家洞察属性达到节点门槛时，运行时暴露洞察提示状态；玩家选择追查后进入隐藏信息分支并触发 `insight_discovered`。

## 范围

### 包含
- 洞察公式：`insight_triggered = player.insight >= node.insight_threshold`
- 洞察仅在 Speech / Narration 节点 WaitingForInput 阶段触发
- 洞察提示无倒计时，持续到玩家追查或推进
- insight 不足时不显示提示，不出现追查入口
- 追查成功进入 InsightPrompt 节点或隐藏分支
- `insight_discovered` 事件恰好入队一次

### 不包含
- 洞察提示的实际水墨效果和图标 → ds-009
- 角色属性系统的 insight 成长
- 探索/洞察系统对事件的消费

## 技术说明

- 对话系统只做布尔门槛比较，不计算洞察属性来源
- 洞察不应在 Choice 节点触发
- 洞察追查和普通推进是互斥决策，同一节点不能同时走两条路
- 对应 GDD 的明确要求：`洞察仅在 Speech / Narration 节点的 WaitingForInput 阶段触发`，`洞察暗示无倒计时`，`insight >= threshold` 时显示提示
- 追查成功后必须进入 `InsightPrompt` 分支，并且 `insight_discovered` 事件只入队一次
- YAML 约定：`next` 表示玩家忽略洞察后的普通推进；`insight_next` 表示玩家追查后的隐藏信息分支，目标必须指向 `insight_prompt` 节点
- `insight_discovered` 的 `insight_id` 使用 `insight_next` 目标节点 id，保证内容侧命名稳定且不需要额外字段
- 本 story 不实现洞察提示的视觉样式，仅实现运行时门槛与分支逻辑；视觉效果留给 ds-009 / UX spec

## 验收标准

- [x] 含洞察线索的节点激活且玩家 insight ≥ 门槛时，文字显示完毕后进入洞察提示状态
- [x] 洞察提示无限期持续，无倒计时
- [x] 玩家 insight < 门槛时，无洞察提示、无追查入口，对话作为普通节点继续
- [x] 玩家选择追查后，隐藏信息分支展开
- [x] 追查成功后，`insight_discovered` 事件恰好触发一次
- [x] Choice 节点不触发洞察提示

## QA 测试用例

- **AC-1**：洞察达标
  - Given：玩家 insight=18，节点 threshold=15
  - When：节点文字显示完毕
  - Then：运行时暴露洞察提示状态，并允许追查
  - 边界：insight=threshold 时触发

- **AC-2**：洞察不足
  - Given：玩家 insight=14，节点 threshold=15
  - When：节点文字显示完毕
  - Then：无洞察提示，无追查入口，确认后按普通 next 推进
  - 边界：节点未配置 threshold

- **AC-3**：追查事件
  - Given：洞察提示显示中
  - When：玩家选择追查
  - Then：进入 InsightPrompt 分支，并将 `insight_discovered` 入队一次
  - 边界：重复点击追查不会重复入队

## 测试证据路径

`tests/Core/Dialogue/DialogueInsightTests.cs`

## 完成记录

- 新增 `DialogueNode.InsightNext` 字段，并在图校验中检查 `speech` / `narration` 洞察源节点必须指向 `insight_prompt`。
- 新增 `IDialogueInsightValueProvider`、`DialogueInsightCue` 与 `DialogueRuntime.InvestigateInsight()`，运行时在 WaitingForInput 阶段暴露无倒计时洞察提示。
- 普通 `Confirm()` 视为忽略洞察并沿 `next` 推进；`InvestigateInsight()` 沿 `insight_next` 推进，两者互斥。
- 追查成功时自动入队一次 `insight_discovered`，事件 id 使用 `insight_next` 目标节点 id。
- 验证：`/Users/bytedance/.dotnet/dotnet test tests/Foundation/Foundation.Tests.csproj --filter DialogueInsightTests`，7/7 通过。
- 验证：`/Users/bytedance/.dotnet/dotnet test tests/Foundation/Foundation.Tests.csproj`，485/485 通过。

## 依赖关系

- Depends on: ds-002, ds-003, ds-004
- Unlocks: ds-009
