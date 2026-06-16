# Story: cd-005 — YAML Character Config

> **Epic**: character-data
> **Type**: Integration
> **Priority**: P1 — 数据驱动架构的落地
> **Depends On**: cd-001
> **Blocked By**: cd-001 (需要数据模型定义)
> **ADR Guidance**: ADR-0003 (核心约束: YAML 1.2, YamlDotNet, DataRegistry, 快速失败)
> **GDD Source**: design/gdd/character-attributes.md §Interactions, §Tuning Knobs
> **Control Manifest Version**: 2026-06-10
> **Status**: Done

## Goal

定义角色模板的 YAML schema 并集成到 DataRegistry，实现数据驱动的角色配置加载。

## Scope

### In Scope
- YAML 角色模板 schema 定义 (`assets/data/characters/`)
  - `player.yaml` — 玩家角色模板
  - `companions/*.yaml` — 同伴模板
  - `enemies/*.yaml` — 敌人模板
- `CharacterTemplate` C# 数据类 (YamlDotNet 反序列化目标)
  - base_hp, base_neixi, base_stagger_threshold
  - 五维初始值
  - scaling_factor, 所属体系
  - 可用招式列表引用
- DataRegistry 集成:
  - 启动时加载 `assets/data/characters/**/*.yaml`
  - 注册为 `IDataTable<CharacterTemplate>`
  - `Get(id)` O(1) 查询
- 快速失败: YAML 格式错误 → 启动时报告文件名+行号，不静默跳过
- Tuning Knobs 外部化: `assets/data/balance/attribute-tuning.yaml`
  - base_hp, base_neixi, attr_cap, 境界阈值数组等

### Out of Scope
- DataRegistry 框架本身（假设已存在或由 Foundation 基础设施提供）
- 招式数据的具体 YAML schema → 武学系统 Epic
- 运行时角色实例化 → cd-006

## Technical Notes

- 路径: `src/FengZhi.Foundation/CharacterData/CharacterTemplate.cs`
- YAML 路径: `assets/data/characters/{type}/{id}.yaml`
- 遵循 ADR-0003 全部规则:
  - UTF-8 BOM-free
  - YamlDotNet NuGet
  - `IDataTable<T>` 接口
  - 配置只读，启动一次性加载
- Control Manifest: "YAML 格式错误必须在启动时立即报告文件名+行号"

## Acceptance Criteria

- [ ] **AC1**: `assets/data/characters/player.yaml` 可被 YamlDotNet 正确反序列化为 `CharacterTemplate`
- [ ] **AC2**: DataRegistry 启动后 `GetTable<CharacterTemplate>().Get("player")` 返回有效对象
- [ ] **AC3**: 故意写入格式错误的 YAML → 启动时抛出包含文件名和行号的异常
- [ ] **AC4**: `GetTable<CharacterTemplate>().GetAll()` 返回全部已注册角色模板
- [ ] **AC5**: `attribute-tuning.yaml` 中的 base_hp=100 能被正确加载并用于 FormulaEngine
- [ ] **AC6**: 多个同 ID 角色模板 → 启动时报错（唯一性约束）

## Test Evidence Path

`tests/Foundation/CharacterData/CharacterConfigTests.cs`
