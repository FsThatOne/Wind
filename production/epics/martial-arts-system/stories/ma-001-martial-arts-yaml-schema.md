# Story: ma-001 — 武学 YAML 数据模型与克制矩阵加载

> **Epic**: martial-arts-system
> **类型**: Config/Data
> **优先级**: P0 — 武学系统数据入口
> **Estimate**: M（约 4-6h）
> **依赖**: 无
> **阻塞**: 无
> **ADR 指引**: ADR-0003（YAML 配置）
> **GDD 来源**: design/gdd/martial-arts-system.md §Detailed Design, §Interactions, §Acceptance Criteria
> **TR-ID**: TR-martial-arts-system-001
> **Control Manifest Version**: 2026-06-10
> **状态**: Complete
> **Last Updated**: 2026-06-12

## 目标

定义武学系统的静态配置数据模型，并加载 `assets/data/martial-arts/` 下的招式、心法、轻功和刚柔巧克制矩阵，使战斗系统后续可通过稳定接口查询武学基础数据。

## 范围

### 包含
- `MoveDefinition`：招式 id、名称、体系、类别、内息消耗、基础倍率、触发条件、特殊效果、类型标签
- `XinfaDefinition`：心法 id、名称、属性门槛、被动加成、专属招式
- `QinggongDefinition`：轻功 id、名称、移动范围修正、主动机动配置占位
- `CounterMatrix`：刚 / 柔 / 巧克制关系数据
- YAML 路径：`assets/data/martial-arts/moves.yaml`、`xinfa.yaml`、`qinggong.yaml`、`counter-matrix.yaml`
- 加载时校验：id 唯一、枚举合法、倍率/消耗范围合法、引用的专属招式存在

### 不包含
- 招式成长状态与残卷合成 → ma-002
- 伤害公式计算 → ma-003
- 特殊条件和效果运行时判定 → ma-004
- 角色装备槽运行时状态 → ma-005
- UI 呈现 → ma-008

## 技术说明

- 必须使用 YAML 1.2 + YamlDotNet
- 配置路径必须位于 `assets/data/martial-arts/`
- 启动时一次性加载到 DataRegistry，运行时只读
- 查询必须通过 `IDataTable<T>` 风格接口或同等 O(1) id 查询接口
- YAML 格式错误必须快速失败，并报告文件名、字段名和可定位信息
- 禁止使用 JSON、Godot Resource `.tres/.res` 或 TOML 承载批量配置

## 验收标准

- [ ] 可加载合法的招式、心法、轻功和克制矩阵 YAML，并按 id 查询
- [ ] 招式必须包含名称、体系、类别、内息消耗、基础倍率、触发条件、特殊效果和类型标签
- [ ] 刚 / 柔 / 巧克制矩阵可查询任意攻击体系对防守体系的关系
- [ ] 重复 id、非法体系、非法类别、非法倍率、非法内息消耗会快速失败
- [ ] 心法引用不存在的专属招式时校验失败

## QA 测试用例

- **AC-1**：加载合法武学配置
  - Given：包含刚/柔/巧招式、1 个心法、1 个轻功和完整克制矩阵的 YAML
  - When：调用武学配置加载器
  - Then：返回可查询的数据表，所有 id、枚举、数值和引用保持一致
  - 边界：只有 1 个招式、空心法表、空轻功表

- **AC-2**：非法配置快速失败
  - Given：存在重复 id、非法体系 `fire`、内息消耗 `-1` 或倍率 `0`
  - When：调用武学配置加载器
  - Then：加载失败，并返回包含文件名和字段名的错误
  - 边界：缺少必填字段、未知类别、重复克制矩阵 key

- **AC-3**：引用完整性校验
  - Given：心法配置引用一个不存在的专属招式 id
  - When：运行配置校验
  - Then：校验失败，并指出心法 id 与缺失招式 id
  - 边界：专属招式存在但体系非法、专属招式重复引用

## 测试证据路径

`tests/unit/martial-arts/martial_arts_yaml_schema_test.cs`

## 依赖关系

- Depends on: 无
- Unlocks: ma-002, ma-003, ma-004, ma-005, ma-006, ma-007, ma-008

## Completion Notes
**Completed**: 2026-06-12
**Criteria**: 5/5 passing（全部由自动化单元测试覆盖，1028/1028 全量通过）
**Deviations**:
- ADVISORY: 语义校验错误 `lineNumber: null`（仅 YAML parse 错误含行号）— 已记 tech-debt
- ADVISORY: 数据模型 `get; set;` 可变，未完全落实 ADR-0003 运行时只读 — 与既有 CharacterData 层模式一致，已记 tech-debt
**Test Evidence**: tests/unit/martial-arts/martial_arts_yaml_schema_test.cs（Config/Data 故事，单测超出最低要求；冒烟报告待 /smoke-check 统一补）
**Code Review**: Complete — /code-review CHANGES REQUIRED → 必改项修复（doc comments、精确断言、非法枚举测试、命名）→ 复测通过
