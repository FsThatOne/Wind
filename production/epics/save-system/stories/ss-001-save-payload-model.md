# Story: ss-001 — 存档 payload 与槽位元数据模型

> **Epic**: save-system
> **类型**: Config/Data
> **优先级**: P0 — 存档系统数据入口
> **依赖**: 无
> **阻塞**: 无
> **ADR 指引**: ADR-0004（存档文件结构、SlotMetadata、schema_version）
> **GDD 来源**: design/gdd/save-system.md §Detailed Design, §Formulas, §GDD Requirements Addressed, §Acceptance Criteria
> **TR-ID**: TR-save-system-???（tr-registry.yaml 暂无真实条目）
> **Control Manifest Version**: 2026-06-10
> **状态**: Complete
> **Last Updated**: 2026-06-11

## 目标

定义存档文件 payload、文件头和槽位元数据结构，让后续加密、迁移、自动存档和 UI 都有稳定数据基础。

## 范围

### 包含
- `SavePayload` / `SaveFileHeader` / `SlotMetadata` 数据模型
- `schema_version`、`flags`、magic `"FZHI"`、IV、密文、HMAC 的结构约定
- 10 手动槽 + 1 自动槽的槽位语义
- 缩略图、章节名、场景名、游戏天数、累计时长、二周目标记
- `ISaveable`、`IMigration`、`ISaveManager` 接口草案

### 不包含
- 实际加密和 HMAC 计算 → ss-003
- 文件读写与系统分发 → ss-002
- 版本迁移实现 → ss-004
- 菜单 UI → ss-006

## 技术说明

- 存档内部使用 JSON，外层加密落盘
- 元数据字段必须可被槽位列表 UI 直接读取
- schema_version 从 1 起步

## 验收标准

- [x] 存档文件头包含 magic、schema_version、flags、IV、payload、HMAC 的结构定义
- [x] `SlotMetadata` 包含时间戳、章节名、场景名、游戏天数、游戏时长、缩略图、二周目标记
- [x] 10 手动槽 + 1 自动槽的槽位命名和语义明确
- [x] `ISaveable` / `IMigration` / `ISaveManager` 接口能支撑后续 story

## QA 测试用例

- **AC-1**：文件头结构
  - Given：一个完整存档 payload
  - When：序列化元数据结构
  - Then：头部字段包含 magic、schema_version、flags、IV、HMAC 所需的所有占位信息
  - 边界：schema_version=1

- **AC-2**：槽位元数据
  - Given：一个手动槽和一个自动槽
  - When：读取元数据
  - Then：能区分 `slot_01` 与 `autosave`，并读取章节名、日期、时长、缩略图
  - 边界：空槽 metadata 允许为空或默认值

## 测试证据路径

`tests/unit/save-system/save-payload-model_test.cs`

## 实现记录

- 新增 `FengZhi.Foundation.SaveSystem` 契约层：`SaveFileHeader`、`SaveFileFlags`、`SavePayload`、`SaveSnapshot`、`SaveSlotId`、`SlotMetadata`、`SaveResult`、`ISaveable`、`IMigration`、`ISaveManager`。
- `SaveSlotId.AllSlots()` 固定返回 `slot_01` ~ `slot_10` 与 `autosave`，供槽位 UI 与 SaveManager 后续 story 复用。
- `SaveSnapshot` 使用 `Dictionary<string, JsonElement>` 承载 JSON 对象字段，避免 `object` 反序列化类型不稳定。
- 已通过 `/Users/bytedance/.dotnet/dotnet test tests/Foundation/Foundation.Tests.csproj`：516/516。

## 依赖关系

- Depends on: 无
- Unlocks: ss-002, ss-003, ss-004, ss-005, ss-006
