# Story: ds-003 — 条件评估与选项过滤

> **Epic**: dialogue-system
> **类型**: Logic
> **优先级**: P0 — 分支与选择基础
> **Estimate**: M（约 6h）
> **依赖**: ds-001, ds-002
> **阻塞**: 无
> **ADR 指引**: ADR-0005（conditions / conditions_any_of），ADR-0001（跨系统查询边界）
> **GDD 来源**: design/gdd/dialogue-system.md §Detailed Design, §Formulas, §Edge Cases, §Acceptance Criteria
> **TR-ID**: TR-dialogue-system-003
> **Control Manifest Version**: 2026-06-10
> **状态**: Complete
> **Last Updated**: 2026-06-11

## 目标

实现对话条件评估和 Choice 选项过滤，让系统只显示条件满足的选项，并在所有选项不满足时回退到默认分支或兜底选项。

## 范围

### 包含
- 条件三元组 `{source, op, value}`
- 支持 `==`、`!=`、`>`、`>=`、`<`、`<=`
- 默认 AND，`conditions_any_of` 表示 OR
- 节点入口条件和选项条件
- 条件不满足选项完全不可见，不灰化
- 可见选项按编辑顺序排列
- Choice 节点所有选项条件均不满足时，使用默认分支或兜底选项“……（无言以对）”

### 不包含
- 各下游系统真实数值计算；本 story 只通过接口查询 source_value
- 心境暗示 UI → ds-009
- 洞察门控分支 → ds-005
- 暗号选项 → ds-006

## 技术说明

- 条件评估器不应直接依赖心境、NPC、物品等具体系统实现，应通过查询接口或测试替身读取值
- 对话系统只执行比较，不计算 `source_value`
- 条件查询失败时应返回可诊断错误，不让对话挂起
- 对应 GDD 的明确要求：`条件支持 AND / OR 组合`，`条件判定在节点显示之前执行`，且 `不可用选项不显示（不灰化）`
- 条件三元组 `{source, op, value}` 以及 `conditions_any_of` 的语义必须与 ADR-0005 保持一致
- 本 story 不涉及真实数值规则，只负责读取条件来源并执行布尔过滤；无额外引擎 API 依赖

## 验收标准

- [x] Choice 节点到达时，仅条件满足的选项可见
- [x] 条件不满足的选项完全不存在，不显示灰化状态
- [x] 可见选项按编辑顺序排列
- [x] AND / OR 条件组合结果正确
- [x] 所有选项条件均不满足时，不显示空面板，不挂起，回退到默认分支或兜底选项
- [x] 对话节点入口条件不满足时，按 fallback 或下一可用路由处理

## QA 测试用例

- **AC-1**：选项过滤
  - Given：Choice 节点有 4 个选项，其中 2 个条件满足
  - When：渲染候选选项
  - Then：只返回 2 个可见选项，顺序与 YAML 编辑顺序一致
  - 边界：0 个满足、全部满足

- **AC-2**：条件运算符
  - Given：source_value 分别为 10、20、30
  - When：执行 `==`、`!=`、`>`、`>=`、`<`、`<=`
  - Then：结果符合布尔比较语义
  - 边界：等于阈值时 `>=` 和 `<=` 为 true

- **AC-3**：默认兜底
  - Given：Choice 节点所有条件选项均不满足且配置了默认选项
  - When：选项面板将渲染
  - Then：只显示默认选项
  - 边界：未配置默认选项时显示“……（无言以对）”并记录错误

## 测试证据路径

`tests/Core/Dialogue/DialogueConditionTests.cs`

## 依赖关系

- Depends on: ds-001, ds-002
- Unlocks: ds-005, ds-006, ds-009

## 完成记录

- Implementation: `src/FengZhi.Foundation/Dialogue/DialogueConditions.cs`, `src/FengZhi.Foundation/Dialogue/DialogueRuntime.cs`
- Tests: `tests/Core/Dialogue/DialogueConditionTests.cs`
- Evidence: `/Users/bytedance/.dotnet/dotnet test tests/Foundation/Foundation.Tests.csproj --filter DialogueConditionTests` → 14/14 通过
- Regression: `/Users/bytedance/.dotnet/dotnet test tests/Foundation/Foundation.Tests.csproj` → 474/474 通过
