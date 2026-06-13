# Sprint 3 — 敌方 AI 决策系统

> **Sprint Goal**: 实现完整的敌方 AI 决策管线（性格模板 + 状态机 + 反读 + Boss 多阶段），使 AI 可在纯 C# 环境中为任意战斗配置提供符合 GDD 统计分布的决策输出。
>
> **Duration**: 2026-06-12 ~ 2026-06-26 (2 weeks)
>
> **Prerequisite**: Sprint 2 (战斗核心) 全部完成

## Sprint Scope

本 Sprint 只实现 Foundation 层 AI 逻辑（纯 C#, xUnit 可测），不包含：
- Godot 场景/动画集成
- 战斗 UI 中的 AI 信号展示
- 具体 Boss 配置数据（只做框架）
- 教学模式 AI 限制

## Stories

| ID | 名称 | 优先级 | 估时 | 依赖 |
|----|------|--------|------|------|
| ai-001 | 性格模板与体系选择 (Phase A) | must-have | S | — |
| ai-002 | 状态机与综合修正系统 | must-have | M | ai-001 |
| ai-003 | 招式预兆序列 | should-have | S | ai-001 |
| ai-004 | 调息决策与选招逻辑 | must-have | S | ai-001 |
| ai-005 | 反读系统 (Phase B) | must-have | M | ai-001 |
| ai-006 | 目标选择 (F3) | must-have | S | — |
| ai-007 | Boss 阶段与特殊机制 | should-have | M | ai-001, ai-005 |
| ai-008 | AI 管线集成与验证 | must-have | M | all |

## Dependency Graph

```
ai-001 ──┬── ai-002 ──┐
          ├── ai-003   ├── ai-008 (集成)
          ├── ai-004   │
          └── ai-005 ──┤
ai-006 ────────────────┤
ai-007 ────────────────┘
```

## Key Design Decisions

- AI 核心在 `FengZhi.Foundation.Combat.AI` 命名空间
- 所有概率决策通过可注入的 `IAIRandomSource` 接口
- 性格模板为 POCO 不可变数据对象
- Boss 阶段脚本为配置数据（不是硬编码）
