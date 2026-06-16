# Story: ai-001 — 性格模板与体系选择 (Phase A)

> **Epic**: enemy-ai
> **类型**: Foundation
> **优先级**: P0 — AI 系统基石
> **Estimate**: S（约 2-3h）
> **依赖**: 无
> **阻塞**: ai-002, ai-003, ai-004, ai-005
> **GDD 来源**: design/gdd/enemy-ai.md §Core Rules 2-3, §Formulas F1
> **TR-ID**: TR-enemy-ai-001
> **状态**: Complete

## 目标

实现性格模板数据模型和 Phase A 体系选择的核心概率计算：基础权重 + min_type_weight 钳位 + 归一化。

## 范围

### 包含
- PersonalityTemplate POCO：name, base_weights[Gang/Rou/Qiao], weakness_type
- 3 种预设模板（刚猛型 60/15/25、柔韧型 15/55/30、诡诈型 25/20/55）
- TypeSelectionEngine 纯函数：raw_weight → clamped → normalized probability
- IAIRandomSource 接口（可注入，测试用 seeded）
- min_type_weight 钳位（默认 5）

### 不包含
- 状态修正 / 情境修正 / 连续惩罚 → ai-002
- 招式预兆加成 → ai-003
- 选招逻辑 → ai-004

## 技术说明

- 命名空间 `FengZhi.Foundation.Combat.AI`
- F1 公式: raw_weight[type] = base_weight[type] + modifiers (此 story 中 modifiers = 0)
- clamped_weight[type] = max(min_type_weight, raw_weight[type])
- P(type) = clamped_weight[type] / Σ clamped_weight[all types]

## 验收标准

- [x] PersonalityTemplate 包含 name, base_weights, weakness_type 字段
- [x] 3 种预设模板数据正确
- [x] TypeSelectionEngine.ComputeProbabilities 对 (60/15/25) 返回 (0.6/0.15/0.25)
- [x] min_type_weight 钳位：输入 (-10/15/25) → clamp(5/15/25) → normalize(5/15/25 → 11.1%/33.3%/55.6%)
- [x] SelectType 使用 IAIRandomSource 按概率选择一个体系
- [x] 1000 次模拟刚猛型选择，刚系频率在 57%-63% 范围内 (GDD AC#1)

## Completion Notes

- Synced from implementation evidence on 2026-06-12.
- Verification: `/usr/local/share/dotnet/dotnet test tests/Foundation/Foundation.Tests.csproj --no-restore -v q` — 1033/1033 passed.

