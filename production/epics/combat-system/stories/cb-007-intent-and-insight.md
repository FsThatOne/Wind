# Story: cb-007 — 意图公开与洞察概率系统

> **Epic**: combat-system
> **类型**: Foundation
> **优先级**: P1 — 读意核心
> **Estimate**: S（约 3-4h）
> **依赖**: cb-002
> **阻塞**: 无
> **ADR 指引**: ADR-0003（YAML 配置）
> **GDD 来源**: design/gdd/combat-system.md §Core Rules 2-②, §Formulas F9
> **TR-ID**: TR-combat-system-007
> **Control Manifest Version**: 2026-06-10
> **状态**: Complete
> **Last Updated**: 2026-06-12

## 目标

实现意图公开系统：基于功力比和洞察属性计算意图可见度，返回完全看破/正常/模糊/? 四种结果。

## 范围

### 包含
- IntentVisibility 枚举：FullReveal（体系+招式名）、Normal（体系）、Hidden（?）
- F9 公式：insight_chance = base_chance + insight × 1%
- 功力比分档（>1.5 / 1.0-1.5 / 0.7-1.0 / 0.5-0.7 / <0.5）
- 可注入随机源（测试确定性）
- 每回合每敌人独立判定

### 不包含
- AI 实际选招逻辑 → enemy-ai
- 意图 UI 展示 → combat-ui

## 技术说明

- 功力比 = 己方境界数值 / 敌方境界数值（具体映射待定，可用 RealmTier 序数比）
- 概率上限 100%

## 验收标准

- [x] 功力比 > 1.5 时总是 FullReveal
- [x] 功力比 1.0-1.5 时总是 Normal
- [x] 功力比 0.7-1.0 时 80% Normal / 20% Hidden
- [x] 功力比 0.5-0.7 时 50% Normal / 50% Hidden
- [x] 功力比 < 0.5 时 30% Normal / 70% Hidden
- [x] 洞察属性每点 +1% 概率

## 测试证据路径

`tests/unit/combat/intent_and_insight_test.cs`

## 依赖关系

- Depends on: cb-002
- Unlocks: cb-010

## Completion Notes

- Synced from implementation evidence on 2026-06-12.
- Verification: `/usr/local/share/dotnet/dotnet test tests/Foundation/Foundation.Tests.csproj --no-restore -v q` — 1033/1033 passed.

