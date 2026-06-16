# Story: cb-008 — 战斗事件总线

> **Epic**: combat-system
> **类型**: Foundation
> **优先级**: P1 — 系统间通信
> **Estimate**: S（约 2-3h）
> **依赖**: cb-001
> **阻塞**: 无
> **ADR 指引**: ADR-0003（YAML 配置）
> **GDD 来源**: design/gdd/combat-system.md §Event Bus
> **TR-ID**: TR-combat-system-008
> **Control Manifest Version**: 2026-06-10
> **状态**: Complete
> **Last Updated**: 2026-06-12

## 目标

实现类型安全的战斗事件总线，支持订阅/发布战斗中所有状态变更事件。

## 范围

### 包含
- BattleEventBus 类（Subscribe/Unsubscribe/Publish）
- 全部事件 DTO：OnRoundStart、OnIntentRevealed、OnDamageDealt、OnStaggerChanged、OnNeixiChanged、OnDecisiveStrikeAvailable、OnBattleEnd、OnRoundEnd
- 战斗结束时自动清除所有订阅
- 类型安全（泛型事件）

### 不包含
- 具体 UI 处理逻辑 → combat-ui
- 顿悟事件 → 后续 Sprint

## 技术说明

- 使用泛型 Subscribe<TEvent>(Action<TEvent>) 模式
- 线程安全不要求（战斗单线程）
- Publish 时按注册顺序调用

## 验收标准

- [x] Subscribe 后 Publish 能正确回调
- [x] Unsubscribe 后不再收到事件
- [x] 战斗结束时 ClearAll 清除所有订阅
- [x] 所有 GDD 定义的事件 DTO 均已实现
- [x] 多订阅者按注册顺序接收

## 测试证据路径

`tests/unit/combat/battle_event_bus_test.cs`

## 依赖关系

- Depends on: cb-001
- Unlocks: cb-010

## Completion Notes

- Synced from implementation evidence on 2026-06-12.
- Verification: `/usr/local/share/dotnet/dotnet test tests/Foundation/Foundation.Tests.csproj --no-restore -v q` — 1033/1033 passed.

