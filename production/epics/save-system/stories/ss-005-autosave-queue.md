# Story: ss-005 — 自动存档触发、阻断条件与排队

> **Epic**: save-system
> **类型**: Integration
> **优先级**: P1 — 自动保存体验
> **依赖**: ss-002, ss-004
> **阻塞**: 无
> **ADR 指引**: ADR-0004（自动存档延迟、阻断条件、槽位规则），ADR-0001（事件通知）
> **GDD 来源**: design/gdd/save-system.md §Detailed Design, §Edge Cases, §Acceptance Criteria
> **TR-ID**: TR-save-system-???（tr-registry.yaml 暂无真实条目）
> **Control Manifest Version**: 2026-06-10
> **状态**: Complete
> **Last Updated**: 2026-06-11

## 目标

实现自动存档的触发、延迟、阻断和排队行为，让场景切换、客栈休息、章节推进和锁定对话完成后能够稳定写入 autosave。

## 范围

### 包含
- 场景切换完成后 500ms 触发自动存档
- 客栈休息完成后 500ms 触发自动存档
- 主线章节推进后 500ms 触发自动存档
- 锁定对话序列完成后 500ms 触发自动存档
- 战斗/过场/锁定对话/存档进行中禁止触发
- 自动存档与手动存档冲突时排队执行

### 不包含
- 菜单 UI → ss-006
- 加密和迁移实现细节 → ss-003 / ss-004
- 具体场景管理或对话系统实现

## 技术说明

- 自动存档目标槽固定为 autosave
- 自动存档不应并发写同一文件
- 触发器应尽量通过事件驱动而非轮询

## 验收标准

- [x] 场景切换完成后，500ms 延迟触发 autosave
- [x] 客栈休息、章节推进、锁定对话完成后也会触发 autosave
- [x] 战斗、过场、锁定对话进行中或存档进行中不会触发
- [x] 与手动存档冲突时自动存档排队等待
- [x] 自动存档成功后可在槽位列表中识别为 autosave

## QA 测试用例

- **AC-1**：延迟触发
  - Given：触发场景切换完成事件
  - When：等待 500ms
  - Then：执行一次 autosave
  - 边界：500ms 前不应写入

- **AC-2**：阻断条件
  - Given：当前处于战斗中或过场中
  - When：自动存档触发条件到达
  - Then：不执行写入
  - 边界：锁定对话中同样阻断

- **AC-3**：排队策略
  - Given：手动存档正在进行
  - When：自动存档触发
  - Then：自动存档排队，待手动存档完成后再执行
  - 边界：连续多个触发只保留合理队列，不并发写入

## 测试证据路径

`tests/integration/save-system/autosave-triggers_test.cs`

## 实现记录

- 新增 `AutosaveScheduler`，统一处理自动存档 500ms 延迟、阻断状态、手动/自动存档串行化和连续触发合并。
- 新增 `AutosaveTriggerReason`、`AutosaveTriggerEvent` 与 `AutosaveTriggerRules`，让场景、客栈、章节推进和锁定对话完成可通过事件触发 autosave。
- 新增 `IAutosaveBlocker` 与 `AutosaveBlocker`，为战斗、过场、锁定对话和存档进行中等阻断条件提供可替换聚合入口。
- 自动存档固定写入 `SaveSlotId.AutoSave`，手动保存通过同一 gate 串行，避免并发写同一槽位或文件。

## 验证结果

- 2026-06-11：`/Users/bytedance/.dotnet/dotnet test tests/Foundation/Foundation.Tests.csproj`
- 结果：通过 550，失败 0，跳过 0。
- 备注：测试输出存在既有 `SceneConfigTests.cs` 可空警告，与本 story 无关。

## 依赖关系

- Depends on: ss-002, ss-004
- Unlocks: ss-006
