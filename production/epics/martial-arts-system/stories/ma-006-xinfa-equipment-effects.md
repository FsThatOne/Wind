# Story: ma-006 — 心法装备门槛、被动加成与专属招式

> **Epic**: martial-arts-system
> **类型**: Integration
> **优先级**: P1 — 构建深度与战斗选项扩展
> **Estimate**: M（约 4-6h）
> **依赖**: ma-001, ma-003, ma-005
> **阻塞**: 无
> **ADR 指引**: ADR-0003（YAML 配置）
> **GDD 来源**: design/gdd/martial-arts-system.md §Detailed Design, §States and Transitions, §Formulas, §Edge Cases, §Acceptance Criteria
> **TR-ID**: TR-martial-arts-system-006
> **Control Manifest Version**: 2026-06-10
> **状态**: Ready
> **Last Updated**: —

## 目标

实现心法/内功的装备门槛、被动属性加成和专属招式查询，使心法成为战斗构建的一部分，并能在被封印时暂时失效。

## 范围

### 包含
- 心法装备和卸下状态
- 基础属性门槛检查
- 门槛不足时禁止装备并返回差值提示
- 心法被动加成：`modifier = xinfa_base_bonus × realm_scaling`
- 装备心法后，战斗招式面板额外显示 1-2 个心法专属招式
- 心法被封印时，被动加成和专属招式暂时失效

### 不包含
- 战斗中切换内功的行动代价和冷却 → combat-system
- 心法之间冲突关系 → 当前 GDD Open Question，暂不实现
- UI 具体列表和拖拽 → ma-008

## 技术说明

- 心法门槛通过角色属性系统查询基础或当前属性
- 被动加成以 modifier DTO 返回，由角色属性系统应用或预览
- 专属招式不占 6 个基础招式槽，但应在战斗查询接口中附带来源标记
- “封印”状态应由战斗系统或状态效果传入，本 story 只响应查询

## 验收标准

- [ ] 心法需要力量 ≥ 20、玩家力量为 15 时，尝试装备失败，并提示“力量不足 5 点”
- [ ] 心法装备成功后，被动属性加成按 `xinfa_base_bonus × realm_scaling` 计算
- [ ] 装备心法后，战斗招式面板除 6 个基础槽位外，额外显示 1-2 个心法专属招式
- [ ] 心法被封印时，心法专属招式不可用，被动加成暂时失效
- [ ] 卸下心法后，被动加成和专属招式不再出现在查询结果中

## QA 测试用例

- **AC-1**：门槛不足
  - Given：心法要求力量 ≥ 20，玩家力量为 15
  - When：尝试装备该心法
  - Then：装备失败，并返回“力量不足 5 点”
  - 边界：力量正好 20 可装备；多个属性门槛同时不足

- **AC-2**：被动加成
  - Given：心法基础加成为 10，角色境界缩放为 1.3
  - When：查询心法加成
  - Then：返回 modifier=13
  - 边界：初学乍练 0.7；返璞归真 2.0

- **AC-3**：专属招式与封印
  - Given：角色装备含 2 个专属招式的心法
  - When：查询战斗可用招式
  - Then：返回 6 个基础招式 + 2 个心法专属招式
  - 边界：心法封印时专属招式不可用；卸下后不返回

## 测试证据路径

`tests/integration/martial-arts/xinfa_equipment_effects_test.cs`

## 依赖关系

- Depends on: ma-001, ma-003, ma-005
- Unlocks: combat-system inner-art action stories
