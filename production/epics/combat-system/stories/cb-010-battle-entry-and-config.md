# Story: cb-010 — 战斗入口与配置接口

> **Epic**: combat-system
> **类型**: Integration
> **优先级**: P1 — 外部对接
> **Estimate**: M（约 4-6h）
> **依赖**: cb-001, cb-002, cb-005, cb-008
> **阻塞**: 无
> **ADR 指引**: ADR-0003（YAML 配置）
> **GDD 来源**: design/gdd/combat-system.md §Battle Entry Interface
> **TR-ID**: TR-combat-system-010
> **Control Manifest Version**: 2026-06-10
> **状态**: Complete
> **Last Updated**: 2026-06-12

## 目标

实现 BattleConfig → InitiateBattle → BattleInstance 的完整入口接口，将所有子系统串联为一场完整战斗。

## 范围

### 包含
- BattleConfig DTO（battle_type、player_party、enemy_group、max_rounds、callbacks）
- InitiateBattle(config) → BattleInstance 工厂方法
- 从 CharacterLoadout + 属性系统初始化 BattleCombatant
- 完整战斗流程集成测试（init → 多回合 → 结束）
- BattleResult DTO（Victory/Defeat/Draw/NearDefeat + 统计数据）

### 不包含
- 具体叙事回调执行 → narrative-system
- 战后奖励发放 → progression-system

## 技术说明

- 集成 cb-001~009 的所有子系统
- 使用接口注入 AI 决策（本 story 用 ScriptedAI 做测试）
- 提供 RunFullBattle(config, decisions[]) 方法用于测试

## 验收标准

- [x] BattleConfig 包含 GDD 定义的所有字段
- [x] InitiateBattle 正确初始化所有参战角色
- [x] 完整 3 回合战斗可从创建到结束走通
- [x] 战斗结果正确返回 Victory/Defeat/Draw/NearDefeat
- [x] 从 CharacterLoadout 读取装备招式并用于战斗

## 测试证据路径

`tests/unit/combat/battle_entry_config_test.cs`

## 依赖关系

- Depends on: cb-001, cb-002, cb-005, cb-008
- Unlocks: enemy-ai, combat-ui, narrative integration

## Completion Notes

- Synced from implementation evidence on 2026-06-12.
- Verification: `/usr/local/share/dotnet/dotnet test tests/Foundation/Foundation.Tests.csproj --no-restore -v q` — 1033/1033 passed.

