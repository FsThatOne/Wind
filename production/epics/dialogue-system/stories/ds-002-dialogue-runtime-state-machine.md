# Story: ds-002 — 对话运行时状态机与节点推进

> **Epic**: dialogue-system
> **类型**: Logic
> **优先级**: P0 — 对话运行时骨架
> **Estimate**: M（约 6h）
> **依赖**: ds-001
> **阻塞**: 无
> **ADR 指引**: ADR-0005（图节点遍历），ADR-0001（本 story 暂不派发跨层事件）
> **GDD 来源**: design/gdd/dialogue-system.md §States and Transitions, §Edge Cases, §Acceptance Criteria
> **TR-ID**: TR-dialogue-system-002
> **Control Manifest Version**: 2026-06-10
> **状态**: Complete
> **Last Updated**: 2026-06-11

## 目标

实现对话运行时状态机，支持从入口节点开始推进、逐字显示阶段、等待输入、普通节点确认、结束退出和 500 节点死循环保护。

## 范围

### 包含
- 状态：Idle、Entering、Displaying、WaitingForInput、ProcessingChoice、LetterReading、Exiting
- 普通文本节点进入、显示、等待确认、跳转到下一节点
- 打字机显示中的第一次确认立即显示完整文字
- 文字完整后的第二次确认推进到下一节点
- 快速连续按键不能跳过多个节点
- 对话结束后恢复 Idle
- 500 节点访问上限触发强制退出并记录错误

### 不包含
- UI 动画和实际文本渲染 → ds-008
- 选择过滤 → ds-003
- 事件队列派发 → ds-004
- 洞察状态 → ds-005
- 书信 UI → ds-007 / ds-008

## 技术说明

- 运行时应与 Godot UI 解耦，核心推进逻辑可单元测试
- 同模块内部使用直接方法调用
- 后续 UI 通过运行时状态和显示 DTO 读取当前节点表现数据
- 对应 GDD 的明确要求：`Idle → Entering → Displaying → WaitingForInput → ProcessingChoice → Exiting` 的状态流必须稳定，且单次对话最多访问 500 节点
- 必须遵守 ADR-0005 的图遍历与 500 节点防御规则；本 story 不负责跨层事件派发，避免混入 ADR-0001 的下游逻辑
- 由于本 story 直接触碰引擎输入与状态推进，需验证快速连续按键不会在一帧内跨越多个节点

## 验收标准

- [x] Speech/Narration/InnerMonologue 节点可从 Entering 进入 Displaying，再进入 WaitingForInput
- [x] 玩家在打字机显示中按一次确认键，剩余文字立即全部显示
- [x] 文字完全显示后再次确认，才推进到下一节点
- [x] 快速连续按键不会在一帧内跳过多个节点
- [x] 到达 END 或无后续节点时进入 Exiting，随后恢复 Idle
- [x] 访问节点次数达到 500 时强制退出并记录错误日志

## QA 测试用例

- **AC-1**：普通节点推进
  - Given：包含两个 speech 节点和 END 的对话序列
  - When：启动对话并依次确认
  - Then：状态按 Entering → Displaying → WaitingForInput → Displaying → WaitingForInput → Exiting → Idle 推进
  - 边界：单节点直接 END

- **AC-2**：打字机输入节流
  - Given：当前节点文字尚未显示完
  - When：同一帧连续触发两次确认
  - Then：第一次仅补全文字，第二次不跨过下一节点
  - 边界：文字显示速度为瞬间时，确认直接进入下一节点

- **AC-3**：死循环保护
  - Given：A.next 指向 A 的循环对话
  - When：运行时自动推进到超过 500 次访问
  - Then：对话强制进入 Exiting 并记录错误
  - 边界：499 次不触发，500 次触发

## 测试证据路径

`tests/Core/Dialogue/DialogueRuntimeTests.cs`

## 依赖关系

- Depends on: ds-001
- Unlocks: ds-003, ds-004, ds-005, ds-007, ds-008

## 完成记录

- Implementation: `src/FengZhi.Foundation/Dialogue/DialogueRuntime.cs`
- Tests: `tests/Core/Dialogue/DialogueRuntimeTests.cs`
- Evidence: `/Users/bytedance/.dotnet/dotnet test tests/Foundation/Foundation.Tests.csproj --filter DialogueRuntimeTests` → 7/7 通过
- Regression: `/Users/bytedance/.dotnet/dotnet test tests/Foundation/Foundation.Tests.csproj` → 460/460 通过
