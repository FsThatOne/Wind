# Story: ma-004 — 特殊触发条件与特殊效果解析契约

> **Epic**: martial-arts-system
> **类型**: Logic
> **优先级**: P0 — 招式差异化核心
> **Estimate**: M（约 4-6h）
> **依赖**: ma-001, ma-002
> **阻塞**: 无
> **ADR 指引**: ADR-0003（YAML 配置）
> **GDD 来源**: design/gdd/martial-arts-system.md §Detailed Design, §Edge Cases, §Acceptance Criteria
> **TR-ID**: TR-martial-arts-system-004
> **Control Manifest Version**: 2026-06-10
> **状态**: Complete
> **Last Updated**: 2026-06-12

## 目标

定义并实现招式特殊触发条件与特殊效果的解析契约，使战斗系统可以传入回合上下文，得到“是否触发”和“触发后应应用的效果列表”。

## 范围

### 包含
- 条件定义模型：如本回合第 N 次克制同一目标、命中时、无条件等
- 效果定义模型：如破绽 +N、无视防御、回复内息、防御减免等结构化效果
- 批注版 `condition_override` 覆盖原条件
- 批注版 `effect_override` 覆盖或强化原效果
- 返回给战斗系统的纯数据结果，不直接修改战斗状态

### 不包含
- 具体战斗回合状态维护和破绽写入 → combat-system
- UI 图标表现 → ma-008 / combat-ui
- 完整 Boss/AI 行为触发 → enemy-ai

## 技术说明

- 条件和效果必须结构化，禁止把条件写成只能人工阅读的自由文本
- 特殊效果执行权归战斗系统，本 story 只负责解析、校验、判定和输出效果 DTO
- 批注版条件/效果优先级高于原版
- 绝学特殊效果是否解锁取决于 ma-002 提供的成长状态

## 验收标准

- [x] 批注版罗汉拳 `condition_override="第1次克制即触发"` 时，第 1 次克制命中立即触发破绽 +3
- [x] 原版罗汉拳 `condition="第2次克制触发"` 时，第 1 次克制不触发，第 2 次克制触发
- [x] 条件不满足时返回未触发，不输出特殊效果
- [x] 批注版效果使用 `effect_override`，原版效果不并行叠加
- [x] 未解锁的绝学特殊效果不会触发

## QA 测试用例

- **AC-1**：批注版条件覆盖
  - Given：批注版罗汉拳，条件为第 1 次克制即触发
  - When：本回合第 1 次克制命中同一目标
  - Then：返回触发成功，并输出破绽 +3 效果
  - 边界：未命中、非克制命中、不同目标

- **AC-2**：原版第 2 次克制
  - Given：原版罗汉拳，条件为第 2 次克制触发
  - When：第 1 次克制命中
  - Then：不触发
  - Edge cases：第 2 次克制同一目标触发；第 2 次克制不同目标不触发

- **AC-3**：绝学效果锁定
  - Given：绝学处于残卷状态，特殊效果需完本解锁
  - When：满足触发条件
  - Then：不输出未解锁特殊效果
  - 边界：完本解锁 v1；真传解锁 v2

## 测试证据路径

`tests/unit/martial-arts/special_condition_effects_test.cs`

## 依赖关系

- Depends on: ma-001, ma-002
- Unlocks: combat-system special effect stories

## Completion Notes

- Synced from implementation evidence on 2026-06-12.
- Verification: `/usr/local/share/dotnet/dotnet test tests/Foundation/Foundation.Tests.csproj --no-restore -v q` — 1033/1033 passed.

