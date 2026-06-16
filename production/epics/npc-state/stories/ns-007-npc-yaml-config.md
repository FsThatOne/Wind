# Story: ns-007 — NPC YAML Config

> **Epic**: npc-state
> **Type**: Integration
> **Priority**: P2 — 数据驱动 NPC 配置
> **Depends On**: ns-002, DataRegistry (cd-005)
> **ADR Guidance**: ADR-0003 (YAML + DataRegistry)
> **GDD Source**: design/gdd/npc-state.md §Core Rules 1
> **Status**: Done

## Goal

定义 NPC 模板 YAML schema 并集成 DataRegistry，支持 NPC 初始状态、态度表和旅程配置的数据驱动加载。

## Acceptance Criteria

- [ ] **AC1**: `NpcTemplate` YAML 反序列化正确（id, name, 初始状态各维度, 态度配置表）
- [ ] **AC2**: DataRegistry 启动后 `GetTable<NpcTemplate>().Get("bai_ling")` 返回有效对象
- [ ] **AC3**: 态度配置表包含 mindset_compatibility 和 morality_reaction 映射
- [ ] **AC4**: 格式错误 YAML → DataLoadException 含文件名+行号
- [ ] **AC5**: 同 ID NPC 模板 → 启动时报错
- [ ] **AC6**: 缺少可选字段时使用安全默认值，不报错

## Test Evidence Path

`tests/Foundation/NpcState/NpcConfigTests.cs`
