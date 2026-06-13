# Story: ss-002 — SaveManager 核心读写与 ISaveable 分发

> **Epic**: save-system
> **类型**: Integration
> **优先级**: P0 — 存档系统主循环
> **依赖**: ss-001
> **阻塞**: 无
> **ADR 指引**: ADR-0004（SaveManager / ISaveable / SaveResult），ADR-0001（SaveLoadedEvent / SaveCompletedEvent）
> **GDD 来源**: design/gdd/save-system.md §Detailed Design, §Interactions with Other Systems, §Acceptance Criteria
> **TR-ID**: TR-save-system-???（tr-registry.yaml 暂无真实条目）
> **Control Manifest Version**: 2026-06-10
> **状态**: Complete
> **Last Updated**: 2026-06-11

## 目标

实现 SaveManager 的基础读写能力，以及对所有已注册系统的序列化 / 反序列化分发。

## 范围

### 包含
- `SaveGame(slot)`、`LoadGame(slot)`、`ListSlots()`、`DeleteSlot()`、`GetMetadata()`
- `RegisterSerializer(key, serializeFunc, deserializeFunc)`
- 保存时收集所有 `ISaveable` 数据并组装 payload
- 加载时把对应 key 的数据分发给系统
- 未知 key 向前兼容，忽略不崩溃
- 成功后发布 SaveCompleted / SaveLoaded 事件

### 不包含
- 实际加密/HMAC → ss-003
- 迁移链 → ss-004
- 自动存档触发 → ss-005
- UI 菜单 → ss-006

## 技术说明

- `SaveManager` 作为 Godot Autoload
- 保存/加载逻辑应可在测试中替身化
- 对外错误应通过 `SaveResult` 明确返回，不吞异常

## 验收标准

- [x] `SaveGame` 能收集各系统序列化数据并写入单个槽位
- [x] `LoadGame` 能将存档数据按 key 分发回对应系统
- [x] 未注册 key 在加载时被忽略，不导致崩溃
- [x] 成功保存后发布 SaveCompleted 事件
- [x] 成功加载后发布 SaveLoaded 事件
- [x] `ListSlots()` 与 `GetMetadata()` 返回可供 UI 使用的数据

## QA 测试用例

- **AC-1**：保存回合
  - Given：两个已注册系统分别返回不同字典
  - When：调用 `SaveGame(1)`
  - Then：生成的 payload 含两个 key，且保存成功
  - 边界：某系统返回空字典

- **AC-2**：加载回合
  - Given：一个包含已注册 key 和未知 key 的存档文件
  - When：调用 `LoadGame(1)`
  - Then：已注册 key 被分发，未知 key 被忽略
  - 边界：缺少某个已注册 key 时使用默认值或不调用对应反序列化

- **AC-3**：事件发布
  - Given：保存或加载成功完成
  - When：流程结束
  - Then：对应完成事件被发布一次
  - 边界：失败时不发布成功事件

## 测试证据路径

`tests/integration/save-system/save-manager-roundtrip_test.cs`

## 实现记录

- 新增 `SaveManager`，支持 `RegisterSerializer(ISaveable)`、保存汇总 `SavePayload`、加载按 `SaveKey` 分发、未知 key 向前兼容忽略。
- 新增 `ISavePayloadStore` 与 `InMemorySavePayloadStore`，作为 ss-002 的可替换存储底座；真实磁盘写入、加密、HMAC、迁移仍留给 ss-003/ss-004。
- 新增 `SaveCompletedEvent` / `SaveLoadedEvent`，成功保存/加载后通过现有 `IEventBus` 发布。
- 已通过 `/Users/bytedance/.dotnet/dotnet test tests/Foundation/Foundation.Tests.csproj`：522/522。

## 依赖关系

- Depends on: ss-001
- Unlocks: ss-003, ss-004, ss-005
