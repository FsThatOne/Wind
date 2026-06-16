# Story: ai-004 — 调息决策与选招逻辑

> **Epic**: enemy-ai
> **类型**: Logic
> **优先级**: P0
> **GDD 来源**: design/gdd/enemy-ai.md
> **TR-ID**: TR-enemy-ai-004
> **状态**: Complete
> **Last Updated**: 2026-06-12

## 目标

同步已实现的敌方 AI story：调息概率、体系内选招和回退策略。

## 验收标准

- [x] 对应 Foundation AI 模块已实现。
- [x] 对应自动化测试文件已存在并纳入 Foundation 测试项目。
- [x] 全量 Foundation 测试通过。

## 测试证据路径

`tests/unit/combat/ai/meditation_move_selection_test.cs`

## Completion Notes

- Synced from Sprint 3 implementation evidence on 2026-06-12.
- Verification: `/usr/local/share/dotnet/dotnet test tests/Foundation/Foundation.Tests.csproj --no-restore -v q` — 1033/1033 passed.
