# Story: ma-005 — 角色武学配置槽、体系覆盖警告与同伴锁定

> **Epic**: martial-arts-system
> **类型**: Integration
> **优先级**: P0 — 战斗出招入口
> **Estimate**: M（约 4-6h）
> **依赖**: ma-001, ma-002
> **阻塞**: 无
> **ADR 指引**: ADR-0003（YAML 配置）
> **GDD 来源**: design/gdd/martial-arts-system.md §Detailed Design, §Interactions, §Edge Cases, §Acceptance Criteria
> **TR-ID**: TR-martial-arts-system-005
> **Control Manifest Version**: 2026-06-10
> **状态**: Ready
> **Last Updated**: —

## 目标

实现主角与同伴共用的武学配置槽结构：6 个招式槽、1 个心法/内功槽、1 个轻功槽，并提供体系覆盖检查、剧情锁定和战斗可读取的 loadout 查询。

## 范围

### 包含
- 每个可玩角色独立 loadout
- 主角和同伴拥有相同槽位结构
- 6 个招式槽必须能装备已习得招式
- 1 个心法/内功槽和 1 个轻功槽的占位与查询
- 刚/柔/巧覆盖检查：缺体系时警告但允许强制确认
- 同伴剧情锁定槽位：禁止替换并显示锁定原因
- 同伴偏好冲突：只给轻量提醒，不阻止
- `GetEquippedMoves()` / `SerializeCharacterLoadout(character_id)` 风格接口

### 不包含
- 心法门槛与被动效果 → ma-006
- 轻功移动数值解释 → ma-007
- UI 拖拽实现 → ma-008
- 队伍部署状态机 → party-management

## 技术说明

- loadout 是角色运行时/存档状态，不写回静态武学定义
- 剧情锁定查询由队伍/叙事状态提供，本 story 可定义接口与默认实现
- 缺体系覆盖是 warning，不是 validation error
- 出战查询必须返回稳定顺序，方便战斗面板和冷却系统使用

## 验收标准

- [ ] 同伴处于普通可控状态时，玩家可调整 6 个招式槽、1 个心法/内功槽和 1 个轻功槽
- [ ] 装备 5 个刚系招式和 1 个柔系招式进入战斗时，显示“缺少巧系招式”警告但允许确认进入
- [ ] 同伴某槽位被剧情锁定时，替换该槽位失败并返回锁定原因
- [ ] 同伴偏好与玩家配置冲突时，配置允许成功，并返回轻量提醒
- [ ] 战斗系统可读取 6 个基础招式槽的当前配置

## QA 测试用例

- **AC-1**：同伴 loadout 可配置
  - Given：同伴处于普通可控状态，已习得 6 个招式、1 个心法、1 个轻功
  - When：玩家替换所有槽位
  - Then：loadout 更新成功，查询结果包含新配置
  - 边界：槽位为空、重复装备同一招式、装备未习得招式

- **AC-2**：体系覆盖警告
  - Given：角色装备 5 个刚系招式和 1 个柔系招式
  - When：执行出战前检查
  - Then：返回缺少巧系招式 warning，且允许强制确认
  - 边界：缺少两个体系；三体系各至少 1 个时无 warning

- **AC-3**：剧情锁定槽位
  - Given：同伴某招式槽 `loadout_locked=true` 且锁定原因为“疗伤中不可换招”
  - When：尝试替换该槽位
  - Then：替换失败，并返回锁定原因
  - 边界：未锁定槽位可替换；偏好冲突不阻止

## 测试证据路径

`tests/integration/martial-arts/loadout_slots_and_locks_test.cs`

## 依赖关系

- Depends on: ma-001, ma-002
- Unlocks: ma-006, ma-007, ma-008, combat-system action selection stories
