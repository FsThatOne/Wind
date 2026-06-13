# Story: ma-007 — 轻功装备位与战棋移动契约

> **Epic**: martial-arts-system
> **类型**: Integration
> **优先级**: P1 — 战棋移动契约
> **Estimate**: S（约 3-4h）
> **依赖**: ma-001, ma-005
> **阻塞**: 无
> **ADR 指引**: ADR-0003（YAML 配置）
> **GDD 来源**: design/gdd/martial-arts-system.md §Detailed Design, §Tuning Knobs, §Acceptance Criteria
> **TR-ID**: TR-martial-arts-system-007
> **Control Manifest Version**: 2026-06-10
> **状态**: Complete
> **Last Updated**: 2026-06-12

## 目标

将轻功作为独立武学类和独立装备位落到数据与查询契约中，为战棋移动范围和主动机动效果提供稳定输入。

## 范围

### 包含
- 每个可玩角色 1 个轻功槽
- 轻功静态定义：移动范围修正、主动机动效果占位、内息/冷却占位
- loadout 查询中返回当前轻功
- 战斗系统可查询轻功对移动范围的修正
- 未装备轻功时返回默认移动契约

### 不包含
- 战棋格移动、寻路、碰撞和位移执行 → combat-system / scene-management
- 轻功 UI 具体呈现 → ma-008
- 轻功完整内容平衡 → 后续轻功专项设计

## 技术说明

- 轻功数据仍遵循 ADR-0003 的 YAML 配置规则
- 本 story 只提供契约，不在武学系统中实现寻路
- 主动机动效果以结构化数据表达，但具体执行由战斗系统决定
- 轻功槽固定为 1，不开放调参为多槽

## 验收标准

- [x] 主角和同伴 loadout 均包含 1 个轻功槽
- [x] 装备轻功后，查询角色武学配置可返回轻功 id 和移动范围修正
- [x] 未装备轻功时，查询返回默认移动范围修正 0 或默认契约
- [x] 轻功主动机动效果以结构化数据返回，供战斗系统后续解释
- [x] 装备未习得或不存在的轻功失败

## QA 测试用例

- **AC-1**：轻功槽存在
  - Given：主角和同伴角色 loadout 初始化
  - When：查询槽位结构
  - Then：均包含 1 个轻功槽
  - 边界：剧情锁定角色仍保留槽位，只是可能不可替换

- **AC-2**：移动修正查询
  - Given：角色装备轻功“踏雪无痕”，移动范围修正 +1
  - When：战斗系统查询移动契约
  - Then：返回 qinggong_id 和 move_range_bonus=1
  - 边界：未装备轻功返回默认契约

- **AC-3**：非法装备
  - Given：角色未习得某轻功
  - When：尝试装备该轻功
  - Then：装备失败
  - 边界：轻功 id 不存在；轻功槽被剧情锁定

## 测试证据路径

`tests/integration/martial-arts/qinggong_movement_contract_test.cs`

## 依赖关系

- Depends on: ma-001, ma-005
- Unlocks: combat-system tactical movement stories

## Completion Notes

- Synced from implementation evidence on 2026-06-12.
- Verification: `/usr/local/share/dotnet/dotnet test tests/Foundation/Foundation.Tests.csproj --no-restore -v q` — 1033/1033 passed.

