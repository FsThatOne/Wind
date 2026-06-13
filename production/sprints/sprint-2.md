# Sprint 2 — 回合制战斗核心逻辑

> **Sprint Goal**: 实现回合制战斗的完整 Foundation 层逻辑，使战斗可以在纯 C# 环境中从初始化到结束走通全流程。
>
> **Duration**: 2026-06-12 ~ 2026-06-26 (2 weeks)
>
> **Prerequisite**: Sprint 1 (武学数据层) 全部完成

## Sprint Scope

本 Sprint 只实现 Foundation 层战斗逻辑（纯 C#, xUnit 可测），不包含：
- Godot 场景/UI 集成
- 敌方 AI 完整策略
- 顿悟突破完整流程
- 战后奖励/经验系统

## Stories

| ID | 名称 | 优先级 | 估时 | 依赖 |
|----|------|--------|------|------|
| cb-001 | 战斗状态机与回合流程框架 | must-have | M | — |
| cb-002 | 战斗角色模型与资源系统 | must-have | S | — |
| cb-003 | 伤害结算管线（F1-F4） | must-have | M | cb-002, ma-001, ma-003 |
| cb-004 | 破绽系统与一击决胜 | must-have | S | cb-002, cb-003 |
| cb-005 | 行动注册表与执行框架 | must-have | M | cb-001, cb-002 |
| cb-006 | 反制机制 | should-have | S | cb-003, cb-004, cb-005 |
| cb-007 | 意图公开与洞察概率系统 | should-have | S | cb-002 |
| cb-008 | 战斗事件总线 | should-have | S | cb-001 |
| cb-009 | 多人战斗与协同破绽 | should-have | M | cb-003~006 |
| cb-010 | 战斗入口与配置接口 | should-have | M | cb-001, cb-002, cb-005, cb-008 |

## Dependency Graph

```
cb-001 (状态机) ──┬── cb-005 (行动框架) ──┬── cb-006 (反制) ──── cb-009 (多人协同)
                  │                        │
                  └── cb-008 (事件总线) ────┴── cb-010 (入口集成)

cb-002 (角色模型) ─── cb-003 (伤害管线) ─── cb-004 (破绽/决胜) ─┘
                  │
                  └── cb-007 (意图洞察)
```

## Execution Order (推荐)

1. cb-001 + cb-002 (并行，无依赖)
2. cb-003 (依赖 cb-002)
3. cb-004 + cb-005 (并行，分别依赖 cb-003 和 cb-001+002)
4. cb-006 + cb-007 + cb-008 (并行)
5. cb-009 (依赖 cb-006)
6. cb-010 (集成测试，依赖 cb-001/002/005/008)

## Definition of Done

- 所有 AC 有对应测试覆盖
- `dotnet test` 全量通过（包含 Sprint 1 回归）
- 可通过 cb-010 的集成测试完整跑通 "创建战斗 → 3回合出招 → 战斗结束" 流程
