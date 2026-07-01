# Story: ms-009 — Content: Chapter 0 Misunderstanding Instances

> **Epic**: misunderstanding-system
> **Status**: Complete
> **Last Updated**: 2026-06-30
> **Layer**: Config/Data
> **Type**: Config/Data
> **Priority**: P2
> **Estimate**: 1 day
> **Manifest Version**: 2026-06-30
> **Blocked By**: ms-006 (trigger 集成), 序章叙事内容 (prologue-content epic)
> **GDD 来源**: design/gdd/misunderstanding-system.md §Core Rules 2 (三大触发来源示例)
> **TR-ID**: 待 architecture-review 分配

## Context

序章（Chapter 0）中师兄误会的具体配置数据。包括误会触发条件（trigger 配置）、澄清条件（resolution_conditions）、窗口期参数和透明度信号内容。此 story 是纯数据/配置工作，依赖 ms-006 的触发集成和序章叙事内容的确定。

**ADR Governing Implementation**: 通用架构
**Engine**: Godot 4.7-stable | **Risk**: LOW
**Engine Notes**: 数据文件为 JSON/Resource，无引擎 API 风险。

## Acceptance Criteria

- [ ] AC-1: 序章师兄误会的 trigger 配置完整：包含 trigger_type、匹配条件、target_npc、初始 severity、窗口天数。
- [ ] AC-2: 每条误会配置包含完整的 resolution_conditions 列表（每个条件有 type、target、value）。
- [ ] AC-3: 称呼对照表（亲密/疏远称呼对）为师兄 NPC 配置完毕。
- [ ] AC-4: 配置数据可被 ms-006 的 TriggerService 正确加载和解析（通过集成测试验证）。
- [ ] AC-5: 文学信号文本（HINTED 暗示、PERCEIVED 提示、URGENT 紧迫信号）为师兄误会配置完毕。

## Implementation Notes

**Control Manifest Rules (Config/Data Layer)**:
- Required: 所有配置为数据驱动文件，放置在 `data/misunderstanding/` 目录下。
- Required: 配置格式与 ms-006 的 TriggerConfig schema 一致。
- Required: 称呼表格式与 ms-008 的 DialogueSignalChannel 预期一致。
- Required: 文学信号文本使用本地化 key，实际文本在 `data/localization/` 中。
- Forbidden: 不硬编码在 C# 代码中——所有内容必须是外部数据。

**配置内容参考（GDD 示例）**:
- 绝江渡传闻"停云见死不救" → 白苓注册了对此敏感的 trigger
- 向明德堂弟子说出白苓行踪 → 对话 mis_trigger
- 白苓等你三天未至 → 缺席误解

## Files to Create/Modify

- `data/misunderstanding/chapter00/shixiong_triggers.json`
- `data/misunderstanding/chapter00/shixiong_resolution.json`
- `data/misunderstanding/address_tables/shixiong.json`
- `data/misunderstanding/literary_signals/shixiong.json`

## Test Evidence

**Required evidence**:
- `tests/integration/misunderstanding/Chapter0ContentLoadTest.cs`

**Test cases**:
- 加载 shixiong_triggers.json → 解析为有效 TriggerConfig 列表
- 加载 shixiong_resolution.json → 解析为有效 ResolutionCondition 列表
- 模拟触发匹配事件 → 创建正确的误会实例
- 称呼表加载后有亲密/疏远两组称呼

## Out of Scope

- 后续章节的误会内容（各章 content story 单独处理）
- 误会系统的逻辑实现（ms-001 ~ ms-005 已完成）
- 叙事文本和对话文本（prologue-content epic）

## Dependencies

- Depends on: ms-006, ms-008（称呼表 schema）; prologue-content epic（叙事内容确定）
- Unlocks: 无（终端 story）
