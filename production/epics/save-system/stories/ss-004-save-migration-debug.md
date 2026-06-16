# Story: ss-004 — 版本迁移链与明文调试旁路

> **Epic**: save-system
> **类型**: Logic
> **优先级**: P1 — 兼容旧档与开发调试
> **依赖**: ss-001, ss-003
> **阻塞**: 无
> **ADR 指引**: ADR-0004（MigrationChain、.bak、.dev.json）
> **GDD 来源**: design/gdd/save-system.md §Detailed Design, §Edge Cases, §Open Questions
> **TR-ID**: TR-save-system-???（tr-registry.yaml 暂无真实条目）
> **Control Manifest Version**: 2026-06-10
> **状态**: Complete
> **Last Updated**: 2026-06-11

## 目标

实现存档版本迁移链和开发构建的明文旁路，保证旧档可升级、新档可调试。

## 范围

### 包含
- `MigrationChain` 注册与顺序执行
- v1→v2→v3 链式迁移，不跳版本
- 迁移前创建 `.bak` 备份
- 迁移超时 500ms 时中断并保守失败
- DEBUG 构建额外写出 `.dev.json`
- 正式构建不产出明文旁路

### 不包含
- 真正的加密算法 → ss-003
- SaveManager 读写分发 → ss-002
- 自动存档触发 → ss-005

## 技术说明

- 迁移函数只补充默认值，不删除旧字段
- 迁移失败应尽量保留原文件
- `schema_version` 从 1 开始，按单步递增

## 验收标准

- [x] 低版本存档按顺序执行所有迁移
- [x] 缺少某一步迁移时明确报错，不跳版本
- [x] 迁移前创建 `.bak` 备份
- [x] 迁移超过 500ms 时中断并提示
- [x] DEBUG 构建额外输出 `.dev.json`
- [x] 正式构建不写明文旁路

## QA 测试用例

- **AC-1**：链式迁移
  - Given：一个 schema_version=1 的存档和 v1→v2、v2→v3 两个迁移
  - When：加载该存档
  - Then：两步迁移按顺序执行，最终得到当前结构
  - 边界：中间缺失迁移时应失败

- **AC-2**：备份保护
  - Given：准备迁移的旧存档文件
  - When：开始迁移
  - Then：原文件的 `.bak` 备份先被创建
  - 边界：迁移失败后仍保留备份

- **AC-3**：DEBUG 旁路
  - Given：DEBUG 构建
  - When：执行保存
  - Then：额外生成 `.dev.json`
  - 边界：正式构建时不生成该文件

## 测试证据路径

`tests/unit/save-system/save-migration_test.cs`

## 实现记录

- 新增 `MigrationChain`，支持按 `vN -> vN+1` 单步注册与链式应用，遇到缺失步骤时直接失败，不允许跳版本。
- 新增 `SaveMigrationResult`，显式返回成功、失败原因、超时标记与已执行迁移列表，方便上层做保守回滚。
- 新增 `SaveDebugFileService`，封装迁移前 `.bak` 备份与 DEBUG 构建 `.dev.json` 明文旁路输出。
- DEBUG 明文旁路通过 `#if DEBUG` 编译期开关控制，正式构建不包含写出路径。

## 验证结果

- 2026-06-11：`/Users/bytedance/.dotnet/dotnet test tests/Foundation/Foundation.Tests.csproj`
- 结果：通过 539，失败 0，跳过 0。
- 备注：测试输出存在既有 `SceneConfigTests.cs` 可空警告，与本 story 无关。

## 依赖关系

- Depends on: ss-001, ss-003
- Unlocks: ss-002, ss-005, ss-006
